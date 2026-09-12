using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Biathlon.PlayModeTests
{
    public class ShootingDeviceInputTests
    {
        private GameObject root;
        private Mouse mouse;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private ShootingDeviceInput input;
        [SetUp] public void Setup()
        {
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();gamepad=InputSystem.AddDevice<Gamepad>();
            root=new GameObject("shooting_input_test");input=root.AddComponent<ShootingDeviceInput>();input.enabled=false;input.Initialize(new AthleteId("player"),20,new SimulationTick(100));
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(root);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(gamepad);}
        [Test] public void MouseDeltaAndTriggerAreConsumedOnceAndInvalidTickIsAtomic()
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(10,-4)}.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();input.Capture(mouse,keyboard,null);
            Assert.Throws<ArgumentException>(()=>input.Build(new SimulationTick(102)));
            var first=input.Build(new SimulationTick(101));Assert.That(first.Aim.Horizontal,Is.EqualTo(50));Assert.That(first.Aim.Vertical,Is.EqualTo(-20));Assert.That(first.Trigger&&first.HoldBreath,Is.True);
            var second=input.Build(new SimulationTick(102));Assert.That(second.Aim.Horizontal,Is.Zero);Assert.That(second.Trigger,Is.False);Assert.That(second.HoldBreath,Is.True);
            input.SetInputFocus(false);var released=input.Build(new SimulationTick(103));Assert.That(released.HoldBreath||released.Trigger,Is.False);
        }
        [Test] public void StickFineControlAndBreathFeedTheSameDeterministicAim()
        {
            InputSystem.QueueStateEvent(gamepad,new GamepadState{rightStick=new Vector2(.7f,.2f),rightTrigger=1,leftTrigger=1}.WithButton(GamepadButton.LeftShoulder));InputSystem.Update();input.Capture(null,null,gamepad);
            var d=WeaponAimDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/weapon_aim_v1").text);
            var a=new WeaponAimSystem(d,new AthleteId("player"),20,initialTick:new SimulationTick(100));var b=new WeaponAimSystem(d,new AthleteId("player"),20,initialTick:new SimulationTick(100));
            for(int i=101;i<=120;i++){var c=input.Build(new SimulationTick(i));Assert.That(c.Aim.FineControl&&c.HoldBreath,Is.True);Assert.That(c.Trigger,Is.EqualTo(i==101));Assert.That(a.Step(c.Aim),Is.EqualTo(b.Step(c.Aim)));}
            Assert.That(a.State.YawRadians,Is.GreaterThan(0));input.Capture(null,null,null);Assert.That(input.Build(new SimulationTick(121)).Aim.Horizontal,Is.Zero);
        }
        [Test] public void MousePacketsAccumulateAndLargeMovementRespectsTickRate()
        {
            for(int i=0;i<2;i++){InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(200,0)});InputSystem.Update();input.Capture(mouse,keyboard,null);}
            Assert.That(input.Build(new SimulationTick(101)).Aim.Horizontal,Is.EqualTo(1000));
            Assert.That(input.Build(new SimulationTick(102)).Aim.Horizontal,Is.EqualTo(1000));
            Assert.That(input.Build(new SimulationTick(103)).Aim.Horizontal,Is.Zero);
        }
    }
}
