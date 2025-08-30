using System.Reflection;

namespace Foundry.Agent.Api.Endpoints;

public static class VersionEndpoints
{
  public static IEndpointRouteBuilder MapVersionEndpoints(this IEndpointRouteBuilder builder)
  {
    builder.MapGet("version", () =>
    {
      var assembly = Assembly.GetExecutingAssembly();
      var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()
          ?.Version
        ?? "Version not found";

      return Results.Ok(version);
    });

    return builder;
  }
}
