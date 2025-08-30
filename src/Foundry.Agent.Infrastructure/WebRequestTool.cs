using System.Text.Json;
using Foundry.Agent.Domain;
using Microsoft.Extensions.Options;

namespace Foundry.Agent.Infrastructure;

public sealed class WebRequestTool : ITool
{
  private readonly static ActionName GetJsonActionName = new ("get_json");
  private readonly static ActionName GetActionName = new ("get");
  
  private readonly HashSet<string> _allowedHosts;
  private readonly HttpClient _httpClient;
  public ToolId Id => ToolId.Parse("web.request@1");
  public string Name => "Web Request";
  public string Description => 
    "Performs safe web requests (HTTP/HTTPS: GET, POST, etc.) to allow-listed hosts. " +
    "Supports raw and JSON workflows with content-type validation, returns response body and MIME type, " +
    "applies default headers, and provides structured errors for timeouts, upstream failures, and policy violations.";

  public WebRequestTool(IHttpClientFactory httpFactory, IOptions<WebToolSettings> webToolSettings)
  {
    _httpClient = httpFactory.CreateClient(nameof(WebRequestTool));

    _allowedHosts = webToolSettings.Value.AllowedHosts.Select(currentHost => currentHost.ToLowerInvariant()).ToHashSet();
  }

  public IReadOnlyList<ToolActionSpec> Actions =>
  [
    new(
      GetActionName,
      "HTTP GET for the specified absolute URL. Returns the response body as-is along with MIME type.",
      """
      {
        "type": "object",
        "properties": {
          "url": { "type": "string", "description": "Absolute URL (https://...)" }
        },
        "required": ["url"],
        "additionalProperties": false
      }
      """
    ),
    new(
      GetJsonActionName,
      "HTTP GET expecting a JSON response. Validates Content-Type is application/json and returns the response body as JSON; fails if the response is not JSON.",
      """
      {
        "type": "object",
        "properties": {
          "url": { "type": "string", "description": "Absolute URL (https://...)" }
        },
        "required": ["url"],
        "additionalProperties": false
      }
      """
    )
  ];

  public async Task<ToolResult> Execute(ActionName actionName, JsonElement args, CancellationToken ct)
  {
    if (Actions.All(currentAction => currentAction.Name != actionName))
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.Unsupported, $"Action '{actionName}' is not supported by this tool")
      };
    }

    if (!args.TryGetProperty("url", out var urlProperty) || urlProperty.ValueKind != JsonValueKind.String)
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.ValidationFailed, "Expected string argument 'url'")
      };
    }

    var url = urlProperty.GetString();

    if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.ValidationFailed, $"Invalid URL '{url}'")
      };
    }

    if (!_allowedHosts.Contains(uri.Host.ToLowerInvariant()))
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.AccessDenied, $"Access to host '{uri.Host}' is denied by policy")
      };
    }

    try
    {
      var req = new HttpRequestMessage(HttpMethod.Get, uri);
      req.Headers.TryAddWithoutValidation("User-Agent", "FoundryAgent/1.0");
      req.Headers.TryAddWithoutValidation("Accept", "application/json");
      req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

      using var res = await _httpClient.SendAsync(req, ct);
      var bytes = await res.Content.ReadAsByteArrayAsync(ct);
      var mime = res.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

      if (GetJsonActionName.Equals(actionName) && !mime.Contains("json", StringComparison.OrdinalIgnoreCase))
      {
        return new ToolResult
        {
          IsSuccess = false,
          MimeType = mime,
          Data = bytes,
          Error = new ToolError(
            ToolErrorCode.UpstreamError,
            $"Expected a JSON response, but received content of type '{mime}'"
          )
        };
      }

      return new ToolResult
      {
        IsSuccess = true,
        MimeType = mime,
        Data = bytes
      };
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.Timeout, "The request was canceled due to timeout/cancellation token")
      };
    }
    catch (HttpRequestException ex)
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.InternalError, $"HTTP error: {ex.Message}")
      };
    }
    catch (Exception ex)
    {
      return new ToolResult
      {
        IsSuccess = false,
        Error = new ToolError(ToolErrorCode.InternalError, ex.Message)
      };
    }
  }
}
