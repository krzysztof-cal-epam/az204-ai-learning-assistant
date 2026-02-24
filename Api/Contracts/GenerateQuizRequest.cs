namespace Az204AiLearningAssistant.Api.Contracts;

public sealed class GenerateQuizRequest
{
    public required string Topic { get; init; }

    public int QuestionCount { get; init; } = 10;

    public string? Difficulty { get; init; }
}

