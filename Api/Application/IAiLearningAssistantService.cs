namespace Az204AiLearningAssistant.Api.Application;

using Az204AiLearningAssistant.Api.Contracts;

public interface IAiLearningAssistantService
{
    Task<GenerateQuizResponse> GenerateQuizAsync(GenerateQuizRequest request, CancellationToken cancellationToken = default);

    Task<ExplainAnswerResponse> ExplainAnswerAsync(ExplainAnswerRequest request, CancellationToken cancellationToken = default);
}

