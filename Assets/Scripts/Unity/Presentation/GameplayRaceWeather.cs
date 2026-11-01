using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private System.Globalization.CultureInfo WeatherCulture=>System.Globalization.CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US");
        private Label raceWeatherCompact,raceWeatherDetails;
        private void RefreshRaceWeather()
        {
            var field=host?.Runner?.Field;var timeline=field?.WeatherTimeline;
            if(raceWeatherCompact==null)return;
            raceWeatherCompact.style.display=timeline==null?DisplayStyle.None:DisplayStyle.Flex;
            if(timeline==null){if(raceWeatherDetails!=null)raceWeatherDetails.style.display=DisplayStyle.None;return;}
            var athlete=field.GetAthlete(host.PlayerIndex);var frame=timeline.Sample(field.Tick.Value/(double)field.TickFrequency);var gear=athlete.EvaluateWeatherEquipment(frame);
            raceWeatherCompact.text=frame.AirTemperatureC.ToString("0.0",WeatherCulture)+"°C · "+L("лыжи ","ski glide ")+(1/gear.SnowResistanceMultiplier).ToString("0.00",WeatherCulture);
            if(raceWeatherDetails==null||raceWeatherDetails.parent!=checkpointPanel)raceWeatherDetails=Text(checkpointPanel,"race_weather_details","",settings.caption_size);
            var later=athlete.EvaluateWeatherEquipment(timeline.Sample(field.Tick.Value/(double)field.TickFrequency+900));
            string surface=L(frame.SnowProfile=="wet_slow"?"мокрый снег":frame.SnowProfile=="icy_fast"?"лёд":frame.SnowProfile=="fresh_soft"?"свежий снег":"плотный снег",frame.SnowProfile=="wet_slow"?"wet snow":frame.SnowProfile=="icy_fast"?"ice":frame.SnowProfile=="fresh_soft"?"fresh snow":"packed snow");
            raceWeatherDetails.text=L("Воздух ","Air ")+frame.AirTemperatureC.ToString("0.0",WeatherCulture)+"°C · "+L("снег ","snow ")+frame.SnowTemperatureC.ToString("0.0",WeatherCulture)+"°C · "+surface+" · "+L("ветер ","wind ")+Math.Sqrt(frame.Wind.EastMetersPerSecond*frame.Wind.EastMetersPerSecond+frame.Wind.NorthMetersPerSecond*frame.Wind.NorthMetersPerSecond).ToString("0.0",WeatherCulture)+L(" м/с"," m/s")+"\n"+L("Лыжи ","Ski glide ")+(1/gear.SnowResistanceMultiplier).ToString("0.00",WeatherCulture)+L(" → прогноз15мин "," →15min forecast ")+(1/later.SnowResistanceMultiplier).ToString("0.00",WeatherCulture)+L(" · контроль "," · control ")+gear.CorneringGripMultiplier.ToString("0.00",WeatherCulture);
        }
    }
}
