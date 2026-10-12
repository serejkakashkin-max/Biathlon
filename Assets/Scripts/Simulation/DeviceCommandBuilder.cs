using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Latest held values; action edges are latched separately until the next tick.</summary>
    public readonly struct DeviceControlSample
    {
        public bool Gamepad { get; }
        public double Effort { get; }
        public double Line { get; }
        public EffortPreset Preset { get; }
        public bool Tuck { get; }
        public bool Brake { get; }
        public DeviceControlSample(bool gamepad, double effort, double line,
            EffortPreset preset = EffortPreset.Analog, bool tuck = false,bool brake=false)
        {
            if (double.IsNaN(effort) || effort < (gamepad ? 0 : -1) || effort > 1 ||
                double.IsNaN(line) || line < -1 || line > 1) throw new ArgumentOutOfRangeException(nameof(effort));
            if (preset < EffortPreset.Analog || preset > EffortPreset.FinalSprint) throw new ArgumentOutOfRangeException(nameof(preset));
            Gamepad = gamepad; Effort = effort; Line = line; Preset = preset; Tuck = tuck;
            Brake=brake;
        }
    }
    public sealed class DeviceCommandBuilder
    {
        private readonly DeviceControlDefinition definition;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private DeviceControlSample sample;
        private double keyboardEffort;
        private double keyboardSteering;
        private long tick;
        private bool overtake, cancel;
        public DeviceCommandBuilder(DeviceControlDefinition definition, AthleteId athlete, int frequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!athlete.IsValid || frequency <= 0) throw new ArgumentException("Invalid command clock/athlete.");
            this.athlete = athlete; this.frequency = frequency;
        }
        public void Sample(DeviceControlSample value, bool overtakePressed = false, bool cancelPressed = false)
        {
            sample = value;
            // Cancellation wins until consumed; repeated frame samples coalesce to one action.
            cancel |= cancelPressed; overtake = (overtake || overtakePressed) && !cancel;
        }
        public PlayerCommand Build(SimulationTick target)
            => BuildCore(target, null, 0, 0);
        /// <summary>Build the v2 signed-yaw intent from the same sampled device axis.
        /// This consumes one fixed tick and its latched action edges. No lateral
        /// position, route centre or previous line is consulted.</summary>
        public ManualSkiCommand BuildManual(SimulationTick target)
        {
            var legacy=BuildCore(target,null,0,0);
            double steering=legacy.LineIntent;
            if(!sample.Gamepad&&definition.KeyboardSteeringRate>0)
            {
                // A tap is a small ski deflection, not an immediate maximum
                // braking turn. Release/reversal never retains the old sign.
                double current=sample.Line==0||Math.Sign(sample.Line)!=Math.Sign(keyboardSteering)?0:keyboardSteering;
                double limit=definition.KeyboardSteeringRate/frequency;
                steering=sample.Line==0?0:current+Math.Max(-limit,Math.Min(limit,steering-current));
            }
            keyboardSteering=sample.Gamepad?0:steering;
            return new ManualSkiCommand(target,athlete,legacy.AnalogEffort,
                (int)Math.Round(steering,MidpointRounding.ToEven),legacy.Preset,legacy.TuckHeld,
                legacy.OvertakeRequested,legacy.CancelOvertake,legacy.TechniqueId,
                sample.Brake||definition.StopAtZeroEffort&&legacy.Preset==EffortPreset.Analog&&legacy.AnalogEffort==0?definition.ManualBrakeFraction:0);
        }
        /// <summary>Live steering is directional; recorded commands remain absolute line targets.
        /// Neutral input holds the achieved line, including after a blocked manoeuvre.</summary>
        public PlayerCommand BuildDirectional(SimulationTick target, double currentMeters, double extentMeters, double speedMetersPerSecond)
        {
            if(double.IsNaN(currentMeters)||double.IsInfinity(currentMeters)||double.IsNaN(extentMeters)||double.IsInfinity(extentMeters)||extentMeters<=0||
                double.IsNaN(speedMetersPerSecond)||double.IsInfinity(speedMetersPerSecond)||speedMetersPerSecond<=0)
                throw new ArgumentOutOfRangeException(nameof(extentMeters));
            return BuildCore(target,currentMeters,extentMeters,speedMetersPerSecond);
        }
        private PlayerCommand BuildCore(SimulationTick target,double? currentMeters,double extentMeters,double speedMetersPerSecond)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Commands require consecutive target ticks.");
            double nextEffort = sample.Gamepad ? keyboardEffort : Math.Max(0, Math.Min(definition.KeyboardEffortCeiling,
                keyboardEffort + sample.Effort * definition.KeyboardEffortRate / frequency));
            double line = sample.Line;
            if (sample.Gamepad)
                line = Math.Abs(line) <= definition.StickDeadzone ? 0 : Math.Sign(line) *
                    Math.Pow((Math.Abs(line) - definition.StickDeadzone) / (1 - definition.StickDeadzone), definition.StickExponent);
            if(currentMeters.HasValue)
                line=Math.Max(-1,Math.Min(1,(currentMeters.Value+line*speedMetersPerSecond/frequency)/extentMeters));
            var command = new PlayerCommand(target, athlete,
                (int)Math.Round(sample.Gamepad ? sample.Effort * definition.GamepadEffortCeiling : nextEffort, MidpointRounding.ToEven),
                (int)Math.Round(line * PlayerCommand.LineIntentUnits, MidpointRounding.ToEven), sample.Preset,
                sample.Tuck, overtake, cancel);
            keyboardEffort = nextEffort; tick = target.Value; overtake = cancel = false;
            return command;
        }
    }
}
