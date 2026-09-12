using System;
namespace Biathlon.Career
{
    public readonly struct CareerAgeingFrame
    {
        public int PreviousAge { get; } public int Age { get; } public int Decline { get; }
        internal CareerAgeingFrame(int previous,int age,int decline){PreviousAge=previous;Age=age;Decline=decline;}
    }
    public static class CareerAgeing
    {
        public static CareerAgeingFrame AdvanceYear(CareerAthlete athlete,CareerAdaptationDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            if(athlete.Age>=120)throw new InvalidOperationException("Athlete age exceeds save domain.");
            int age=athlete.Age+1;int decline=definition.AnnualDecline(age);int previous=athlete.Age;
            for(int i=0;i<CareerUnits.StatCount;i++)athlete.SetStat((CareerStat)i,Math.Max(0,athlete.Stat((CareerStat)i)-decline));
            athlete.Age=age;return new CareerAgeingFrame(previous,age,decline);
        }
    }
}
