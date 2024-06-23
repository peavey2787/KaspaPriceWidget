using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KaspaInfoWidget
{
    public class KaspaInfo
    {
        private static readonly string cryptoApiEndpoint = "https://api.kaspa.org/info/price";
        private static readonly string cryptoHashrateEndpoint = "https://api.kaspa.org/info/hashrate?stringOnly=false";
        private static readonly string cryptoMaxHashrateEndpoint = "https://api.kaspa.org/info/hashrate/max?stringOnly=false";

        public static async Task<string> GetPrice()
        {
            return await GetCryptoPrice(cryptoApiEndpoint, "KAS");
        }

        public static async Task<string> GetHashrate()
        {
            return await GetNetworkHashrate(cryptoHashrateEndpoint);
        }

        public static async Task<string> GetMaxHashrate()
        {
            return await GetMaxNetworkHashrate(cryptoMaxHashrateEndpoint);
        }

        private static async Task<string> GetCryptoPrice(string endpoint, string cryptoId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(endpoint);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(responseBody);

                    decimal priceDecimal;

                    if (Decimal.TryParse(Convert.ToString(data.price), out priceDecimal))
                    {
                        priceDecimal = Math.Round(priceDecimal, 3);
                        return priceDecimal.ToString();
                    }
                    else
                    {
                        // Handle the case where price is not a valid decimal
                        Console.WriteLine("The price returned by KaspaInfo.GetPrice() is not a valid decimal.");
                    }

                    return "Error";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cryptocurrency price: {ex.Message}");
                return "Error";
            }
        }

        private static async Task<string> GetNetworkHashrate(string endpoint)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(endpoint);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(responseBody);
                    return ConvertToMiningUnit((double)data.hashrate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cryptocurrency network hashrate: {ex.Message}");
                return "Error";
            }
        }

        private static async Task<string> GetMaxNetworkHashrate(string endpoint)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(endpoint);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(responseBody);
                    return ConvertToMiningUnit((double)data.hashrate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cryptocurrency max network hashrate: {ex.Message}");
                return "Error";
            }
        }

        private static string ConvertToMiningUnit(double number)
        {
            string[] units = { "TH/s", "PH/s", "EH/s" };
            int unitIndex = 0;

            while (number >= 1000 && unitIndex < units.Length - 1)
            {
                number /= 1000;
                unitIndex++;
            }

            return $"{number:F2} {units[unitIndex]}";
        }
    }
}

