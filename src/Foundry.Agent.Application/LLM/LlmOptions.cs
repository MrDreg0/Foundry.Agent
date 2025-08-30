namespace Foundry.Agent.Application.LLM;

public record LlmOptions(
  int NumCtx = 4096,
  float Temperature = 0.2f,
  float TopP = 0.95f,
  int? MaxTokens = null,
  bool Stream = false);
