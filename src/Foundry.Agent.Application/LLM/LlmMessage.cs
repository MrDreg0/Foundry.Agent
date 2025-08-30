using System.Text.Json;
using Foundry.Agent.Domain;

namespace Foundry.Agent.Application.LLM;

public abstract record LlmMessage
{
  public sealed record PlainText(string Content) : LlmMessage;
  public sealed record ToolCall(ToolId ToolId, ActionName ActionName, JsonElement Arguments) : LlmMessage;
}
