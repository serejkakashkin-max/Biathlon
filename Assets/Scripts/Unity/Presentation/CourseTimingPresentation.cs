using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Static rectangular timing portals and painted snow lines at authoritative split anchors.</summary>
    public sealed class CourseTimingPresentation : MonoBehaviour
    {
        [Serializable] private sealed class Settings
        {
            public int schema_version;
            public string id;
            public string reused_stadium_checkpoint_id;
            public float pass_label_hold_m;
            public float height_m,support_clearance_m,post_width_m,post_depth_m;
            public float foot_width_m,foot_depth_m,foot_height_m,foot_embedding_m;
            public float header_height_m,header_depth_m,snow_cap_m,stripe_width_m,surface_offset_m,label_character_size_m;
            public Color frame_color,foot_color,stripe_color;
            public static Settings Load()
            {
                var value=JsonUtility.FromJson<Settings>(ProductionVenueHost.ReadResource("Biathlon/UI/course_timing_v1"));
                if(value==null||value.schema_version!=1||string.IsNullOrWhiteSpace(value.id))throw new ArgumentException("Invalid course timing portal definition.");
                foreach(float number in new[]{value.pass_label_hold_m,value.height_m,value.support_clearance_m,value.post_width_m,value.post_depth_m,
                    value.foot_width_m,value.foot_depth_m,value.foot_height_m,value.foot_embedding_m,value.header_height_m,
                    value.header_depth_m,value.snow_cap_m,value.stripe_width_m,value.surface_offset_m,value.label_character_size_m})
                    if(!float.IsFinite(number)||number<=0)throw new ArgumentException("Positive course timing portal dimensions required.");
                if(value.support_clearance_m<value.foot_width_m*.5f||value.height_m-value.header_height_m*.5f<3)
                    throw new ArgumentException("Timing supports and header must clear the ski corridor.");
                return value;
            }
        }
        private sealed class Station
        {
            public Mesh Mesh;
            public Transform Frame;
            public float HalfWidth,LeftGround,RightGround;
            public string Id;
            public bool UsesExistingPortal;
            public TextMesh Label;public double[] RouteMeters;public string[] Labels;public int Shown=-1;
        }
        [Serializable] private sealed class ReusedLabel {public float height_m,face_offset_m,character_size_m;}
        private RacePresentationFeed boundFeed;private int boundPlayer;private TrackDefinition boundTrack;
        private Settings settings;
        private Station[] stations;
        private Material[] materials;
        public int TimingLines=>stations?.Length??0;
        public Mesh GateMesh(int index)=>stations[index].Mesh;
        public Transform GateTransform(int index)=>stations[index].Frame;
        public string CheckpointId(int index)=>stations[index].Id;
        public float CorridorHalfWidth(int index)=>stations[index].HalfWidth;
        public bool UsesExistingStadiumPortal(int index)=>stations[index].UsesExistingPortal;
        public string DistanceLabel(int index)=>stations[index].Label.text;

        public void Initialize(VenueAuthoringPlan venue)
        {
            if(venue==null)throw new ArgumentNullException(nameof(venue));
            if(stations!=null)throw new InvalidOperationException("Initialize timing portals once.");
            settings=Settings.Load();boundTrack=venue.Track;
            var template=Resources.Load<Material>("Biathlon/Stadium/white");
            if(template==null)throw new InvalidOperationException("Timing portal material is unavailable.");
            materials=new[]{new Material(template),new Material(template),new Material(template)};
            materials[0].SetColor("_BaseColor",settings.frame_color);materials[1].SetColor("_BaseColor",settings.foot_color);materials[2].SetColor("_BaseColor",settings.stripe_color);
            stations=new Station[venue.Track.CheckpointCount];
            for(int i=0;i<stations.Length;i++)
            {
                var marker=venue.Track.GetCheckpoint(i);var point=venue.Visual.Get(marker.Anchor.LoopId).Sample(marker.Anchor.Distance.Meters);
                var root=new GameObject("Timing portal "+marker.Id);root.transform.SetParent(transform,false);
                root.transform.SetPositionAndRotation(new Vector3((float)point.X,(float)point.Y,(float)point.Z),
                    Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ),Vector3.up));
                var station=new Station{Frame=root.transform,Id=marker.Id,Mesh=new Mesh{name="Timing portal and snow stripe "+marker.Id},
                    UsesExistingPortal=marker.Id==settings.reused_stadium_checkpoint_id,
                    HalfWidth=(float)new TrackSampler(venue.Track.GetLoop(marker.Anchor.LoopId)).Sample(marker.Anchor.Distance).WidthMeters*.5f};
                if(station.UsesExistingPortal)
                {
                    if(venue.StartApproach==null||marker.Anchor.LoopId!=venue.StartApproach.RouteEntryLoopId||
                        marker.Anchor.Distance.Millimeters!=venue.Track.GetLoop(marker.Anchor.LoopId).Length.Millimeters)
                        throw new ArgumentException("The retained stadium timing portal must bind its main loop-end checkpoint.");
                    var gate=venue.Visual.Get(marker.Anchor.LoopId).Sample(0);
                    if(Math.Abs(point.X-gate.X)>.001||Math.Abs(point.Y-gate.Y)>.001||Math.Abs(point.Z-gate.Z)>.001)
                        throw new ArgumentException("The loop-end checkpoint must coincide with the retained stadium timing portal.");
                }
                stations[i]=station;Build(station);
                root.AddComponent<MeshFilter>().sharedMesh=station.Mesh;root.AddComponent<MeshRenderer>().sharedMaterials=materials;
                var label=new GameObject("Intermediate distance",typeof(TextMesh));label.transform.SetParent(root.transform,false);
                label.transform.localPosition=new Vector3(0,settings.height_m,-settings.header_depth_m*.5f-.012f);
                var text=label.GetComponent<TextMesh>();station.Label=text;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text="";
                text.fontSize=64;text.characterSize=settings.label_character_size_m;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;
                if(station.UsesExistingPortal){var original=JsonUtility.FromJson<ReusedLabel>(ProductionVenueHost.ReadResource("Biathlon/stadium_portal_labels_v1"));label.transform.localPosition=new Vector3(0,original.height_m,-original.face_offset_m);text.characterSize=original.character_size_m;}
                var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                label.AddComponent<VenueSignText>();
            }
        }
        public void Bind(CompetitionProfile profile,Biathlon.Simulation.RaceRoute route,RacePresentationFeed source,int playerIndex,Biathlon.Presentation.RaceDistanceView distance)
        {
            if(stations==null||profile==null||route==null||source==null||distance==null)throw new ArgumentException("Initialized timing and actual race binding required.");
            var layout=new CheckpointLayout(boundTrack,route);boundFeed=source;boundPlayer=playerIndex;
            foreach(var station in stations)
            {
                var positions=new List<double>();var labels=new List<string>();
                for(int i=0;i<layout.Count;i++)if(layout.Get(i).CheckpointId==station.Id)
                {double meters=layout.Get(i).RouteNanometers/(double)SkiState.NanometersPerMeter;positions.Add(meters);labels.Add(distance.Kilometers(meters,System.Globalization.CultureInfo.InvariantCulture)+" KM");}
                station.RouteMeters=positions.ToArray();station.Labels=labels.ToArray();station.Shown=int.MinValue;
            }
            RefreshDistance(0);
        }
        public void RefreshDistance(double routeMeters)
        {
            if(!double.IsFinite(routeMeters))throw new ArgumentOutOfRangeException(nameof(routeMeters));
            foreach(var station in stations)
            {
                if(station.Labels==null||station.Labels.Length==0)continue;int next=0;
                // Keep the passed value until the gantry is behind the follow camera.
                // The same marker can belong to a preceding lap after the range exit.
                while(next+1<station.Labels.Length&&routeMeters>station.RouteMeters[next]+settings.pass_label_hold_m)next++;
                // This physical gantry also spans the launch corridor. Do not show a
                // future lap-end split while the athlete is starting below it.
                // Other course signs still announce the distance on approach.
                if(station.UsesExistingPortal&&routeMeters<station.RouteMeters[0])next=-1;
                if(next==station.Shown)continue;station.Shown=next;station.Label.text=next<0?"":station.Labels[next];
            }
        }
        private void LateUpdate(){if(boundFeed?.Current!=null)RefreshDistance(boundFeed.Current.Get(boundPlayer).RouteMeters);}
        /// <summary>One setup pass after native terrain exists. No per-frame geometry or ground sampling.</summary>
        public void GroundSupports(ProductionVenueEnvironment environment)
        {
            if(stations==null||environment==null||!environment.Initialized)throw new ArgumentException("Initialized portals and native snow required.");
            foreach(var station in stations)
            {
                if(station.UsesExistingPortal)continue;
                float half=station.HalfWidth+settings.support_clearance_m;
                station.LeftGround=environment.SampleGround(station.Frame.TransformPoint(new Vector3(-half,0,0)))-station.Frame.position.y;
                station.RightGround=environment.SampleGround(station.Frame.TransformPoint(new Vector3(half,0,0)))-station.Frame.position.y;
                Build(station);
            }
        }
        private void Build(Station station)
        {
            var points=new List<Vector3>(172);var faces=new[]{new List<int>(108),new List<int>(108),new List<int>(6)};
            float half=station.HalfWidth+settings.support_clearance_m;
            if(!station.UsesExistingPortal)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    float ground=(side<0?station.LeftGround:station.RightGround)-settings.foot_embedding_m;
                    Box(new Vector3(side*half,ground+settings.foot_height_m*.5f,0),new Vector3(settings.foot_width_m,settings.foot_height_m,settings.foot_depth_m),1);
                    float bottom=ground+settings.foot_height_m,top=settings.height_m;
                    if(top<=bottom)throw new ArgumentException("Timing support snow exceeds the header clearance.");
                    Box(new Vector3(side*half,(top+bottom)*.5f,0),new Vector3(settings.post_width_m,top-bottom,settings.post_depth_m),0);
                }
                Box(new Vector3(0,settings.height_m,0),new Vector3(half*2+settings.post_width_m,settings.header_height_m,settings.header_depth_m),0);
                Box(new Vector3(0,settings.height_m+settings.header_height_m*.5f+settings.snow_cap_m*.5f,0),
                    new Vector3(half*2+settings.post_width_m,settings.snow_cap_m,settings.header_depth_m+.08f),1);
            }
            Quad(new Vector3(0,settings.surface_offset_m,0),Vector3.right*station.HalfWidth,Vector3.back*settings.stripe_width_m*.5f,2);
            station.Mesh.Clear();station.Mesh.SetVertices(points);station.Mesh.subMeshCount=faces.Length;
            for(int submesh=0;submesh<faces.Length;submesh++)station.Mesh.SetTriangles(faces[submesh],submesh);
            station.Mesh.RecalculateNormals();station.Mesh.RecalculateBounds();
            void Quad(Vector3 center,Vector3 u,Vector3 v,int submesh)
            {
                int index=points.Count;points.Add(center-u-v);points.Add(center+u-v);points.Add(center-u+v);points.Add(center+u+v);
                faces[submesh].AddRange(new[]{index,index+1,index+2,index+2,index+1,index+3});
            }
            void Box(Vector3 center,Vector3 size,int submesh)
            {
                var x=Vector3.right*size.x*.5f;var y=Vector3.up*size.y*.5f;var z=Vector3.forward*size.z*.5f;
                Quad(center+x,-z,y,submesh);Quad(center-x,z,y,submesh);Quad(center+y,x,-z,submesh);
                Quad(center-y,x,z,submesh);Quad(center+z,x,y,submesh);Quad(center-z,-x,y,submesh);
            }
        }
        private void OnDestroy()
        {
            if(stations!=null)foreach(var station in stations)if(station!=null){Release(station.Frame.gameObject);Release(station.Mesh);}
            if(materials!=null)foreach(var material in materials)Release(material);
        }
        private static void Release(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
