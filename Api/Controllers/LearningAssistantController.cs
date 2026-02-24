namespace Az204AiLearningAssistant.Api.Controllers;

using Az204AiLearningAssistant.Api.Application;
using Az204AiLearningAssistant.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public sealed class LearningAssistantController : ControllerBase
{
    private readonly IAiLearningAssistantService _assistantService;

    public LearningAssistantController(IAiLearningAssistantService assistantService)
    {
        _assistantService = assistantService;
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
}

