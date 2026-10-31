using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional, cosmetic venue dressing. Coordinates follow the real firing-line frame.</summary>
    [Serializable] public sealed class RangeCoachesDefinition
    {
        [Serializable] public sealed class Coach
        {
            public string id,nation,face,skin,hair,headwear;
            public int age,height_mm;
            public bool beard;
        }
        public int schema_version;
        public string id,art_resource;
        public float rear_setback_m,depth_m,raised_m,fence_height_m,gate_width_m,route_clearance_m;
        public Coach[] coaches;
        public static RangeCoachesDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<RangeCoachesDefinition>(json);
            if(d==null||d.schema_version!=1)throw new ArgumentException("Versioned coaches' enclosure required.");
            ProductionEnvironmentDefinition.Identity(d.id);ProductionEnvironmentDefinition.Resource(d.art_resource);
            ProductionEnvironmentDefinition.Bound(d.rear_setback_m,12,30);
            ProductionEnvironmentDefinition.Bound(d.depth_m,2,5);ProductionEnvironmentDefinition.Bound(d.raised_m,.3f,.7f);
            ProductionEnvironmentDefinition.Bound(d.fence_height_m,.8f,1.1f);ProductionEnvironmentDefinition.Bound(d.gate_width_m,1.2f,2);
            ProductionEnvironmentDefinition.Bound(d.route_clearance_m,3,6);
            if(d.coaches==null||d.coaches.Length<6||d.coaches.Length>16)throw new ArgumentException("Bounded coaches' row required.");
            var ids=new HashSet<string>();var faces=AthleteFaceCatalog.Load();var look=AthleteLookCatalog.Load();
            foreach(var c in d.coaches)
            {
                if(c==null||!ids.Add(c.id))throw new ArgumentException("Distinct coach identities required.");
                ProductionEnvironmentDefinition.Identity(c.id);faces.SkinIndex(c.skin);
                if(Array.FindIndex(faces.profiles,p=>p.id==c.face)<0)throw new ArgumentException("Unknown coach face.");
                look.Index("hair_color",c.hair);look.Index("headwear",c.headwear);
                bool nation=false;for(int i=0;i<AthleteAppearancePresentation.NationCount;i++)nation|=AthleteAppearancePresentation.NationAt(i)==c.nation;
                if(!nation||c.age<35||c.age>75||c.height_mm<1700||c.height_mm>1980)throw new ArgumentException("Invalid coach appearance.");
            }
            return d;
        }
        public RangeCoachesLayout Layout(ProductionRangeRecipe range,Func<Vector3,float> ground=null)=>new RangeCoachesLayout(this,range,ground);
    }

    public sealed class RangeCoachesLayout
    {
        public readonly Vector3 Origin,Right,Forward;
        public readonly float HalfWidth,Front,Back,Top;
        public Vector3 World(float x,float y,float z)=>Origin+Right*x+Vector3.up*y+Forward*z;
        public Vector3 Local(Vector3 p){p-=Origin;return new Vector3(Vector3.Dot(p,Right),p.y,Vector3.Dot(p,Forward));}
        public RangeCoachesLayout(RangeCoachesDefinition d,ProductionRangeRecipe range,Func<Vector3,float> ground=null)
        {
            if(d==null||range?.banks==null||range.banks.Length<2)throw new ArgumentException("An actual range row is required.");
            Forward=range.forward;Right=Vector3.Cross(Vector3.up,Forward).normalized;
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(var bank in range.banks){float x=Vector3.Dot(bank.mat_position-range.origin,Right);min=Mathf.Min(min,x);max=Mathf.Max(max,x);}
            HalfWidth=(max-min)*.5f+1.5f;
            if(HalfWidth<d.coaches.Length*.65f)throw new ArgumentException("Coaches cannot crowd the enclosure.");
            Origin=range.origin+Right*((min+max)*.5f);Front=-d.rear_setback_m;Back=Front-d.depth_m;Top=d.raised_m;
            if(ground!=null)for(float x=-HalfWidth;x<=HalfWidth+.01f;x+=1)
                for(float z=Back;z<=Front+.01f;z+=.4f)
                {float y=ground(World(x,0,z));if(!float.IsFinite(y))throw new ArgumentException("Finite coach platform ground required.");Top=Mathf.Max(Top,y-Origin.y+.08f);}
        }
        public Vector3 CoachPosition(int index,int count)=>World(Mathf.Lerp(-HalfWidth+2,HalfWidth-2,index/(float)(count-1)),Top,Front-1.05f);
        public float DistanceToFootprint(Vector3 point)
        {
            var p=Local(point);float x=Mathf.Max(0,Mathf.Abs(p.x)-HalfWidth),z=Mathf.Max(0,Mathf.Max(Back-p.z,p.z-Front));
            return Mathf.Sqrt(x*x+z*z);
        }
        /// <summary>Exact segment/rectangle distance in the horizontal firing frame. Includes endpoints and intersections.</summary>
        public bool ClearsCourses(ProductionRouteCorridor[] corridors,float margin)
        {
            foreach(var path in corridors)for(int i=1;i<path.knots.Length;i++)
            {
                var a=Local(path.knots[i-1].Position);var b=Local(path.knots[i].Position);
                float distance=Mathf.Min(LocalDistance(a),LocalDistance(b));
                var corners=new[]{new Vector3(-HalfWidth,0,Back),new Vector3(HalfWidth,0,Back),new Vector3(HalfWidth,0,Front),new Vector3(-HalfWidth,0,Front)};
                foreach(var corner in corners)distance=Mathf.Min(distance,SegmentDistance(corner,a,b));
                // A segment may pass through the rectangle with both endpoints outside.
                for(int c=0;c<4;c++)if(Crosses(a,b,corners[c],corners[(c+1)%4]))distance=0;
                if(distance<Mathf.Max(path.widths_m[i-1],path.widths_m[i])*.5f+margin)return false;
            }
            return true;
        }
        private float LocalDistance(Vector3 p){float x=Mathf.Max(0,Mathf.Abs(p.x)-HalfWidth),z=Mathf.Max(0,Mathf.Max(Back-p.z,p.z-Front));return Mathf.Sqrt(x*x+z*z);}
        private static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var v=b-a;float t=v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude):0;return Vector3.Distance(p,a+v*t);}
        private static bool Crosses(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            float Cross(Vector3 u,Vector3 v)=>u.x*v.z-u.z*v.x;
            var v=b-a;var w=d-c;float det=Cross(v,w);if(Mathf.Abs(det)<1e-8f)return false;
            float t=Cross(c-a,w)/det,u=Cross(c-a,v)/det;return t>=0&&t<=1&&u>=0&&u<=1;
        }
    }
}
