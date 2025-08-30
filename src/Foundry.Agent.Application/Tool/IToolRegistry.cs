using Foundry.Agent.Domain;

namespace Foundry.Agent.Application.Tool;

public interface IToolRegistry
{
  IReadOnlyList<ITool> All { get; }
  
  ITool? Resolve(ToolId id);
}