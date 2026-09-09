using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Simulation;
namespace Biathlon.Replay
{
    /// <summary>Separate physiology v1 fingerprint. Foundation fingerprint is unaffected.</summary>
    public static class PhysiologyFingerprint
    {
        public static string Compute(PhysiologySession session, RaceEventLog events)
        {
            if (session == null || events == null) throw new ArgumentNullException();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(1);
                writer.Write(session.Config.SchemaVersion); writer.Write(session.Config.Id);
                writer.Write(session.Config.TickFrequency); writer.Write(session.Config.Seed); writer.Write(session.RandomState);
                var d = session.Definition;
                writer.Write(d.SchemaVersion); writer.Write(d.Id);
                writer.Write(d.RestingHrBpm);
                writer.Write(d.MaxHrBpm);
                writer.Write(d.HrSafetyCeilingBpm);
                writer.Write(d.SustainablePowerWatts);
                writer.Write(d.MaximumRelativeEffort);
                writer.Write(d.HrRiseTauSeconds);
                writer.Write(d.HrRecoveryTauSeconds);
                writer.Write(d.HrLoadExponent);
                writer.Write(d.HrMaximumLoad);
                writer.Write(d.HrOverloadScale);
                writer.Write(d.FatigueHrModifier);
                writer.Write(d.DriftStrengthBpm);
                writer.Write(d.DriftThreshold);
                writer.Write(d.DriftSaturationSeconds);
                writer.Write(d.AnaerobicCapacityJoules);
                writer.Write(d.AerobicThreshold);
                writer.Write(d.AnaerobicDrainExponent);
                writer.Write(d.AnaerobicDrainWatts);
                writer.Write(d.AnaerobicRecoveryWatts);
                writer.Write(d.RecoveryModifier);
                writer.Write(d.FatigueExponent);
                writer.Write(d.FatigueGainPerSecond);
                writer.Write(d.FatigueRecoveryPerSecond);
                writer.Write(d.MinimumPowerEfficiency);
                writer.Write(d.FatigueRecoveryPenalty);
                writer.Write(d.MaximumTerrainMuscleFactor);
                writer.Write(session.Clock.Tick.Value); writer.Write(session.AthleteId.Value); writer.Write(session.RequestedControl);
                writer.Write(checked((long)Math.Round(session.State.HeartRateBpm * 1e9, MidpointRounding.ToEven)));
                writer.Write(checked((long)Math.Round(session.State.AnaerobicReserve * 1e9, MidpointRounding.ToEven)));
                writer.Write(checked((long)Math.Round(session.State.MuscularFatigue * 1e9, MidpointRounding.ToEven)));
                writer.Write(checked((long)Math.Round(session.State.HighLoadSeconds * 1e9, MidpointRounding.ToEven)));
                writer.Write(events.Count);
                for (int i = 0; i < events.Count; i++)
                {
                    var item = events[i];
                    writer.Write(item.Tick.Value); writer.Write(item.TickFrequency); writer.Write(item.AthleteId.Value);
                    writer.Write((int)item.EventType); writer.Write(item.Payload);
                }
                writer.Flush();
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

