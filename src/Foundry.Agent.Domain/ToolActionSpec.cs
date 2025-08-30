namespace Foundry.Agent.Domain;

public record ToolActionSpec(
  ActionName Name,
  string Summary,
  string ParametersSchema);
