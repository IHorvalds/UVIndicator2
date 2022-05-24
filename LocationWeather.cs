using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVIndicator2
{
    internal class LocationWeather
    {
        public string city;
        public string country;
        public double latitude;
        public double longitude;
        public double? degrees;
        public string unit = "C";
        public int? uvIndex;
        public double? humidity; // percentage

        public LocationWeather(string city, string country, double latitude, double longitude, double? degrees, string unit, int? uvIndex, double? humidity)
        {
            this.city = city;
            this.country = country;
            this.latitude = latitude;
            this.longitude = longitude;
            this.degrees = degrees;
            this.unit = unit;
            this.uvIndex = uvIndex;
            this.humidity = humidity;
        }

        public LocationWeather(string city, string country, double latitude, double longitude)
        {
            this.city = city;
            this.country = country;
            this.latitude = latitude;
            this.longitude = longitude;
        }
    }
}
