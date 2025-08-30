namespace Foundry.Agent.Api.Endpoints;

public sealed record AgentRequest(string Prompt, string? Model);