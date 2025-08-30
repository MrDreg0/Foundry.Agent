namespace Foundry.Agent.Infrastructure;

public record WebToolSettings
{
  public required string[] AllowedHosts { get; init; }
  public required int TimeoutMs { get; init; }
}
