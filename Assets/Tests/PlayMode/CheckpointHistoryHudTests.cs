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
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
using Cursor=UnityEngine.Cursor;

namespace Biathlon.PlayModeTests
{
    public sealed class CheckpointHistoryHudTests
    {
        private GameObject root;private ProductionVenueHost host;
        private InputTestScope inputs;private Keyboard keyboard;private Gamepad pad;
        private bool priorVisible;private CursorLockMode priorLock;
        [SetUp]public void Setup()
        {
            priorVisible=Cursor.visible;priorLock=Cursor.lockState;inputs=new InputTestScope();
            keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();
            var resources=PathPresentationFixture.Resources();
            resources["track"]=resources["track"].Replace("\"checkpoints\": []","\"checkpoints\": [{\"id\":\"split_a\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":140}},{\"id\":\"split_b\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":230}},{\"id\":\"lap_line\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":300}}]");
            var venue=PathPresentationFixture.Plan(resources);
            string Read(string n)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(n));
            var profile=CompetitionProfile.FromJson(Read("sprint_test_v1.json").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("history_hud_fixture",venue.Track.Id,"main",profile.ToRouteRequirements());
            var snow=RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json").Replace("snow_test_packed","packed"));
            root=new GameObject("History and cursor acceptance");host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;
            host.InputPreferences=new PlayerSettings();host.Initialize(venue,profile,route,snow,count:2);
        }
        [TearDown]public void Cleanup()
        {
            if(root!=null)Object.DestroyImmediate(root);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);inputs.Dispose();
            Cursor.lockState=priorLock;Cursor.visible=priorVisible;
            if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);
        }
        private void Reach(int ordinal)
        {
            for(int i=0;i<25000&&!host.Runner.Field.IsComplete;i++)
            {
                if(host.Runner.Field.GetAthlete(host.PlayerIndex).Checkpoints.Count>ordinal)break;
                host.Runner.Step();
            }
            Assert.That(host.Runner.Field.GetAthlete(host.PlayerIndex).Checkpoints.Count,Is.GreaterThan(ordinal));
            host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));host.GameplayHud.Refresh();
        }
        private void AssertSelectedRows()
        {
            var hud=host.GameplayHud;var table=CheckpointStandings.Capture(host.Runner.Field,hud.SelectedCheckpointOrdinal);
            Assert.That(table.Count,Is.GreaterThan(0));
            for(int i=0;i<table.Count;i++)
            {
                var row=hud.Root.Q<TimingAthleteRow>("checkpoint_row_"+i);
                Assert.That(row.AthleteId,Is.EqualTo(table.Get(i).AthleteId));
                Assert.That(row.tooltip,Does.Contain(GameplayHudModel.FormatClock(table.Get(i).Seconds)));
            }
        }
        [Test]
        public void LiveSplitUsesOnlyRecordedSameLineTimesAndHandsOffToHistory()
        {
            var field=host.Runner.Field;
            var hud=host.GameplayHud;
            hud.SetPreferences("en",2);
            var layout=new CheckpointLayout(field.Track,field.Route);
            var card=hud.Root.Q("checkpoint_notice");
            Assert.That(card.style.display.value,Is.EqualTo(DisplayStyle.None));

            bool firstApproach=false, laterApproach=false, crossed=false, running=false;
            double? previousClock=null;
            for(int step=0;step<12000&&!laterApproach;step++)
            {
                host.Runner.Step();
                var snapshot=RacePresentationSnapshot.Capture(field);
                host.Feed.Publish(snapshot);
                var first=snapshot.Get(host.PlayerIndex);
                var own=LiveSplit.Capture(field,layout,host.PlayerIndex,first,60,6);
                if(own.Visible&&own.Ordinal==0&&!own.Crossed)
                {
                    hud.Refresh();
                    Assert.That(own.LeaderSeconds,Is.Null);
                    Assert.That(card.style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(hud.Root.Q<Label>("live_split_reference").text,Does.Contain("No time to beat"));
                    if(previousClock.HasValue&&own.Seconds>previousClock.Value)running=true;
                    previousClock=own.Seconds;
                    firstApproach=true;
                }
                if(!crossed&&own.Visible&&own.Ordinal==0&&own.Crossed)
                {
                    hud.Refresh();
                    var actual=field.GetAthlete(host.PlayerIndex).Checkpoints.Get(0);
                    Assert.That(own.Seconds,Is.EqualTo(actual.ElapsedTime.Seconds));
                    Assert.That(own.Rank,Is.EqualTo(1));
                    Assert.That(hud.Root.Q<Label>("live_split_heading").text,Does.Contain("RANK 1 / 1"));
                    Assert.That(hud.Root.Q<Label>("live_split_time").text,Is.EqualTo(GameplayHudModel.FormatClock(own.Seconds)));
                    hud.SetCheckpointTableVisible(true);
                    Assert.That(card.style.display.value,Is.EqualTo(DisplayStyle.None));
                    AssertSelectedRows();
                    hud.SetCheckpointTableVisible(false);
                    crossed=true;
                }
                int other=1-host.PlayerIndex;
                var later=snapshot.Get(other);
                var estimate=LiveSplit.Capture(field,layout,other,later,60,6);
                if(estimate.Visible&&!estimate.Crossed&&estimate.Ordinal==0&&estimate.LeaderSeconds.HasValue)
                {
                    Assert.That(estimate.LeaderSeconds,Is.EqualTo(field.Timing.Leader(0).ElapsedTime.Seconds));
                    Assert.That(estimate.Seconds,Is.EqualTo(later.ElapsedSeconds));
                    Assert.That(estimate.DifferenceSeconds,Is.EqualTo(later.ElapsedSeconds-estimate.LeaderSeconds.Value));
                    Assert.That(estimate.Rank,Is.Zero);
                    laterApproach=true;
                }
            }
            Assert.That(firstApproach&&running&&crossed&&laterApproach,Is.True);
        }
        [Test]public void MinutePlusGapRowsReuseClockNotationForBothLocales()
        {
            var tick=host.Runner.Field.Tick;var row=new TimingAthleteRow(1.3f);
            foreach(string locale in new[]{"ru","en"})foreach(var sample in new[]{(70.2,"+1:10.2"),(125.7,"+2:05.7"),(59.96,"+1:00.0"),(59.9,"+59.9"),(70.2,"+1:10.2")})
            {
                row.BindCheckpoint("late",2,"Alex","NOR",180,sample.Item1,locale,true);
                string expected=locale=="ru"?sample.Item2.Replace('.',','):sample.Item2;
                Assert.That(row.Q<Label>("timing_gap").text,Is.EqualTo(expected));
                Assert.That(row.Q<Label>("timing_elapsed").text,Is.EqualTo("03:00.0"));
                Assert.That(row.tooltip,Does.Contain(expected));
                Assert.That(row.AthleteId,Is.EqualTo("late"));
            }
            row.BindCheckpoint("first",1,"Leader","SWE",109.8,0,"ru",false);
            Assert.That(row.Q<Label>("timing_gap").text,Is.EqualTo("ЛИДЕР"));
            Assert.That(row.tooltip,Does.Contain("ЛИДЕР"));Assert.That(row.tooltip,Does.Not.Contain("+1:"));
            Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));
        }
        [UnityTest]public IEnumerator EarlierOccurrenceRemainsSelectedAcrossNewSplitsPreferencesAndPause()
        {
            Reach(4);var hud=host.GameplayHud;hud.SetPreferences("ru",0);hud.SetCheckpointTableVisible(true);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(4));hud.MoveCheckpointHistory(-1);AssertSelectedRows();
            Assert.That(hud.Root.Q<Label>("checkpoint_status").text,Does.Contain("КРУГ 2"));
            Reach(5);Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(3));Assert.That(hud.FollowingLatestCheckpoint,Is.False);AssertSelectedRows();
            var tick=host.Runner.Field.Tick;hud.SetPreferences("en",2);hud.SetAccessibility(true);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(3));Assert.That(hud.Root.Q<Label>("checkpoint_mode").text,Is.EqualTo("SPLIT HISTORY"));
            hud.enabled=false;yield return null;hud.enabled=true;yield return null;
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(3));AssertSelectedRows();
            hud.SetCheckpointTableVisible(false);hud.SetCheckpointTableVisible(true);Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(5));
            Assert.That(hud.FollowingLatestCheckpoint,Is.True);Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));
            hud.Root.Q("checkpoint_table").Query<VisualElement>().ForEach(v=>Assert.That(v.pickingMode,Is.EqualTo(PickingMode.Ignore)));
            host.Feed.ResetRace();hud.Refresh();Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(-1));
        }
        [Test]public void KeyboardAndPadHistoryEdgesDoNotConsumeSkiCommandsOrAdvanceRace()
        {
            Reach(4);var hud=host.GameplayHud;hud.SetCheckpointTableVisible(true);var tick=host.Runner.Field.Tick;int pace=host.SelectedPace;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageUp));InputTestScope.Update();hud.CaptureCheckpointHistoryInput(keyboard,null);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(3));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageDown));InputTestScope.Update();hud.CaptureCheckpointHistoryInput(keyboard,null);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(4));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();
            hud.MoveCheckpointHistory(-1);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.End));InputTestScope.Update();hud.CaptureCheckpointHistoryInput(keyboard,null);
            Assert.That(hud.FollowingLatestCheckpoint,Is.True);
            InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(-1,0)});InputTestScope.Update();hud.CaptureCheckpointHistoryInput(null,pad);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(3));
            InputSystem.QueueStateEvent(pad,new GamepadState());InputTestScope.Update();
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.RightStick));InputTestScope.Update();hud.CaptureCheckpointHistoryInput(null,pad);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(4));
            hud.SetCheckpointTableVisible(false);InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageUp));InputTestScope.Update();hud.CaptureCheckpointHistoryInput(keyboard,null);
            Assert.That(hud.SelectedCheckpointOrdinal,Is.EqualTo(4));Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));Assert.That(host.SelectedPace,Is.EqualTo(pace));
        }
        [Test]public void LiveHostHidesCursorAcrossTabAndRestoresItForPauseFocusLossAndDisposal()
        {
            host.enabled=true;host.SetInputFocus(true);Assert.That(host.GameplayCursorCaptured,Is.True);Assert.That(Cursor.visible,Is.False);
            host.GameplayHud.SetCheckpointTableVisible(true);Assert.That(Cursor.visible,Is.False);
            host.SetInputFocus(false);Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
            host.SetInputFocus(true);Assert.That(host.GameplayCursorCaptured,Is.True);Assert.That(Cursor.visible,Is.False);
            root.SetActive(false);Assert.That(Cursor.visible,Is.True);root.SetActive(true);host.SetInputFocus(true);Assert.That(Cursor.visible,Is.False);
            host.enabled=false;Assert.That(Cursor.visible,Is.True);host.enabled=true;host.SetInputFocus(true);
            Object.DestroyImmediate(root);Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
        }
    }
}
