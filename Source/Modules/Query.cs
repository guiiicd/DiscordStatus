using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordStatus
{
    internal class Query : IQuery
    {
        public async Task<string> GetCountryCodeAsync(string ipAddress)
        {
            using var client = new HttpClient();
            try
            {
                string requestUri = $"[http://ip-api.com/json/](http://ip-api.com/json/){ipAddress}";
                HttpResponseMessage response = await client.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic? data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);
                    
                    // Verificação para garantir que 'data' e 'countryCode' não são nulos.
                    string? countryCode = data?.countryCode;
                    return countryCode ?? "CC Error";
                }
                else
                {
                    DSLog.Log(2, $"Error getting country code. Status code: {response.StatusCode}");
                    return "CC Error";
                }
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Exception in GetCountryCodeAsync: {ex.Message}");
                return "CC Error";
            }
        }

        public async Task<string> IPQueryAsync(string ipAddress, string endpoint)
        {
            using var client = new HttpClient();
            try
            {
                string apiUrl = $"[https://ipapi.co/](https://ipapi.co/){ipAddress}/{endpoint}/";
                string response = await client.GetStringAsync(apiUrl).ConfigureAwait(false);
                return response.Trim();
            }
            catch (HttpRequestException ex)
            {
                DSLog.Log(2, $"HttpRequestException in IPQueryAsync: {ex.Message}");
                return "Error";
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Exception in IPQueryAsync: {ex.Message}");
                return "Error";
            }
        }
    }
}