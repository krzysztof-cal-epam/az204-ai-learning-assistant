namespace Az204AiLearningAssistant.Api.Controllers;

using Az204AiLearningAssistant.Api.Application;
using Az204AiLearningAssistant.Api.Contracts;
using Az204AiLearningAssistant.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public sealed class LearningAssistantController : ControllerBase
{
    private readonly IAiLearningAssistantService _assistantService;
        private readonly AzureOpenAiClient _azureOpenAiClient;

        public LearningAssistantController(IAiLearningAssistantService assistantService, AzureOpenAiClient azureOpenAiClient)
    {
        _assistantService = assistantService;
            _azureOpenAiClient = azureOpenAiClient;
    }

    [HttpPost("GenerateQuiz")]
    [ProducesResponseType(typeof(GenerateQuizResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateQuizResponse>> GenerateQuiz([FromBody] GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        var result = await _assistantService.GenerateQuizAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ExplainAnswer")]
    [ProducesResponseType(typeof(ExplainAnswerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExplainAnswerResponse>> ExplainAnswer([FromBody] ExplainAnswerRequest request, CancellationToken cancellationToken)
    {
        var result = await _assistantService.ExplainAnswerAsync(request, cancellationToken);
        return Ok(result);
    }

        [HttpGet("Health")]
        [ProducesResponseType(typeof(LearningAssistantHealthResponse), StatusCodes.Status200OK)]
        public ActionResult<LearningAssistantHealthResponse> Health()
        {
            var status = _azureOpenAiClient.GetConfigStatus();

            var response = new LearningAssistantHealthResponse
            {
                HasEndpoint = status.HasEndpoint,
                HasApiKey = status.HasApiKey,
                DeploymentName = status.DeploymentName,
                EndpointHost = status.EndpointHost,
                Mode = status.Mode
            };

            return Ok(response);
        }
}

