using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class ShootingModelDefinitions
    {
        public PhysiologyDefinition Physiology { get; }
        public ShootingDefinition Lifecycle { get; }
        public TargetGeometryDefinition Targets { get; }
        public WeaponAimDefinition Aim { get; }
        public BreathingDefinition Breathing { get; }
        public HeartbeatDefinition Heartbeat { get; }
        public MuscularSwayDefinition Muscular { get; }
        public ShootingStanceDefinition Stances { get; }
        public BreathControlDefinition BreathControl { get; }
        public ShootingWindDefinition Wind { get; }
        public ShootingHandlingDefinition Handling { get; }
        public ShootingEquipmentDefinition Equipment { get; }
        public ShootingPressureDefinition Pressure { get; }
        public string ContextJson { get; }
        public ShootingModelDefinitions WithEquipment(ShootingEquipmentDefinition equipment)
        {if(equipment==null)throw new ArgumentNullException(nameof(equipment));return new ShootingModelDefinitions(this,equipment);}
        private ShootingModelDefinitions(ShootingModelDefinitions source,ShootingEquipmentDefinition equipment)
        {
            Physiology=source.Physiology;Lifecycle=source.Lifecycle;Targets=source.Targets;Aim=source.Aim;Breathing=source.Breathing;Heartbeat=source.Heartbeat;
            Muscular=source.Muscular;Stances=source.Stances;BreathControl=source.BreathControl;Wind=source.Wind;Handling=source.Handling;Equipment=equipment;Pressure=source.Pressure;
            ContextJson=EncodeContext(Physiology.ToJson(),Lifecycle.ToJson(),Targets.ToJson(),Aim.ToJson(),Breathing.ToJson(),Heartbeat.ToJson(),Muscular.ToJson(),Stances.ToJson(),BreathControl.ToJson(),Wind.ToJson(),Handling.ToJson(),Equipment.ToJson(),Pressure.ToJson());
        }
        public ShootingModelDefinitions(Func<string, string> readJson)
        {
            if (readJson == null) throw new ArgumentNullException(nameof(readJson));
            Physiology = PhysiologyDefinition.FromJson(readJson("physiology_provisional_v1.json"));
            Lifecycle = ShootingDefinition.FromJson(readJson("shooting_lifecycle_v1.json"));
            Targets = TargetGeometryDefinition.FromJson(readJson("target_geometry_v1.json"));
            Aim = WeaponAimDefinition.FromJson(readJson("weapon_aim_v1.json"));
            Breathing = BreathingDefinition.FromJson(readJson("breathing_v1.json"));
            Heartbeat = HeartbeatDefinition.FromJson(readJson("heartbeat_v1.json"));
            Muscular = MuscularSwayDefinition.FromJson(readJson("muscular_sway_v1.json"));
            Stances = ShootingStanceDefinition.FromJson(readJson("shooting_stances_v1.json"));
            BreathControl = BreathControlDefinition.FromJson(readJson("breath_control_v1.json"));
            Wind = ShootingWindDefinition.FromJson(readJson("shooting_wind_v1.json"));
            Handling = ShootingHandlingDefinition.FromJson(readJson("shooting_handling_v1.json"));
            Equipment = ShootingEquipmentDefinition.FromJson(readJson("rifle_neutral_v1.json"));
            Pressure = ShootingPressureDefinition.FromJson(readJson("shooting_pressure_v1.json"));
            ContextJson = EncodeContext(Physiology.ToJson(), Lifecycle.ToJson(), Targets.ToJson(), Aim.ToJson(), Breathing.ToJson(),
                Heartbeat.ToJson(), Muscular.ToJson(), Stances.ToJson(), BreathControl.ToJson(), Wind.ToJson(), Handling.ToJson(), Equipment.ToJson(), Pressure.ToJson());
        }
        internal static string EncodeContext(params string[] values)
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(string[])).WriteObject(stream, values); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        // Extend the existing array without serializing its accumulated JSON as
        // another string. Nested extensions previously doubled escaping at each
        // layer and retained hundreds of MB of identical immutable definitions.
        public static string AppendContext(string context, params string[] values)
        {
            string added=EncodeContext(values);
            return context.Substring(0,context.Length-1)+","+added.Substring(1);
        }
    }
}
