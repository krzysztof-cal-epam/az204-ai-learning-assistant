using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Az204AiLearningAssistant.Api.Application;
using Az204AiLearningAssistant.Api.Contracts;
using Az204AiLearningAssistant.Api.Controllers;
using Az204AiLearningAssistant.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Az204AiLearningAssistant.Api.Tests;

public sealed class LearningAssistantControllerTests
{
    private static LearningAssistantController CreateController(IAiLearningAssistantService assistantService)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://foo.openai.azure.com",
            ["AzureOpenAI:ApiKey"] = "test-key",
            ["AzureOpenAI:DeploymentName"] = "test-deployment"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var azureOpenAiClient = new AzureOpenAiClient(new HttpClient(), configuration);

        return new LearningAssistantController(assistantService, azureOpenAiClient);
    }

    [Fact]
    public async Task GenerateQuiz_InvalidTopic_Returns400()
    {
        // Arrange
        var assistantService = new ThrowingAssistantService();
        var controller = CreateController(assistantService);
        var request = new GenerateQuizRequest
        {
            Topic = "bitcoin",
            QuestionCount = 3
        };

        // Act
        var actionResult = await controller.GenerateQuiz(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var payload = badRequest.Value ?? throw new InvalidOperationException("BadRequest payload was null.");

        var payloadType = payload.GetType();
        var error = payloadType.GetProperty("error")?.GetValue(payload) as string;
        var message = payloadType.GetProperty("message")?.GetValue(payload) as string;

        Assert.Equal("invalid_topic", error);
        Assert.Equal("Requested topic is not allowed.", message);
    }

    [Fact]
    public async Task SmokeQuiz_InvalidTopic_Returns400()
    {
        // Arrange
        var assistantService = new ThrowingAssistantService();
        var controller = CreateController(assistantService);

        // Act
        var actionResult = await controller.SmokeQuiz("bitcoin", 3, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var payload = badRequest.Value ?? throw new InvalidOperationException("BadRequest payload was null.");

        var payloadType = payload.GetType();
        var error = payloadType.GetProperty("error")?.GetValue(payload) as string;
        var message = payloadType.GetProperty("message")?.GetValue(payload) as string;

        Assert.Equal("invalid_topic", error);
        Assert.Equal("Requested topic is not allowed.", message);
    }

    private sealed class ThrowingAssistantService : IAiLearningAssistantService
    {
        public Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Requested topic is not allowed.");
        }

        public Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

