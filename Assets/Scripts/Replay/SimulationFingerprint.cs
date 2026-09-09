using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Simulation;
namespace Biathlon.Replay
{
    /// <summary>Versioned canonical little-endian encoding. Intended for verification, outside tick loops.</summary>
    public static class SimulationFingerprint
    {
        public static string Compute(SimulationWorld world, RaceEventLog events, bool includeRandomState = true)
        {
            if (world == null || events == null) throw new ArgumentNullException();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(1); // Fingerprint format version.
                writer.Write(world.Config.SchemaVersion);
                writer.Write(world.Config.Id);
                writer.Write(world.Clock.Frequency);
                writer.Write(world.Clock.Tick.Value);
                writer.Write(includeRandomState);
                if (includeRandomState)
                {
                    writer.Write(world.Config.Seed);
                    writer.Write(world.Random.State);
                }
                writer.Write(world.AthleteCount);
                for (int i = 0; i < world.AthleteCount; i++)
                {
                    var state = world.GetAthlete(i);
                    writer.Write(state.Id.Value);
                    writer.Write(state.RequestedEffort);
                }
                writer.Write(events.Count);
                for (int i = 0; i < events.Count; i++)
                {
                    var item = events[i];
                    writer.Write(item.Tick.Value);
                    writer.Write(item.TickFrequency);
                    writer.Write(item.AthleteId.Value);
                    writer.Write((int)item.EventType);
                    writer.Write(item.Payload);
                }
                writer.Flush();
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
