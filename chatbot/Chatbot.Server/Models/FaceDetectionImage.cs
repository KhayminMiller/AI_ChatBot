namespace Chatbot.Server.Models
{
    public class FaceDetectionImage
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime Timestamp { get; set; }

        public List<DetectedFace> Faces { get; set; } = new();
        public byte[]? ImageData { get; set; }
    }

}
