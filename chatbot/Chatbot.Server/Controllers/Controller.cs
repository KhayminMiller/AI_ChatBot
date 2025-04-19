using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> GetChatResponse(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return BadRequest("Question cannot be empty.");
        }
        // Simulated chatbot response (Replace this with AI logic)
        //Response.Headers.Add("Access-Control-Allow-Origin", "*");
        return Ok($"You asked: {question}. Here’s a response!");
    }
}


public class ChatRequest
{
    public string Question { get; set; }
}
