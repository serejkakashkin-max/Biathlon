#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class ProductionVenueAuthoring
    {
        public const string Fixture="Biathlon/Venues/venue_pipeline_fixture_v1";
        public static string ReadResource(string id)
        {VenueManifest.ResourceId(id);var text=Resources.Load<TextAsset>(id);if(text==null)throw new FileNotFoundException("Venue text resource missing: "+id);return text.text;}
        [MenuItem("BIATHLON/Venues/Generate Pipeline Fixture")]
        public static void GenerateFixture()=>Generate(Fixture);
        public static string Generate(string resourceId)
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ReadResource(resourceId)),ReadResource);var v=plan.Manifest;
            // Validate every dependency BEFORE any output mutation.
            var assets=new GameObject[v.AssetCount];
            for(int i=0;i<assets.Length;i++){assets[i]=Resources.Load<GameObject>(v.Asset(i).Resource);if(assets[i]==null)throw new FileNotFoundException("Venue prefab missing: "+v.Asset(i).Resource);}
            string folder="Assets/Generated/Venues/"+v.Id;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var root=new GameObject("Venue authoring — "+v.Id);
                root.AddComponent<VenueAuthoringMarker>().Configure(v.Id,"venue",resourceId);
                for(int i=0;i<plan.ZoneCount;i++)
                {
                    var zone=plan.Zone(i);var p=plan.Visual.Get(zone.LoopId).Sample((zone.Start+zone.End)*.5);
                    Marker(zone.LoopId+"/"+zone.SegmentId,"semantic_zone",zone.Kind.ToString(),Position(p),(float)(zone.End-zone.Start));
                }
                for(int i=0;i<v.CameraCount;i++)
                {
                    var c=v.Camera(i);Vector3 at=Position(plan.At(c.Anchor));var go=Marker(c.Id,"camera",c.Anchor.LoopId,at+Vector(c.Offset));
                    go.transform.LookAt(at+Vector(c.LookOffset));var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=(float)c.FieldOfView;
                }
                for(int i=0;i<v.AmbienceCount;i++){var a=v.Ambience(i);Marker(a.Id,"ambience",a.EventId,Position(plan.At(a.Anchor)),(float)a.Radius);}
                for(int i=0;i<v.AssetCount;i++)
                {
                    var a=v.Asset(i);var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[i],root.transform);
                    go.transform.position=Position(plan.At(a.Anchor))+Vector(a.Offset);go.transform.rotation=Quaternion.Euler(0,(float)a.Yaw,0);
                    go.AddComponent<VenueAuthoringMarker>().Configure(a.Id,"asset",a.Resource);
                }
                Marker("range_entry","range_entry",v.Range.Entry.LoopId,Position(plan.At(v.Range.Entry)));
                Marker("range_exit","range_exit",v.Range.Exit.LoopId,Position(plan.At(v.Range.Exit)));
                for(int i=0;i<plan.Lanes.Count;i++)
                {
                    string loop=plan.Lanes.GetLane(i);double stop=plan.Track.Range.FiringLine.Distance.Meters-plan.Lanes.Entry.Meters;
                    Marker("firing_lane_"+(i+1),"firing_lane",loop,Position(plan.Visual.Get(loop).Sample(stop)));
                }
                string path=folder+"/Authoring.prefab";
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
                if(!success||saved==null)throw new IOException("Venue prefab could not be saved: "+path);
                File.WriteAllText(folder+"/manifest.json",v.ToJson());AssetDatabase.Refresh();AssetDatabase.SaveAssets();return path;
                GameObject Marker(string id,string kind,string reference,Vector3 position,float extent=0)
                {var go=new GameObject(id);go.transform.SetParent(root.transform,false);go.transform.position=position;go.AddComponent<VenueAuthoringMarker>().Configure(id,kind,reference,extent);return go;}
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
        private static Vector3 Position(TrackVisualPoint p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
        private static Vector3 Vector(VenueVector p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
    }
}
#endif
