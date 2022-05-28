using System;

namespace Odering.API
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }

        public string MySummary { get; set; }

        private int MyNumber { get; set; }

        private string MyName { get; set; }

        public int DaughterName { get; set; }
    }
}
