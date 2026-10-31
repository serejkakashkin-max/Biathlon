using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class CameraOrbitTests
    {
        [Test]public void MouseOrbitWrapsClampsReturnsWithoutMovingTargetOrAllocating()
        {
            var target=new GameObject("Orbit target");var view=new GameObject("Orbit camera").AddComponent<AthleteChaseCamera>();
            try
            {
                view.enabled=false;view.Follow(target.transform,ChaseCameraSettings.FromJson(Resources.Load<TextAsset>("Biathlon/chase_camera_v1").text),0);
                var policy=CameraOrbitSettings.Load();view.SetOrbitInput(true,new Vector2(180/policy.degrees_per_pixel,0));view.Render(0);
                Assert.That(view.transform.position.z,Is.GreaterThan(8),"Full orbit includes a front view of the athlete.");
                view.SetOrbitInput(true,new Vector2(0,-10000));Assert.That(view.OrbitPitchDegrees,Is.EqualTo(policy.maximum_pitch_degrees));
                view.SetOrbitInput(true,new Vector2(0,10000));Assert.That(view.OrbitPitchDegrees,Is.EqualTo(policy.minimum_pitch_degrees));
                var position=view.transform.position;view.SetOrbitInput(false,Vector2.zero);view.Render(.01f);
                Assert.That(Mathf.Abs(view.OrbitYawDegrees),Is.InRange(1,179.99f),"Releasing the button must blend rather than snap.");
                Assert.That(Vector3.Distance(position,view.transform.position),Is.LessThan(3));
                for(int i=0;i<200;i++)view.Render(.016f);
                Assert.That(view.OrbitYawDegrees,Is.Zero);Assert.That(view.OrbitPitchDegrees,Is.Zero);Assert.That(view.transform.position.z,Is.LessThan(-8.9f));
                Assert.That(target.transform.position,Is.EqualTo(Vector3.zero));Assert.That(target.transform.rotation,Is.EqualTo(Quaternion.identity));
                view.CancelOrbit();view.SetTerrainGrade(-.5f);view.SetOrbitInput(true,new Vector2(0,-1000));view.SnapToTarget();
                Assert.That(view.transform.position.z,Is.LessThan(0),"Steep grades and upward drag must not flip the camera through its vertical pole.");
                view.CancelOrbit();view.SetTerrainGrade(0);view.SnapToTarget();
                for(int i=0;i<20;i++){view.SetOrbitInput(true,new Vector2(2,1));view.Render(.016f);}
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++){view.SetOrbitInput(true,new Vector2(2,1));view.Render(.016f);}
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                view.CancelOrbit();Assert.That(view.OrbitHeld,Is.False);Assert.That(view.OrbitYawDegrees,Is.Zero);
                Assert.Throws<ArgumentOutOfRangeException>(()=>view.SetOrbitInput(true,new Vector2(float.NaN,0)));
                Assert.Throws<ArgumentException>(()=>CameraOrbitSettings.FromJson(Resources.Load<TextAsset>("Biathlon/camera_orbit_v1").text.Replace("0.22","0")));
            }
            finally{Object.DestroyImmediate(view.gameObject);Object.DestroyImmediate(target);}
        }
        private static string Read(string n)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(n));
        [UnityTest,Timeout(180000)]public IEnumerator ActualMouseIsSkiOnlyRespectsFocusTablesAndRendersRearSideFrontViews()
        {
            var root=new GameObject("Native mouse orbit acceptance");bool priorVisible=Cursor.visible;var priorLock=Cursor.lockState;float captureDelta=Time.captureDeltaTime;
            using(var input=new InputTestScope())
            {
                var mouse=InputSystem.AddDevice<Mouse>();
                try
                {
                    const string prefix="Biathlon/Venues/woodland_basin_v1/";
                    var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
                    var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                    host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),RaceSnowConditions.FromJson(Read("race_snow_neutral_v1")),count:2);
                    host.AdvanceSimulationFrame(.05);
                    // Reach an ordinary course section so the start portal does not
                    // hide the subject in the side-view visual qualification.
                    for(int i=0;i<100;i++)
                    {
                        var next=host.Runner.Field.Tick.Next();var id=host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId;
                        host.Step(new ManualSkiCommand(next,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(next,id,0,0),false,false));
                    }
                    host.Proxies.Render(1);
                    host.enabled=true;host.SetInputFocus(true);
                    var controller=host.ChaseCamera.GetComponent<AthleteChaseCamera>();
                    InputSystem.QueueStateEvent(mouse,new MouseState());InputTestScope.Update();host.CaptureCameraOrbit(mouse);
                    var tick=host.Runner.Field.Tick;var pose=host.ManualStepper.World.GetActor(host.PlayerIndex).Pose;
                    InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(400,0)}.WithButton(MouseButton.Left));InputTestScope.Update();host.CaptureCameraOrbit(mouse);
                    Assert.That(controller.OrbitHeld,Is.True);Assert.That(Mathf.Abs(controller.OrbitYawDegrees),Is.GreaterThan(80));controller.Render(.1f);
                    Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));Assert.That(host.ManualStepper.World.GetActor(host.PlayerIndex).Pose,Is.EqualTo(pose));
                    host.GameplayHud.SetCheckpointTableVisible(true);host.CaptureCameraOrbit(mouse);Assert.That(controller.OrbitHeld,Is.False);
                    host.GameplayHud.SetCheckpointTableVisible(false);host.CaptureCameraOrbit(mouse);Assert.That(controller.OrbitHeld,Is.False,"A click started over a table cannot become a camera drag.");
                    InputSystem.QueueStateEvent(mouse,new MouseState());InputTestScope.Update();host.CaptureCameraOrbit(mouse);
                    InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(100,0)}.WithButton(MouseButton.Left));InputTestScope.Update();host.CaptureCameraOrbit(mouse);Assert.That(controller.OrbitHeld,Is.True);
                    host.SetInputFocus(false);host.CaptureCameraOrbit(mouse);Assert.That(controller.OrbitHeld,Is.False);host.enabled=false;
                    host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment")));host.Environment.AutomaticRendering=false;
                    host.GameplayHud.enabled=false;host.Proxies.Render(1);
                    // Rapid view changes must cull the world against this frame's final
                    // camera, not the previous chase view. Exercise real LateUpdates.
                    controller.CancelOrbit();controller.SnapToTarget();Time.captureDeltaTime=.1f;host.Environment.AutomaticRendering=true;
                    controller.SetOrbitInput(true,new Vector2(180/CameraOrbitSettings.Load().degrees_per_pixel,0));yield return null;
                    int submitted=host.Environment.VisibleTrees,expected=0;
                    for(int i=0;i<host.Environment.ForestGroupCount;i++){var forest=host.Environment.Forest(i);forest.Prepare(host.ChaseCamera,"high",Time.time);expected+=forest.VisibleInstances;}
                    Assert.That(submitted,Is.EqualTo(expected),"The normal automatic forest submission must follow the final orbit pose in the same frame.");
                    Time.captureDeltaTime=captureDelta;host.Environment.AutomaticRendering=false;host.ChaseCamera.enabled=false;controller.enabled=false;
                    yield return null;
                    foreach(int degrees in new[]{0,90,180})
                    {
                        controller.CancelOrbit();controller.SnapToTarget();controller.SetOrbitInput(true,new Vector2(degrees/CameraOrbitSettings.Load().degrees_per_pixel,0));
                        for(int i=0;i<50;i++)controller.Render(.05f);
                        using(var target=new BenchmarkRenderTarget(1280,720))
                        {
                            for(int i=0;i<3;i++){host.Environment.Render(host.ChaseCamera,"high",12);target.Render(host.ChaseCamera);yield return null;}
                            string folder=Path.GetFullPath("artifacts/m16-4/start-loader-orbit");Directory.CreateDirectory(folder);target.SavePng(Path.Combine(folder,"orbit-"+degrees+".png"));
                        }
                    }
                    Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));Assert.That(host.ManualStepper.World.GetActor(host.PlayerIndex).Pose,Is.EqualTo(pose));
                }
                finally{Object.DestroyImmediate(root);InputSystem.RemoveDevice(mouse);Time.captureDeltaTime=captureDelta;Cursor.lockState=priorLock;Cursor.visible=priorVisible;if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
            }
        }
        [UnityTest,Timeout(120000)]public IEnumerator HeldLookButtonCannotBecomeAShotWhenTheActualRangeTakesOver()
        {
            var root=new GameObject("Orbit to shooting input boundary");float timeScale=Time.timeScale;bool visible=Cursor.visible;var locked=Cursor.lockState;
            using(var input=new InputTestScope())
            {
                var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
                try
                {
                    var venue=PathPresentationFixture.Plan(PathPresentationFixture.Resources());var profile=CompetitionProfile.FromJson(Read("sprint_test_v1.json").Replace("\"distance_m\":3000","\"distance_m\":900"));
                    var route=RaceRoutePlan.TimeTrial("orbit_range_input",venue.Track.Id,"main",profile.ToRouteRequirements());
                    var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;
                    host.Initialize(venue,profile,route,RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json").Replace("snow_test_packed","packed")),count:1);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputTestScope.Update();
                    for(int i=0;i<10000&&host.Runner.Field.GetAthlete(0).ActiveShooting==null;i++)
                    {host.GetComponent<PlayerDeviceInput>().Capture(keyboard,null);host.AdvanceSimulationFrame(.05);}
                    Assert.That(host.Runner.Field.GetAthlete(0).ActiveShooting,Is.Not.Null);
                    Time.timeScale=0;yield return null;host.enabled=true;host.SetInputFocus(true);
                    var camera=host.ChaseCamera.GetComponent<AthleteChaseCamera>();camera.SetOrbitInput(true,new Vector2(200,0));
                    InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputTestScope.Update();
                    long tick=host.Runner.Field.Tick.Value;host.SendMessage("Update");Assert.That(camera.OrbitHeld,Is.False);Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));
                    host.enabled=false;var shot=root.GetComponent<ShootingDeviceInput>();Assert.That(shot.Build(new SimulationTick(tick+1)).Trigger,Is.False,"The held drag is not a new trigger press.");
                    InputSystem.QueueStateEvent(mouse,new MouseState());InputTestScope.Update();shot.Capture(mouse,keyboard,null);Assert.That(shot.Build(new SimulationTick(tick+2)).Trigger,Is.False);
                    InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputTestScope.Update();shot.Capture(mouse,keyboard,null);Assert.That(shot.Build(new SimulationTick(tick+3)).Trigger,Is.True,"A fresh LMB press still fires normally.");
                }
                finally{Object.DestroyImmediate(root);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);Time.timeScale=timeScale;Cursor.lockState=locked;Cursor.visible=visible;if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
            }
        }
    }
}
