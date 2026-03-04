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
        try
        {
            var result = await _assistantService.GenerateQuizAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Requested topic is not allowed.")
        {
            return BadRequest(new
            {
                error = "invalid_topic",
                message = ex.Message
            });
        }
    }

    [HttpGet("SmokeQuiz")]
    [ProducesResponseType(typeof(GenerateQuizResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateQuizResponse>> SmokeQuiz(
        [FromQuery] string topic = "azure-functions",
        [FromQuery] int count = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest("topic query parameter is required.");
        }

        if (count <= 0)
        {
            return BadRequest("count must be greater than zero.");
        }

        var request = new GenerateQuizRequest
        {
            Topic = topic,
            QuestionCount = count
        };

        try
        {
            var result = await _assistantService.GenerateQuizAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Requested topic is not allowed.")
        {
            return BadRequest(new
            {
                error = "invalid_topic",
                message = ex.Message
            });
        }
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
            Mode = status.Mode,
            ResponsesUrl = status.ResponsesUrl
        };

        return Ok(response);
    }
}

