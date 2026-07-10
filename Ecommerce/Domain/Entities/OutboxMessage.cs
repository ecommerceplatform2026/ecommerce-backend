using System;

namespace Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; } = string.Empty;
        public string JsonContent { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }

        private OutboxMessage() { }

        public OutboxMessage(string eventType, string jsonContent)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            JsonContent = jsonContent;
            CreatedAt = DateTime.UtcNow;
            RetryCount = 0;
        }
    }
}
