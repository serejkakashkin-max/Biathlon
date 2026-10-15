using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    public enum CareerGrowthGroup { Aerobic, Power, Technique, Shooting, Mental }

    [DataContract] public sealed class CareerGrowthCurve
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="knots",IsRequired=true)] private CareerAgePoint[] knots {get;set;}
        internal void Validate()
        {
            CareerUnits.Id(Id);if(knots==null||knots.Length<2||knots.Length>120)throw new SerializationException("Invalid growth curve.");
            int previous=0;foreach(var k in knots)
            {
                if(k==null)throw new SerializationException("Null growth age knot.");CareerUnits.Range(k.Age,previous+1,120,"ordered growth age");
                CareerUnits.Range(k.TrainingFactor,0,20000,"growth age response");CareerUnits.Range(k.AnnualDecline,0,100000,"growth decline");previous=k.Age;
            }
        }
        internal int Value(int age,bool decline)
        {
            int At(CareerAgePoint p)=>decline?p.AnnualDecline:p.TrainingFactor;
            if(age<=knots[0].Age)return At(knots[0]);
            for(int i=1;i<knots.Length;i++)if(age<=knots[i].Age)
            {var a=knots[i-1];var b=knots[i];return CareerUnits.Round(At(a)+(decimal)(At(b)-At(a))*(age-a.Age)/(b.Age-a.Age));}
            return At(knots[knots.Length-1]);
        }
    }
    [DataContract] public sealed class CareerGrowthStatRule
    {
        [DataMember(Name="stat",IsRequired=true)] public CareerStat Stat {get;private set;}
        [DataMember(Name="group",IsRequired=true)] public CareerGrowthGroup Group {get;private set;}
        [DataMember(Name="curve",IsRequired=true)] public string CurveId {get;private set;}
        [DataMember(Name="learning_factor",IsRequired=true)] public int LearningFactor {get;private set;}
        [DataMember(Name="decline_factor",IsRequired=true)] public int DeclineFactor {get;private set;}
        internal CareerGrowthCurve Curve {get;set;}
    }
    [DataContract] public sealed class CareerTalentBand
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="weight",IsRequired=true)] public int Weight {get;private set;}
        [DataMember(Name="minimum",IsRequired=true)] public int Minimum {get;private set;}
        [DataMember(Name="maximum",IsRequired=true)] public int Maximum {get;private set;}
    }
    [DataContract] public sealed class CareerGrowthStyle
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="weight",IsRequired=true)] public int Weight {get;private set;}
        [DataMember(Name="group_responses",IsRequired=true)] private int[] groupResponses {get;set;}
        [DataMember(Name="ceiling_offsets",IsRequired=true)] private int[] ceilingOffsets {get;set;}
        [DataMember(Name="age_shift_min",IsRequired=true)] public int AgeShiftMin {get;private set;}
        [DataMember(Name="age_shift_max",IsRequired=true)] public int AgeShiftMax {get;private set;}
        [DataMember(Name="early_response",IsRequired=true)] public int EarlyResponse {get;private set;}
        [DataMember(Name="late_response",IsRequired=true)] public int LateResponse {get;private set;}
        [DataMember(Name="response_pivot_age",IsRequired=true)] public int PivotAge {get;private set;}
        [DataMember(Name="response_transition_years",IsRequired=true)] public int TransitionYears {get;private set;}
        [DataMember(Name="retirement_min",IsRequired=true)] public int RetirementMin {get;private set;}
        [DataMember(Name="retirement_max",IsRequired=true)] public int RetirementMax {get;private set;}
        [DataMember(Name="stagnation_probability",IsRequired=true)] public int StagnationProbability {get;private set;}
        [DataMember(Name="stagnation_factor",IsRequired=true)] public int StagnationFactor {get;private set;}
        internal int Response(int group)=>groupResponses[group];internal int Offset(int group)=>ceilingOffsets[group];
        internal int AtAge(int age)
        {
            decimal blend=Math.Max(0,Math.Min(1,(decimal)(age-PivotAge)/TransitionYears));
            return CareerUnits.Round(EarlyResponse+(LateResponse-EarlyResponse)*blend);
        }
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Range(Weight,1,10000,"growth style weight");
            if(groupResponses==null||ceilingOffsets==null||groupResponses.Length!=5||ceilingOffsets.Length!=5)throw new SerializationException("Five growth groups required.");
            foreach(int n in groupResponses)CareerUnits.Range(n,1000,20000,"group response");foreach(int n in ceilingOffsets)CareerUnits.Range(n,-20000,20000,"group ceiling offset");
            CareerUnits.Range(AgeShiftMin,-5,5,"age shift");CareerUnits.Range(AgeShiftMax,AgeShiftMin,5,"age shift");
            CareerUnits.Range(EarlyResponse,0,20000,"early response");CareerUnits.Range(LateResponse,0,20000,"late response");
            CareerUnits.Range(PivotAge,16,60,"response pivot");CareerUnits.Range(TransitionYears,1,20,"response transition");
            CareerUnits.Range(RetirementMin,30,60,"retirement age");CareerUnits.Range(RetirementMax,RetirementMin,60,"retirement age");
            CareerUnits.Range(StagnationProbability,0,10000,"stagnation probability");CareerUnits.Range(StagnationFactor,0,10000,"stagnation factor");
        }
    }
    /// <summary>Single authored source for future growth, never race power.</summary>
    [DataContract] public sealed class CareerGrowthDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="substream_salt",IsRequired=true)] public ulong SubstreamSalt {get;private set;}
        [DataMember(Name="substream_stride",IsRequired=true)] public ulong SubstreamStride {get;private set;}
        [DataMember(Name="ability_margin",IsRequired=true)] public int AbilityMargin {get;private set;}
        [DataMember(Name="ceiling_variation",IsRequired=true)] public int CeilingVariation {get;private set;}
        [DataMember(Name="response_variation",IsRequired=true)] public int ResponseVariation {get;private set;}
        [DataMember(Name="decline_response_min",IsRequired=true)] public int DeclineResponseMin {get;private set;}
        [DataMember(Name="decline_response_max",IsRequired=true)] public int DeclineResponseMax {get;private set;}
        [DataMember(Name="learning_factor",IsRequired=true)] public int LearningFactor {get;private set;}
        [DataMember(Name="history_limit",IsRequired=true)] public int HistoryLimit {get;private set;}
        [DataMember(Name="repeated_session_factors",IsRequired=true)] private int[] repetition {get;set;}
        [DataMember(Name="curves",IsRequired=true)] private CareerGrowthCurve[] curves {get;set;}
        [DataMember(Name="stats",IsRequired=true)] private CareerGrowthStatRule[] stats {get;set;}
        [DataMember(Name="talent_bands",IsRequired=true)] private CareerTalentBand[] bands {get;set;}
        [DataMember(Name="styles",IsRequired=true)] private CareerGrowthStyle[] styles {get;set;}
        private CareerGrowthStatRule[] byStat;private int bandWeight,styleWeight;
        public int RepetitionFactor(int priorSessions){if(priorSessions<0)throw new ArgumentOutOfRangeException(nameof(priorSessions));return repetition[Math.Min(priorSessions,repetition.Length-1)];}
        public CareerGrowthStatRule Rule(CareerStat stat){CareerUnits.Range((int)stat,0,12,"growth stat");return byStat[(int)stat];}
        public CareerGrowthStyle Style(string id){foreach(var s in styles)if(s.Id==id)return s;throw new ArgumentException("Unknown growth style.");}
        internal bool HasBand(string id){foreach(var b in bands)if(b.Id==id)return true;return false;}
        internal CareerGrowthStyle DrawStyle(Biathlon.Domain.IRandomSource random)
        {int draw=CareerFactory.Draw(random,1,styleWeight);foreach(var s in styles){draw-=s.Weight;if(draw<=0)return s;}throw new InvalidOperationException();}
        internal CareerTalentBand DrawBand(Biathlon.Domain.IRandomSource random)
        {int draw=CareerFactory.Draw(random,1,bandWeight);foreach(var b in bands){draw-=b.Weight;if(draw<=0)return b;}throw new InvalidOperationException();}
        internal void RequireProfile(CareerAthlete athlete)
        {if(athlete.Growth==null||athlete.Growth.PolicyId!=Id||!HasBand(athlete.Growth.TalentBandId))throw new InvalidOperationException("Missing or mismatched growth profile.");Style(athlete.Growth.StyleId);}
        public int AgeResponse(CareerAthlete athlete,CareerStat stat)
        {
            RequireProfile(athlete);var p=athlete.Growth;var s=Style(p.StyleId);int age=athlete.Age+p.AgeShift;
            decimal value=(decimal)Rule(stat).Curve.Value(age,false)*s.AtAge(age)/10000;
            if(p.IsStagnating(athlete.Age))value=value*s.StagnationFactor/10000;
            return CareerUnits.Round(value);
        }
        public int AnnualDecline(CareerAthlete athlete,CareerStat stat,int newAge)
        {RequireProfile(athlete);var rule=Rule(stat);return CareerUnits.Round((decimal)rule.Curve.Value(newAge+athlete.Growth.AgeShift,true)*rule.DeclineFactor/10000*athlete.Growth.DeclineResponse/10000);}
        public string ToJson()=>CareerJson.Write(this);
        public static CareerGrowthDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerGrowthDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid growth definition.");CareerUnits.Id(d.Id);
            if(d.SubstreamStride==0||d.SubstreamSalt==0)throw new SerializationException("Explicit independent growth substream required.");
            CareerUnits.Range(d.AbilityMargin,0,20000,"ability margin");CareerUnits.Range(d.CeilingVariation,0,20000,"ceiling variation");CareerUnits.Range(d.ResponseVariation,0,5000,"response variation");
            CareerUnits.Range(d.DeclineResponseMin,1000,20000,"decline response");CareerUnits.Range(d.DeclineResponseMax,d.DeclineResponseMin,20000,"decline response");
            CareerUnits.Range(d.LearningFactor,0,50000,"learning factor");CareerUnits.Range(d.HistoryLimit,2,100,"development history limit");
            if(d.repetition==null||d.repetition.Length<1||d.repetition.Length>14)throw new SerializationException("Invalid repetition schedule.");
            int last=10000;foreach(int n in d.repetition){CareerUnits.Range(n,0,last,"non-increasing repetition factor");last=n;}
            if(d.repetition[0]!=10000)throw new SerializationException("First training session must have full response.");
            if(d.curves==null||d.curves.Length<1||d.curves.Length>20||d.stats==null||d.stats.Length!=13||d.bands==null||d.bands.Length<1||d.bands.Length>20||d.styles==null||d.styles.Length<1||d.styles.Length>20)throw new SerializationException("Incomplete growth definition.");
            var curveIds=new Dictionary<string,CareerGrowthCurve>(StringComparer.Ordinal);
            foreach(var c in d.curves){if(c==null)throw new SerializationException("Null growth curve.");c.Validate();if(curveIds.ContainsKey(c.Id))throw new SerializationException("Duplicate growth curve.");curveIds.Add(c.Id,c);}
            d.byStat=new CareerGrowthStatRule[13];var groups=new bool[5];
            foreach(var s in d.stats)
            {
                if(s==null)throw new SerializationException("Null growth stat.");CareerUnits.Range((int)s.Stat,0,12,"growth stat");CareerUnits.Range((int)s.Group,0,4,"growth group");
                if(d.byStat[(int)s.Stat]!=null||!curveIds.TryGetValue(s.CurveId,out var curve))throw new SerializationException("Duplicate stat or missing growth curve.");
                CareerUnits.Range(s.LearningFactor,0,50000,"stat learning factor");CareerUnits.Range(s.DeclineFactor,0,20000,"stat decline factor");s.Curve=curve;d.byStat[(int)s.Stat]=s;groups[(int)s.Group]=true;
            }
            foreach(bool present in groups)if(!present)throw new SerializationException("Unused growth group.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in d.bands){if(b==null)throw new SerializationException("Null talent band.");CareerUnits.Id(b.Id);if(!ids.Add(b.Id))throw new SerializationException("Duplicate talent band.");CareerUnits.Range(b.Weight,1,10000,"talent weight");CareerUnits.Range(b.Minimum,1000,100000,"talent bound");CareerUnits.Range(b.Maximum,b.Minimum,100000,"talent bound");d.bandWeight=checked(d.bandWeight+b.Weight);}
            ids.Clear();foreach(var s in d.styles){if(s==null)throw new SerializationException("Null growth style.");s.Validate();if(!ids.Add(s.Id))throw new SerializationException("Duplicate growth style.");d.styleWeight=checked(d.styleWeight+s.Weight);}
            return d;
        }
    }
}
