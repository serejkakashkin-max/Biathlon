using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class StatModifierCurve
    {
        private StatModifierCurve() { }
        [DataContract]
        private sealed class Knot
        {
            [DataMember(Name = "stat", IsRequired = true, Order = 0)] public int Stat { get; private set; }
            [DataMember(Name = "factor", IsRequired = true, Order = 1)] public double Factor { get; private set; }
        }
        [DataMember(Name = "knots", IsRequired = true)] private Knot[] Knots { get; set; }
        internal void Validate(bool increasing)
        {
            if (Knots == null || Knots.Length < 2 || Knots.Length > 101)
                throw new ArgumentException("A stat curve needs 2..101 knots.");
            for (int i = 0; i < Knots.Length; i++)
            {
                var point = Knots[i] ?? throw new ArgumentException("Null stat knot.");
                PhysiologyNumbers.RequireRange(point.Stat, 0, 100, "stat knot");
                PhysiologyNumbers.RequireRange(point.Factor, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, "stat factor");
                if (i > 0 && (point.Stat <= Knots[i - 1].Stat ||
                    (increasing ? point.Factor < Knots[i - 1].Factor : point.Factor > Knots[i - 1].Factor)))
                    throw new ArgumentException("Stat knots must be ordered and modifiers monotone in the required direction.");
            }
            if (Knots[0].Stat != 0 || Knots[Knots.Length - 1].Stat != 100)
                throw new ArgumentException("Stat curves must cover 0..100.");
        }
        public double Evaluate(int stat)
        {
            PhysiologyNumbers.RequireRange(stat, 0, 100, nameof(stat));
            for (int i = 1; i < Knots.Length; i++)
            {
                var right = Knots[i];
                if (stat > right.Stat) continue;
                var left = Knots[i - 1];
                double fraction = (stat - left.Stat) / (double)(right.Stat - left.Stat);
                return left.Factor + (right.Factor - left.Factor) * fraction;
            }
            return Knots[Knots.Length - 1].Factor;
        }
    }

    [DataContract]
    public sealed class PhysiologyStatCurves
    {
        [DataMember(Name = "schema_version", IsRequired = true, Order = 0)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true, Order = 1)] public string Id { get; private set; }
        [DataMember(Name = "endurance_power", IsRequired = true, Order = 2)] public StatModifierCurve EndurancePower { get; private set; }
        [DataMember(Name = "anaerobic_capacity", IsRequired = true, Order = 3)] public StatModifierCurve AnaerobicCapacity { get; private set; }
        [DataMember(Name = "recovery", IsRequired = true, Order = 4)] public StatModifierCurve Recovery { get; private set; }
        [DataMember(Name = "strength_fatigue_gain", IsRequired = true, Order = 5)] public StatModifierCurve StrengthFatigueGain { get; private set; }
        private PhysiologyStatCurves() { }
        public static PhysiologyStatCurves FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (PhysiologyStatCurves)new DataContractJsonSerializer(typeof(PhysiologyStatCurves)).ReadObject(stream);
                if (result == null || result.SchemaVersion != 1 || string.IsNullOrWhiteSpace(result.Id))
                    throw new SerializationException("Unsupported stat schema or invalid ID.");
                if (result.EndurancePower == null || result.AnaerobicCapacity == null || result.Recovery == null || result.StrengthFatigueGain == null)
                    throw new SerializationException("All stat curves are required.");
                result.EndurancePower.Validate(true); result.AnaerobicCapacity.Validate(true);
                result.Recovery.Validate(true); result.StrengthFatigueGain.Validate(false);
                return result;
            }
        }
        public string ToJson()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(PhysiologyStatCurves)).WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public PhysiologyDefinition Apply(PhysiologyDefinition template, AthletePhysiologyStats stats)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            string id = FormattableString.Invariant($"{template.Id}:{Id}:e{stats.Endurance}-a{stats.AnaerobicCapacity}-r{stats.Recovery}-s{stats.Strength}");
            return template.WithStatModifiers(id, EndurancePower.Evaluate(stats.Endurance), AnaerobicCapacity.Evaluate(stats.AnaerobicCapacity),
                Recovery.Evaluate(stats.Recovery), StrengthFatigueGain.Evaluate(stats.Strength));
        }
    }
}

