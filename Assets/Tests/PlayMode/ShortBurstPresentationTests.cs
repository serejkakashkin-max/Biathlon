using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class ShortBurstPresentationTests
    {
        private GameObject root;
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private ProductionVenueHost Host()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            root=new GameObject("Short burst acceptance");var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:1);
            return host;
        }
        private static void Step(ProductionVenueHost host,PlayerDeviceInput device)
        {
            var tick=host.Runner.Field.Tick.Next();var id=host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId;
            host.Step(device.BuildManual(tick),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
        }
        [UnityTest] public IEnumerator ActualShiftPaysReserveAndOverlaysPaceUntilReleased()
        {
            using(var scope=new InputTestScope())
            {
                var keyboard=InputSystem.AddDevice<Keyboard>();
                try
                {
                    var host=Host();var device=root.GetComponent<PlayerDeviceInput>();
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputTestScope.Update();device.Capture(keyboard,null);
                    for(int i=0;i<32;i++)Step(host,device);
                    int pace=host.SelectedPace;Assert.That(pace,Is.EqualTo(800));
                    double before=host.Runner.Field.GetAthlete(0).PhysiologyState.AnaerobicReserve;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));InputTestScope.Update();device.Capture(keyboard,null);
                    for(int i=0;i<24;i++)Step(host,device);
                    host.GameplayHud.Refresh();yield return null;
                    Assert.That(host.BurstRequested,Is.True);Assert.That(host.SelectedPace,Is.EqualTo(pace));
                    Assert.That(host.Runner.Field.GetAthlete(0).PhysiologyState.AnaerobicReserve,Is.LessThan(before-.03),"The visible burst must consume the same physical reserve as the race.");
                    Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.GreaterThan(1.5));
                    Assert.That(host.GameplayHud.Root.Q("burst_overlay").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(host.GameplayHud.Root.Q("pace_layer").style.display.value,Is.EqualTo(DisplayStyle.None));
                    yield return Capture(host.GameplayHud,"burst-ru",1600,900,"ru",0);
                    yield return Capture(host.GameplayHud,"burst-en-large",1024,768,"en",2);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();device.Capture(keyboard,null);
                    Step(host,device);host.GameplayHud.Refresh();yield return null;
                    Assert.That(host.BurstRequested,Is.False);Assert.That(host.SelectedPace,Is.EqualTo(pace),"Release restores the dial; Shift must never overwrite it.");
                    Assert.That(host.GameplayHud.Root.Q("burst_overlay").style.display.value,Is.EqualTo(DisplayStyle.None));
                    Assert.That(host.GameplayHud.Root.Q("pace_layer").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    for(int i=0;i<12;i++)Step(host,device);
                    Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.InRange(1.03,1.08),"The normal physical effort returns after the bounded control response.");
                    host.GameplayHud.Refresh();yield return Capture(host.GameplayHud,"pace-restored-ru",1600,900,"ru",0);
                    Assert.That(host.HasSimulationFault,Is.False);
                }
                finally{InputSystem.RemoveDevice(keyboard);}
            }
        }
        [Test] public void FullGamepadTriggerKeepsNormalPaceAndShoulderPaysExplicitBurst()
        {
            using(var scope=new InputTestScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();
                try
                {
                    var host=Host();var device=root.GetComponent<PlayerDeviceInput>();
                    InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1});InputTestScope.Update();device.Capture(null,pad);
                    for(int i=0;i<32;i++)Step(host,device);
                    Assert.That(host.SelectedPace,Is.EqualTo(800));Assert.That(host.BurstRequested,Is.False);
                    Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.InRange(1.03,1.08));
                    InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1}.WithButton(GamepadButton.RightShoulder));InputTestScope.Update();device.Capture(null,pad);
                    for(int i=0;i<24;i++)Step(host,device);
                    host.GameplayHud.Refresh();
                    Assert.That(host.BurstRequested,Is.True);Assert.That(host.SelectedPace,Is.EqualTo(800));
                    Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.GreaterThan(1.5));
                    Assert.That(host.GameplayHud.Root.Q("burst_overlay").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1});InputTestScope.Update();device.Capture(null,pad);
                    for(int i=0;i<12;i++)Step(host,device);
                    Assert.That(host.BurstRequested,Is.False);Assert.That(host.SelectedPace,Is.EqualTo(800));
                    Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.InRange(1.03,1.08));
                }
                finally{InputSystem.RemoveDevice(pad);}
            }
        }
        [Test] public void TuckDoesNotAdvertiseBurstWhileTheSharedPostureCapsPower()
        {
            var host=Host();var id=host.Runner.Field.GetAthlete(0).AthleteId;
            for(int i=0;i<24;i++)
            {
                var tick=host.Runner.Field.Tick.Next();
                host.Step(new ManualSkiCommand(tick,id,800,0,EffortPreset.Attack,tuckHeld:true),
                    new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
            }
            host.GameplayHud.Refresh();
            Assert.That(host.Runner.Field.GetAthlete(0).TuckActive,Is.True);
            Assert.That(host.Feed.Current.Get(0).Effort.DeliveredRelativeEffort,Is.LessThan(.3));
            Assert.That(host.BurstRequested,Is.False);
            Assert.That(host.GameplayHud.Root.Q("burst_overlay").style.display.value,Is.EqualTo(DisplayStyle.None));
        }
        [Test] public void BrakingCancelsTheBurstIntentAndUsesTheSharedBrake()
        {
            var host=Host();var id=host.Runner.Field.GetAthlete(0).AthleteId;var tick=host.Runner.Field.Tick.Next();
            host.Step(new ManualSkiCommand(tick,id,700,0,EffortPreset.Attack,brakeFraction:1),
                new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
            Assert.That(host.BurstRequested,Is.False);Assert.That(host.Runner.GetLastManualBrake(0),Is.EqualTo(1));
            Assert.That(host.Feed.Current.Get(0).Effort.RequestedPowerWatts,Is.Zero);
        }
        private static IEnumerator Capture(GameplayHud hud,string filename,int width,int height,string locale,int textSize)
        {
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var target=new RenderTexture(width,height,0);target.Create();var priorTarget=panel.targetTexture;var priorActive=RenderTexture.active;Texture2D pixels=null;
            try
            {
                panel.targetTexture=target;hud.SetPreferences(locale,textSize);yield return null;yield return null;yield return null;
                var motion=hud.Root.Q("motion_panel").worldBound;var timing=hud.Root.Q("timing_panel").worldBound;
                Assert.That(motion.yMin,Is.GreaterThan(timing.yMax+12));Assert.That(motion.xMax,Is.LessThan(hud.Root.worldBound.center.x));
                Assert.That(motion.yMax,Is.LessThanOrEqualTo(hud.Root.worldBound.yMax));
                var readout=hud.Root.Q("pace_readout").worldBound;
                foreach(string name in new[]{hostBurstVisible(hud)?"burst_hint":"pace_keys"})
                    Assert.That(hud.Root.Q(name).worldBound.yMax,Is.LessThanOrEqualTo(readout.yMax+1),"Readout text must fit in its existing card.");
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/burst-pack-20261012"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,filename+".png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=priorActive;panel.targetTexture=priorTarget;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);}
        }
        private static bool hostBurstVisible(GameplayHud hud)=>hud.Root.Q("burst_overlay").style.display.value==DisplayStyle.Flex;
    }
}
