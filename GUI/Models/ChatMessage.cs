using System;

namespace GUI.Models
{
    public class ChatMessage
    {
        public string Message { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; }
        public string Sender => IsFromUser ? "You" : "AI Assistant";
    }
}