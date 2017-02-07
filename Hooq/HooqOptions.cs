namespace Hooq
{
    public class HooqOptions
    {
        public string Protocol { get; set; } = "https";
        public string Server { get; set; } = "hooq.io";
        public string ApiKey { get; set; }
        public string QueueKey { get; set; }
        public int Take { get; set; } = 5;
        public int Timeout { get; set; } = 5;
        public int Interval { get; set; } = 5;

        public HooqOptions(string apiKey, string queueKey)
        {
            ApiKey = apiKey;
            QueueKey = queueKey;
        }
    }
}