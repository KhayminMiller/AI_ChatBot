using Microsoft.AspNetCore.Mvc;
using Tesseract;
using ChatBot.Server.Data; // For OCR result model
using Chatbot.Server.Models;
using System.Drawing;

[ApiController]
[Route("api/ocr")]
public class OcrController : ControllerBase
{
    private readonly ORCContext _dbContext;

    public OcrController(ORCContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("extract")]
    public async Task<IActionResult> ExtractText(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("No file uploaded.");

        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);

        var tessPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
        using var engine = new TesseractEngine(tessPath, "eng", EngineMode.Default);
        using var pix = Pix.LoadFromMemory(memoryStream.ToArray());
        using var page = engine.Process(pix);

        string extractedText = page.GetText();

        var result = new ORCdB
        {
            FileName = image.FileName,
            Text = extractedText,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.ORCdB.Add(result);
        await _dbContext.SaveChangesAsync();

        return Ok(result);
    }
    [HttpGet("list")]
    public IActionResult GetAllResults()
    {
        var results = _dbContext.ORCdB
            .OrderByDescending(r => r.Timestamp)
            .Select(r => new
            {
                r.Id,
                r.FileName,
                r.Timestamp
            })
            .ToList();

        return Ok(results);
    }

    [HttpGet("{id}")]
    public IActionResult GetResultById(int id)
    {
        var result = _dbContext.ORCdB.FirstOrDefault(r => r.Id == id);
        if (result == null) return NotFound();
        return Ok(result);
    }
}