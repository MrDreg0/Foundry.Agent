namespace Foundry.Agent.Api.Settings;

public record LlmSettings
{
  public required string Provider { get; init; }
  public required string BaseUrl { get; init; }
  public required string Model { get; init; }
}
