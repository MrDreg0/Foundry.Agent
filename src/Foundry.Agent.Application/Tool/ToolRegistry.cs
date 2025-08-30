using System.Collections.ObjectModel;
using Foundry.Agent.Domain;
using Microsoft.Extensions.Logging;

namespace Foundry.Agent.Application.Tool;

public sealed class ToolRegistry : IToolRegistry
{
  private readonly IReadOnlyDictionary<string, ITool> _tools;
  public IReadOnlyList<ITool> All { get; }

  public ToolRegistry(IEnumerable<ITool> tools, ILogger<ToolRegistry> logger)
  {
    ArgumentNullException.ThrowIfNull(tools);

    var dict = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
    var list = new List<ITool>();

    foreach (var tool in tools)
    {
      var key = tool.Id.ToString();
      if (!dict.TryAdd(key, tool))
      {
        throw new ArgumentException($"Duplicate tool id '{key}'. Each ITool must have a unique ToolId.");
      }

      list.Add(tool);
      logger.LogInformation("Registered tool {ToolId} with {Actions} actions", key, tool.Actions.Count);
    }

    _tools = new ReadOnlyDictionary<string, ITool>(dict);
    All  = new ReadOnlyCollection<ITool>(list);
  }

  public ITool? Resolve(ToolId id) => _tools.GetValueOrDefault(id.ToString());
}
