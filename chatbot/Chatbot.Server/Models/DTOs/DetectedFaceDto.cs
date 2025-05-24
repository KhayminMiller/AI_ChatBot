namespace Chatbot.Server.Models.DTOs
{
    public class DetectedFaceDto
    {
        public int Id { get; set; }
        public string CroppedFileName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? FileName { get; set; }     // From related FaceDetectionImage
        public DateTime? Timestamp { get; set; }  // From related FaceDetectionImage
        public string Base64Image { get; set; }

    }

}
