using Chatbot.Server.Models;

public class DetectedFace
{
    public int Id { get; set; }
    public int FaceDetectionImageId { get; set; } // FK
    public FaceDetectionImage FaceDetectionImage { get; set; }

    public string CroppedFileName { get; set; }
    public byte[]? ImageData { get; set; } // if storing it in DB
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}


