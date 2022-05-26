using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace UVIndicator2
{
    internal class APIRequestsController
    {
        private static readonly string API_KEY = "";
        public static readonly string geocodingAPIURL = "https://api.openweathermap.org/geo/1.0/direct";
        public static readonly string weatherAPIURL = "https://api.openweathermap.org/data/2.5/onecall";

        private static readonly HttpClient _client = new();

        public static async Task<(string, string)?> GetLocationCoordinates(string name)
        {
            string apiBaseUrl = geocodingAPIURL + $"?q={name}" + $"&appid={API_KEY}";
            try
            {
                string responseBody = await _client.GetStringAsync(apiBaseUrl);
                if (responseBody != null)
                {
                    
                    List<JObject> responses = JsonConvert.DeserializeObject<List<JObject>>(responseBody);
                    
                    if (responses != null && responses.Count > 0)
                    {
                        JToken firstOption = responses[0];
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        return (firstOption["lat"].ToString(), firstOption["lon"].ToString());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    return null; // return a bit quicker
                }
                return null;
            }
            catch (HttpRequestException e)
            {
                //TODO: Set up Logging  
                Debug.WriteLine("\nException Caught!");
                Debug.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        // returns temperature, UV, humidity
        public static async Task<(double, double, double)?> GetWeatherFor((string, string) coordinates)
        {
            string apiBaseUrl = weatherAPIURL + $"?lat={coordinates.Item1}&lon={coordinates.Item2}&units=metric&exclude=minutely,hourly,daily,alerts&appid={API_KEY}";
            try
            {
                string responseBody = await _client.GetStringAsync(apiBaseUrl);
                if (responseBody != null)
                {
                    JObject response = JObject.Parse(responseBody);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    double temperature = Double.Parse(response["current"]["temp"].ToString());
                    double uvi = Double.Parse(response["current"]["uvi"].ToString());
                    double humidity = Double.Parse(response["current"]["humidity"].ToString());
                    return (temperature, uvi, humidity);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                return null;
            }
            catch (HttpRequestException e)
            {
                //TODO: Set up Logging  
                Debug.WriteLine("\nException Caught!");
                Debug.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }
    }
}
