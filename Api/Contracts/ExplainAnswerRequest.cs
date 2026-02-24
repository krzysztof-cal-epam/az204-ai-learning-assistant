namespace Az204AiLearningAssistant.Api.Contracts;

public sealed class ExplainAnswerRequest
{
    public required string Topic { get; init; }

    public required string Question { get; init; }

    public required string SelectedAnswer { get; init; }

    public string? CorrectAnswer { get; init; }
}

