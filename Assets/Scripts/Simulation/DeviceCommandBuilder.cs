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
        public DeviceControlSample(bool gamepad, double effort, double line,
            EffortPreset preset = EffortPreset.Analog, bool tuck = false)
        {
            if (double.IsNaN(effort) || effort < (gamepad ? 0 : -1) || effort > 1 ||
                double.IsNaN(line) || line < -1 || line > 1) throw new ArgumentOutOfRangeException(nameof(effort));
            if (preset < EffortPreset.Analog || preset > EffortPreset.FinalSprint) throw new ArgumentOutOfRangeException(nameof(preset));
            Gamepad = gamepad; Effort = effort; Line = line; Preset = preset; Tuck = tuck;
        }
    }
    public sealed class DeviceCommandBuilder
    {
        private readonly DeviceControlDefinition definition;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private DeviceControlSample sample;
        private double keyboardEffort;
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
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Commands require consecutive target ticks.");
            double nextEffort = sample.Gamepad ? keyboardEffort : Math.Max(0, Math.Min(1000,
                keyboardEffort + sample.Effort * definition.KeyboardEffortRate / frequency));
            double line = sample.Line;
            if (sample.Gamepad)
                line = Math.Abs(line) <= definition.StickDeadzone ? 0 : Math.Sign(line) *
                    Math.Pow((Math.Abs(line) - definition.StickDeadzone) / (1 - definition.StickDeadzone), definition.StickExponent);
            var command = new PlayerCommand(target, athlete,
                (int)Math.Round(sample.Gamepad ? sample.Effort * 1000 : nextEffort, MidpointRounding.ToEven),
                (int)Math.Round(line * PlayerCommand.LineIntentUnits, MidpointRounding.ToEven), sample.Preset,
                sample.Tuck, overtake, cancel);
            keyboardEffort = nextEffort; tick = target.Value; overtake = cancel = false;
            return command;
        }
    }
}
