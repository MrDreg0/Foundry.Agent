using System.Net.Http.Json;
using System.Text.Json;

namespace Foundry.Agent.Application.LLM;

public class OllamaLlmClient(HttpClient httpClient) : ILlmClient
{
  public async Task<LlmResponse> Generate(LlmRequest llmRequest, CancellationToken ct)
  {
    var started = DateTime.UtcNow;
    
    var payload = new
    {
      model = llmRequest.Model,
      prompt = llmRequest.Prompt,
      stream = llmRequest.Options.Stream,
      options = new
      {
        num_ctx = llmRequest.Options.NumCtx,
        temperature = llmRequest.Options.Temperature,
        top_p = llmRequest.Options.TopP,
        num_predict = llmRequest.Options.MaxTokens
      }
    };

    using var response = await httpClient.PostAsJsonAsync("/api/generate", payload, ct);
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync(ct);
    
    string raw;
    try
    {
      using var jsonDocument = JsonDocument.Parse(json);
      if (jsonDocument.RootElement.TryGetProperty("response", out var responseProperty) && responseProperty.ValueKind == JsonValueKind.String)
      {
        raw = responseProperty.GetString()!;
      }
      else
      {
        raw = json;
      }
    }
    catch
    {
      raw = json;
    }

    return new LlmResponse(raw, DateTime.UtcNow - started);
  }
}
