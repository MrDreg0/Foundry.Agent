using Microsoft.Extensions.Logging.Console;

namespace Foundry.Agent.Api.Configure;

public static class Logging
{
  public static ILoggingBuilder Configure(this ILoggingBuilder builder)
  {
    builder.ClearProviders();
    builder.AddSimpleConsole(options =>
    {
      options.SingleLine = true;
      options.TimestampFormat = "HH:mm:ss";
      options.IncludeScopes = false;
      options.ColorBehavior = LoggerColorBehavior.Enabled;
    });

    builder.Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.None);
    
    SetLoggingFilters(builder);
    
    return builder;
  }

  private static void SetLoggingFilters(ILoggingBuilder builder)
  {
    builder
      .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
      .AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
  }
}
