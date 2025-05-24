namespace Chatbot.Server.Models
{
    public class ORCdB
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
