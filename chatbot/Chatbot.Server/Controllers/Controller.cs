using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    [HttpGet]
    public async Task StreamChat([FromQuery] string question)
    {
        Response.ContentType = "text/plain";

        using var httpClient = new HttpClient();
        var requestBody = new
        {
            model = "llama3",
            prompt = question,
            stream = true
        };

        using var response = await httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", requestBody);
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                var chunk = JsonDocument.Parse(line).RootElement.GetProperty("response").GetString();
                await Response.WriteAsync(chunk);
                await Response.Body.FlushAsync();
            }
        }
    }

}


public class ChatRequest
{
    public string Question { get; set; }
}
