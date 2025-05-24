namespace Chatbot.Server.Models
{
    public class FaceDetectionDB
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime Timestamp
        {
            get; set;
        }

        public int FaceCount { get; set; } // 👈 Add this line

        public byte[] FaceImage { get; set; } // Optional: for storing cropped face image
    }

}
