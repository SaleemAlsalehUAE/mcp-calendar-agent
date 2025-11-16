using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

// --- Step 1: Load ALL Credentials ---
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var azureOpenAiEndpoint = config["AzureOpenAI:Endpoint"];
var azureOpenAiKey = config["AzureOpenAI:ApiKey"];
var azureOpenAiDeploymentName = config["AzureOpenAI:DeploymentName"];
var accuWeatherApiKey = config["AccuWeather:ApiKey"];

if (string.IsNullOrEmpty(accuWeatherApiKey) ||
    string.IsNullOrEmpty(azureOpenAiEndpoint) ||
    string.IsNullOrEmpty(azureOpenAiKey) ||
    string.IsNullOrEmpty(azureOpenAiDeploymentName))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("One or more required API keys/settings are missing from user secrets.");
    Console.ResetColor();
    return;
}

// --- Step 2: Build the Kernel with ALL Plugins (Custom + MCP) ---
var builder = Kernel.CreateBuilder();

// Azure OpenAI chat completion (gpt-4o-agent)
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: azureOpenAiDeploymentName,
    endpoint: azureOpenAiEndpoint,
    apiKey: azureOpenAiKey
);

// Custom Google Calendar plugin
builder.Plugins.AddFromType<GoogleCalendarPlugin>();
Console.WriteLine("Custom GoogleCalendarPlugin loaded.");

// MCP Weather tools
Console.WriteLine("Connecting to Weather MCP Server...");
await using var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(
        new StdioClientTransportOptions
        {
            Command = "npx",
            Arguments = ["-y", "@timlukahorstmann/mcp-weather"],
            EnvironmentVariables = new Dictionary<string, string>
            {
                { "ACCUWEATHER_API_KEY", accuWeatherApiKey }
            }
        }
    )
);

// Discover MCP tools
var weatherTools = await mcpClient.ListToolsAsync();

var functions = new List<KernelFunction>();

foreach (var tool in weatherTools)
{
    if (tool.Name == "weather-get_hourly")
    {
        // Tool that needs a 'location' parameter
        var function = KernelFunctionFactory.CreateFromMethod(
            async (string location) =>
            {
                var toolArgs = new Dictionary<string, object?>
                {
                    ["location"] = location
                };

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"--- MCP TOOL CALL: {tool.Name} ---");
                Console.WriteLine($"Arguments: {JsonSerializer.Serialize(toolArgs)}");

                var result = await mcpClient.CallToolAsync(tool.Name, toolArgs);

                var resultText = string.Join(
                    "\n",
                    result.Content
                        .OfType<TextContentBlock>()
                        .Select(b => b.Text ?? string.Empty)
                );

                Console.WriteLine($"Result: {resultText}");
                Console.ResetColor();

                return resultText;
            },
            functionName: tool.Name,
            description: tool.Description ?? "Gets hourly weather for a given location."
        );

        functions.Add(function);
    }
    else
    {
        // Simple fallback for any other tools without required parameters
        var function = KernelFunctionFactory.CreateFromMethod(
            async () =>
            {
                var toolArgs = new Dictionary<string, object?>();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"--- MCP TOOL CALL: {tool.Name} ---");
                Console.WriteLine($"Arguments: {JsonSerializer.Serialize(toolArgs)}");

                var result = await mcpClient.CallToolAsync(tool.Name, toolArgs);

                var resultText = string.Join(
                    "\n",
                    result.Content
                        .OfType<TextContentBlock>()
                        .Select(b => b.Text ?? string.Empty)
                );

                Console.WriteLine($"Result: {resultText}");
                Console.ResetColor();

                return resultText;
            },
            functionName: tool.Name,
            description: tool.Description ?? "MCP tool."
        );

        functions.Add(function);
    }
}

var weatherPlugin = KernelPluginFactory.CreateFromFunctions(
    "Weather",
    "Weather tools from an MCP server.",
    functions
);

builder.Plugins.Add(weatherPlugin);
Console.WriteLine($"Loaded {weatherTools.Count} tools from Weather MCP Server.");

var kernel = builder.Build();
Console.WriteLine("------------------------------------------");

// --- Step 3: Configure Execution Settings ---
var executionSettings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

// --- Step 4: Start the Chat Loop ---
var currentTime = DateTime.Now.ToString("o");
var history = new ChatHistory();

// --- SYSTEM PROMPT (with timezone note) ---
history.AddSystemMessage($"""
You are an intelligent assistant that schedules meetings.

Workflow for ALL outdoor events:

1. ASK:
   When the user mentions an outdoor event, your FIRST and ONLY action is to ask them for:
   - the city and state/country for the weather check.
   Reply exactly with:
   "Certainly. What is the city and state/country for the weather check?"
   Do not call any tools yet.

2. ACT (Weather):
   Once the user provides the location, call the 'weather-get_hourly' tool with that location.

3. REASON:
   Based on the weather, decide if the outdoor event is possible.
   - If it is raining, snowing, or below 10°C → suggest an online meeting.
   - Otherwise → proceed with scheduling the outdoor event.

4. TIME ZONE:
   - Assume the local timezone based on the location (e.g., "America/New_York" for New York, "Europe/Berlin" for Berlin).
   - When calling the Google Calendar tool, pass a 'timeZone' argument using an IANA time zone ID (e.g., "America/New_York").
   - If you are unsure, use "America/New_York" as a safe default and say that explicitly in natural language.

5. CONFIRM:
   Always confirm the final action with the user before creating the calendar event.
""");

Console.WriteLine("Agent ready. Ask me to schedule an outdoor event.");

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    history.AddUserMessage(userInput);

    Console.WriteLine("Agent is thinking...");
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings,
        kernel
    );

    history.Add(result);
    Console.WriteLine($"Agent: {result.Content}");
    Console.WriteLine("------------------------------------------");
}
