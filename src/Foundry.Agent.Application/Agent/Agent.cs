using System.Text;
using System.Text.Json;
using Foundry.Agent.Application.LLM;
using Foundry.Agent.Application.Tool;
using Foundry.Agent.Domain;
using Microsoft.Extensions.Logging;

namespace Foundry.Agent.Application.Agent;

public sealed class Agent(ILlmClient llmClient, IToolRegistry toolRegistry, ILogger<Agent> logger) : IAgent
{
  private const int MaxSteps = 5;
  private readonly string _toolsDescription = GetToolsDescription(toolRegistry.All);
  
  public async Task<AgentResult> Run(string userPrompt, string model, CancellationToken cancellationToken)
  {
    var context = new List<string>();
    ToolCallTrace? lastTrace = null;

    for (var step = 1; step <= MaxSteps; step++)
    {
      var prompt = BuildPrompt(userPrompt, context, step, MaxSteps);

      var llmResponse = await llmClient.Generate(
        new LlmRequest(model, prompt, new LlmOptions()),
        cancellationToken);

      logger.LogInformation("[Step {Step}] LLM response: {Response}, duration {Duration}", step, llmResponse.RawText, llmResponse.Duration);

      if (!TryParseLlmMessage(llmResponse.RawText, out var llmMessage))
      {
        return new AgentResult(llmResponse.RawText, lastTrace, IsSuccess: true);
      }

      switch (llmMessage)
      {
        case LlmMessage.PlainText plain:
          return new AgentResult(plain.Content, lastTrace, IsSuccess: true);

        case LlmMessage.ToolCall toolCall:
        {
          var tool = toolRegistry.Resolve(toolCall.ToolId);
          if (tool is null)
          {
            return new AgentResult(
              $"Tool with id '{toolCall.ToolId}' is not found",
              lastTrace,
              IsSuccess: false,
              Error: "ToolNotFound");
          }

          var actionSpec = tool.Actions.FirstOrDefault(a => a.Name == toolCall.ActionName);
          if (actionSpec is null)
          {
            return new AgentResult(
              $"Action '{toolCall.ActionName}' is not supported '{toolCall.ToolId}'",
              lastTrace,
              IsSuccess: false,
              Error: "ActionNotSupported");
          }

          logger.LogInformation("[Step {Step}] Tool call: {ToolId}.{Action} args: {Args}", step, tool.Id, actionSpec.Name, toolCall.Arguments.ToString());

          var execResult = await tool.Execute(actionSpec.Name, toolCall.Arguments, cancellationToken);

          logger.LogInformation("[Step {Step}] Tool result: success={Success}, bytes={Bytes}", step, execResult.IsSuccess, execResult.Data?.Length ?? 0);

          lastTrace = new ToolCallTrace(toolCall.ToolId, actionSpec.Name, toolCall.Arguments, execResult);

          if (execResult is { IsSuccess: true, Data: not null })
          {
            var text = Encoding.UTF8.GetString(execResult.Data);
            context.Add($"Observation (tool {tool.Id}, action {actionSpec.Name.Value}):\n{text}");

            continue;
          }

          var error = execResult.Error is null
            ? "UnexpectedToolError"
            : $"{execResult.Error.Code}: {execResult.Error.Message}";

          return new AgentResult(
            $"Tool error: {error}",
            lastTrace,
            IsSuccess: false,
            error);
        }
        default:
          throw new InvalidOperationException($"Unexpected message type {llmMessage.GetType().Name}");
      }
    }

    return new AgentResult(
      $"Step limit reached ({MaxSteps}). Unable to complete the task in allotted steps.",
      IsSuccess: false,
      Error: "StepLimitExceeded");
  }
  
  private string BuildPrompt(string user, IReadOnlyList<string> context, int step, int maxSteps)
  {
    var sb = new StringBuilder();
    sb.AppendLine("You are an agent. You have tools available. Always respond strictly as JSON in ONE of the following formats:");
    sb.AppendLine("""1) {"type":"plain_text","content":"..."}""");
    sb.AppendLine("""2) {"type":"tool_call","toolId":"<domain.name@major>","action":"<action>","arguments":{...}}""");
    sb.AppendLine("Do not include any text outside of JSON. If you need tools, return exactly one tool_call. When you have enough information, return exactly one plain_text.");
    sb.AppendLine();
    sb.Append("Step info: You have at most ").Append(maxSteps).Append(" steps. This is step ").Append(step).Append(" of ").Append(maxSteps).AppendLine(".");
    if (step >= maxSteps)
    {
      sb.AppendLine("This is your final step. If you can, produce a {\"type\":\"plain_text\"} answer. Avoid unnecessary tool calls.");
    }
    else
    {
      var remaining = maxSteps - step;
      sb.Append("Remaining steps after this: ").Append(remaining).AppendLine(". Use tools only if necessary.");
    }
    sb.AppendLine();

    if (context.Count > 0)
    {
      sb.AppendLine("Context (previous observations):");
      for (var i = 0; i < context.Count; i++)
      {
        sb.Append('#').Append(i + 1).Append(") ").AppendLine(context[i]);
      }
      sb.AppendLine();
      sb.AppendLine("Based on the context, decide whether more tool calls are needed or produce the final plain_text answer.");
      sb.AppendLine("When tool observations provide the necessary data to fully satisfy the User task, return a single {\"type\":\"plain_text\",\"content\":\"...\"} containing only the final answer requested by the user (e.g., extract a specific field like 'Description' instead of dumping the whole payload).");
      sb.AppendLine("Do not echo the prompt or raw tool output unless explicitly requested. Prefer concise, task-focused results.");
      sb.AppendLine("If the user task implies multiple sub-steps (analyze → call tool → analyze result → finalize), you may use multiple steps within the limit to achieve this, but the final step MUST be plain_text when sufficient information is available.");
    }

    sb.AppendLine("Available tools and actions:");
    sb.AppendLine(_toolsDescription);
    sb.AppendLine();
    sb.Append("User task: ").AppendLine(user);
    sb.AppendLine("Produce ONE valid JSON according to the protocol.");

    return sb.ToString();
  }

  private static bool TryParseLlmMessage(string rawText, out LlmMessage llmMessage)
  {
    llmMessage = null!;

    try
    {
      using var jsonDocument = JsonDocument.Parse(rawText);

      if (!jsonDocument.RootElement.TryGetProperty("type", out var typeProperty) || typeProperty.ValueKind != JsonValueKind.String)
      {
        return false;
      }

      var typeValue = typeProperty.GetString();
      
      switch (typeValue)
      {
        case "plain_text":
        {
          if (!jsonDocument.RootElement.TryGetProperty("content", out var contentProperty) || contentProperty.ValueKind != JsonValueKind.String)
          {
            return false;
          }

          llmMessage = new LlmMessage.PlainText(contentProperty.GetString()!);
          return true;
        }
        case "tool_call":
        {
          var root = jsonDocument.RootElement;

          if (!root.TryGetProperty("toolId", out var toolIdProperty) || toolIdProperty.ValueKind != JsonValueKind.String)
          {
            return false;
          }

          if (!root.TryGetProperty("action", out var actionProperty) || actionProperty.ValueKind != JsonValueKind.String)
          {
            return false;
          }

          if (!root.TryGetProperty("arguments", out var argsProperty))
          {
            return false;
          }

          llmMessage = new LlmMessage.ToolCall(
            ToolId.Parse(toolIdProperty.GetString()!),
            ActionName.Parse(actionProperty.GetString()!),
            argsProperty.Clone());
          return true;
        }
        default:
          return false;
      }

    }
    catch
    {
      return false;
    }
  }
  
  private static string GetToolsDescription(IEnumerable<ITool> tools)
  {
    var sb = new StringBuilder();
    foreach (var currentTool in tools)
    {
      sb.Append("- ").Append(currentTool.Id).Append(": ").AppendLine(currentTool.Name);
      foreach (var currentAction in currentTool.Actions) sb.Append("  • ").Append(currentAction.Name.Value).Append(" — ").AppendLine(currentAction.Summary);
    }
    
    return sb.ToString();
  }
}
