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
        [SetUp] public void SetUp()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); gamepad = InputSystem.AddDevice<Gamepad>();
            instance = new GameObject("Device harness test"); adapter = instance.AddComponent<PlayerDeviceInput>();
            adapter.enabled = false; adapter.Initialize(new AthleteId("player"), 20);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(instance); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
        }
        [Test] public void KeyboardHeldAndEdgesSurviveFramesWithoutTicks()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.E, Key.Space)); InputSystem.Update();
            adapter.Capture(keyboard, null); adapter.Capture(keyboard, null);
            var first = adapter.Build(new SimulationTick(1));
            Assert.That(first.AnalogEffort, Is.EqualTo(25)); Assert.That(first.LineIntent, Is.EqualTo(1000));
            Assert.That(first.TuckHeld && first.OvertakeRequested, Is.True);
            adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(2)).OvertakeRequested, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); adapter.Capture(keyboard, null);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update(); adapter.Capture(keyboard, null);
            Assert.That(adapter.Build(new SimulationTick(3)).OvertakeRequested, Is.True);
        }
        [Test]
        public void FocusLossCancelsPendingEdgeAndDeviceAbsenceStaysNeutral()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.Space, Key.LeftShift)); InputSystem.Update();
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
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit3)); InputSystem.Update();
            adapter.Capture(keyboard, null); tape.Append(adapter.Build(new SimulationTick(1)));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.RightShoulder)); InputSystem.Update();
            adapter.Capture(null, gamepad); tape.Append(adapter.Build(new SimulationTick(2)));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder)); InputSystem.Update();
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
                .WithButton(GamepadButton.South).WithButton(GamepadButton.West)); InputSystem.Update();
            adapter.Capture(null, gamepad);
            var c = adapter.Build(new SimulationTick(1));
            Assert.That(c.AnalogEffort, Is.EqualTo(800)); Assert.That(c.LineIntent, Is.Zero);
            Assert.That(c.TuckHeld && c.OvertakeRequested, Is.True);
            Assert.That(adapter.Build(new SimulationTick(2)).OvertakeRequested, Is.False);
        }
    }
}
