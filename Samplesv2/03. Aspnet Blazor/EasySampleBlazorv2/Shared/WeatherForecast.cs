using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySampleBlazorv2.Shared
{
    public class WeatherForecast: ISupportLogString
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public string Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string ToLogString()
        {
            var logString = $"{{WeatherForecast:{{Date:{Date},TemperatureC:{TemperatureC},Summary:{Summary},TemperatureF:{TemperatureF}}}}}";
            return logString;
        }
    }
}
