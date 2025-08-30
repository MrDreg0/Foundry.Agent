using System.Text.Json;
using Foundry.Agent.Domain;

namespace Foundry.Agent.Application.Tool;

public record ToolCallTrace(
  ToolId ToolId,
  ActionName Action,
  JsonElement Arguments,
  ToolResult ToolResult);
