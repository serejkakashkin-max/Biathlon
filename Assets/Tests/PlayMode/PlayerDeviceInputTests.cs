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
