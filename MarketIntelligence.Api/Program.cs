#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using MarketIntelligence.Api.Plugins;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// 2. Semantic Kernel Configuration
builder.Services.AddTransient<Kernel>(sp =>
{
    var config = builder.Configuration;
    
    // --- FIX: RENAME THIS TO 'kernelBuilder' TO AVOID CONFLICT ---
    var kernelBuilder = Kernel.CreateBuilder(); 

    // Chat Service
    kernelBuilder.AddAzureOpenAIChatCompletion(
        config["AI:DeploymentChat"]!,
        config["AI:Endpoint"]!,
        config["AI:Key"]!
    );

    // RAG Memory (For NewsPlugin)
    var embeddingService = new AzureOpenAITextEmbeddingGenerationService(
        config["AI:DeploymentEmbedding"]!,
        config["AI:Endpoint"]!,
        config["AI:Key"]!
    );
    
    var memoryStore = new AzureAISearchMemoryStore(
        config["Search:Endpoint"]!,
        config["Search:Key"]!
    );

    var memory = new MemoryBuilder()
        .WithTextEmbeddingGeneration(embeddingService)
        .WithMemoryStore(memoryStore)
        .Build();

    // Register Plugins
    kernelBuilder.Plugins.AddFromType<MarketPlugin>("MarketData");
    kernelBuilder.Plugins.AddFromObject(new NewsPlugin(memory), "NewsData");

    return kernelBuilder.Build();
});

// Allow the Frontend to talk to the Backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();