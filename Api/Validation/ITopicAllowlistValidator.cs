namespace Az204AiLearningAssistant.Api.Validation;

public interface ITopicAllowlistValidator
{
    bool IsAllowedTopic(string topic);
}

