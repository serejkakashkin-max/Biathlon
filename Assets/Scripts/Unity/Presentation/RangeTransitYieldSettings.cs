using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RangeTransitYieldSettings
    {
        public int schema_version;public string id;
        public float separation_m,maximum_offset_m,response_per_s;
        public static RangeTransitYieldSettings Load()
        {
            var d=JsonUtility.FromJson<RangeTransitYieldSettings>(Resources.Load<TextAsset>("Biathlon/range_transit_yield_v1").text);
            if(d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!float.IsFinite(d.separation_m)||d.separation_m<=0||
                !float.IsFinite(d.maximum_offset_m)||d.maximum_offset_m<=0||!float.IsFinite(d.response_per_s)||d.response_per_s<=0)
                throw new ArgumentException("Invalid range transit presentation policy.");return d;
        }
    }
}
