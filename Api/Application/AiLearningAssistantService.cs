namespace Az204AiLearningAssistant.Api.Application;

using System.Text.Json;
using Az204AiLearningAssistant.Api.Contracts;
using Az204AiLearningAssistant.Api.Infrastructure;
using Az204AiLearningAssistant.Api.Validation;

public sealed class AiLearningAssistantService : IAiLearningAssistantService
{
    private readonly ITopicAllowlistValidator _topicAllowlistValidator;
    private readonly IJsonSchemaValidator _jsonSchemaValidator;
    private readonly IAnswerSelfCheckService _answerSelfCheckService;
    private readonly AzureOpenAiClient _azureOpenAiClient;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public AiLearningAssistantService(
        ITopicAllowlistValidator topicAllowlistValidator,
        IJsonSchemaValidator jsonSchemaValidator,
        IAnswerSelfCheckService answerSelfCheckService,
        AzureOpenAiClient azureOpenAiClient)
    {
        _topicAllowlistValidator = topicAllowlistValidator;
        _jsonSchemaValidator = jsonSchemaValidator;
        _answerSelfCheckService = answerSelfCheckService;
        _azureOpenAiClient = azureOpenAiClient;
    }

    public async Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default)
    {
        if (!_topicAllowlistValidator.IsAllowedTopic(request.Topic))
        {
            throw new InvalidOperationException("Requested topic is not allowed.");
        }

        _jsonSchemaValidator.ValidateGenerateQuizRequest(request);

        var rawJson = await _azureOpenAiClient.GenerateQuizJsonAsync(
            request.Topic,
            request.QuestionCount,
            request.Difficulty,
            cancellationToken);

        var payload = JsonSerializer.Deserialize<GeneratedQuizPayload>(rawJson, _serializerOptions)
                      ?? throw new InvalidOperationException("invalid_model_output: deserialization_failed");

        if (payload.Questions is null || payload.Questions.Count == 0)
        {
            throw new InvalidOperationException("invalid_model_output: no_questions");
        }

        var questions = new List<QuizQuestionDto>(payload.Questions.Count);

        foreach (var modelQuestion in payload.Questions)
        {
            if (modelQuestion.Options is null || modelQuestion.Options.Count != 4)
            {
                throw new InvalidOperationException("invalid_model_output: options_must_have_exactly_four_items");
            }

            if (string.IsNullOrWhiteSpace(modelQuestion.CorrectAnswer))
            {
                throw new InvalidOperationException("invalid_model_output: missing_correct_answer");
            }

            var correctIndex = modelQuestion.Options.FindIndex(
                option => string.Equals(option, modelQuestion.CorrectAnswer, StringComparison.Ordinal));

            if (correctIndex < 0 || correctIndex > 3)
            {
                throw new InvalidOperationException("invalid_model_output: correct_answer_not_in_options");
            }

            questions.Add(new QuizQuestionDto
            {
                Question = modelQuestion.Question,
                Options = modelQuestion.Options,
                CorrectOptionIndex = correctIndex
            });
        }

        return new GenerateQuizResponse
        {
            Topic = request.Topic,
            Questions = questions
        };
    }

    public async Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length < 10)
        {
            throw new ArgumentException("Invalid question.");
        }

        if (string.IsNullOrWhiteSpace(request.SelectedAnswer))
        {
            throw new ArgumentException("Invalid selectedAnswer.");
        }

        if (request.CorrectAnswer is not null && string.IsNullOrWhiteSpace(request.CorrectAnswer))
        {
            throw new ArgumentException("Invalid correctAnswer.");
        }

        if (!_topicAllowlistValidator.IsAllowedTopic(request.Topic))
        {
            throw new InvalidOperationException("Requested topic is not allowed.");
        }

        _jsonSchemaValidator.ValidateExplainAnswerRequest(request);

        var explanationText = await _azureOpenAiClient.GenerateExplanationAsync(
            request.Topic,
            request.Question,
            request.SelectedAnswer,
            request.CorrectAnswer,
            cancellationToken);

        var explanation = new ExplainAnswerResponse
        {
            Topic = request.Topic,
            Explanation = explanationText,
            Confidence = null
        };

        // Self-check is intentionally a no-op for the minimal demo; the infrastructure is in place
        // and can be enabled in a future iteration without changing the public contract.
        var checkedExplanation = await _answerSelfCheckService.SelfCheckExplanationAsync(explanation, cancellationToken);

        return checkedExplanation;
    }

    private sealed class GeneratedQuizPayload
    {
        public List<GeneratedQuizQuestion> Questions { get; init; } = new();
    }

    private sealed class GeneratedQuizQuestion
    {
        public string Question { get; init; } = string.Empty;

        public List<string> Options { get; init; } = new();

        public string CorrectAnswer { get; init; } = string.Empty;
    }
}

