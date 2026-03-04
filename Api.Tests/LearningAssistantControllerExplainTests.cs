using System;
using System.Collections.Generic;
using System.Net.Http;
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

public sealed class LearningAssistantControllerExplainTests
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
    public async Task ExplainAnswer_InvalidTopic_Returns400()
    {
        // Arrange
        var assistantService = new InvalidTopicAssistantService();
        var controller = CreateController(assistantService);

        var request = new ExplainAnswerRequest
        {
            Topic = "bitcoin",
            Question = "Which trigger runs on a schedule?",
            SelectedAnswer = "Timer trigger",
            CorrectAnswer = "Timer trigger"
        };

        // Act
        var actionResult = await controller.ExplainAnswer(request, CancellationToken.None);

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
    public async Task ExplainAnswer_InvalidQuestion_Returns400()
    {
        // Arrange
        var assistantService = new InvalidQuestionAssistantService();
        var controller = CreateController(assistantService);

        var request = new ExplainAnswerRequest
        {
            Topic = "azure-functions",
            Question = " ",
            SelectedAnswer = "Timer trigger",
            CorrectAnswer = "Timer trigger"
        };

        // Act
        var actionResult = await controller.ExplainAnswer(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var payload = badRequest.Value ?? throw new InvalidOperationException("BadRequest payload was null.");

        var payloadType = payload.GetType();
        var error = payloadType.GetProperty("error")?.GetValue(payload) as string;
        var message = payloadType.GetProperty("message")?.GetValue(payload) as string;

        Assert.Equal("invalid_request", error);
        Assert.Equal("Invalid question.", message);
    }

    [Fact]
    public async Task ExplainAnswer_InvalidSelectedAnswer_Returns400()
    {
        // Arrange
        var assistantService = new InvalidSelectedAnswerAssistantService();
        var controller = CreateController(assistantService);

        var request = new ExplainAnswerRequest
        {
            Topic = "azure-functions",
            Question = "Which trigger runs on a schedule?",
            SelectedAnswer = " ",
            CorrectAnswer = "Timer trigger"
        };

        // Act
        var actionResult = await controller.ExplainAnswer(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var payload = badRequest.Value ?? throw new InvalidOperationException("BadRequest payload was null.");

        var payloadType = payload.GetType();
        var error = payloadType.GetProperty("error")?.GetValue(payload) as string;
        var message = payloadType.GetProperty("message")?.GetValue(payload) as string;

        Assert.Equal("invalid_request", error);
        Assert.Equal("Invalid selectedAnswer.", message);
    }

    [Fact]
    public async Task ExplainAnswer_LlmInvalidOutput_Returns502()
    {
        // Arrange
        var assistantService = new LlmInvalidOutputAssistantService();
        var controller = CreateController(assistantService);

        var request = new ExplainAnswerRequest
        {
            Topic = "azure-functions",
            Question = "Which trigger runs on a schedule?",
            SelectedAnswer = "Timer trigger",
            CorrectAnswer = "Timer trigger"
        };

        // Act
        var actionResult = await controller.ExplainAnswer(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(502, objectResult.StatusCode);

        var payload = objectResult.Value ?? throw new InvalidOperationException("502 payload was null.");
        var payloadType = payload.GetType();
        var error = payloadType.GetProperty("error")?.GetValue(payload) as string;
        var message = payloadType.GetProperty("message")?.GetValue(payload) as string;

        Assert.Equal("llm_invalid_output", error);
        Assert.Equal("LLM response missing explanation text.", message);
    }

    private sealed class InvalidTopicAssistantService : IAiLearningAssistantService
    {
        public Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Requested topic is not allowed.");
        }
    }

    private sealed class InvalidQuestionAssistantService : IAiLearningAssistantService
    {
        public Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("Invalid question.");
        }
    }

    private sealed class InvalidSelectedAnswerAssistantService : IAiLearningAssistantService
    {
        public Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("Invalid selectedAnswer.");
        }
    }

    private sealed class LlmInvalidOutputAssistantService : IAiLearningAssistantService
    {
        public Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("invalid_model_output: missing_output_text");
        }
    }
}

