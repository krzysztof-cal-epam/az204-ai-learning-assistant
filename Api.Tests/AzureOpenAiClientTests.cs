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

public sealed class AzureOpenAiClientTests
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
    public void GetResponsesUrl_BuildsExpectedUrl_ForOpenAiEndpoint()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        var client = CreateClient(endpoint);

        // Act
        var uri = client.GetResponsesUrl();

        // Assert
        Assert.Equal("https://foo.openai.azure.com/openai/responses?api-version=2025-04-01-preview", uri.ToString());
    }

    [Fact]
    public async Task GenerateQuizJsonAsync_UnsupportedEndpoint_ThrowsInvalidOperation()
    {
        // Arrange
        const string endpoint = "https://foo.cognitiveservices.azure.com";
        var handler = new ThrowingHandler();
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateQuizJsonAsync("topic", 1, null));

        // Assert
        Assert.Contains("unsupported_endpoint", ex.Message);
    }

    [Fact]
    public async Task GenerateQuizJsonAsync_ErrorBodyIsSurfacedInException()
    {
        // Arrange
        const string endpoint = "https://foo.openai.azure.com";
        var handler = new ErrorResponseHandler();
        var client = CreateClient(endpoint, handler: handler);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateQuizJsonAsync("topic", 1, null));

        // Assert
        Assert.Contains("401", ex.Message);
        Assert.Contains("PermissionDenied", ex.Message);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(
                new InvalidOperationException("HTTP call should not be invoked for unsupported endpoints."));
        }
    }

    private sealed class ErrorResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"PermissionDenied\"}", Encoding.UTF8, "application/json"),
                RequestMessage = request
            };

            return Task.FromResult(response);
        }
    }
}

