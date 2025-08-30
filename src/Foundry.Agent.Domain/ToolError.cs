namespace Foundry.Agent.Domain;

public record ToolError(
  ToolErrorCode Code,
  string Message,
  bool Retryable = false);