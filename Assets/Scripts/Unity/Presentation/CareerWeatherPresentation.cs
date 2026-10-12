using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public static class CareerWeatherPresentation
    {
        public static ProductionEnvironmentDefinition Bind(ProductionEnvironmentDefinition source,RaceWeatherPreset weather)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var result=ProductionEnvironmentDefinition.FromJson(source.ToJson());
            if(weather==null)return result; // Replays recorded before weather binding retain the old environment.
            weather.Validate();result.lighting.weather_preset=weather.VisualPreset;result.lighting.preset_id=weather.LightingPreset;
            result.lighting.temperature_c=(float)weather.AirTemperatureC;
            result.lighting.visual_wind=new Vector2((float)weather.WindEast,(float)weather.WindNorth);
            result.terrain.snow_material_preset=weather.SnowMaterial;
            result.terrain.snow_surface_profile=weather.SnowProfile;
            return result;
        }
    }
}
