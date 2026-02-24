namespace Az204AiLearningAssistant.Api.Validation;

using Az204AiLearningAssistant.Api.Contracts;

public interface IAnswerSelfCheckService
{
    Task<GenerateQuizResponse> SelfCheckQuizAsync(GenerateQuizResponse quiz, CancellationToken cancellationToken = default);

    Task<ExplainAnswerResponse> SelfCheckExplanationAsync(ExplainAnswerResponse explanation, CancellationToken cancellationToken = default);
}

