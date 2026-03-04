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

public sealed class AzureOpenAiClientExplainTests
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
    public async Task GenerateExplanationAsync_ExtractsText_FromResponsesApiOutputText()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        const string explanation = "Selected answer is correct because it uses a timer-based schedule.";

        var responseBody = $$"""{"output_text":"{{explanation}}"}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateExplanationAsync(
            "azure-functions",
            "Which trigger runs on a schedule?",
            "Timer trigger",
            "Timer trigger",
            CancellationToken.None);

        // Assert
        Assert.Equal(explanation, result);
        Assert.DoesNotContain("```", result, StringComparison.Ordinal);
        Assert.True(result.Length <= 1501);
    }

    [Fact]
    public async Task GenerateExplanationAsync_StripsFences_AndTruncatesLongText()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";

        var longTextBuilder = new StringBuilder();
        longTextBuilder.Append("This is a very long explanation. ");
        longTextBuilder.Append(new string('x', 2000));

        var fenced =
            $"```text\n{longTextBuilder}\n```";

        var responseBody = $$"""{"output_text":"{{fenced}}"}""";

        var handler = new StubHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var result = await client.GenerateExplanationAsync(
            "azure-functions",
            "Which trigger runs on a schedule?",
            "Timer trigger",
            "Timer trigger",
            CancellationToken.None);

        // Assert
        Assert.DoesNotContain("```", result, StringComparison.Ordinal);
        Assert.True(result.Length <= 1501);
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

