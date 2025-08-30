using Foundry.Agent.Application.Tool;

namespace Foundry.Agent.Application.Agent;

public record AgentResult(
  string Payload,
  ToolCallTrace? ToolCallTrace = null,
  bool IsSuccess = false,
  string? Error = null);