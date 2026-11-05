using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class TurnAndRangeRevealTests
    {
        [UnityEngine.TestTools.UnityTest] public IEnumerator UnseenRangeBoardHasPreparedLayoutBeforeFirstRaceWindow()
        {
            var root=new GameObject("Pre-range layout contract");
            try
            {
                string Read(string id)=>ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/"+id);
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("manifest")),ProductionVenueHost.ReadResource);
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(Read("mass_start_profile")),RaceRoutePlan.FromJson(Read("mass_start_route")),RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:1);
                var hud=host.GameplayHud;for(int i=0;i<5;i++)yield return null;
                Assert.That(hud.RangeBroadcastVisible,Is.False);
                var board=hud.Root.Q("range_broadcast");
                Assert.That(board.worldBound.height,Is.GreaterThan(50),"The first live window must reveal an already measured board instead of doing its first text/layout work on the skiing frame.");
                Assert.That(board.resolvedStyle.visibility,Is.EqualTo(Visibility.Hidden));
            }
            finally{Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        }
        [UnityEngine.TestTools.UnityTest] public IEnumerator CaptureRearQuarterTurningPose()
        {
            var root=new GameObject("Turn visual review");BenchmarkRenderTarget target=null;
            try
            {
                var rig=AthleteRigFactory.Create(root.transform);var camera=new GameObject("Turn camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;
                camera.transform.position=new Vector3(2,2.4f,-6);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=26;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.22f,.27f);
                var light=new GameObject("Turn key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(40,145,0);
                var q=root.AddComponent<RenderQualityPresentation>();q.Initialize(camera);q.Apply("high");
                target=new BenchmarkRenderTarget(800,800);var driver=rig.GetComponent<AthleteHybridLocomotionDriver>();
                driver.Sample(rig,new LocomotionInput(7,0,.9f,curvature:.16f),.4);
                var rifle=rig.GetComponent<AthleteRiflePresentation>();rifle.Sample(rig,0);
                rig.GetComponent<AthleteGroundContact>().SampleMoving(rig,0,driver.Cycle,driver.ContactState,0,.4);rig.GetComponent<AthleteGripPresentation>().Render(rig);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;camera.cullingMask=1<<30;light.cullingMask=1<<30;
                for(int i=0;i<5;i++){target.Render(camera);yield return null;}
                string folder="artifacts/m16-6/turn-board/visual";Directory.CreateDirectory(folder);target.SavePng(Path.Combine(folder,"turn-rear.png"));
            }
            finally{target?.Dispose();Object.DestroyImmediate(root);}
        }
        [TestCase(-1)][TestCase(1)] public void TightSkiTurnTransfersThePelvisAndKeepsTorsoOverSupport(int sign)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                d.Sample(rig,new LocomotionInput(7,0,.9f,curvature:0),.4);
                float neutral=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Hips).position).x;
                d.Sample(rig,new LocomotionInput(7,0,.9f,curvature:sign*.16f),2);
                var hip=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Hips).position);
                var neck=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Neck).position);
                float lateral=Mathf.Abs(neck.x-hip.x),rise=neck.y-hip.y;
                TestContext.WriteLine($"[turn-review] hip_transfer={(hip.x-neutral)*sign:R} torso_bank={Mathf.Atan2(lateral,rise)*Mathf.Rad2Deg:R} hips={hip} neck={neck}");
                Assert.That((hip.x-neutral)*sign,Is.GreaterThan(.025f),"A turn must move weight toward the inside ski instead of folding the entire torso sideways around a stationary pelvis.");
                Assert.That(Mathf.Atan2(lateral,rise)*Mathf.Rad2Deg,Is.LessThanOrEqualTo(18),"The loaded skier must keep a bounded upper-body inclination above the transferred pelvis.");
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
