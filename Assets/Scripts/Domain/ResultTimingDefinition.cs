using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    public enum ResultTimeRounding { Truncate = 0, NearestEven = 1 }
    [DataContract]
    public sealed class ResultTimingDefinition
    {
        private ResultTimingDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "units_per_second", IsRequired = true)] public int UnitsPerSecond { get; private set; }
        [DataMember(Name = "rounding", IsRequired = true)] public ResultTimeRounding Rounding { get; private set; }
        public static ResultTimingDefinition FromJson(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? throw new ArgumentNullException(nameof(json)))))
            {
                var value = (ResultTimingDefinition)new DataContractJsonSerializer(typeof(ResultTimingDefinition)).ReadObject(stream);
                if (value == null || value.SchemaVersion != 1 || string.IsNullOrWhiteSpace(value.Id) ||
                    (value.UnitsPerSecond != 1 && value.UnitsPerSecond != 10 && value.UnitsPerSecond != 100 && value.UnitsPerSecond != 1000) ||
                    (value.Rounding != ResultTimeRounding.Truncate && value.Rounding != ResultTimeRounding.NearestEven))
                    throw new SerializationException("Invalid result timing definition.");
                return value;
            }
        }
        public string ToJson()
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(ResultTimingDefinition)).WriteObject(stream, this); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public long Register(FinishTime time)
        {
            if (time.TickFrequency <= 0) throw new ArgumentException("Valid elapsed time required.");
            long seconds = time.WholeTicks / time.TickFrequency;
            long wholeUnits = checked(seconds * UnitsPerSecond);
            decimal fraction = ((decimal)(time.WholeTicks % time.TickFrequency) * FinishTime.PartsPerTick + time.FractionParts) * UnitsPerSecond /
                ((decimal)time.TickFrequency * FinishTime.PartsPerTick);
            // Whole-second units are even for decimal precision; units=1 also needs the total's parity on exact halves.
            decimal registered = Rounding == ResultTimeRounding.Truncate ? decimal.Truncate(fraction) :
                Math.Round(wholeUnits + fraction, 0, MidpointRounding.ToEven) - wholeUnits;
            return checked(wholeUnits + (long)registered);
        }
    }
}
