using System.Text.Json;

namespace Foundry.Agent.Domain;

public interface ITool
{
  ToolId Id { get; }
  string Name { get; }
  string Description { get; }
  IReadOnlyList<ToolActionSpec> Actions { get; }
  
  Task<ToolResult> Execute(ActionName actionName, JsonElement arguments, CancellationToken cancellationToken);
}
