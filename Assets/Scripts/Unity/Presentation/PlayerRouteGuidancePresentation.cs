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
        private RangeGuidanceTrailDefinition visualSettings;
        private GameObject[] matArrows,forkArrows;
        private Material material;
        private Mesh mesh;
        public int MatArrowCount {get;private set;}
        public int ForkArrowCount {get;private set;}
        public int GuidedLaneOrdinal {get;private set;}
        public long GuidedBranchRoute {get;private set;}=-1;
        public string GuidedNextLoopId {get;private set;}
        public bool MatPulseActive {get;private set;}
        public void Initialize(ProductionVenueHost source,RangeGuidanceDefinition settings)
        {
            if(host!=null||source==null||settings==null)throw new ArgumentException("Fresh initialized guidance required.");
            host=source;definition=settings;visualSettings=RangeGuidanceTrailDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/range_guidance_visual_v1"));
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name="Original cyan race guidance";material.SetColor("_BaseColor",new Color(.03f,.85f,1f,1));
            material.SetFloat("_Cull",(float)CullMode.Off);
            mesh=ChevronMesh();matArrows=CreateArrows("Assigned mat chevron");forkArrows=CreateArrows("Selected branch chevron");
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
            Hide(matArrows);Hide(forkArrows);MatArrowCount=ForkArrowCount=GuidedLaneOrdinal=0;GuidedBranchRoute=-1;GuidedNextLoopId=null;MatPulseActive=false;
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
                    // Early navigation travels in front of the athlete along the selected
                    // paid path. Final capture retains three small, separated mat arrows.
                    int count=MatPulseActive?3:matArrows.Length;
                    if(i>=count)break;
                    long at;
                    if(MatPulseActive)
                    {
                        double before=definition.MatAssistLead*(count-i)/(count+1.0);
                        at=stop-(long)Math.Round(before*SkiState.NanometersPerMeter);
                    }
                    else
                    {
                        at=RangeMatGuidanceGeometry.Station(definition,actor.RouteNanometers,i);
                        if(at>=stop)
                        {
                            if(i>0)break;
                            at=actor.RouteNanometers+(stop-actor.RouteNanometers)/2;
                        }
                    }
                    if(host.Venue.Manifest.Range.MatDefinition!=null)
                        PlaceMatTrail(matArrows[i],path,at,stop,on,MatPulseActive,i,count);
                    else Place(matArrows[i],path.MapRoute(at),on,MatPulseActive);
                    if(on)MatArrowCount++;
                }
            }
            // The selected mat trail already explains this branch. A second path
            // overlay would put the old close-to-mat arrows back over the new trail.
            if(GuidedLaneOrdinal>0)return;
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
        private void PlaceMatTrail(GameObject go,PhysicalRangePathMap path,long at,long stop,bool visible,bool pulse,int index,int count)
        {
            Vector3 Sample(long station)
            {
                var point=RangeMatGuidanceGeometry.Point(host.Venue,path,visualSettings,station,stop,pulse,index,count);
                return new Vector3((float)point.X,(float)point.Y,(float)point.Z);
            }
            Vector3 position=Sample(at),direction;
            if(pulse){var forward=host.Venue.Manifest.Range.Forward;direction=new Vector3((float)forward.X,0,(float)forward.Z);}
            else direction=Sample(Math.Min(stop,at+SkiState.NanometersPerMeter/5))-position;
            if(direction.sqrMagnitude<.000001f)direction=Vector3.forward;
            float y=position.y;
            if(host.Environment!=null)y=Mathf.Max(y,host.Environment.SampleGround(position));
            position.y=y+(float)definition.GroundOffset;
            go.transform.position=position;go.transform.localScale=Vector3.one*(pulse?.55f:1);
            direction.y=0;go.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);go.SetActive(visible);
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
