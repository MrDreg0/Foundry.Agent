namespace Foundry.Agent.Domain;

public record ToolResult
{
  public bool IsSuccess { get; init; }
  public string? MimeType { get; init; }
  public byte[]? Data { get; init; }
  public ToolError? Error { get; init; }
}