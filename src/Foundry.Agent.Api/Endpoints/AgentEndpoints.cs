using Foundry.Agent.Api.Settings;
using Foundry.Agent.Application.Agent;
using Microsoft.Extensions.Options;

namespace Foundry.Agent.Api.Endpoints;

public static class AgentEndpoints
{
  public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder builder)
  {
    builder.MapGet("test", () => "test");

    builder.MapPost("agent", async (AgentRequest request, IAgent agent, IOptions<LlmSettings> llmSettings, CancellationToken ct) =>
    {
      var result = await agent.Run(
        request.Prompt,
        request.Model ?? llmSettings.Value.Model,
        ct);

      return Results.Json(result);
    });
    
    return builder;
  }
}
