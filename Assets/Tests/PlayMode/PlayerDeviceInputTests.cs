using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Biathlon.PlayModeTests
{
    public class PlayerDeviceInputTests
    {
        private GameObject instance;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private PlayerDeviceInput adapter;
        private InputTestScope inputScope;
        [SetUp] public void SetUp()
        {
            inputScope = new InputTestScope();
            keyboard = InputSystem.AddDevice<Keyboard>(); gamepad = InputSystem.AddDevice<Gamepad>();
            instance = new GameObject("Device harness test"); adapter = instance.AddComponent<PlayerDeviceInput>();
            adapter.enabled = false; adapter.Initialize(new AthleteId("player"), 20);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(instance); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
            inputScope.Dispose();
        }
        [Test] public void KeyboardHeldAndEdgesSurviveFramesWithoutTicks()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.E, Key.Space)); InputTestScope.Update();
            adapter.Capture(keyboard, null); adapter.Capture(keyboard, null);
            var first = adapter.Build(new SimulationTick(1));
            Assert.That(first.AnalogEffort, Is.EqualTo(25)); Assert.That(first.LineIntent, Is.EqualTo(1000));
            Assert.That(first.TuckHeld && first.OvertakeRequested, Is.True);
            adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(2)).OvertakeRequested, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputTestScope.Update(); adapter.Capture(keyboard, null);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputTestScope.Update(); adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(3)).OvertakeRequested, Is.True);
        }
        [Test] public void PendingManualCommandIsConsumedOnlyAfterItsRaceTickWasAccepted()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D,Key.E));InputTestScope.Update();adapter.Capture(keyboard,null);
            var first=adapter.PeekManual(new SimulationTick(1));
            Assert.That(adapter.PeekManual(new SimulationTick(1)),Is.EqualTo(first));
            Assert.That(first.ToEffortCommand().OvertakeRequested,Is.True);
            Assert.Throws<System.InvalidOperationException>(()=>adapter.PeekManual(new SimulationTick(2)));
            adapter.AcceptManual(new SimulationTick(1));
            var next=adapter.PeekManual(new SimulationTick(2));
            Assert.That(next.Tick.Value,Is.EqualTo(2));
            Assert.That(next.ToEffortCommand().OvertakeRequested,Is.False);
            adapter.AcceptManual(new SimulationTick(2));
        }
        [Test]
        public void FocusLossCancelsPendingEdgeAndDeviceAbsenceStaysNeutral()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.Space, Key.LeftShift)); InputTestScope.Update();
            adapter.Capture(keyboard, null); adapter.SetInputFocus(false);
            var c = adapter.Build(new SimulationTick(1));
            Assert.That(c.CancelOvertake, Is.True); Assert.That(c.OvertakeRequested || c.TuckHeld, Is.False);
            Assert.That(c.Preset, Is.EqualTo(EffortPreset.Recovery)); Assert.That(c.AnalogEffort, Is.Zero);
            adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(2)).CancelOvertake, Is.False);
            adapter.SetInputFocus(true); adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(3)).OvertakeRequested, Is.True);
            adapter.Capture(null, null);
            Assert.That(adapter.Build(new SimulationTick(4)).Preset, Is.EqualTo(EffortPreset.Recovery));
        }
        [Test]
        public void PresetsAndRecordedDeviceCommandsRoundTripWithoutFrameTime()
        {
            var tape = new Biathlon.Replay.PlayerCommandTape("unity_input", "fixed_context", new AthleteId("player"), 3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit3)); InputTestScope.Update();
            adapter.Capture(keyboard, null); tape.Append(adapter.Build(new SimulationTick(1)));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.RightShoulder)); InputTestScope.Update();
            adapter.Capture(null, gamepad); tape.Append(adapter.Build(new SimulationTick(2)));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder)); InputTestScope.Update();
            adapter.Capture(null, gamepad); tape.Append(adapter.Build(new SimulationTick(3)));
            Assert.That(tape.GetCommand(0).Preset, Is.EqualTo(EffortPreset.FinalSprint));
            Assert.That(tape.GetCommand(1).Preset, Is.EqualTo(EffortPreset.Attack));
            Assert.That(tape.GetCommand(2).Preset, Is.EqualTo(EffortPreset.Race));
            var restored = Biathlon.Replay.PlayerCommandTape.FromJson(tape.ToJson());
            for(int i=0;i<tape.Count;i++) Assert.That(restored.GetCommand(i).ToJson(), Is.EqualTo(tape.GetCommand(i).ToJson()));
        }
        [Test] public void GamepadUsesRawAxisOnceAndSharesCommandContract()
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.1f, 0), rightTrigger = 0.8f }
                .WithButton(GamepadButton.South).WithButton(GamepadButton.West)); InputTestScope.Update();
            adapter.Capture(null, gamepad);
            var c = adapter.Build(new SimulationTick(1));
            Assert.That(c.AnalogEffort, Is.EqualTo(800)); Assert.That(c.LineIntent, Is.Zero);
            Assert.That(c.TuckHeld && c.OvertakeRequested, Is.True);
            Assert.That(adapter.Build(new SimulationTick(2)).OvertakeRequested, Is.False);
        }
        [Test] public void LiveKeyboardReleaseAndFocusLossKeepTheAchievedPassingLine()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));InputTestScope.Update();adapter.Capture(keyboard,null);
            Assert.That(adapter.BuildDirectional(new SimulationTick(1),-.6,2).LineIntent,Is.LessThan(-300));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();adapter.Capture(keyboard,null);
            Assert.That(adapter.BuildDirectional(new SimulationTick(2),-.6,2).LineIntent,Is.EqualTo(-300));
            adapter.SetInputFocus(false);
            var lost=adapter.BuildDirectional(new SimulationTick(3),-.6,2);Assert.That(lost.LineIntent,Is.EqualTo(-300));
            Assert.That(lost.Preset,Is.EqualTo(EffortPreset.Recovery));Assert.That(lost.CancelOvertake,Is.True);
        }
        [Test] public void ManualKeyboardAxisBuildsYawAndFocusLossNeutralizesIt()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.Space));
            InputTestScope.Update();adapter.Capture(keyboard,null);
            var left=adapter.BuildManual(new SimulationTick(1));
            Assert.That(left.SteeringInput,Is.EqualTo(-500));
            Assert.That(left.TuckHeld,Is.True);
            for(int t=2;t<=5;t++)left=adapter.BuildManual(new SimulationTick(t));
            Assert.That(left.SteeringInput,Is.EqualTo(-ManualSkiCommand.SteeringUnits));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            InputTestScope.Update();adapter.Capture(keyboard,null);
            Assert.That(adapter.BuildManual(new SimulationTick(6)).SteeringInput,Is.Zero);
            adapter.SetInputFocus(false);
            var lost=adapter.BuildManual(new SimulationTick(7));
            Assert.That(lost.SteeringInput,Is.Zero);
            Assert.That(lost.Preset,Is.EqualTo(EffortPreset.Recovery));
            Assert.That(lost.CancelOvertake,Is.True);
        }
        [Test] public void SKeyRequestsPhysicalBrakeAndReleaseClearsIt()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S,Key.D));InputTestScope.Update();adapter.Capture(keyboard,null);
            var c=adapter.BuildManual(new SimulationTick(1));Assert.That(c.BrakeFraction,Is.EqualTo(1));Assert.That(c.SteeringInput,Is.EqualTo(500));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));InputTestScope.Update();adapter.Capture(keyboard,null);
            Assert.That(adapter.BuildManual(new SimulationTick(2)).BrakeFraction,Is.Zero);
        }
    }
}
