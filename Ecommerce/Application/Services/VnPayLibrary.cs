using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Application.Services
{
    public sealed class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(StringComparer.Ordinal);
        private readonly SortedList<string, string> _responseData = new(StringComparer.Ordinal);

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var queryString = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }
                queryString.Append(HttpUtility.UrlEncode(kv.Key));
                queryString.Append('=');
                queryString.Append(HttpUtility.UrlEncode(kv.Value));
            }

            var rawData = string.Join("&", _requestData.Select(kv => $"{kv.Key}={kv.Value}"));
            var vnpSecureHash = HmacSha512(vnpHashSecret, rawData);

            return $"{baseUrl}?{queryString}&vnp_SecureHash={vnpSecureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rawData = string.Join("&", _responseData
                .Where(kv => kv.Key != "vnp_SecureHashType" && kv.Key != "vnp_SecureHash")
                .Select(kv => $"{kv.Key}={kv.Value}"));

            var myChecksum = HmacSha512(secretKey, rawData);
            return myChecksum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}
