﻿namespace DotNetRabbitMQExample
{
    public class SendMessage
    {
        public string Message { get; set; }

        public long Timestamp { get; set; }
    }

    public class ConsumedMessage
    {
        public string Message { get; set; }

        public long Timestamp { get; set; }
    }
}