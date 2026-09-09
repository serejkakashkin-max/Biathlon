using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class FinalSprintDefinition
    {
        private FinalSprintDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "activation_distance_m", IsRequired = true)] public double ActivationDistanceMeters { get; private set; }
        [DataMember(Name = "effort_control", IsRequired = true)] public int EffortControl { get; private set; }
        public static FinalSprintDefinition FromJson(string json)
        {
            var v = RouteJson.Read<FinalSprintDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid final sprint schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.ActivationDistanceMeters, 0.000000001, 1000000, nameof(ActivationDistanceMeters));
            AthleteState.ValidateEffort(v.EffortControl);
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct FinishTime : System.IComparable<FinishTime>
    {
        public const int PartsPerTick = 1000000000;
        public long WholeTicks { get; }
        public int FractionParts { get; }
        public int TickFrequency { get; }
        public double Seconds => TickFrequency > 0 ? WholeTicks / (double)TickFrequency + FractionParts / (double)PartsPerTick / TickFrequency :
            throw new System.InvalidOperationException("No valid finish time is available.");
        public FinishTime(long wholeTicks, int fractionParts, int tickFrequency)
        {
            if (wholeTicks < 0 || fractionParts < 0 || fractionParts >= PartsPerTick || tickFrequency <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(wholeTicks));
            WholeTicks = wholeTicks; FractionParts = fractionParts; TickFrequency = tickFrequency;
        }
        public int CompareTo(FinishTime other)
        {
            if (TickFrequency <= 0 || TickFrequency != other.TickFrequency) throw new System.ArgumentException("Finish times require the same valid race clock.");
            int whole = WholeTicks.CompareTo(other.WholeTicks);
            return whole != 0 ? whole : FractionParts.CompareTo(other.FractionParts);
        }
        public static FinishTime AtCrossing(SimulationTick containingTick, int frequency, long beforeNanometers, long afterNanometers, long boundaryNanometers)
        {
            if (containingTick.Value <= 0 || frequency <= 0 || beforeNanometers < 0 || boundaryNanometers <= beforeNanometers || afterNanometers < boundaryNanometers)
                throw new System.ArgumentException("Invalid crossing interval.");
            long distance = afterNanometers - beforeNanometers;
            long remaining = boundaryNanometers - beforeNanometers;
            long parts = (long)System.Math.Round((decimal)remaining * PartsPerTick / distance, 0, System.MidpointRounding.ToEven);
            long whole = containingTick.Value - 1;
            if (parts == PartsPerTick) { whole = checked(whole + 1); parts = 0; }
            return new FinishTime(whole, (int)parts, frequency);
        }
        public static FinishTime InsideTick(SimulationTick containingTick, int frequency, double secondsIntoTick)
        {
            if (containingTick.Value <= 0 || frequency <= 0 || double.IsNaN(secondsIntoTick) ||
                secondsIntoTick < 0 || secondsIntoTick > 1.0 / frequency) throw new System.ArgumentException("Invalid substep time.");
            long parts = (long)System.Math.Round((decimal)secondsIntoTick * frequency * PartsPerTick, 0, System.MidpointRounding.ToEven);
            return parts == PartsPerTick ? new FinishTime(containingTick.Value, 0, frequency) : new FinishTime(containingTick.Value - 1, (int)parts, frequency);
        }
        public static FinishTime AtSubstepCrossing(SimulationTick tick, int frequency, long beforeNanometers, long afterNanometers,
            long boundaryNanometers, double startSeconds, double endSeconds)
        {
            InsideTick(tick, frequency, startSeconds); InsideTick(tick, frequency, endSeconds);
            if (endSeconds <= startSeconds || beforeNanometers < 0 || boundaryNanometers <= beforeNanometers || afterNanometers < boundaryNanometers)
                throw new System.ArgumentException("Invalid timed crossing interval.");
            decimal fraction = (decimal)(boundaryNanometers - beforeNanometers) / (afterNanometers - beforeNanometers);
            decimal seconds = (decimal)startSeconds + ((decimal)endSeconds - (decimal)startSeconds) * fraction;
            long parts = (long)System.Math.Round(seconds * frequency * PartsPerTick, 0, System.MidpointRounding.ToEven);
            return parts == PartsPerTick ? new FinishTime(tick.Value, 0, frequency) : new FinishTime(tick.Value - 1, (int)parts, frequency);
        }
    }
}
