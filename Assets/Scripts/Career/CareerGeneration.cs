using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerGenerationDefinition
    {
        private CareerGenerationDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="retirement_age",IsRequired=true)] public int RetirementAge { get; private set; }
        [DataMember(Name="prospect_age_min",IsRequired=true)] public int ProspectAgeMin { get; private set; }
        [DataMember(Name="prospect_age_max",IsRequired=true)] public int ProspectAgeMax { get; private set; }
        [DataMember(Name="prospect_skill_min",IsRequired=true)] public int ProspectSkillMin { get; private set; }
        [DataMember(Name="prospect_skill_max",IsRequired=true)] public int ProspectSkillMax { get; private set; }
        public static CareerGenerationDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerGenerationDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid generation schema.");
            CareerUnits.Id(d.Id);CareerUnits.Range(d.RetirementAge,20,119,"retirement age");
            CareerUnits.Range(d.ProspectAgeMin,16,d.RetirementAge-1,"prospect age");CareerUnits.Range(d.ProspectAgeMax,d.ProspectAgeMin,d.RetirementAge-1,"prospect age");
            CareerUnits.Range(d.ProspectSkillMin,0,CareerUnits.MaximumStat,"prospect skill");CareerUnits.Range(d.ProspectSkillMax,d.ProspectSkillMin,CareerUnits.MaximumStat,"prospect skill");return d;
        }
    }
    public sealed class CareerGenerationResult
    {
        public CareerSave Save { get; }
        private readonly string[] retired;
        public int RetiredCount=>retired.Length;
        public string RetiredId(int index)=>retired[index];
        internal CareerGenerationResult(CareerSave save,List<string> ids){Save=save;retired=ids.ToArray();}
    }
    public static class CareerGeneration
    {
        // Run after current-season results have been archived. Returns a new save;
        // a failed transition cannot change the caller's roster or RNG checkpoint.
        public static CareerGenerationResult ReplaceRetirees(CareerSave source,CareerRosterDefinition roster,CareerGenerationDefinition definition,Func<ulong,IRandomSource> restoreRandom,CareerGrowthDefinition growth=null,BiathlonRatingDefinition ratings=null)
        {
            if(source==null||roster==null||definition==null||restoreRandom==null)throw new ArgumentNullException();
            source.Validate();
            if(growth!=null&&ratings==null)throw new ArgumentNullException(nameof(ratings));
            int Retirement(CareerAthlete a)=>growth==null?definition.RetirementAge:a.Growth?.RetirementAge??throw new InvalidOperationException("Individual generation requires saved growth profiles.");
            if(source.RulesId!=roster.Id||source.AthleteCount!=roster.Population||source.RaceCount!=0)throw new ArgumentException("Generation requires a matching roster and archived season results.");
            int count=0;for(int i=0;i<source.AthleteCount;i++)if(source.Athlete(i).Id!=source.PlayerId&&source.Athlete(i).Age>=Retirement(source.Athlete(i)))count++;
            checked{long next=source.NextAthleteOrdinal+count;}
            var save=CareerSave.FromJson(source.ToJson());var random=restoreRandom(save.RandomState);
            if(random==null||random.State!=save.RandomState)throw new ArgumentException("Random checkpoint mismatch.");
            var retired=new List<string>();
            for(int i=0;i<save.AthleteCount;i++)
            {
                var old=save.Athlete(i);if(old.Id==save.PlayerId||old.Age<Retirement(old))continue;
                var replacement=CareerFactory.Generate(roster,save.Id,save.NextAthleteOrdinal++,random,age:CareerFactory.Draw(random,definition.ProspectAgeMin,definition.ProspectAgeMax));
                for(int stat=0;stat<CareerUnits.StatCount;stat++)replacement.SetStat((CareerStat)stat,Math.Min(replacement.Potential,CareerFactory.Draw(random,definition.ProspectSkillMin,definition.ProspectSkillMax)));
                retired.Add(old.Id);save.Athletes[i]=replacement;
            }
            save.RandomState=random.State;
            if(growth!=null)CareerDevelopment.InitializeWorld(save,growth,ratings,restoreRandom);
            save.Validate();return new CareerGenerationResult(save,retired);
        }
    }
}
