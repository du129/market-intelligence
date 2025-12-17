using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MarketIntelligence.Api.Controllers;

public class ChatRequest
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly IMemoryCache _cache;

    public ChatController(Kernel kernel, IMemoryCache cache)
    {
        _kernel = kernel;
        _cache = cache;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        Console.WriteLine($"[API] Received message: {request.Message}");

        var cacheKey = $"chat_{request.SessionId}";
        if (!_cache.TryGetValue(cacheKey, out ChatHistory? history))
        {
            // SYSTEM PROMPT: We force it to acknowledge it has data.
            history = new ChatHistory(
                "You are the 'Alpha Hunter' AI Financial Analyst. " +
                "You have access to a SQL Database containing real-time market data and bank insights. " +
                "Your primary job is to query this database using your tools. " +
                
                "RULES:" +
                "1. If asked for 'recommendations', 'alpha hunter', or 'picks', you MUST call `GetDailyAlphaReport`." +
                "2. If asked for prices or charts, you MUST call `GetChartData` or `GetStockHistory`." +
                "3. If the tool returns 'No data', tell the user exactly that. Do NOT make up an answer."
            );
        }

        history!.AddUserMessage(request.Message);

        // --- CRITICAL SETTING: AUTO-INVOKE TOOLS ---
        var settings = new OpenAIPromptExecutionSettings 
        { 
            // This allows the AI to run SQL queries automatically
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0 // Low temperature = precise, factual answers
        };

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        try 
        {
            // Pass the 'kernel' here so it knows about the plugins
            var result = await chatService.GetChatMessageContentAsync(history, settings, _kernel);

            Console.WriteLine($"[API] AI Response: {result.Content}");

            history.AddAssistantMessage(result.Content ?? "");
            _cache.Set(cacheKey, history, TimeSpan.FromMinutes(20));

            return Ok(new { response = result.Content });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API Error] {ex.Message}");
            return StatusCode(500, new { response = "Error processing your request. Check Docker logs." });
        }
    }
}