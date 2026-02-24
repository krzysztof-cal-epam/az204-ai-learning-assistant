var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// MVC / controllers
builder.Services.AddControllers();

// Core application services
builder.Services.AddScoped<Az204AiLearningAssistant.Api.Application.IAiLearningAssistantService, Az204AiLearningAssistant.Api.Application.AiLearningAssistantService>();

// Validation components
builder.Services.AddSingleton<Az204AiLearningAssistant.Api.Validation.ITopicAllowlistValidator, Az204AiLearningAssistant.Api.Validation.TopicAllowlistValidator>();
builder.Services.AddSingleton<Az204AiLearningAssistant.Api.Validation.IJsonSchemaValidator, Az204AiLearningAssistant.Api.Validation.JsonSchemaValidator>();
builder.Services.AddSingleton<Az204AiLearningAssistant.Api.Validation.IAnswerSelfCheckService, Az204AiLearningAssistant.Api.Validation.AnswerSelfCheckService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
