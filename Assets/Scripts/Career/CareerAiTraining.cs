using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Biathlon.Career
{
    [DataContract] public sealed class CareerTrainingPolicy
    {
        private CareerTrainingPolicy() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="recovery_threshold",IsRequired=true)] public int RecoveryThreshold { get; private set; }
        [DataMember(Name="quality",IsRequired=true)] public int Quality { get; private set; }
        public static CareerTrainingPolicy FromJson(string json)
        {var d=CareerJson.Read<CareerTrainingPolicy>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid training policy.");CareerUnits.Id(d.Id);CareerUnits.Range(d.RecoveryThreshold,0,CareerUnits.Basis,"recovery threshold");CareerUnits.Range(d.Quality,0,CareerUnits.Basis,"quality");return d;}
    }
    public static class CareerAiTraining
    {
        public static CareerWeekPlan Plan(CareerAthlete athlete,CareerTrainingDefinition training,CareerCalendar calendar,CareerTrainingPolicy policy,int week)
        {
            if(athlete==null||training==null||calendar==null||policy==null)throw new ArgumentNullException();athlete.Validate();
            int remaining=calendar.TrainingSlots(week),fatigue=athlete.Fatigue;var sessions=new List<CareerTrainingKind>();
            while(remaining>0)
            {
                CareerTrainingKind selected=CareerTrainingKind.Recovery;long best=-1;
                if(fatigue<policy.RecoveryThreshold)
                {
                    for(int kind=0;kind<5;kind++)
                    {
                        var session=training.Get((CareerTrainingKind)kind);if(session.TimeSlots>remaining)continue;long score=0;
                        for(int stat=0;stat<CareerUnits.StatCount;stat++)score+=(long)session.Gain((CareerStat)stat)*Math.Max(0,athlete.Potential-athlete.Stat((CareerStat)stat));
                        score/=session.TimeSlots;
                        if(score>best){best=score;selected=(CareerTrainingKind)kind;}
                    }
                }
                var chosen=training.Get(selected);if(chosen.TimeSlots>remaining)break;
                sessions.Add(selected);remaining-=chosen.TimeSlots;fatigue=CareerUnits.ClampBasis(fatigue+chosen.FatigueCost-chosen.RecoveryGain);
            }
            return new CareerWeekPlan(training,calendar,week,sessions);
        }
    }
    public static class CareerTrainingWeek
    {
        public static int Apply(CareerAthlete athlete,CareerWeekPlan plan,CareerTrainingDefinition training,CareerAdaptationDefinition adaptation,
            CareerFatigueDefinition fatigue,CareerFormDefinition form,CareerMentalDefinition mental,CareerHealthDefinition health,int quality,bool taper)
        {
            if(athlete==null||plan==null||training==null||adaptation==null||fatigue==null||form==null||mental==null||health==null)throw new ArgumentNullException();
            athlete.Validate();CareerUnits.Range(quality,0,adaptation.MaximumQuality,"quality");checked{int future=athlete.TrainingSessions+plan.Count;}
            // Resolve all sessions before beginning mutation.
            for(int i=0;i<plan.Count;i++)training.Get(plan.Get(i));int gain=0;
            for(int i=0;i<plan.Count;i++)
            {
                var session=training.Get(plan.Get(i));gain+=CareerAdaptation.Apply(athlete,session,adaptation,quality).TotalGain;
                CareerFatigue.Training(athlete,session);CareerHealth.RecordRisk(athlete,health,session.HealthRisk);
            }
            CareerMental.Training(athlete,mental,gain>0);CareerForm.Advance(athlete,form,gain>0,taper);return gain;
        }
    }
}
