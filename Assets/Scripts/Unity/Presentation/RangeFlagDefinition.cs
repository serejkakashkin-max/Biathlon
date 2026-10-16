using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RangeFlagDefinition
    {
        [Serializable] public sealed class Paint { public string color; public float x,y,width,height; }
        [Serializable] public sealed class Nation { public string id,background; public float aspect; public Paint[] marks; }
        public int schema_version,columns,rows,texture_width;
        public string id,material_resource;
        public float pole_height_m,pole_radius_m,flag_height_m,backstop_clearance_m,end_inset_m,tree_clearance_m;
        public float full_wind_mps,maximum_wave_m,flutter_hz;
        public Nation[] nations;
        public static RangeFlagDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<RangeFlagDefinition>(json);
            if(d==null||d.schema_version!=1)throw new ArgumentException("Versioned range flag definition required.");
            ProductionEnvironmentDefinition.Identity(d.id);ProductionEnvironmentDefinition.Resource(d.material_resource);
            Bound(d.pole_height_m,8,12);Bound(d.pole_radius_m,.025f,.1f);Bound(d.flag_height_m,1,3);
            Bound(d.backstop_clearance_m,3,25);Bound(d.end_inset_m,0,20);Bound(d.tree_clearance_m,1,8);
            Bound(d.full_wind_mps,1,20);Bound(d.maximum_wave_m,0,.7f);Bound(d.flutter_hz,.1f,2);
            if(d.columns<8||d.columns>48||d.rows<4||d.rows>24||d.texture_width<64||d.texture_width>512)
                throw new ArgumentException("Bounded flag geometry and texture budget required.");
            if(d.nations==null||d.nations.Length<2||d.nations.Length>16)throw new ArgumentException("Bounded national flag row required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var n in d.nations)
            {
                if(n==null||string.IsNullOrWhiteSpace(n.id)||!ids.Add(n.id))throw new ArgumentException("Unique nation required.");
                Bound(n.aspect,1,2);ColorOf(n.background);
                if(n.marks==null||n.marks.Length>12)throw new ArgumentException("Flag paint rectangles required.");
                foreach(var m in n.marks)
                {
                    if(m==null)throw new ArgumentException("Missing paint rectangle.");ColorOf(m.color);
                    Bound(m.x,0,1);Bound(m.y,0,1);Bound(m.width,.001f,1);Bound(m.height,.001f,1);
                    if(m.x+m.width>1.00001f||m.y+m.height>1.00001f)throw new ArgumentException("Paint outside flag.");
                }
            }
            return d;
        }
        private static void Bound(float v,float min,float max)=>ProductionEnvironmentDefinition.Bound(v,min,max);
        public static Color ColorOf(string value)
        {if(!ColorUtility.TryParseHtmlString(value,out var color)||color.a!=1)throw new ArgumentException("Opaque flag color required.");return color;}
        public static Color PaintAt(Nation nation,float u,float v)
        {
            Color color=ColorOf(nation.background);
            foreach(var m in nation.marks)if(u>=m.x&&u<m.x+m.width&&v>=m.y&&v<m.y+m.height)color=ColorOf(m.color);
            return color;
        }
        public Vector3[] Positions(ProductionRangeRecipe range,Func<Vector3,float> ground)
        {
            if(range?.backstop==null||ground==null)throw new ArgumentException("Real backstop and ground required.");
            var right=Vector3.Cross(Vector3.up,range.forward).normalized;
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(var bank in range.banks){float x=Vector3.Dot(bank.position-range.origin,right);min=Mathf.Min(min,x);max=Mathf.Max(max,x);}
            float half=Mathf.Min((max-min)*.5f,range.backstop.size.x*.5f)-end_inset_m;
            if(half*2/(nations.Length-1)<flag_height_m*2+1)throw new ArgumentException("Flag row is too dense for the actual range.");
            var center=range.origin+right*((min+max)*.5f);
            center+=range.forward*(Vector3.Dot(range.backstop.position-center,range.forward)+range.backstop.size.z*.5f+backstop_clearance_m);
            var result=new Vector3[nations.Length];
            for(int i=0;i<result.Length;i++)
            {
                var p=center+right*Mathf.Lerp(-half,half,(float)i/(result.Length-1));p.y=ground(p);
                if(!float.IsFinite(p.y))throw new ArgumentException("Finite ground under every flag required.");
                result[i]=p;
            }
            return result;
        }
    }
}
