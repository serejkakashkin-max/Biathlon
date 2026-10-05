using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Original ground chevrons read the selected physical race path. No route,
    /// reservation, movement, or hit authority lives in this presentation component.</summary>
    public sealed class PlayerRouteGuidancePresentation:MonoBehaviour
    {
        private ProductionVenueHost host;
        private RangeGuidanceDefinition definition;
        private GameObject[] matArrows,forkArrows;
        private Material material;
        private Mesh mesh;
        private TextMesh label;
        public int MatArrowCount {get;private set;}
        public int ForkArrowCount {get;private set;}
        public int GuidedLaneOrdinal {get;private set;}
        public long GuidedBranchRoute {get;private set;}=-1;
        public string GuidedNextLoopId {get;private set;}
        public bool MatPulseActive {get;private set;}
        public void Initialize(ProductionVenueHost source,RangeGuidanceDefinition settings)
        {
            if(host!=null||source==null||settings==null)throw new ArgumentException("Fresh initialized guidance required.");
            host=source;definition=settings;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name="Original cyan race guidance";material.SetColor("_BaseColor",new Color(.03f,.85f,1f,1));
            material.SetFloat("_Cull",(float)CullMode.Off);
            mesh=ChevronMesh();matArrows=CreateArrows("Assigned mat chevron");forkArrows=CreateArrows("Selected branch chevron");
            var text=new GameObject("Assigned mat label");text.transform.SetParent(transform,false);
            label=text.AddComponent<TextMesh>();label.anchor=TextAnchor.MiddleCenter;label.characterSize=.04f;label.fontSize=48;
            label.color=new Color(.05f,.85f,1f);text.SetActive(false);
        }
        private GameObject[] CreateArrows(string prefix)
        {
            var result=new GameObject[definition.ChevronCount];
            for(int i=0;i<result.Length;i++)
            {
                var go=new GameObject(prefix);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                go.SetActive(false);result[i]=go;
            }
            return result;
        }
        private static Mesh ChevronMesh()
        {
            // Two broad strips in the local X/Z plane, tip along positive Z.
            var m=new Mesh{name="Original race guidance chevron"};
            m.vertices=new[]{new Vector3(-.6f,0,-.38f),new Vector3(-.6f,0,-.18f),new Vector3(0,0,.35f),new Vector3(0,0,.13f),
                new Vector3(.6f,0,-.38f),new Vector3(.6f,0,-.18f)};
            m.triangles=new[]{0,1,2,0,2,3,3,2,5,3,5,4};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public void Refresh(float unscaledSeconds)
        {
            if(host==null||host.Runner==null)return;
            Hide(matArrows);Hide(forkArrows);label.gameObject.SetActive(false);MatArrowCount=ForkArrowCount=GuidedLaneOrdinal=0;GuidedBranchRoute=-1;GuidedNextLoopId=null;MatPulseActive=false;
            var field=host.Runner.Field;var actor=field.GetAthlete(host.PlayerIndex);var path=actor.RangePath;
            if(actor.HasFinished||path==null||actor.ActiveShooting!=null||actor.RemainingStartApproachNanometers>0)return;
            var phase=actor.CurrentRangeMotion?.Phase;
            if(phase==RangeTraversalPhase.PenaltyLoop)
            {
                var current=field.GetPhysicalPoint(host.PlayerIndex);
                if((current.LoopLengthNanometers-current.LocalNanometers)/(double)SkiState.NanometersPerMeter>definition.ForkShowLead)return;
                bool anotherLoop=actor.CurrentRangeRemainingPenaltyLoops>1;
                GuidedBranchRoute=actor.RouteNanometers;
                for(int i=0;i<forkArrows.Length;i++)
                {
                    long advance=(long)Math.Round((i+1)*definition.ChevronSpacing*SkiState.NanometersPerMeter);
                    var point=anotherLoop?path.PhysicalMap.MapPenalty(advance):path.MapRoute(actor.RouteNanometers+advance);
                    GuidedNextLoopId=point.LoopId;Place(forkArrows[i],point,true);ForkArrowCount++;
                }
                return;
            }
            int lap=field.Route.Sample(new TrackDistance(actor.RouteNanometers/SkiState.NanometersPerMillimeter)).LapIndex;
            long stop=0;bool shootingLap=host.IsShootingLap(lap);
            if(shootingLap)stop=path.FiringRouteNanometers(lap);
            double distance=(stop-actor.RouteNanometers)/(double)SkiState.NanometersPerMeter;
            if(shootingLap&&phase==RangeTraversalPhase.Approach&&distance>0&&distance<=definition.MatShowLead)
            {
                GuidedLaneOrdinal=path.LaneOrdinal;MatPulseActive=distance<=definition.MatAssistLead;
                bool on=!MatPulseActive||Mathf.Sin(unscaledSeconds*(float)(2*Math.PI*definition.FlashHz))>-.15f;
                for(int i=0;i<matArrows.Length;i++)
                {
                    // The final approach has original arrows at3,2,1m before the
                    // actual reserved mat; earlier arrows lie on its paid branch.
                    double before=definition.MatAssistLead*(matArrows.Length-i)/(matArrows.Length+1.0);
                    Place(matArrows[i],path.MapRoute(stop-(long)Math.Round(before*SkiState.NanometersPerMeter)),on,true);
                    if(on)MatArrowCount++;
                }
                var p=host.Venue.Visual.Get(path.MatAnchor.LoopId).Sample(path.MatAnchor.Distance.Meters);
                label.text="КОВРИК "+path.LaneOrdinal;label.transform.position=new Vector3((float)p.X,(float)p.Y+1.2f,(float)p.Z);
                if(host.ChaseCamera!=null)label.transform.rotation=host.ChaseCamera.transform.rotation;label.gameObject.SetActive(true);
            }
            GuidedBranchRoute=RaceRouteGuidance.NextBranch(path,field.Route,actor.RouteNanometers,(long)Math.Round(definition.ForkShowLead*SkiState.NanometersPerMeter));
            if(GuidedBranchRoute>=0)
            {
                long routeEnd=field.Route.Length.Millimeters*SkiState.NanometersPerMillimeter;
                // A mat fork guides the approach up to its firing stop; showing the
                // subsequent departure at the same time would point past the mat.
                if(shootingLap&&stop>actor.RouteNanometers)routeEnd=Math.Min(routeEnd,stop);
                for(int i=0;i<forkArrows.Length;i++)
                {
                    long at=GuidedBranchRoute+(long)Math.Round((i+1)*definition.ChevronSpacing*SkiState.NanometersPerMeter);
                    if(at>=routeEnd)break;var point=path.MapRoute(at);GuidedNextLoopId=point.LoopId;Place(forkArrows[i],point,true);ForkArrowCount++;
                }
            }
        }
        private void Place(GameObject go,PhysicalTrackPoint point,bool visible,bool assignedMat=false)
        {
            var p=host.Venue.Visual.Get(point.LoopId).Sample(point.LocalNanometers/(double)SkiState.NanometersPerMeter);
            float y=(float)p.Y;
            if(host.Environment!=null)y=Mathf.Max(y,host.Environment.SampleGround(new Vector3((float)p.X,y,(float)p.Z)));
            go.transform.position=new Vector3((float)p.X,y+(float)definition.GroundOffset,(float)p.Z);
            go.transform.localScale=UnityEngine.Vector3.one*(assignedMat?.55f:1f);
            go.transform.rotation=Quaternion.LookRotation(new Vector3((float)p.ForwardX,0,(float)p.ForwardZ));go.SetActive(visible);
        }
        private static void Hide(GameObject[] list){if(list!=null)for(int i=0;i<list.Length;i++)list[i].SetActive(false);}
        private void LateUpdate()=>Refresh(Time.unscaledTime);
        private void OnDestroy(){if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);}
    }
}
