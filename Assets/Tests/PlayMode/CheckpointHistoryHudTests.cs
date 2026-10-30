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
