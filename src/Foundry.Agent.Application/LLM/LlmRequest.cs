namespace Foundry.Agent.Application.LLM;

public sealed record LlmRequest(string Model, string Prompt, LlmOptions Options);
