namespace Az204AiLearningAssistant.Api.Contracts;

public sealed class ExplainAnswerResponse
{
    public required string Topic { get; init; }

    public required string Explanation { get; init; }

    public double? Confidence { get; init; }
}

