using System;

namespace SignitIntegrationSample.Models
{
    public class SigningModel
    {
        public DateTime Deadline { get; set; }
        public string Message { get; set; }
        public string IframeCode { get; set; }
        public string Sref { get; set; }

    }
}