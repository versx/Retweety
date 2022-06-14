namespace Retweety.Net
{
    using System;
    using System.Net;

    public static class NetUtils
    {
        public static void SendWebhook(string webhookUrl, string json)
        {
            using var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            //wc.Headers.Add(HttpRequestHeader.Authorization, "Bot base64_auth_token");
            //wc.Headers.Add(HttpRequestHeader.UserAgent, "");
            try
            {
                var resp = wc.UploadString(webhookUrl, json);
                Console.WriteLine($"Response: {resp}");
                System.Threading.Thread.Sleep(500);
            }
            catch (WebException ex)
            {
                var resp = (HttpWebResponse)ex.Response;
                switch ((int)resp.StatusCode)
                {
                    //https://discordapp.com/developers/docs/topics/rate-limits
                    case 429:
                        Console.WriteLine("RATE LIMITED");
                        var retryAfter = resp.Headers["Retry-After"];
                        //var limit = resp.Headers["X-RateLimit-Limit"];
                        //var remaining = resp.Headers["X-RateLimit-Remaining"];
                        //var reset = resp.Headers["X-RateLimit-Reset"];
                        if (!int.TryParse(retryAfter, out var retry))
                            return;

                        System.Threading.Thread.Sleep(retry);
                        SendWebhook(webhookUrl, json);
                        break;
                }
            }
        }
    }
}