using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Az204AiLearningAssistant.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Az204AiLearningAssistant.Api.Tests;

public sealed class AzureOpenAiClientResponseParsingTests
{
    private static AzureOpenAiClient CreateClient(
        string endpoint,
        string apiKey = "test-key",
        string deploymentName = "test-deployment",
        HttpMessageHandler? handler = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = endpoint,
            ["AzureOpenAI:ApiKey"] = apiKey,
            ["AzureOpenAI:DeploymentName"] = deploymentName
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var httpClient = handler is null
            ? new HttpClient()
            : new HttpClient(handler);

        return new AzureOpenAiClient(httpClient, configuration);
    }

    [Fact]
    public async Task ExtractText_PrefersOutputText_WhenPresent()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        const string jsonPayload = """{"questions":[{"question":"Q1","options":["A","B","C","D"],"correctAnswer":"A"}]}""";

        var responseBody =
            $$"""{"output_text":"{{jsonPayload}}","output":[{"content":[{"text":"should_not_be_used"}]}]}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateQuizJsonAsync("topic", 1, null);

        // Assert
        Assert.Equal(jsonPayload, result);
    }

    [Fact]
    public async Task ExtractText_FindsTextByScanningOutputContentText()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        const string jsonPayload = """{"questions":[{"question":"Q1","options":["A","B","C","D"],"correctAnswer":"A"}]}""";

        var responseBody =
            $$"""{"output":[{"content":[{"type":"output_text","text":"{{jsonPayload}}"}]}]}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateQuizJsonAsync("topic", 1, null);

        // Assert
        Assert.Equal(jsonPayload, result);
    }

    [Fact]
    public async Task ExtractText_FindsTextWhenNotFirstItem()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        const string jsonPayload = """{"questions":[{"question":"Q1","options":["A","B","C","D"],"correctAnswer":"A"}]}""";

        var responseBody =
            $$"""{"output":[{"content":[{"type":"output_text"},{"type":"output_text","text":"{{jsonPayload}}"}]}]}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateQuizJsonAsync("topic", 1, null);

        // Assert
        Assert.Equal(jsonPayload, result);
    }

    [Fact]
    public async Task ExtractText_FallsBackToChatCompletionsShape_WhenPresent()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        const string jsonPayload = """{"questions":[{"question":"Q1","options":["A","B","C","D"],"correctAnswer":"A"}]}""";

        var responseBody =
            $$"""{"choices":[{"message":{"content":"{{jsonPayload}}"},"text":"should_not_be_used"}]}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateQuizJsonAsync("topic", 1, null);

        // Assert
        Assert.Equal(jsonPayload, result);
    }

    [Fact]
    public void BuildUrl_UsesApiVersion_ForOpenAiAzureHost()
    {
        // Arrange
        const string endpoint = "https://x.openai.azure.com/";
        var client = CreateClient(endpoint);

        // Act
        var uri = client.GetResponsesUrl();

        // Assert
        Assert.Equal("https://x.openai.azure.com/openai/responses?api-version=2025-04-01-preview", uri.ToString());
    }

    [Fact]
    public void BuildUrl_UsesV1_ForFoundryHost()
    {
        // Arrange
        const string endpoint = "https://x.services.ai.azure.com";
        var client = CreateClient(endpoint);

        // Act
        var uri = client.GetResponsesUrl();

        // Assert
        Assert.Equal("https://x.services.ai.azure.com/openai/v1/responses", uri.ToString());
    }

    [Fact]
    public async Task MissingOutputText_ThrowsWithLargeRawSnippet()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";

        var sb = new StringBuilder();
        sb.Append("{\"outer\":\"");
        sb.Append(new string('x', 25000));
        sb.Append("\",\"tail\":\"end\"}");

        var responseBody = sb.ToString();

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateQuizJsonAsync("topic", 1, null));

        // Assert
        Assert.Contains("invalid_model_output: missing_output_text", ex.Message);
        Assert.Contains("raw={", ex.Message);
        Assert.Contains("tail", ex.Message);
        Assert.True(ex.Message.Length > 5000);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public StubHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
                RequestMessage = request
            };

            return Task.FromResult(response);
        }
    }
}

