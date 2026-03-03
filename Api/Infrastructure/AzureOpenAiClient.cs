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
        if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_deploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI configuration is missing or incomplete.");
        }

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

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint.TrimEnd('/')}/openai/v1/responses");

        request.Headers.Add("api-key", _apiKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var envelope = JsonSerializer.Deserialize<ResponsesApiEnvelope>(responseJson, _serializerOptions)
                       ?? throw new InvalidOperationException("invalid_model_output: empty_response");

        var outputJson = envelope.OutputText
                         ?? envelope.Output?
                             .FirstOrDefault()?
                             .Content?
                             .FirstOrDefault()?
                             .Text;

        if (string.IsNullOrWhiteSpace(outputJson))
        {
            throw new InvalidOperationException("invalid_model_output: missing_output_text");
        }

        return NormalizeToJson(outputJson);
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

    private sealed class ResponsesApiEnvelope
    {
        [JsonPropertyName("output_text")]
        public string? OutputText { get; init; }

        [JsonPropertyName("output")]
        public List<OutputItem>? Output { get; init; }
    }

    private sealed class OutputItem
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("role")]
        public string? Role { get; init; }

        [JsonPropertyName("content")]
        public List<OutputContentItem>? Content { get; init; }
    }

    private sealed class OutputContentItem
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}

