using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Wall-time adapter. All authoritative state belongs to World.</summary>
    public sealed class FoundationBootstrap : MonoBehaviour
    {
        public SimulationWorld World { get; private set; }
        public RaceEventLog Events { get; private set; }
        private double startTime;
        private void Awake()
        {
            var asset = Resources.Load<TextAsset>("Biathlon/simulation_foundation");
            if (asset == null) throw new InvalidOperationException("Missing foundation config.");
            var data = JsonUtility.FromJson<Definition>(asset.text);
            var config = new SimulationConfig(data.schema_version, data.id, data.tick_frequency, data.seed);
            Events = new RaceEventLog();
            World = new SimulationWorld(config, new[] { new AthleteState(new AthleteId(data.athlete_id)) }, Events);
            startTime = Time.timeAsDouble;
        }
        private void Update()
        {
            long targetTick = (long)Math.Floor((Time.timeAsDouble - startTime) * World.Clock.Frequency);
            while (World.Clock.Tick.Value < targetTick) World.Step();
        }
        private void OnGUI()
        {
            if (World != null)
                GUI.Label(new Rect(16, 16, 650, 40),
                    $"BIATHLON Foundation | Tick {World.Clock.Tick.Value} | {World.Clock.ElapsedSeconds:F3} s | {World.Clock.Frequency} Hz");
        }
        [Serializable]
        private sealed class Definition
        {
            public int schema_version;
            public string id;
            public int tick_frequency;
            public ulong seed;
            public string athlete_id;
        }
    }
}
