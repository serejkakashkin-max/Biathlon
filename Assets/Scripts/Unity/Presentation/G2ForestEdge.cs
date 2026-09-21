using System;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Small authored groves connecting the benchmark trail and distant forest. Visual only.</summary>
    [Serializable] public sealed class G2ForestEdge
    {
        [Serializable] public sealed class Tree {public string id;public float x,z,scale,yaw;}
        public int schema_version;public string id;public float camera_clearance_m;public Tree[] trees;
        public static G2ForestEdge Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/g2_forest_edge_v1").text);
        public static G2ForestEdge FromJson(string json)
        {
            var result=JsonUtility.FromJson<G2ForestEdge>(json);if(result==null)throw new ArgumentException("Missing forest edge.");result.Validate();return result;
        }
        public void Validate()
        {
            if(schema_version!=1||string.IsNullOrWhiteSpace(id)||trees==null||trees.Length<1||trees.Length>32)throw new ArgumentException("Invalid forest edge schema/capacity.");
            Bound(camera_clearance_m,10,25);
            var ids=new HashSet<string>();var route=G2BenchmarkSurface.CopyRoute();var vegetation=VegetationDefinition.Load();var shots=BroadcastShotDefinition.Load();
            foreach(var t in trees)
            {
                if(t==null||string.IsNullOrWhiteSpace(t.id)||!ids.Add(t.id))throw new ArgumentException("Unique tree IDs required.");
                Bound(t.x,-45,45);Bound(t.z,-140,-28);Bound(t.scale,.55f,1.3f);Bound(t.yaw,0,360);
                float clearance=vegetation.route_half_width_m+vegetation.canopy_radius_m*t.scale+vegetation.wind_amplitude_m;
                if(VegetationDefinition.DistanceToRoute(new Vector3(t.x,0,t.z),route)<clearance)throw new ArgumentException("Tree canopy intrudes on benchmark route.");
                var center=new Vector3(t.x,0,t.z);center.y=G2BenchmarkSurface.Height(center)+4*t.scale;
                foreach(var shot in shots.shots)if(shot.mode=="fixed")
                {
                    var direction=(shot.look_at-shot.position).normalized;float depth=Mathf.Clamp(Vector3.Dot(center-shot.position,direction),0,camera_clearance_m);
                    if(Vector3.Distance(center,shot.position+direction*depth)<3*t.scale+vegetation.wind_amplitude_m)throw new ArgumentException("Tree canopy blocks a fixed review camera.");
                }
            }
            for(int i=0;i<trees.Length;i++)for(int j=i+1;j<trees.Length;j++)
                if(new Vector2(trees[i].x-trees[j].x,trees[i].z-trees[j].z).sqrMagnitude<4)throw new ArgumentException("Forest trunks overlap.");
        }
        private static void Bound(float value,float min,float max){if(!float.IsFinite(value)||value<min||value>max)throw new ArgumentException("Invalid forest edge value.");}
        public VegetationInstance[] Place()
        {
            Validate();var result=new VegetationInstance[trees.Length];
            for(int i=0;i<trees.Length;i++)
            {
                var t=trees[i];uint key=2166136261;foreach(char c in t.id)key=unchecked((key^c)*16777619);
                result[i]=new VegetationInstance(new Vector3(t.x,0,t.z),t.scale,t.yaw,(VegetationDefinition.Mix(key)&0xffffff)/16777216f);
            }
            return result;
        }
    }
}
