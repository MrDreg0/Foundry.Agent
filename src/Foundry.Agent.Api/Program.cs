using Foundry.Agent.Api.Configure;
using Foundry.Agent.Api.Settings;
using Foundry.Agent.Application.Agent;
using Foundry.Agent.Application.LLM;
using Foundry.Agent.Application.Tool;
using Foundry.Agent.Domain;
using Microsoft.Extensions.Options;
using Foundry.Agent.Api.Endpoints;
using Foundry.Agent.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure();

builder.Services.AddOptions()
  .Configure<LlmSettings>(builder.Configuration.GetSection("Llm"))
  .Configure<WebToolSettings>(builder.Configuration.GetSection("Tools:Web"));

builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>((serviceProvider, httpClient) =>
{
  var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmSettings>>().Value;
  
  httpClient.BaseAddress = new Uri(llmSettings.BaseUrl);
});

builder.Services.AddHttpClient<HttpClient>();
builder.Services.AddSingleton<ITool, WebRequestTool>();

builder.Services.AddSingleton<IToolRegistry>(serviceProvider =>
{
  var allTools = serviceProvider.GetServices<ITool>();
  var logger = serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
  
  return new ToolRegistry(allTools, logger);
});

builder.Services.AddSingleton<IAgent, Agent>();

var app = builder.Build();

app
  .MapVersionEndpoints()
  .MapAgentEndpoints();

app.Run();