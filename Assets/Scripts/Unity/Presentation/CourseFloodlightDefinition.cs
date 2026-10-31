using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class CourseFloodlightDefinition
    {
        public int schema_version,max_masts,map_resolution;
        public string id,metal_material,lens_material;
        public float spacing_m,setback_m,height_m,foot_radius_m,minimum_separation_m,range_spacing_m,range_setback_m;
        public float pool_radius_m,range_pool_radius_m,illumination_gain,height_falloff_m,lens_gain;
        public float target_spacing_m,target_height_m,target_range_m,target_intensity,target_setback_m;
        public Color light_color;
        public static CourseFloodlightDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<CourseFloodlightDefinition>(json);
            if(d==null||d.schema_version!=1||d.max_masts<8||d.max_masts>128||(d.map_resolution!=128&&d.map_resolution!=256&&d.map_resolution!=512))throw new ArgumentException("Invalid floodlight schema.");
            ProductionEnvironmentDefinition.Identity(d.id);ProductionEnvironmentDefinition.Resource(d.metal_material);ProductionEnvironmentDefinition.Resource(d.lens_material);
            Bound(d.spacing_m,20,60);Bound(d.setback_m,4,16);Bound(d.height_m,7,18);Bound(d.foot_radius_m,.1f,1);
            Bound(d.minimum_separation_m,6,20);Bound(d.range_spacing_m,15,40);Bound(d.range_setback_m,6,24);
            Bound(d.pool_radius_m,d.spacing_m*.65f,80);Bound(d.range_pool_radius_m,20,100);Bound(d.illumination_gain,.1f,3);Bound(d.height_falloff_m,10,40);Bound(d.lens_gain,1,8);
            Bound(d.target_spacing_m,12,35);Bound(d.target_height_m,3,8);Bound(d.target_range_m,15,40);Bound(d.target_intensity,.5f,150);Bound(d.target_setback_m,2,12);
            for(int i=0;i<4;i++)Bound(d.light_color[i],0,1);return d;
        }
        private static void Bound(float x,float lo,float hi){if(!float.IsFinite(x)||x<lo||x>hi)throw new ArgumentException("Invalid floodlight range.");}
    }
    public readonly struct CourseFloodlightAnchor
    {
        public readonly Vector3 Foot,Focus;
        public readonly bool Range;
        public CourseFloodlightAnchor(Vector3 foot,Vector3 focus,bool range){Foot=foot;Focus=focus;Range=range;}
    }
    /// <summary>Cold authored-corridor layout; never a gameplay obstacle or course edit.</summary>
    public static class CourseFloodlightLayout
    {
        public static CourseFloodlightAnchor[] Build(CourseFloodlightDefinition d,ProductionEnvironmentDefinition venue,Func<Vector3,float> ground,Func<Vector3,bool> clear=null)
        {
            if(d==null||venue==null||ground==null)throw new ArgumentNullException();
            var result=new List<CourseFloodlightAnchor>();
            foreach(var route in venue.route_corridors)
            {
                float carry=0;int ordinal=0;
                for(int k=0;k<route.knots.Length-1;k++)
                {
                    var a=route.knots[k].Position;var b=route.knots[k+1].Position;float length=Vector3.Distance(a,b);if(length<.001f)continue;
                    var forward=(b-a).normalized;var right=new Vector3(forward.z,0,-forward.x).normalized;
                    for(float at=carry;at<length;at+=d.spacing_m)
                    {
                        var focus=Vector3.Lerp(a,b,at/length);float width=Mathf.Lerp(route.widths_m[k],route.widths_m[k+1],at/length)*.5f;
                        for(int attempt=0;attempt<8;attempt++)
                        {
                            float side=((ordinal+attempt)%2==0?1:-1);var foot=focus+right*side*(width+d.setback_m+(attempt/2)*2);
                            if(TryAdd(foot,focus,false))break;
                        }
                        ordinal++;
                    }
                    carry=Mathf.Repeat(carry-length,d.spacing_m);
                }
            }
            var range=venue.range;var f=range.forward;var r=new Vector3(f.z,0,-f.x);
            // Two staggered banks light both firing line and targets evenly.
            var first=range.banks[0].mat_position;var last=range.banks[range.banks.Length-1].mat_position;float span=Vector3.Distance(first,last);
            int count=Math.Max(1,Mathf.CeilToInt(span/d.range_spacing_m));
            for(int i=0;i<=count;i++)
            {
                var center=Vector3.Lerp(first,last,(float)i/count);
                for(int side=0;side<2;side++)
                {
                    var focus=center+f*(side==0?range.target_distance_m*.3f:range.target_distance_m);
                    var foot=center+f*(side==0?-d.range_setback_m:range.target_distance_m+d.range_setback_m);
                    for(int attempt=0;attempt<6;attempt++)if(TryAdd(foot+r*(attempt%2==0?1:-1)*(attempt/2)*3,focus,true))break;
                }
            }
            if(result.Count>d.max_masts)throw new ArgumentException("Floodlight layout exceeds its fixed budget.");return result.ToArray();
            bool TryAdd(Vector3 foot,Vector3 focus,bool isRange)
            {
                if(Clearance(venue,foot)<d.setback_m*.5f+d.foot_radius_m||clear!=null&&!clear(foot))return false;
                var direction=venue.range.forward;var origin=venue.range.banks[0].mat_position;
                var right=new Vector3(direction.z,0,-direction.x);var relative=foot-origin;float depth=Vector3.Dot(relative,direction),lateral=Vector3.Dot(relative,right);
                float extent=Vector3.Dot(venue.range.banks[venue.range.banks.Length-1].mat_position-origin,right);
                if(depth>=-6&&depth<=venue.range.target_distance_m+6&&lateral>=Mathf.Min(0,extent)-4&&lateral<=Mathf.Max(0,extent)+4)return false;
                foreach(var placed in result)if(new Vector2(foot.x-placed.Foot.x,foot.z-placed.Foot.z).sqrMagnitude<d.minimum_separation_m*d.minimum_separation_m)return false;
                foot.y=ground(foot);focus.y=ground(focus);result.Add(new CourseFloodlightAnchor(foot,focus,isRange));return true;
            }
        }
        public static float Clearance(ProductionEnvironmentDefinition venue,Vector3 point)
        {
            float minimum=float.PositiveInfinity;
            foreach(var route in venue.route_corridors)for(int k=0;k<route.knots.Length-1;k++)
            {
                var a=route.knots[k].Position;var b=route.knots[k+1].Position;var p=new Vector2(point.x-a.x,point.z-a.z);var v=new Vector2(b.x-a.x,b.z-a.z);
                float t=v.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p,v)/v.sqrMagnitude):0;
                minimum=Mathf.Min(minimum,(p-v*t).magnitude-Mathf.Lerp(route.widths_m[k],route.widths_m[k+1],t)*.5f);
            }
            return minimum;
        }
    }
}
