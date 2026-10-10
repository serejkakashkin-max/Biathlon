using System;
namespace Biathlon.Simulation
{
    /// <summary>Explicit production revision. Historical balance workloads keep their original resources.</summary>
    public static class ProductionGameplayDefinitions
    {
        public static Func<string,string> Bind(Func<string,string> read)
        {
            if(read==null)throw new ArgumentNullException(nameof(read));
            return name=>read(Resource(name));
        }
        public static string Resource(string name)
        {
            switch(name)
            {
                case "physiology_provisional_v1.json":return "physiology_gameplay_v2.json";
                case "tuck_provisional_v1.json":return "tuck_gameplay_v2.json";
                case "breathing_v1.json":return "breathing_gameplay_v2.json";
                case "heartbeat_v1.json":return "heartbeat_gameplay_v2.json";
                case "muscular_sway_v1.json":return "muscular_sway_gameplay_v2.json";
                default:return name;
            }
        }
    }
}
