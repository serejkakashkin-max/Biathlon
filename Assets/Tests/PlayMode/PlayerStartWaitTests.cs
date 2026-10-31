using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Replay;
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
    public sealed class PlayerStartWaitTests
    {
        private GameObject root;
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        [TearDown]public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private ProductionVenueHost Host(string format="pursuit",int player=1,int gapSeconds=30)
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_profile"));
            PursuitQualificationSnapshot qualification=null;
            if(format=="pursuit")
            {
                var sprint=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_profile"));
                var ids=new[]{new AthleteId("start_leader"),new AthleteId("start_player")};
                var book=new SprintResultBook(sprint,ResultTimingDefinition.FromJson(Read("result_timing_v1.json")),ids,20,new RaceEventLog(20));
                for(int i=0;i<2;i++)
                {
                    long start=1+i*600,finish=start+1200+i*gapSeconds*20;
                    var life=new CompetitionLifecycle(sprint,ids[i],20,new RaceEventLog(100));
                    for(long tick=1;tick<=finish;tick++)
                    {
                        var action=tick==start?CompetitionAction.Start:tick==start+1||tick==start+4?CompetitionAction.EnterRange:
                            tick==start+2||tick==start+5?CompetitionAction.CompleteBout:tick==start+3||tick==start+6?CompetitionAction.ReachLapBoundary:
                            tick==finish?CompetitionAction.Finish:CompetitionAction.None;
                        life.Step(new SimulationTick(tick),action,action==CompetitionAction.CompleteBout?31:0,action==CompetitionAction.CompleteBout?5:0,
                            action==CompetitionAction.Finish?new FinishTime(finish,0,20):default);
                    }
                    book.Record(life);
                }
                qualification=PursuitQualificationSnapshot.Create("start_wait_qualification",book.Complete("start_wait_sprint"),profile);
            }
            if(root==null)root=new GameObject("Personal start acceptance");
            var go=new GameObject("Player start host");go.transform.SetParent(root.transform,false);
            var host=go.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            host.Initialize(venue,profile,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_route")),
                RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")),2,player,qualification);
            host.ArmRaceStartCountdown();return host;
        }
        [Test]public void PursuitSkipKeepsEveryCommandAndOpponentStateIdenticalToOrdinaryWaiting()
        {
            var ordinary=Host();var skipped=Host();
            using(var a=new MemoryStream())using(var b=new MemoryStream())
            using(var wa=new ManualRaceJournalWriter(a,ordinary.ManualStepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            using(var wb=new ManualRaceJournalWriter(b,skipped.ManualStepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            {
                ordinary.ManualStepper.CommandObserver=wa;skipped.ManualStepper.CommandObserver=wb;
                for(int i=0;i<10;i++)skipped.AdvanceSimulationFrame(.05);
                Assert.That(skipped.RequestSkipStartWait(),Is.True);Assert.That(skipped.RequestSkipStartWait(),Is.False);
                int maximumSteps=RaceStartDefinition.FromJson(Read("race_start_v2.json")).SkipMaximumSteps;
                for(int i=0;i<1000&&skipped.StartWaitFastForwarding;i++)
                {long before=skipped.Runner.Field.Tick.Value;skipped.AdvanceSimulationFrame(10);Assert.That(skipped.Runner.Field.Tick.Value-before,Is.LessThanOrEqualTo(maximumSteps));}
                Assert.That(skipped.StartWaitFastForwarding,Is.False);Assert.That(skipped.HasSimulationFault,Is.False);
                Assert.That(skipped.Runner.Field.Tick.Value,Is.EqualTo(541));Assert.That(skipped.CountdownRemaining,Is.EqualTo(3));
                Assert.That(skipped.Feed.Previous,Is.Null,"Normal frame interpolation must not rewind the coalesced waiting interval.");
                Assert.That(skipped.Feed.Current.Tick,Is.EqualTo(541));
                Assert.That(skipped.Runner.Field.GetAthlete(1).Lifecycle.Phase,Is.EqualTo(CompetitionPhase.Prestart));
                Assert.That(skipped.Runner.Field.GetAthlete(0).Motion.DistanceNanometers,Is.GreaterThan(0));
                Assert.That(skipped.SelectedPace,Is.Zero);Assert.That(skipped.BurstRequested,Is.False);
                for(int i=0;i<60;i++)skipped.AdvanceSimulationFrame(.05);
                Assert.That(skipped.Runner.Field.Tick.Value,Is.EqualTo(601));Assert.That(skipped.WaitingForPlayerStart,Is.False);
                Assert.That(skipped.Runner.Field.GetAthlete(1).Lifecycle.StartTick.Value,Is.EqualTo(601));
                for(int i=0;i<601;i++)ordinary.AdvanceSimulationFrame(.05);
                Assert.That(ordinary.HasSimulationFault,Is.False);
                wa.Complete();wb.Complete();Assert.That(wa.Failure,Is.Null);Assert.That(wb.Failure,Is.Null);
                Assert.That(wa.TickCount,Is.EqualTo(601));Assert.That(wb.TickCount,Is.EqualTo(601));
                Assert.That(b.ToArray(),Is.EqualTo(a.ToArray()),"Skipping only coalesces rendering; every accepted authority command is identical.");
                using(var hash=System.Security.Cryptography.SHA256.Create())
                {
                    string sha=BitConverter.ToString(hash.ComputeHash(b.ToArray())).Replace("-","").ToLowerInvariant();
                    string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/player-start-20261013"));Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir,"skip-determinism.json"),"{\"schema_version\":1,\"id\":\"player_start_skip_determinism_20261013\",\"tick_frequency\":20,\"player_start_tick\":601,\"skip_target_tick\":541,\"recorded_ticks\":601,\"journal_bytes\":"+b.Length+",\"journal_sha256\":\""+sha+"\",\"ordinary_and_skipped_identical\":true}");
                }
                for(int i=0;i<2;i++)
                {
                    Assert.That(skipped.ManualStepper.World.GetActor(i).Pose,Is.EqualTo(ordinary.ManualStepper.World.GetActor(i).Pose));
                    Assert.That(skipped.Runner.Field.GetAthlete(i).LastFrame,Is.EqualTo(ordinary.Runner.Field.GetAthlete(i).LastFrame));
                }
            }
        }
        [Test]public void MassStartLoadingHitchCannotConsumeTheVisiblePreparation()
        {
            var host=Host("mass_start",player:0);
            Assert.That(host.CountdownNumber,Is.EqualTo(3));
            host.AdvanceSimulationFrame(8); // First frame includes synchronous venue/shader loading.
            Assert.That(host.CountdownNumber,Is.EqualTo(3),"Loading time must not swallow 3–2–1.");
            Assert.That(host.Runner.Field.Tick.Value,Is.Zero);
            Assert.That(host.WaitingForPlayerStart,Is.True);
            Assert.That(host.CountdownRemaining,Is.EqualTo(3));
            for(int expected=2;expected>=0;expected--)
            {
                for(int frame=0;frame<8;frame++)host.AdvanceSimulationFrame(.125);
                Assert.That(host.CountdownNumber,Is.EqualTo(expected));
                Assert.That(host.Runner.Field.Tick.Value,Is.Zero);
                if(expected>0)host.AdvanceSimulationFrame(8); // A later rendering stall cannot skip 2 or 1 either.
            }
            host.AdvanceSimulationFrame(.05);Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(1));
        }
        [TestCase("pursuit")][TestCase("sprint")]
        public void DelayedPlayerIsHiddenUntilExactThreeSecondBoundaryWhileEarlierStarterMoves(string format)
        {
            var host=Host(format);var player=host.Proxies.GetProxy(host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId.Value);
            Assert.That(player.gameObject.activeInHierarchy,Is.False);
            long boundary=host.Runner.Field.GetAthlete(host.PlayerIndex).ScheduledStart.Value-(long)(RaceStartDefinition.FromJson(Read("race_start_v2.json")).CountdownSeconds*host.Runner.Field.TickFrequency);
            while(host.Runner.Field.Tick.Value<boundary-1)host.AdvanceSimulationFrame(.05);
            host.Proxies.Render(1);Assert.That(player.gameObject.activeInHierarchy,Is.False,"One tick before3s the start position is visually unoccupied.");
            Assert.That(host.Runner.Field.GetAthlete(0).Motion.DistanceNanometers,Is.GreaterThan(0));
            host.AdvanceSimulationFrame(.05);host.Proxies.Render(1);
            Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(boundary));Assert.That(host.CountdownNumber,Is.EqualTo(3));Assert.That(player.gameObject.activeInHierarchy,Is.True);
        }
        [Test]public void DelayedSprintUsesItsPersonalStartAndPursuitLeaderKeepsTheNormalThreeSecondHold()
        {
            var sprint=Host("sprint");Assert.That(sprint.PlayerStartWaitSeconds,Is.EqualTo(30));Assert.That(sprint.CanSkipStartWait,Is.True);
            using(var inputScope=new InputTestScope())
            {
                var keyboard=InputSystem.AddDevice<Keyboard>();
                try
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter,Key.W,Key.LeftShift));InputTestScope.Update();
                    sprint.SendMessage("Update");Assert.That(sprint.StartWaitFastForwarding,Is.True,"Enter requests bounded waiting skip.");
                    Assert.That(sprint.SelectedPace,Is.Zero);Assert.That(sprint.BurstRequested,Is.False,"Held race controls cannot spend power or prime the pace before departure.");
                }
                finally{InputSystem.RemoveDevice(keyboard);}
            }
            var leader=Host(player:0);Assert.That(leader.CountdownNumber,Is.EqualTo(3));Assert.That(leader.CanSkipStartWait,Is.False);
            leader.AdvanceSimulationFrame(10);Assert.That(leader.Runner.Field.Tick.Value,Is.Zero);
            Assert.That(leader.CountdownNumber,Is.EqualTo(3),"Loading cannot consume local preparation.");
            for(int frame=0;frame<24;frame++)leader.AdvanceSimulationFrame(.125);
            leader.AdvanceSimulationFrame(.05);
            Assert.That(leader.Runner.Field.Tick.Value,Is.EqualTo(1));Assert.That(leader.WaitingForPlayerStart,Is.False);
            Assert.That(leader.HasSimulationFault,Is.False);
        }
        [UnityTest]public IEnumerator PersonalWaitButtonAndCountdownFocusOnThePlayerAndRemainReadable()
        {
            var host=Host();host.GameplayHud.SetPreferences("ru",0);host.GameplayHud.Refresh();yield return null;
            var hud=host.GameplayHud;var button=hud.Root.Q<Button>("skip_start_wait");
            Assert.That(hud.Root.Q("player_start_wait").style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(button.pickingMode,Is.EqualTo(PickingMode.Position));Assert.That(button.enabledInHierarchy,Is.True);
            Assert.That(hud.Root.Q("start_countdown").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(host.ChaseCamera.GetComponent<AthleteChaseCamera>().Target,Is.EqualTo(host.Proxies.GetProxy("start_player")));
            Assert.That(host.Proxies.GetProxy("start_player").gameObject.activeInHierarchy,Is.False,"The personal waiting skier appears only in the final three seconds before departure.");
            Assert.That(host.Proxies.GetProxy("start_leader").gameObject.activeInHierarchy,Is.False,"Unstarted rivals retain their normal visibility policy.");
            var accepted=host.ManualStepper.World.GetActor(host.PlayerIndex).Pose;
            Assert.That(host.Proxies.GetProxy("start_player").position.x,Is.EqualTo((float)accepted.X).Within(.001));
            Assert.That(host.Proxies.GetProxy("start_player").position.z,Is.EqualTo((float)accepted.Z).Within(.001));
            yield return Capture(host,1600,900,"pursuit-wait-ru");
            hud.SetPreferences("en",2);hud.Refresh();yield return Capture(host,1024,768,"pursuit-wait-en-large");
            hud.SetPreferences("ru",0);hud.Refresh();yield return null;button=hud.Root.Q<Button>("skip_start_wait");
            button.Focus();using(var submit=NavigationSubmitEvent.GetPooled())button.SendEvent(submit);
            Assert.That(host.StartWaitFastForwarding,Is.True);Assert.That(button.enabledInHierarchy,Is.False);
            long pausedTick=host.Runner.Field.Tick.Value;host.gameObject.SetActive(false);host.enabled=true;
            for(int i=0;i<3;i++)yield return null;
            Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(pausedTick),"Race pause also suspends fast-forward.");
            host.enabled=false;host.gameObject.SetActive(true);
            for(int i=0;i<1000&&host.StartWaitFastForwarding;i++)host.AdvanceStartWaitFrame();
            Assert.That(host.HasSimulationFault,Is.False);hud.Refresh();yield return null;
            Assert.That(hud.Root.Q("player_start_wait").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(hud.Root.Q<Label>("countdown_number").text,Is.EqualTo("3"));
            Assert.That(host.Proxies.GetProxy("start_player").gameObject.activeInHierarchy,Is.True);
            yield return Capture(host,1600,900,"pursuit-countdown-ru");
            hud.SetPreferences("en",2);hud.Refresh();yield return Capture(host,1024,768,"pursuit-countdown-en-large");
            for(int i=0;i<20;i++)host.AdvanceSimulationFrame(.05);hud.Refresh();Assert.That(host.CountdownNumber,Is.EqualTo(2));
            for(int i=0;i<20;i++)host.AdvanceSimulationFrame(.05);hud.Refresh();Assert.That(host.CountdownNumber,Is.EqualTo(1));
            for(int i=0;i<20;i++)host.AdvanceSimulationFrame(.05);hud.Refresh();Assert.That(host.WaitingForPlayerStart,Is.False);
            Assert.That(host.HasSimulationFault,Is.False);
        }
        private static IEnumerator Capture(ProductionVenueHost host,int width,int height,string name)
        {
            var hud=host.GameplayHud;var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var target=new RenderTexture(width,height,24);target.Create();var previous=RenderTexture.active;Texture2D image=null;
            panel.targetTexture=target;panel.clearColor=false;host.ChaseCamera.targetTexture=target;
            try
            {
                for(int i=0;i<3;i++)yield return null;
                var visible=hud.Root.Q(host.CountdownNumber>0?"start_countdown":"player_start_wait");
                Assert.That(visible.worldBound.width,Is.GreaterThan(100));
                RenderTexture.active=target;image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/player-start-20261013"));Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());
            }
            finally{host.ChaseCamera.targetTexture=null;panel.targetTexture=null;RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
