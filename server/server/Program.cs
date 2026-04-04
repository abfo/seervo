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
var compactInstructions = await File.ReadAllTextAsync(Path.Combine(app.Environment.ContentRootPath, "compact.md"));

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
    Debug.WriteLine("Processing request");

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

    // Validate JPEG header (SOI marker FF D8 FF)
    if (ms.Length < 3)
    {
        return Results.BadRequest("Image data too short to be a valid JPEG.");
    }
    var header = new byte[3];
    await ms.ReadAsync(header.AsMemory(0, 3));
    if (header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
    {
        return Results.BadRequest("Invalid JPEG image data.");
    }
    ms.Position = 0;

    // Save received image to disk
    var imagePath = Path.Combine(app.Environment.ContentRootPath, "latest.jpg");
    await using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
    {
        await ms.CopyToAsync(fileStream);
    }
    ms.Position = 0;

    Debug.WriteLine($"Received {ms.Length:n0} byte image");

    var base64Image = Convert.ToBase64String(ms.ToArray());

    // Build system prompt with current memories appended
    var prompt = systemInstructions;
    if (memories.Count > 0)
    {
        var memoryList = string.Join("\n", memories.Select(m => $"- {m}"));
        prompt += "\n\n" + memoryList;
    }

    var messages = new object[]
    {
        new
        {
            role = "system",
            content = new object[]
            {
                new { type = "input_text", text = prompt }
            }
        },
        new
        {
            role = "user",
            content = new object[]
            {
                new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
            }
        }
    };

    var (responseText, success) = await ChatCompleteAsync(httpClient, "gpt-5.4", messages, "medium");

    if (success)
    {
        Debug.WriteLine($"OpenAI response received in {stopwatch.ElapsedMilliseconds} ms: {responseText}");

        var jsonResponse = JObject.Parse(responseText);

        // Extract and persist memory if present
        var memoryToken = jsonResponse["memory"];
        if (memoryToken != null)
        {
            var memory = memoryToken.Value<string>();
            if (!string.IsNullOrWhiteSpace(memory))
            {
                memories.Add($"{DateTime.Now:yyyy-MM-dd: HH:mm:ss} - {memory}");
                if (memories.Count > 10)
                {
                    // Compact: summarize the first two memories into a rolling summary
                    var summary = await CompactMemoriesAsync(httpClient, compactInstructions, memories[0], memories[1]);
                    memories.RemoveAt(0); // remove old summary
                    memories[0] = summary; // replace second entry with new combined summary

                    Debug.WriteLine("Memories compacted into summary: " + summary);
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

static async Task<string> CompactMemoriesAsync(HttpClient httpClient, string compactInstructions, string storySoFar, string newMemory)
{
    var messages = new object[]
    {
        new
        {
            role = "system",
            content = compactInstructions
        },
        new
        {
            role = "user",
            content = $"Story so far:\n{storySoFar}\n\nNew memory:\n{newMemory}"
        }
    };

    var (responseText, success) = await ChatCompleteAsync(httpClient, "gpt-5.4", messages, "low");

    // If summarization fails, fall back to simple concatenation
    return success ? responseText : $"{storySoFar} | {newMemory}";
}

static async Task<(string Content, bool Success)> ChatCompleteAsync(HttpClient httpClient, string model, object[] messages, string reasoningEffort)
{
    var requestBody = new
    {
        model,
        input = messages,
        reasoning = new { effort = reasoningEffort }
    };
    var json = JsonConvert.SerializeObject(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync("v1/responses", content);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Debug.WriteLine($"OpenAI API error: {response.StatusCode} - {responseBody}");
        return (responseBody, false);
    }

    var responseObject = JObject.Parse(responseBody);

    // Extract text from the first message output content block
    var output = responseObject["output"] as JArray;
    string? responseText = null;
    foreach (var item in output!)
    {
        if (item["type"]?.Value<string>() == "message")
        {
            var contentArray = item["content"] as JArray;
            foreach (var block in contentArray!)
            {
                if (block["type"]?.Value<string>() == "output_text")
                {
                    responseText = block["text"]?.Value<string>();
                    break;
                }
            }
            if (responseText != null) break;
        }
    }

    if (responseText == null)
    {
        Debug.WriteLine($"OpenAI API error: no text content in response - {responseBody}");
        return (responseBody, false);
    }

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

    return (responseText, true);
}