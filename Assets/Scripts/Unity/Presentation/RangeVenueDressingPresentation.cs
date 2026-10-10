using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Physical lane signs, observed-wind flags and painted start slots. No race ownership.</summary>
    public sealed class RangeVenueDressingPresentation : MonoBehaviour
    {
        [Serializable] private sealed class Settings
        {
            public int schema_version;public string id;
            public float number_post_height_m,number_board_size_m,number_post_side_m;
            public float wind_flag_height_m,wind_flag_length_m,wind_flag_width_m;
            public float[] wind_flag_fractions;
            public float grid_slot_length_m,grid_slot_width_m,grid_line_width_m,surface_offset_m;
            public Color accent_color,number_color;
        }
        private Settings settings;
        private RacePresentationFeed feed;
        private Vector3 forward,right;
        private Transform[] flags;
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private Material white,dark,accent;
        public int NumberedLaneCount {get;private set;}
        public int WindFlagCount=>flags?.Length??0;
        public int StartSlotCount {get;private set;}
        public Vector3 StartSlot(int index)=>startSlots[index];
        private Vector3[] startSlots;
        private long observedTick=-1;
        public void Initialize(VenueAuthoringPlan venue,PackSprintFieldSession field,ManualScopedWorldRoster world,RacePresentationFeed source)
        {
            if(settings!=null||venue==null||field==null||source==null)throw new ArgumentException("Fresh range dressing and a bound venue required.");
            settings=JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("Biathlon/range_venue_presentation_v1").text);
            if(settings.schema_version!=1||string.IsNullOrWhiteSpace(settings.id)||settings.wind_flag_fractions==null||settings.wind_flag_fractions.Length==0||
                !(settings.number_post_height_m>0&&settings.wind_flag_height_m>0&&settings.grid_slot_length_m>0&&settings.grid_line_width_m>0))throw new ArgumentException("Invalid range dressing data.");
            feed=source;forward=new Vector3((float)venue.Manifest.Range.Forward.X,0,(float)venue.Manifest.Range.Forward.Z);right=new Vector3(forward.z,0,-forward.x);
            var template=Resources.Load<Material>("Biathlon/Stadium/white");
            white=new Material(template);dark=new Material(template);accent=new Material(template);owned.Add(white);owned.Add(dark);owned.Add(accent);
            white.SetColor("_BaseColor",Color.white);dark.SetColor("_BaseColor",settings.number_color);accent.SetColor("_BaseColor",settings.accent_color);
            var geometry=TargetGeometryDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/target_geometry_v1"));
            flags=new Transform[venue.LaneCount*settings.wind_flag_fractions.Length];int count=0;
            for(int lane=0;lane<venue.LaneCount;lane++)
            {
                var p=venue.MatPoint(lane);var mat=new Vector3((float)p.X,(float)p.Y,(float)p.Z);
                var post=mat+right*settings.number_post_side_m;
                Pole(post,settings.number_post_height_m);
                var board=Cube("Lane number panel",post+Vector3.up*(settings.number_post_height_m-.18f),new Vector3(settings.number_board_size_m,settings.number_board_size_m,.025f),dark);
                var rotation=Quaternion.LookRotation(forward,Vector3.up);board.rotation=rotation;
                Text((lane+1).ToString(),board.position-forward*.02f,rotation,settings.number_board_size_m*.65f,Color.white);
                NumberedLaneCount++;
                var target=mat+forward*(float)(venue.FiringDatumDepth+geometry.DistanceMeters)+Vector3.up*2;
                var targetNumber=Cube("Target lane number",target,new Vector3(.7f,.45f,.035f),dark);targetNumber.rotation=rotation;
                Text((lane+1).ToString(),target-forward*.025f,rotation,.38f,Color.white);
                foreach(float fraction in settings.wind_flag_fractions)
                {
                    if(!(fraction>0&&fraction<1))throw new ArgumentException("Wind flags must lie between the mat and target.");
                    var origin=mat+forward*(float)(venue.FiringDatumDepth+geometry.DistanceMeters*fraction)+right*settings.number_post_side_m;
                    Pole(origin,settings.wind_flag_height_m);
                    var pivot=new GameObject("Observed wind flag");pivot.transform.SetParent(transform,false);pivot.transform.position=origin+Vector3.up*settings.wind_flag_height_m;
                    var fabric=Cube("Wind fabric",pivot.transform.position,new Vector3(settings.wind_flag_width_m,settings.wind_flag_length_m,.008f),accent);
                    fabric.SetParent(pivot.transform,false);fabric.localPosition=Vector3.down*(settings.wind_flag_length_m*.5f);
                    flags[count++]=pivot.transform;
                }
            }
            StartSlotCount=field.StartGrid?.Count??1;startSlots=new Vector3[StartSlotCount];
            for(int i=0;i<StartSlotCount;i++)
            {
                var p=field.GetPhysicalPoint(i);var sample=venue.Visual.Get(p.LoopId).Sample(p.LocalNanometers/(double)SkiState.NanometersPerMeter);
                var f=new Vector3((float)sample.ForwardX,0,(float)sample.ForwardZ);var r=new Vector3(f.z,0,-f.x);
                var position=world!=null?new Vector3((float)world.GetActor(i).Pose.X,(float)sample.Y,(float)world.GetActor(i).Pose.Z):
                    new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z)+r*(float)field.GetLateral(i);
                startSlots[i]=position;
                var rotation=Quaternion.LookRotation(f,Vector3.up);
                float half=settings.grid_slot_width_m*.5f;
                var back=position-f*settings.grid_slot_length_m*.5f+Vector3.up*settings.surface_offset_m;
                Cube("Start slot back",back,new Vector3(settings.grid_slot_width_m,settings.surface_offset_m,settings.grid_line_width_m),accent).rotation=rotation;
                for(int side=-1;side<=1;side+=2)Cube("Start slot edge",position+r*(half*side)+Vector3.up*settings.surface_offset_m,
                    new Vector3(settings.grid_line_width_m,settings.surface_offset_m,settings.grid_slot_length_m),white).rotation=rotation;
                Text((i+1).ToString(),back-f*.3f,Quaternion.Euler(90,rotation.eulerAngles.y,0),.3f,settings.number_color);
            }
            Refresh();
        }
        private void Pole(Vector3 foot,float height)=>Cube("Range marker pole",foot+Vector3.up*(height*.5f),new Vector3(.025f,height,.025f),white);
        private Transform Cube(string label,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;go.transform.SetParent(transform,false);go.transform.position=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;var collider=go.GetComponent<Collider>();collider.enabled=false;Release(collider);return go.transform;
        }
        private void Text(string value,Vector3 position,Quaternion rotation,float size,Color color)
        {
            var go=new GameObject("Range number");go.transform.SetParent(transform,false);go.transform.SetPositionAndRotation(position,rotation);
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=size/6;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
        }
        public void Refresh()
        {
            var snapshot=feed?.Current;if(snapshot==null||snapshot.Tick==observedTick)return;observedTick=snapshot.Tick;
            // Use the real shooting input, not the decorative forest wind preset.
            var wind=Vector3.zero;
            for(int i=0;i<snapshot.Count;i++){var a=snapshot.Get(i);if(a.ShootingActive){wind=new Vector3((float)a.ShootingWind.EastMetersPerSecond,0,(float)a.ShootingWind.NorthMetersPerSecond);break;}}
            float speed=wind.magnitude;var direction=speed>0?wind/speed:forward;
            var rotation=Quaternion.LookRotation(-direction,Vector3.up)*Quaternion.Euler(Mathf.Lerp(0,82,Mathf.Clamp01(speed/6)),0,0);
            foreach(var flag in flags)flag.rotation=rotation;
        }
        private void LateUpdate()=>Refresh();
        private void OnDestroy(){foreach(var item in owned)Release(item);}
        private static void Release(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
