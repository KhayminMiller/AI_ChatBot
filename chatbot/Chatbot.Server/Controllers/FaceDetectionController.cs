using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Chatbot.Server.Data;
using Chatbot.Server.Models;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.EntityFrameworkCore;
using Chatbot.Server.Models.DTOs;
using Chatbot.Server.Services;
using Azure.Storage.Blobs;

namespace Chatbot.Server.Controllers;

[ApiController]
[Route("api/FaceDetection")]
public class FaceDetectionController : ControllerBase
{
    private readonly VisionDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly FaceDetectionService _faceDetectionService;
    private readonly BlobServiceClient _blobServiceClient;

    public FaceDetectionController(
        VisionDbContext dbContext,
        IWebHostEnvironment env,
        FaceDetectionService faceDetectionService,
        BlobServiceClient blobServiceClient)
    {
        _dbContext = dbContext;
        _env = env;
        _faceDetectionService = faceDetectionService;
        _blobServiceClient = blobServiceClient;
    }


    [HttpPost("detect")]
    public async Task<IActionResult> DetectFaces([FromForm] IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("No image uploaded.");

        using var stream = image.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var imgData = ms.ToArray();
        using var mat = Cv2.ImDecode(imgData, ImreadModes.Color);

        var cascadePath = Path.Combine(_env.ContentRootPath, "haarcascades", "haarcascade_frontalface_default.xml");
        if (!System.IO.File.Exists(cascadePath))
            return StatusCode(500, $"Cascade file not found at: {cascadePath}");

        try
        {
            using var cascade = new CascadeClassifier(cascadePath);
            if (cascade.Empty())
                return StatusCode(500, $"CascadeClassifier failed to load from: {cascadePath}");

            var faces = cascade.DetectMultiScale(mat);

            var faceDetectionImage = new FaceDetectionImage
            {
                FileName = image.FileName,
                Timestamp = DateTime.UtcNow,
                ImageData = imgData
            };
            _dbContext.FaceDetectionImages.Add(faceDetectionImage);
            await _dbContext.SaveChangesAsync();

            var containerClient = _blobServiceClient.GetBlobContainerClient("faces");
            await containerClient.CreateIfNotExistsAsync();

            foreach (var rect in faces)
            {
                var faceMat = new Mat(mat, rect);
                using var bitmap = BitmapConverter.ToBitmap(faceMat);
                using var croppedStream = new MemoryStream();
                bitmap.Save(croppedStream, ImageFormat.Png);
                croppedStream.Position = 0;

                var blobName = $"cropped/{Guid.NewGuid()}.png";
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(croppedStream, overwrite: true);

                var detectedFace = new DetectedFace
                {
                    CroppedFileName = blobName,
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height,
                    FaceDetectionImageId = faceDetectionImage.Id,
                    ImageData = imgData //
                };

                _dbContext.DetectedFaces.Add(detectedFace);
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { Count = faces.Length });
        }
        catch (Exception ex)
        {
            var error = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, $"Face detection or blob/database error: {error}\nPath attempted: {cascadePath}");
        }

    }




    [HttpGet("cropped-faces")]
    public IActionResult GetAllCroppedFaces()
    {
        var faces = _dbContext.DetectedFaces
            .Select(f => new {
                f.Id,
                f.CroppedFileName,
                Base64Image = Convert.ToBase64String(f.ImageData)
            })
            .ToList();

        return Ok(faces);
    }
    [HttpGet("faces")]
    public IActionResult GetFilteredFaces(string? filename, DateTime? startDate, DateTime? endDate, int? sessionId)
    {
        var query = _dbContext.DetectedFaces
            .Include(f => f.FaceDetectionImage)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filename))
            query = query.Where(f => f.FaceDetectionImage.FileName.Contains(filename));

        if (startDate.HasValue)
            query = query.Where(f => f.FaceDetectionImage.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(f => f.FaceDetectionImage.Timestamp <= endDate.Value);

        if (sessionId.HasValue)
            query = query.Where(f => f.FaceDetectionImageId == sessionId.Value);

        var results = query
            .ToList()
            .Select(f => new DetectedFaceDto
            {
                Id = f.Id,
                CroppedFileName = f.CroppedFileName,
                X = f.X,
                Y = f.Y,
                Width = f.Width,
                Height = f.Height,
                FileName = f.FaceDetectionImage?.FileName,
                Timestamp = f.FaceDetectionImage?.Timestamp ?? DateTime.MinValue,
                Base64Image = Convert.ToBase64String(f.ImageData)
            });

        return Ok(results);
    }
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAndDetect(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var detectedFaces = _faceDetectionService.DetectFaces(imageBytes);

            // Save cropped faces and metadata
            var sessionId = await _faceDetectionService.SaveDetectedFacesAsync(file.FileName, imageBytes, detectedFaces);

            return Ok(new { Message = "Faces detected", SessionId = sessionId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Face detection failed: {ex.Message}");
        }
    }


}
