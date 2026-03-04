namespace Az204AiLearningAssistant.Api.Infrastructure;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

public sealed class AzureOpenAiClient
{
    private const string AzureOpenAiConfigSectionName = "AzureOpenAI";

    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public AzureOpenAiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var section = configuration.GetSection(AzureOpenAiConfigSectionName);

        _endpoint = section.GetValue<string>("Endpoint") ?? string.Empty;
        _apiKey = section.GetValue<string>("ApiKey") ?? string.Empty;
        _deploymentName = section.GetValue<string>("DeploymentName") ?? "gpt-5-mini";
    }

    public async Task<string> GenerateQuizJsonAsync(string topic, int questionCount, string? difficulty, CancellationToken cancellationToken = default)
    {
        var requestUri = GetResponsesUrl();

        var systemPrompt = "Return ONLY valid JSON. No markdown. Do not wrap in markdown fences.";

        var userPromptBuilder = new StringBuilder();
        userPromptBuilder.AppendLine("Generate a multiple-choice quiz for the given Azure certification topic.");
        userPromptBuilder.AppendLine("The quiz must be returned strictly as JSON in the following shape:");
        userPromptBuilder.AppendLine("{\"questions\":[{\"question\":\"string\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctAnswer\":\"A\"}]}");
        userPromptBuilder.AppendLine("Rules:");
        userPromptBuilder.AppendLine("- The root object must have a 'questions' array.");
        userPromptBuilder.AppendLine("- Each question must have exactly 4 options.");
        userPromptBuilder.AppendLine("- 'correctAnswer' must exactly match one of the option strings.");
        userPromptBuilder.AppendLine("- Do not include any explanations, markdown, or additional fields.");
        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine($"Topic: {topic}");
        userPromptBuilder.AppendLine($"Number of questions: {questionCount}");

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            userPromptBuilder.AppendLine($"Difficulty: {difficulty}");
        }

        var userPrompt = userPromptBuilder.ToString();

        var requestBody = new
        {
            model = _deploymentName,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = systemPrompt
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = userPrompt
                        }
                    }
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody, _serializerOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

        request.Headers.Add("api-key", _apiKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var requestUrl = response.RequestMessage?.RequestUri;
            var url = requestUrl is null
                ? "<unknown>"
                : requestUrl.ToString();

            var truncatedBody = responseJson.Length > 4000
                ? responseJson[..4000]
                : responseJson;

            var message =
                $"AzureOpenAI call failed: {(int)response.StatusCode} {response.ReasonPhrase} | Url: {url} | Body: {truncatedBody}";

            throw new InvalidOperationException(message);
        }

        var outputText = ExtractOutputText(responseJson);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            var truncated = responseJson.Length > 20000 ? responseJson[..20000] : responseJson;
            throw new InvalidOperationException($"invalid_model_output: missing_output_text | raw={truncated}");
        }

        return NormalizeToJson(outputText);
    }

    public Uri GetResponsesUrl()
    {
        if (string.IsNullOrWhiteSpace(_endpoint) ||
            string.IsNullOrWhiteSpace(_apiKey) ||
            string.IsNullOrWhiteSpace(_deploymentName))
        {
            throw new InvalidOperationException("invalid_config: missing_endpoint_or_key_or_deployment");
        }

        if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("invalid_config: invalid_endpoint_uri");
        }

        return BuildResponsesUrl(baseUri);
    }

    public AzureOpenAiConfigStatus GetConfigStatus()
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(_endpoint);
        var hasApiKey = !string.IsNullOrWhiteSpace(_apiKey);
        var hasDeploymentName = !string.IsNullOrWhiteSpace(_deploymentName);
        var deploymentName = hasDeploymentName ? _deploymentName : null;

        string? host = null;
        var mode = "unknown";
        string? responsesUrl = null;

        if (hasEndpoint && Uri.TryCreate(_endpoint, UriKind.Absolute, out var uri))
        {
            host = uri.Host;

            if (host is not null)
            {
                if (host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
                {
                    mode = "openai";
                }
                else if (host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
                {
                    mode = "foundry";
                }
            }

            try
            {
                responsesUrl = BuildResponsesUrl(uri).ToString();
            }
            catch
            {
                responsesUrl = null;
            }
        }

        return new AzureOpenAiConfigStatus(
            hasEndpoint,
            hasApiKey,
            deploymentName,
            host,
            mode,
            responsesUrl);
    }

    private static Uri BuildResponsesUrl(Uri baseUri)
    {
        var host = baseUri.Host;

        if (host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(baseUri)
            {
                Path = "/openai/responses",
                Query = "api-version=2025-04-01-preview"
            };

            return builder.Uri;
        }

        if (host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri($"{baseUri.Scheme}://{baseUri.Authority}/openai/v1/responses");
        }

        throw new InvalidOperationException($"invalid_config: unsupported_endpoint ({host})");
    }

    private static string NormalizeToJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("invalid_model_output: not_json");
        }

        var t = text.Trim();

        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLineIndex = t.IndexOf('\n');
            if (firstNewLineIndex < 0)
            {
                throw new InvalidOperationException("invalid_model_output: not_json");
            }

            // Remove opening fence line (e.g. ```json or ```)
            t = t[(firstNewLineIndex + 1)..];

            var lastFenceIndex = t.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex >= 0)
            {
                t = t[..lastFenceIndex];
            }

            t = t.Trim();
        }

        if (!t.StartsWith("{", StringComparison.Ordinal))
        {
            var start = t.IndexOf('{');
            var end = t.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                t = t.Substring(start, end - start + 1).Trim();
            }
        }

        if (!t.StartsWith("{", StringComparison.Ordinal) || !t.EndsWith("}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("invalid_model_output: not_json");
        }

        return t;
    }

    private static string? ExtractOutputText(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        // 1) $.output_text
        if (root.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString();
        }

        // 2) $.output[*].content[*].text / .output_text (Responses API)
        if (root.TryGetProperty("output", out var outputElement) &&
            outputElement.ValueKind == JsonValueKind.Array &&
            outputElement.GetArrayLength() > 0)
        {
            // First pass: prefer items where type is "output_text" or "text"
            foreach (var preferredType in new[] { "output_text", "text" })
            {
                foreach (var outputItem in outputElement.EnumerateArray())
                {
                    if (!outputItem.TryGetProperty("content", out var contentArray) ||
                        contentArray.ValueKind != JsonValueKind.Array ||
                        contentArray.GetArrayLength() == 0)
                    {
                        continue;
                    }

                    foreach (var contentItem in contentArray.EnumerateArray())
                    {
                        if (contentItem.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        if (!contentItem.TryGetProperty("type", out var typeElement) ||
                            typeElement.ValueKind != JsonValueKind.String ||
                            !string.Equals(typeElement.GetString(), preferredType, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (contentItem.TryGetProperty("text", out var textElement) &&
                            textElement.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(textElement.GetString()))
                        {
                            return textElement.GetString();
                        }

                        if (contentItem.TryGetProperty("output_text", out var contentOutputTextElement) &&
                            contentOutputTextElement.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(contentOutputTextElement.GetString()))
                        {
                            return contentOutputTextElement.GetString();
                        }
                    }
                }
            }

            // Second pass: any content item with non-empty text/output_text, regardless of type
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array ||
                    contentArray.GetArrayLength() == 0)
                {
                    continue;
                }

                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (contentItem.TryGetProperty("text", out var textElement) &&
                        textElement.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(textElement.GetString()))
                    {
                        return textElement.GetString();
                    }

                    if (contentItem.TryGetProperty("output_text", out var contentOutputTextElement) &&
                        contentOutputTextElement.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(contentOutputTextElement.GetString()))
                    {
                        return contentOutputTextElement.GetString();
                    }
                }
            }
        }

        // 3) Chat completions style: $.choices[0].message.content
        if (TryGetFirstChoice(root, out var choiceElement))
        {
            if (choiceElement.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.Object &&
                messageElement.TryGetProperty("content", out var messageContentElement) &&
                messageContentElement.ValueKind == JsonValueKind.String)
            {
                var messageContent = messageContentElement.GetString();
                if (!string.IsNullOrWhiteSpace(messageContent))
                {
                    return messageContent;
                }
            }

            // 4) Chat completions style: $.choices[0].text
            if (choiceElement.TryGetProperty("text", out var choiceTextElement) &&
                choiceTextElement.ValueKind == JsonValueKind.String)
            {
                var choiceText = choiceTextElement.GetString();
                if (!string.IsNullOrWhiteSpace(choiceText))
                {
                    return choiceText;
                }
            }
        }

        return null;
    }

    private static bool TryGetFirstChoice(JsonElement root, out JsonElement choiceElement)
    {
        choiceElement = default;

        if (!root.TryGetProperty("choices", out var choicesElement) ||
            choicesElement.ValueKind != JsonValueKind.Array ||
            choicesElement.GetArrayLength() == 0)
        {
            return false;
        }

        choiceElement = choicesElement[0];
        return choiceElement.ValueKind == JsonValueKind.Object;
    }

    public sealed record AzureOpenAiConfigStatus(
        bool HasEndpoint,
        bool HasApiKey,
        string? DeploymentName,
        string? EndpointHost,
        string Mode,
        string? ResponsesUrl);
}

