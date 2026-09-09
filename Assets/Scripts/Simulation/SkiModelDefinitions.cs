using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Cold composition of independently versioned authoritative definitions; reader owns file access.</summary>
    public sealed class SkiModelDefinitions
    {
        public PhysiologyDefinition Physiology { get; }
        public PhysiologyStatCurves StatCurves { get; }
        public SkiPhysicsDefinition Physics { get; }
        public SkiTechniqueDefinition Technique { get; }
        public TuckDefinition Tuck { get; }
        public CorneringDefinition Cornering { get; }
        public SnowCatalog Snow { get; }
        public SkiEquipmentCatalog Equipment { get; }
        public FinalSprintDefinition Sprint { get; }
        public SkiModelDefinitions(Func<string, string> readJson)
        {
            if (readJson == null) throw new ArgumentNullException(nameof(readJson));
            Physiology = PhysiologyDefinition.FromJson(readJson("physiology_provisional_v1.json"));
            StatCurves = PhysiologyStatCurves.FromJson(readJson("physiology_stat_curves_v1.json"));
            Physics = SkiPhysicsDefinition.FromJson(readJson("ski_physics_provisional_v1.json"));
            Technique = SkiTechniqueDefinition.FromJson(readJson("ski_technique_provisional_v1.json"));
            Tuck = TuckDefinition.FromJson(readJson("tuck_provisional_v1.json"));
            Cornering = CorneringDefinition.FromJson(readJson("cornering_provisional_v1.json"));
            Snow = SnowCatalog.FromJson(readJson("snow_catalog_provisional_v1.json"));
            Equipment = SkiEquipmentCatalog.FromJson(readJson("ski_equipment_provisional_v1.json"));
            Sprint = FinalSprintDefinition.FromJson(readJson("final_sprint_provisional_v1.json"));
        }
    }
}
