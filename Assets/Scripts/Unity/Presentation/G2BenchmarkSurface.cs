using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Authoring/decoration height only. The ski corridor retains the M12 contact plane.</summary>
    public static class G2BenchmarkSurface
    {
        [Serializable] public sealed class Definition
        {
            public int schema_version;public string id;
            public float bank_height_m,bank_center_m,bank_width_m,offroute_relief_m,relief_start_m,relief_full_m;
            public float snow_detail_m,snow_detail_weight,snow_normal_strength;
        }
        private static readonly Vector3[] Route={new Vector3(0,0,-150),new Vector3(0,0,-7),new Vector3(-23,0,-7),new Vector3(-23,0,-80)};
        private static Definition definition;
        public static Vector3[] CopyRoute()=>(Vector3[])Route.Clone();
        public static Definition Load()
        {
            var d=JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("Biathlon/g2_benchmark_surface_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id))throw new ArgumentException("Invalid benchmark surface schema.");
            Check(d.bank_height_m,0,.3f);Check(d.bank_center_m,4,6);Check(d.bank_width_m,.2f,1.5f);Check(d.offroute_relief_m,0,.5f);
            Check(d.relief_start_m,7,20);Check(d.relief_full_m,d.relief_start_m+1,40);Check(d.snow_detail_m,.5f,12);Check(d.snow_detail_weight,0,1);Check(d.snow_normal_strength,0,1);
            return d;
        }
        private static void Check(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid benchmark surface value.");}
        public static float Height(Vector3 p)
        {
            if(!float.IsFinite(p.x)||!float.IsFinite(p.z))throw new ArgumentException("Finite decoration position required.");
            if(definition==null)definition=Load();
            float original=VisualBenchmarkAthletes.Height(p),distance=VegetationDefinition.DistanceToRoute(p,Route);
            // Keep the complete seven-metre ski corridor and the stadium platform unchanged.
            // One-metre mesh cells need a diagonal guard outside the 3.5m corridor:
            // otherwise triangles interpolate a raised outer vertex back under the ski lane.
            if(distance<=5f||p.z>=-10)return original;
            float entrance=Mathf.SmoothStep(0,1,Mathf.Clamp01((-10-p.z)/10));
            float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-5f)/.5f));
            float ridge=(distance-definition.bank_center_m)/definition.bank_width_m;
            float bank=definition.bank_height_m*Mathf.Exp(-ridge*ridge)*edge;
            float outer=Mathf.SmoothStep(0,1,Mathf.InverseLerp(definition.relief_start_m,definition.relief_full_m,distance));
            float relief=definition.offroute_relief_m*Mathf.Pow(Mathf.Sin(p.x*.071f)*Mathf.Cos(p.z*.043f),2)*outer;
            return original+entrance*(bank+relief);
        }
    }
}
