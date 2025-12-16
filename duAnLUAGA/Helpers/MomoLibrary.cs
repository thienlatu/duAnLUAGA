using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace duAnLUAGA.Helpers
{
    public class MomoLibrary
    {
        public static string ComputeHmacSha256(string message, string secretKey)
        {
            var keyByte = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                var hashmessage = hmacsha256.ComputeHash(messageBytes);
                var hex = BitConverter.ToString(hashmessage);
                return hex.Replace("-", "").ToLower();
            }
        }

        public static async Task<string> SendPaymentRequest(string endpoint, string postJson)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(postJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }

    public class MomoRequest
    {
        public string partnerCode { get; set; }
        public string requestId { get; set; }
        public long amount { get; set; }
        public string orderId { get; set; }
        public string orderInfo { get; set; }
        public string redirectUrl { get; set; }
        public string ipnUrl { get; set; }
        public string requestType { get; set; }
        public string extraData { get; set; }
        public string signature { get; set; }
        public string lang { get; set; } = "vi";
    }

    public class MomoResponse
    {
        public string partnerCode { get; set; }
        public string requestId { get; set; }
        public string orderId { get; set; }
        public long amount { get; set; }
        public long responseTime { get; set; }
        public string message { get; set; }
        public int resultCode { get; set; }
        public string payUrl { get; set; } 
    }
}

