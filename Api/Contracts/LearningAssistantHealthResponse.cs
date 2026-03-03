namespace Az204AiLearningAssistant.Api.Contracts;

public sealed class LearningAssistantHealthResponse
{
    public bool HasEndpoint { get; init; }

    public bool HasApiKey { get; init; }

    public string? DeploymentName { get; init; }

    public string? EndpointHost { get; init; }

    public string Mode { get; init; } = "unknown";

    public string? ResponsesUrl { get; init; }
}

