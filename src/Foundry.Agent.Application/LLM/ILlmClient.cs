namespace Foundry.Agent.Application.LLM;

public interface ILlmClient
{
  Task<LlmResponse> Generate(LlmRequest llmRequest, CancellationToken ct);
}