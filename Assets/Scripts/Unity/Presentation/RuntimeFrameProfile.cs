using Unity.Profiling;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded markers for real race frames; no deep profiling or simulation state.</summary>
    public static class RuntimeFrameProfile
    {
        public const string TickName="Biathlon.Race.Tick";
        public static readonly ProfilerMarker Tick=new ProfilerMarker(ProfilerCategory.Scripts,TickName);
        public const string SnapshotName="Biathlon.Race.Snapshot";
        public static readonly ProfilerMarker Snapshot=new ProfilerMarker(ProfilerCategory.Scripts,SnapshotName);
        public const string AthletesName="Biathlon.View.Athletes";
        public static readonly ProfilerMarker Athletes=new ProfilerMarker(ProfilerCategory.Scripts,AthletesName);
        public const string EnvironmentName="Biathlon.View.Environment";
        public static readonly ProfilerMarker Environment=new ProfilerMarker(ProfilerCategory.Scripts,EnvironmentName);
        public const string HudName="Biathlon.View.Hud";
        public static readonly ProfilerMarker Hud=new ProfilerMarker(ProfilerCategory.Scripts,HudName);
        public const string EquipmentName="Biathlon.View.Equipment";
        public static readonly ProfilerMarker Equipment=new ProfilerMarker(ProfilerCategory.Scripts,EquipmentName);
    }
}
