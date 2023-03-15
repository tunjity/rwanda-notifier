using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace notifier.Services
{
    public class ApiLinkDTO
    {
        public string method { get; set; }
        public string final_url { get; set; }
        public string headers { get; set; }
        public string body { get; set; }
    }


    public class ApiHelper
    {        

        public static async Task<string> ProxyServiceHelper(ApiLinkDTO payload)
        {
            string responseString = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    string apiUrl = ConfigHelpers.AppSetting("ServiceUrl", "api_proxy");

                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string request = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(request, Encoding.UTF8, "application/json");

                    //make request
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    responseString = await response.Content.ReadAsStringAsync();
                    return responseString;

                }
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return "Internal error occurred! Please try again later.";
            }
        }

        public static async Task<string> MakeRequest(ApiLinkDTO payload)
        {
            var response = string.Empty;
            Dictionary<string, string> headers = null;
            try
            {
                if (string.IsNullOrWhiteSpace(payload.method))
                    return response;

                if (!string.IsNullOrWhiteSpace(payload.headers))
                {
                    headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload.headers);
                }
                payload.method = payload.method.ToUpper();
                var clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using (var _client = new HttpClient(clientHandler))
                {
                    #region header_initialize:

                    _client.DefaultRequestHeaders.Accept.Clear();
                    _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            _client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    #endregion

                    HttpResponseMessage resp;
                    switch (payload.method)
                    {
                        case "POST":

                            var content = new StringContent(payload.body, Encoding.UTF8, "application/json");
                            resp = await _client.PostAsync(payload.final_url, content);
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                response = await resp.Content.ReadAsStringAsync();
                            }
                            break;

                        case "GET":
                            resp = await _client.GetAsync(payload.final_url);
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                response = await resp.Content.ReadAsStringAsync();
                            }
                            break;
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return "Internal error occurred! Please try again later.";
            }
        }


    }
}
