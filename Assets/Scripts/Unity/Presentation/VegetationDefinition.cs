using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class VegetationDefinition
    {
        public int schema_version, columns, rows, max_instances, seed;
        public string id;
        public float spacing_m, jitter_fraction, route_half_width_m, canopy_radius_m, minimum_scale, maximum_scale;
        public float near_lod_m, far_lod_m, cull_m, low_density, balanced_density, wind_amplitude_m, wind_frequency, snow_coverage;
        public static VegetationDefinition Load() => FromJson(Resources.Load<TextAsset>("Biathlon/vegetation_v1").text);
        public static VegetationDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<VegetationDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.columns<1||d.rows<1||d.columns>64||d.rows>64||d.max_instances<1||d.max_instances>1023)
                throw new ArgumentException("Invalid vegetation schema/capacity.");
            Bound(d.spacing_m,2,20);Bound(d.jitter_fraction,0,.9f);Bound(d.route_half_width_m,1,20);Bound(d.canopy_radius_m,2.5f,5);
            Bound(d.minimum_scale,.5f,1);Bound(d.maximum_scale,1,1.5f);Bound(d.near_lod_m,5,50);Bound(d.far_lod_m,d.near_lod_m+1,100);
            Bound(d.cull_m,d.far_lod_m+1,200);Bound(d.low_density,.05f,1);Bound(d.balanced_density,d.low_density,1);
            Bound(d.wind_amplitude_m,0,.3f);Bound(d.wind_frequency,0,3);Bound(d.snow_coverage,0,1);return d;
        }
        private static void Bound(float x,float lo,float hi) { if(float.IsNaN(x)||float.IsInfinity(x)||x<lo||x>hi)throw new ArgumentException("Invalid vegetation range."); }
        public float Density(string tier) => tier=="low"?low_density:tier=="balanced"?balanced_density:tier=="high"?1:throw new ArgumentException("Unknown vegetation tier.");
        public int Lod(float distance) => distance>=cull_m?-1:distance>=far_lod_m?2:distance>=near_lod_m?1:0;
        public static uint Mix(uint x) { unchecked {x^=x>>16;x*=0x7feb352d;x^=x>>15;x*=0x846ca68b;return x^(x>>16);} }
        private static float Unit(uint x) => (Mix(x)&0xffffff)/16777216f;
        public VegetationInstance[] Place(Vector3[] route)
        {
            if(route==null||route.Length<2)throw new ArgumentException("Explicit route polyline required.");
            foreach(var p in route)if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new ArgumentException("Non-finite route.");
            var list=new List<VegetationInstance>(max_instances);
            for(int z=0;z<rows;z++)for(int x=0;x<columns;x++)
            {
                uint key=unchecked((uint)(seed+x*73856093+z*19349663));
                var p=new Vector3((x-(columns-1)*.5f+(Unit(key)-.5f)*jitter_fraction)*spacing_m,0,(z-(rows-1)*.5f+(Unit(key+1)-.5f)*jitter_fraction)*spacing_m);
                float scale=Mathf.Lerp(minimum_scale,maximum_scale,Unit(key+2));
                if(DistanceToRoute(p,route)<route_half_width_m+canopy_radius_m*scale+wind_amplitude_m)continue;
                if(list.Count==max_instances)break;
                list.Add(new VegetationInstance(p,scale,Unit(key+3)*360,Unit(key+4)));
            }
            return list.ToArray();
        }
        public static float DistanceToRoute(Vector3 p,Vector3[] route)
        {
            float best=float.PositiveInfinity;
            for(int i=1;i<route.Length;i++) {var a=route[i-1];var b=route[i];a.y=b.y=p.y=0;var v=b-a;float t=v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude):0;best=Mathf.Min(best,(p-a-v*t).magnitude);}
            return best;
        }
    }
    public readonly struct VegetationInstance
    {
        public readonly Vector3 Position;
        public readonly float Scale, DensityRank;
        public readonly Matrix4x4 Matrix;
        public VegetationInstance(Vector3 p,float scale,float yaw,float rank) { Position=p;Scale=scale;DensityRank=rank;Matrix=Matrix4x4.TRS(p,Quaternion.Euler(0,yaw,0),Vector3.one*scale); }
        private VegetationInstance(Vector3 p,float scale,float rank,Matrix4x4 matrix){Position=p;Scale=scale;DensityRank=rank;Matrix=matrix;}
        public VegetationInstance AtHeight(float y)
        {
            if(!float.IsFinite(y))throw new ArgumentException("Finite tree height required.");
            var p=Position;p.y=y;var matrix=Matrix;matrix.m13=y;
            return new VegetationInstance(p,Scale,DensityRank,matrix);
        }
    }
}
