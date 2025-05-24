using Microsoft.AspNetCore.Mvc;
using ChatBot.Server.Data;
using Chatbot.Server.Models;
using OpenCvSharp;
using System.Drawing.Imaging;
using Chatbot.Server.Data;
using OpenCvSharp.Extensions;
/*
[ApiController]
[Route("api/FaceDetection")]
public class FaceDetectionController : ControllerBase
{
    private readonly FaceDetecContext _dbContext;

    public FaceDetectionController(FaceDetecContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("detect")]
    public async Task<IActionResult> DetectFaces(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("No file uploaded.");

        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);
        byte[] imageData = memoryStream.ToArray();

        // Load image into OpenCvSharp Mat
        using var srcMat = Cv2.ImDecode(imageData, ImreadModes.Color);
        if (srcMat.Empty())
            return BadRequest("Invalid image data.");

        // Load Haar cascade for face detection
        using var cascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
        var faces = cascade.DetectMultiScale(srcMat, 1.1, 4);

        if (faces.Length == 0)
        {
            return Ok(new { Message = "No faces detected." });
        }

        // Save each cropped face
        foreach (var faceRect in faces)
        {
            using var faceMat = new Mat(srcMat, faceRect);
            byte[] croppedFace;

            using (var ms = new MemoryStream())
            {
                var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(faceMat);
                bitmap.Save(ms, ImageFormat.Png);
                croppedFace = ms.ToArray();
            }

            var detection = new FaceDetectionDB
            {
                FileName = image.FileName,
                Timestamp = DateTime.UtcNow,
                FaceCount = faces.Length,
                FaceImage = croppedFace
            };

            _dbContext.FaceDetectionDB.Add(detection);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = $"Detected {faces.Length} face(s).", Count = faces.Length });
    }

    [HttpGet("list")]
    public IActionResult ListDetections()
    {
        var results = _dbContext.FaceDetectionDB
            .OrderByDescending(r => r.Timestamp)
            .Select(r => new
            {
                r.Id,
                r.FileName,
                r.Timestamp,
                r.FaceCount
            })
            .ToList();

        return Ok(results);
    }

    [HttpGet("{id}")]
    public IActionResult GetDetection(int id)
    {
        var result = _dbContext.FaceDetectionDB.FirstOrDefault(r => r.Id == id);
        if (result == null)
            return NotFound();

        return File(result.FaceImage, "image/png");
    }
}
*/