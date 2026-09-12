using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Frame input adapter. The race host requests commands at its own fixed tick boundary.</summary>
    public sealed class PlayerDeviceInput : MonoBehaviour
    {
        public bool UseGamepad { get; set; }
        private DeviceCommandBuilder builder;
        private bool previousOvertake, previousCancel, inactive;
        private bool focused = true;
        public void Initialize(AthleteId athlete, int frequency)
        {
            builder = new DeviceCommandBuilder(DeviceControlDefinition.FromJson(
                Resources.Load<TextAsset>("Biathlon/device_control_v1").text), athlete, frequency);
            previousOvertake = previousCancel = inactive = false;
        }
        private void Update()
        {
            if (builder != null) Capture(UseGamepad ? null : Keyboard.current, UseGamepad ? Gamepad.current : null);
        }
        public void Capture(Keyboard keyboard, Gamepad gamepad)
        {
            if (builder == null) throw new System.InvalidOperationException("Initialize input before capture.");
            if (!focused || (keyboard == null && gamepad == null)) { ReleaseHeldInput(); return; }
            inactive = false;
            bool overtake, cancel;
            DeviceControlSample sample;
            // The host chooses the device; merely connecting a pad never disables keyboard input.
            if (gamepad != null)
            {
                sample = new DeviceControlSample(true, gamepad.rightTrigger.ReadUnprocessedValue(),
                    gamepad.leftStick.x.ReadUnprocessedValue(),
                    gamepad.leftTrigger.isPressed ? EffortPreset.Recovery :
                    gamepad.buttonNorth.isPressed ? EffortPreset.FinalSprint :
                    gamepad.rightShoulder.isPressed ? EffortPreset.Attack :
                    gamepad.leftShoulder.isPressed ? EffortPreset.Race : EffortPreset.Analog,
                    gamepad.buttonSouth.isPressed);
                overtake = gamepad.buttonWest.isPressed; cancel = gamepad.buttonEast.isPressed;
            }
            else
            {
                sample = new DeviceControlSample(false,
                    (keyboard?.wKey.isPressed == true ? 1 : 0) - (keyboard?.sKey.isPressed == true ? 1 : 0),
                    (keyboard?.dKey.isPressed == true ? 1 : 0) - (keyboard?.aKey.isPressed == true ? 1 : 0),
                    keyboard?.leftCtrlKey.isPressed == true ? EffortPreset.Recovery :
                    keyboard?.digit3Key.isPressed == true ? EffortPreset.FinalSprint :
                    keyboard?.leftShiftKey.isPressed == true ? EffortPreset.Attack :
                    keyboard?.digit1Key.isPressed == true ? EffortPreset.Race : EffortPreset.Analog,
                    keyboard?.spaceKey.isPressed == true);
                overtake = keyboard?.eKey.isPressed == true; cancel = keyboard?.qKey.isPressed == true;
            }
            builder.Sample(sample, overtake && !previousOvertake, cancel && !previousCancel);
            previousOvertake = overtake; previousCancel = cancel;
        }
        // Losing the selected device/focus must not keep an attack or an unconsumed edge alive.
        // The simulation clock and accumulated keyboard effort remain owned by the builder.
        public void SetInputFocus(bool value) { focused = value; if (!value) ReleaseHeldInput(); }
        public void ReleaseHeldInput()
        {
            if (builder == null || inactive) return;
            builder.Sample(new DeviceControlSample(true, 0, 0, EffortPreset.Recovery), cancelPressed: true);
            previousOvertake = previousCancel = false; inactive = true;
        }
        private void OnApplicationFocus(bool value) => SetInputFocus(value);
        private void OnDisable() => ReleaseHeldInput();
        public PlayerCommand Build(SimulationTick target) => builder != null ? builder.Build(target) :
            throw new System.InvalidOperationException("Initialize input before building commands.");
    }
}
