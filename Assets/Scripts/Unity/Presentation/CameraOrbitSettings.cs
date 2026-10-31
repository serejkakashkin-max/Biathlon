using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class CameraOrbitSettings
    {
        public int schema_version;
        public string id;
        public float degrees_per_pixel,minimum_pitch_degrees,maximum_pitch_degrees,minimum_elevation_degrees,maximum_elevation_degrees,return_per_s,maximum_delta_pixels,settle_degrees;
        public static CameraOrbitSettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/camera_orbit_v1").text);
        public static CameraOrbitSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<CameraOrbitSettings>(json);
            if(d==null||d.schema_version!=1)throw new ArgumentException("Versioned camera orbit required.");ProductionEnvironmentDefinition.Identity(d.id);
            Bound(d.degrees_per_pixel,.01f,1);Bound(d.minimum_pitch_degrees,-30,0);Bound(d.maximum_pitch_degrees,1,65);
            Bound(d.minimum_elevation_degrees,1,30);Bound(d.maximum_elevation_degrees,45,85);
            Bound(d.return_per_s,1,30);Bound(d.maximum_delta_pixels,10,4000);Bound(d.settle_degrees,.001f,1);return d;
        }
        private static void Bound(float value,float minimum,float maximum)
        {if(!float.IsFinite(value)||value<minimum||value>maximum)throw new ArgumentException("Invalid orbit camera range.");}
    }
}
