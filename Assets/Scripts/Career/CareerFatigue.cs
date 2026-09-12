using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerFatigueDefinition
    {
        private CareerFatigueDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="weekly_recovery",IsRequired=true)] public int WeeklyRecovery { get; private set; }
        [DataMember(Name="rest_slot_recovery",IsRequired=true)] public int RestSlotRecovery { get; private set; }
        [DataMember(Name="race_load",IsRequired=true)] public int RaceLoad { get; private set; }
        [DataMember(Name="race_load_per_minute",IsRequired=true)] public int RaceLoadPerMinute { get; private set; }
        [DataMember(Name="minimum_recovery_factor",IsRequired=true)] public int MinimumRecoveryFactor { get; private set; }
        [DataMember(Name="maximum_recovery_factor",IsRequired=true)] public int MaximumRecoveryFactor { get; private set; }
        [DataMember(Name="maximum_race_minutes",IsRequired=true)] public int MaximumRaceMinutes { get; private set; }
        public static CareerFatigueDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerFatigueDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid fatigue definition.");CareerUnits.Id(d.Id);
            foreach(int value in new[]{d.WeeklyRecovery,d.RestSlotRecovery,d.RaceLoad,d.RaceLoadPerMinute})CareerUnits.Range(value,0,CareerUnits.Basis,"fatigue coefficient");
            CareerUnits.Range(d.MinimumRecoveryFactor,0,CareerUnits.Basis,"recovery factor");CareerUnits.Range(d.MaximumRecoveryFactor,d.MinimumRecoveryFactor,2*CareerUnits.Basis,"recovery factor");CareerUnits.Range(d.MaximumRaceMinutes,1,1440,"race duration");return d;
        }
    }
    public readonly struct CareerFatigueFrame
    {
        public int Before { get; } public int After { get; } public int Load { get; } public int Recovery { get; }
        internal CareerFatigueFrame(int before,int after,int load,int recovery){Before=before;After=after;Load=load;Recovery=recovery;}
    }
    public static class CareerFatigue
    {
        public static CareerFatigueFrame Training(CareerAthlete athlete,CareerTrainingSession session)
        {
            if(athlete==null||session==null)throw new ArgumentNullException();athlete.Validate();int count=checked(athlete.TrainingSessions+1);
            var result=Apply(athlete,session.FatigueCost,session.RecoveryGain);athlete.TrainingSessions=count;return result;
        }
        public static CareerFatigueFrame Race(CareerAthlete athlete,CareerFatigueDefinition definition,long elapsedMilliseconds)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            if(elapsedMilliseconds<1||elapsedMilliseconds>definition.MaximumRaceMinutes*60000L)throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
            int load=checked(definition.RaceLoad+CareerUnits.Round((decimal)definition.RaceLoadPerMinute*elapsedMilliseconds/60000));return Apply(athlete,load,0);
        }
        public static CareerFatigueFrame Rest(CareerAthlete athlete,CareerFatigueDefinition definition,int unusedSlots)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();CareerUnits.Range(unusedSlots,0,14,"unused slots");
            decimal factor=definition.MinimumRecoveryFactor+(decimal)(definition.MaximumRecoveryFactor-definition.MinimumRecoveryFactor)*athlete.Stat(CareerStat.Recovery)/CareerUnits.MaximumStat;
            int recovery=CareerUnits.Round((definition.WeeklyRecovery+definition.RestSlotRecovery*unusedSlots)*factor/CareerUnits.Basis);return Apply(athlete,0,recovery);
        }
        private static CareerFatigueFrame Apply(CareerAthlete athlete,int load,int recovery)
        {int before=athlete.Fatigue;int after=CareerUnits.ClampBasis(checked(before+load-recovery));athlete.Fatigue=after;return new CareerFatigueFrame(before,after,load,recovery);}
    }
}
