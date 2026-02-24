namespace Az204AiLearningAssistant.Api.Validation;

using Az204AiLearningAssistant.Api.Contracts;

public interface IJsonSchemaValidator
{
    void ValidateGenerateQuizRequest(GenerateQuizRequest request);

    void ValidateExplainAnswerRequest(ExplainAnswerRequest request);
}

