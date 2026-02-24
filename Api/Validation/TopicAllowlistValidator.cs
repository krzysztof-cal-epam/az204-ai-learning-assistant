namespace Az204AiLearningAssistant.Api.Validation;

public sealed class TopicAllowlistValidator : ITopicAllowlistValidator
{
    // For the PoC we hard-code AZ-204 related topics.
    private static readonly HashSet<string> AllowedTopics =
    [
        "azure-functions",
        "app-service",
        "azure-storage",
        "cosmos-db",
        "event-grid",
        "service-bus",
        "key-vault",
        "managed-identities",
        "api-management",
        "containers",
        "monitoring",
        "messaging",
        "security",
        "compute",
        "networking"
    ];

    public bool IsAllowedTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return false;
        }

        return AllowedTopics.Contains(topic.Trim().ToLowerInvariant());
    }
}

