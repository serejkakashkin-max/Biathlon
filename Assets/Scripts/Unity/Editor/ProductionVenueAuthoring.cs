#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            return Generate(new VenueAuthoringPlan(VenueManifest.FromJson(ReadResource(resourceId)),ReadResource),resourceId);
        }
        public static string Generate(VenueAuthoringPlan plan,string resourceId)
        {
            if(plan==null)throw new ArgumentNullException(nameof(plan));VenueManifest.ResourceId(resourceId);var v=plan.Manifest;
            var rangeRotation=RangeRotation(plan);
            // Validate every dependency BEFORE any output mutation.
            var assets=new GameObject[v.AssetCount];
            for(int i=0;i<assets.Length;i++){assets[i]=Resources.Load<GameObject>(v.Asset(i).Resource);if(assets[i]==null)throw new FileNotFoundException("Venue prefab missing: "+v.Asset(i).Resource);}
            string folder="Assets/Generated/Venues/"+v.Id;
            var selection=Selection.objects;var activeSelection=Selection.activeObject;var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                var root=Create("Venue authoring — "+v.Id);
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
                    var a=v.Asset(i);var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[i],scene);go.transform.SetParent(root.transform,false);
                    go.transform.position=Position(plan.At(a.Anchor))+Vector(a.Offset);go.transform.rotation=Quaternion.Euler(0,(float)a.Yaw,0);
                    go.AddComponent<VenueAuthoringMarker>().Configure(a.Id,"asset",a.Resource);
                }
                Marker("range_entry","range_entry",v.Range.Entry.LoopId,Position(plan.At(v.Range.Entry)));
                Marker("range_exit","range_exit",v.Range.Exit.LoopId,Position(plan.At(v.Range.Exit)));
                for(int i=0;i<plan.LaneCount;i++)
                {
                    var marker=Marker("firing_lane_"+(i+1),"firing_lane",plan.LaneId(i),Position(plan.MatPoint(i)));
                    // Shooting direction is authored independently of an incoming path's tangent.
                    if(plan.UsesPhysicalPaths)marker.transform.rotation=rangeRotation;
                }
                string path=folder+"/Authoring.prefab";
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
                if(!success||saved==null)throw new IOException("Venue prefab could not be saved: "+path);
                File.WriteAllText(folder+"/manifest.json",v.ToJson());AssetDatabase.Refresh();AssetDatabase.SaveAssets();return path;
                GameObject Create(string name)
                {var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
                GameObject Marker(string id,string kind,string reference,Vector3 position,float extent=0)
                {var go=Create(id);go.transform.SetParent(root.transform,false);go.transform.position=position;go.AddComponent<VenueAuthoringMarker>().Configure(id,kind,reference,extent);return go;}
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                // Neither generation nor asset import may take ownership of the caller's selection.
                if(!SameSelection(Selection.objects,selection))Selection.objects=selection;
                if(Selection.activeObject!=activeSelection)Selection.activeObject=activeSelection;
            }
        }
        private static Quaternion RangeRotation(VenueAuthoringPlan plan)
        {
            if(!plan.UsesPhysicalPaths)return Quaternion.identity;
            var forward=plan.Manifest.Range.Forward;
            if(forward==null||!double.IsFinite(forward.X)||!double.IsFinite(forward.Y)||!double.IsFinite(forward.Z)||
                Math.Abs(forward.Y)>1e-9||Math.Abs(forward.X*forward.X+forward.Z*forward.Z-1)>1e-9)
                throw new ArgumentException("A finite horizontal unit shooting direction is required.",nameof(plan));
            return Quaternion.LookRotation(new Vector3((float)forward.X,0,(float)forward.Z).normalized);
        }
        private static bool SameSelection(UnityEngine.Object[] a,UnityEngine.Object[] b)
        {if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
        private static Vector3 Position(TrackVisualPoint p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
        private static Vector3 Vector(VenueVector p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
    }
}
#endif
