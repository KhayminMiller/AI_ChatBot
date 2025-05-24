using Chatbot.Server.Data;
using Chatbot.Server.Models; // ✅ Use your actual models
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;

namespace Chatbot.Server.Services
{
    public class FaceDetectionService
    {
        private readonly BlobStorage _blobStorageService;
        private readonly VisionDbContext _dbContext;

        public FaceDetectionService(BlobStorage blobStorageService, VisionDbContext dbContext)
        {
            _blobStorageService = blobStorageService;
            _dbContext = dbContext;
        }

        public List<Rect> DetectFaces(byte[] imageBytes)
        {
            using var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            var cascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
            var faces = cascade.DetectMultiScale(gray, 1.1, 4);
            return faces.ToList();
        }

        public async Task<int> SaveDetectedFacesAsync(string fileName, byte[] originalImage, List<Rect> faceRects)
        {
            // Step 1: Save the detection session metadata
            var session = new FaceDetectionImage
            {
                FileName = fileName,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.FaceDetectionImages.Add(session);
            await _dbContext.SaveChangesAsync(); // Now session.Id is available

            // Step 2: Load image and save each detected face
            using var mat = Cv2.ImDecode(originalImage, ImreadModes.Color);

            for (int i = 0; i < faceRects.Count; i++)
            {
                var rect = faceRects[i];
                var faceMat = new Mat(mat, rect);
                var croppedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_face{i + 1}.jpg";

                using var croppedStream = faceMat.ToMemoryStream(".jpg");
                var croppedBytes = croppedStream.ToArray();

                // Upload cropped face image to blob storage
                await _blobStorageService.UploadFileAsync(croppedBytes, croppedFileName);

                // Save metadata to database
                var face = new DetectedFace
                {
                    FaceDetectionImageId = session.Id, // ✅ Explicit foreign key
                    CroppedFileName = croppedFileName,
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height
                };

                _dbContext.DetectedFaces.Add(face);
            }

            await _dbContext.SaveChangesAsync();

            return session.Id;
        }

    }
}
