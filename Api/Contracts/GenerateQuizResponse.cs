namespace Az204AiLearningAssistant.Api.Contracts;

public sealed class GenerateQuizResponse
{
    public required string Topic { get; init; }

    public IList<QuizQuestionDto> Questions { get; init; } = new List<QuizQuestionDto>();
}

public sealed class QuizQuestionDto
{
    public required string Question { get; init; }

    public required IReadOnlyList<string> Options { get; init; }

    public int CorrectOptionIndex { get; init; }
}

