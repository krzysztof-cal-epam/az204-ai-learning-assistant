namespace Az204AiLearningAssistant.Api.Validation;

using Az204AiLearningAssistant.Api.Contracts;

public sealed class JsonSchemaValidator : IJsonSchemaValidator
{
    // For the initial PoC this implementation is intentionally minimal.
    // It can later be replaced with a real JSON schema library such as NJsonSchema.

    public void ValidateGenerateQuizRequest(GenerateQuizRequest request)
    {
        if (request.QuestionCount <= 0)
        {
            throw new InvalidOperationException("QuestionCount must be greater than zero.");
        }
    }

    public void ValidateExplainAnswerRequest(ExplainAnswerRequest request)
    {
        // Basic presence checks are already enforced via required properties.
        // Additional structural validation can be added here later.
    }
}

