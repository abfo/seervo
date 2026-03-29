using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

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

    var requestBody = new
    {
        model = "gpt-5.2",
        messages = new[]
        {
            new
            {
                role = "system",
                content = new object[]
                {
                    new { type = "text", text = @$"You are controlling a robot. 

You will be sent an image which is what the robot can currently see. In response you must set the color of four LEDs and a duration in seconds to show that
pattern of colors. Use your best judgement, but know that the LEDs are very bright, so 30 is around the maximum you should set for each color channel unless
there is an emergency. Just respond with JSON ready to send back to the robot in the following format:

{{
  ""colors"": [
    {{ ""r"": 30, ""g"": 10, ""b"": 5 }},
    {{ ""r"": 25, ""g"": 10, ""b"": 0 }},
    {{ ""r"": 15, ""g"": 15, ""b"": 30 }},
    {{ ""r"": 30, ""g"": 0, ""b"": 15 }}
  ],
  ""duration"": 2
}}" }
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
        return Results.Content(jsonResponse.ToString(Formatting.None), "application/json");
    }
    else
    {
        return Results.InternalServerError("Failed to get response from OpenAI.");
    }
});

app.Run();

internal record RgbColor(int R, int G, int B);
internal record NextResponse(RgbColor[] Colors, int Duration);





