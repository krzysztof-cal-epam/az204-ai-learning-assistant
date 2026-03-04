using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Az204AiLearningAssistant.Api.Application;
using Az204AiLearningAssistant.Api.Contracts;
using Az204AiLearningAssistant.Api.Infrastructure;
using Az204AiLearningAssistant.Api.Validation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Az204AiLearningAssistant.Api.Tests;

public sealed class AiLearningAssistantServiceExplainTests
{
    [Fact]
    public async Task ExplainAnswerAsync_ReturnsExplanation()
    {
        // Arrange
        var topicAllowlist = new AllowlistValidator();
        var schemaValidator = new NoOpSchemaValidator();

        const string endpoint = "https://foo.openai.azure.com";
        const string explanation = "The selected answer is correct because the Timer trigger runs on a schedule.";

        var responseBody = $$"""{"output_text":"{{explanation}}"}""";
        var handler = new StubHandler(HttpStatusCode.OK, responseBody);

        var settings = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = endpoint,
            ["AzureOpenAI:ApiKey"] = "test-key",
            ["AzureOpenAI:DeploymentName"] = "test-deployment"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var httpClient = new HttpClient(handler);
        var azureOpenAiClient = new AzureOpenAiClient(httpClient, configuration);

        IAnswerSelfCheckService selfCheckService = new AnswerSelfCheckService();

        var service = new AiLearningAssistantService(
            topicAllowlist,
            schemaValidator,
            selfCheckService,
            azureOpenAiClient);

        var request = new ExplainAnswerRequest
        {
            Topic = "azure-functions",
            Question = "Which trigger runs on a schedule?",
            SelectedAnswer = "Timer trigger",
            CorrectAnswer = "Timer trigger"
        };

        // Act
        var result = await service.ExplainAnswerAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(request.Topic, result.Topic);
        Assert.Contains("Timer trigger", result.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Confidence is null or >= 0);
    }

    private sealed class AllowlistValidator : ITopicAllowlistValidator
    {
        public bool IsAllowedTopic(string topic)
        {
            return string.Equals(topic, "azure-functions", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class NoOpSchemaValidator : IJsonSchemaValidator
    {
        public void ValidateGenerateQuizRequest(GenerateQuizRequest request)
        {
        }

        public void ValidateExplainAnswerRequest(ExplainAnswerRequest request)
        {
        }
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

