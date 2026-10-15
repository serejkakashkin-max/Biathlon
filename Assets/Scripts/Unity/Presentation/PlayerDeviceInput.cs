using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Frame input adapter. The race host requests commands at its own fixed tick boundary.</summary>
    public sealed class PlayerDeviceInput : MonoBehaviour
    {
        public bool UseGamepad { get; set; }
        private DeviceCommandBuilder builder;
        private ManualSkiCommand pendingManual;
        private bool hasPendingManual;
        private double steeringSpeed;
        private bool previousOvertake, previousCancel, inactive;
        private bool focused = true;
        private BoundDeviceControls controls;
        public void Initialize(AthleteId athlete, int frequency,PlayerSettings preferences=null)
        {
            builder = new DeviceCommandBuilder(DeviceControlDefinition.FromJson(
                Resources.Load<TextAsset>("Biathlon/device_control_gameplay_v3").text), athlete, frequency);
            steeringSpeed=SteeringDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/steering_provisional_v1").text).LateralSpeed;
            previousOvertake = previousCancel = inactive = false;
            hasPendingManual=false;
            controls=new BoundDeviceControls(preferences??new PlayerSettings(),PlayerSettingsPreferences.Catalog);
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
            controls.Resolve(keyboard,Mouse.current,gamepad);
            bool overtake, cancel;
            DeviceControlSample sample;
            // The host chooses the device; merely connecting a pad never disables keyboard input.
            if (gamepad != null)
            {
                sample = new DeviceControlSample(true, controls.Value("pace_up",true),
                    controls.Value("steer_right",true)-controls.Value("steer_left",true),
                    controls.Pressed("recovery",true) ? EffortPreset.Recovery :
                    controls.Pressed("final_sprint",true) ? EffortPreset.FinalSprint :
                    controls.Pressed("attack",true) ? EffortPreset.Attack :
                    controls.Pressed("race_pace",true) ? EffortPreset.Race : EffortPreset.Analog,
                    controls.Pressed("tuck",true),controls.Pressed("pace_down",true));
                overtake = controls.Pressed("overtake",true); cancel = controls.Pressed("cancel_overtake",true);
            }
            else
            {
                sample = new DeviceControlSample(false,
                    (controls.Pressed("pace_up",false)?1:0)-(controls.Pressed("pace_down",false)?1:0),
                    (controls.Pressed("steer_right",false)?1:0)-(controls.Pressed("steer_left",false)?1:0),
                    controls.Pressed("recovery",false) ? EffortPreset.Recovery :
                    controls.Pressed("final_sprint",false) ? EffortPreset.FinalSprint :
                    controls.Pressed("attack",false) ? EffortPreset.Attack :
                    controls.Pressed("race_pace",false) ? EffortPreset.Race : EffortPreset.Analog,
                    controls.Pressed("tuck",false),controls.Pressed("pace_down",false));
                overtake = controls.Pressed("overtake",false); cancel = controls.Pressed("cancel_overtake",false);
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
            builder.Sample(new DeviceControlSample(true, 0, 0, EffortPreset.Analog,brake:true), cancelPressed: true);
            previousOvertake = previousCancel = false; inactive = true;
        }
        private void OnApplicationFocus(bool value) => SetInputFocus(value);
        private void OnDisable() => ReleaseHeldInput();
        public PlayerCommand Build(SimulationTick target) => builder != null ? builder.Build(target) :
            throw new System.InvalidOperationException("Initialize input before building commands.");
        public ManualSkiCommand BuildManual(SimulationTick target) => builder != null ? builder.BuildManual(target) :
            throw new System.InvalidOperationException("Initialize input before building commands.");
        // A rejected race tick does not consume another device tick or edges.
        public ManualSkiCommand PeekManual(SimulationTick target)
        {
            if(hasPendingManual)
            {if(!pendingManual.Tick.Equals(target))throw new System.InvalidOperationException("Pending device tick was not accepted.");return pendingManual;}
            pendingManual=BuildManual(target);hasPendingManual=true;return pendingManual;
        }
        public void AcceptManual(SimulationTick target)
        {if(!hasPendingManual||!pendingManual.Tick.Equals(target))throw new System.InvalidOperationException("Matching pending command required.");hasPendingManual=false;}
        public PlayerCommand BuildDirectional(SimulationTick target,double currentMeters,double extentMeters)=>builder!=null?
            builder.BuildDirectional(target,currentMeters,extentMeters,steeringSpeed):throw new System.InvalidOperationException("Initialize input before building commands.");
    }
}
