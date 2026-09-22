using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit visual bindings for the G2 event fixture; no race data or frame loop.</summary>
    public sealed class G2EventDressing : MonoBehaviour
    {
        public Transform Screen;
        public Transform[] Banners;
        public MeshFilter[] Architecture;
        [Serializable] public sealed class Placement {public string id;public Vector3 position;public float yaw;}
        [Serializable] public sealed class Definition
        {
            public int schema_version;public string id;public Placement screen;public Placement[] banners;
            public float concrete_tile_m,iron_tile_m;
            public void Validate()
            {
                if(schema_version!=1||string.IsNullOrWhiteSpace(id)||screen==null||banners==null||banners.Length!=4)throw new ArgumentException("Invalid event definition.");
                if(!float.IsFinite(concrete_tile_m)||concrete_tile_m<1||concrete_tile_m>5||!float.IsFinite(iron_tile_m)||iron_tile_m<1||iron_tile_m>5)throw new ArgumentException("Invalid material scale.");
                var ids=new System.Collections.Generic.HashSet<string>();Check(screen,4);
                foreach(var p in banners)Check(p,1.1f);
                void Check(Placement p,float radius)
                {
                    if(p==null||string.IsNullOrWhiteSpace(p.id)||!ids.Add(p.id)||!float.IsFinite(p.position.sqrMagnitude)||!float.IsFinite(p.yaw)||Mathf.Abs(p.yaw)>360||p.position.y!=0||p.position.sqrMagnitude>10000)throw new ArgumentException("Invalid event placement.");
                    if(VegetationDefinition.DistanceToRoute(p.position,G2BenchmarkSurface.CopyRoute())<5.5f+radius)throw new ArgumentException("Event prop intrudes on route.");
                    if(p.position.z>-3&&p.position.z<55&&Mathf.Abs(p.position.x)<13+radius)throw new ArgumentException("Event prop intrudes on shooting lanes.");
                }
            }
        }
        public static Definition Load()
        {
            var text=Resources.Load<TextAsset>("Biathlon/g2_event_v1");if(text==null)throw new InvalidOperationException("Missing event definition.");
            var d=JsonUtility.FromJson<Definition>(text.text);if(d==null)throw new ArgumentException("Invalid event definition.");d.Validate();return d;
        }
    }
}
