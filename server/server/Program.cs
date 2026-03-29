using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiApiKey);
});

var app = builder.Build();

// Load system instructions from markdown file.
var systemInstructions = await File.ReadAllTextAsync(Path.Combine(app.Environment.ContentRootPath, "instructions.md"));

// Load persisted memories.
var memoriesPath = Path.Combine(app.Environment.ContentRootPath, "memories.json");
var memories = new List<string>();
if (File.Exists(memoriesPath))
{
    var memoriesJson = await File.ReadAllTextAsync(memoriesPath);
    memories = JsonConvert.DeserializeObject<List<string>>(memoriesJson) ?? [];
}

// Configure the HTTP request pipeline.

app.MapPost("/next", async (HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    using var httpClient = httpClientFactory.CreateClient("OpenAI");
    var stopwatch = Stopwatch.StartNew();

    request.EnableBuffering();

    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);

    if (ms.Length == 0)
    {
        return Results.BadRequest("No image data received.");
    }

    ms.Position = 0;

    var base64Image = Convert.ToBase64String(ms.ToArray());

    // Build system prompt with current memories appended
    var prompt = systemInstructions;
    if (memories.Count > 0)
    {
        var memoryList = string.Join("\n", memories.Select(m => $"- {m}"));
        prompt += "\n\n" + memoryList;
    }

    var requestBody = new
    {
        model = "gpt-5.4",
        messages = new[]
        {
            new
            {
                role = "system",
                content = new object[]
                {
                    new { type = "text", text = prompt }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                }
            }
        }
    };

    string jsonBody = JsonConvert.SerializeObject(requestBody);
    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

    HttpResponseMessage httpResponse = await httpClient.PostAsync("v1/chat/completions", content);
    string openAiResponse = await httpResponse.Content.ReadAsStringAsync();

    if (httpResponse.IsSuccessStatusCode)
    {
        JObject responseObject = JObject.Parse(openAiResponse);
        JArray choices = responseObject["choices"] as JArray;
        JObject first = choices[0] as JObject;
        string responseText = first["message"]["content"].Value<string>();

        // Strip markdown code fences if present
        responseText = responseText.Trim();
        if (responseText.StartsWith("```"))
        {
            int firstNewline = responseText.IndexOf('\n');
            if (firstNewline >= 0)
                responseText = responseText[(firstNewline + 1)..];
            if (responseText.EndsWith("```"))
                responseText = responseText[..^3];
            responseText = responseText.Trim();
        }

        var jsonResponse = JObject.Parse(responseText);

        // Extract and persist memory if present
        var memoryToken = jsonResponse["memory"];
        if (memoryToken != null)
        {
            var memory = memoryToken.Value<string>();
            if (!string.IsNullOrWhiteSpace(memory))
            {
                memories.Add(memory);
                if (memories.Count > 20)
                {
                    // Compact: summarize the first two memories into a rolling summary
                    var summary = await CompactMemoriesAsync(httpClient, memories[0], memories[1]);
                    memories.RemoveAt(0); // remove old summary
                    memories[0] = summary; // replace second entry with new combined summary
                }
                await File.WriteAllTextAsync(memoriesPath, JsonConvert.SerializeObject(memories, Formatting.Indented));
            }
        }

        return Results.Content(jsonResponse.ToString(Formatting.None), "application/json");
    }
    else
    {
        return Results.InternalServerError("Failed to get response from OpenAI.");
    }
});

app.Run();

static async Task<string> CompactMemoriesAsync(HttpClient httpClient, string storySoFar, string newMemory)
{
    var requestBody = new
    {
        model = "gpt-5.4",
        messages = new[]
        {
            new
            {
                role = "system",
                content = "You are a memory compactor for a robot controller. You will be given two memories: a running summary of everything the robot has learned so far, and a new memory. Combine them into a single concise summary that preserves the most important information. Respond with only the summary text, no other formatting."
            },
            new
            {
                role = "user",
                content = $"Story so far:\n{storySoFar}\n\nNew memory:\n{newMemory}"
            }
        }
    };

    var json = JsonConvert.SerializeObject(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync("v1/chat/completions", content);
    var responseText = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var responseObject = JObject.Parse(responseText);
        return responseObject["choices"]![0]!["message"]!["content"]!.Value<string>()!.Trim();
    }

    // If summarization fails, fall back to simple concatenation
    return $"{storySoFar} | {newMemory}";
}