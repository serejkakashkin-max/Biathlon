using System;
namespace Biathlon.Career
{
    public readonly struct CareerAgeingFrame
    {
        public int PreviousAge { get; } public int Age { get; } public int Decline { get; }
        public int TotalDecline {get;}
        internal CareerAgeingFrame(int previous,int age,int decline,int total){PreviousAge=previous;Age=age;Decline=decline;TotalDecline=total;}
    }
    public static class CareerAgeing
    {
        public static CareerAgeingFrame AdvanceYear(CareerAthlete athlete,CareerAdaptationDefinition definition,CareerGrowthDefinition growth=null)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            if(athlete.Age>=120)throw new InvalidOperationException("Athlete age exceeds save domain.");
            int age=athlete.Age+1;int decline=definition.AnnualDecline(age);int previous=athlete.Age;
            var values=new int[CareerUnits.StatCount];int total=0,maximum=0;if(growth!=null)growth.RequireProfile(athlete);
            for(int i=0;i<CareerUnits.StatCount;i++)
            {int loss=growth==null?decline:growth.AnnualDecline(athlete,(CareerStat)i,age);int current=athlete.Stat((CareerStat)i);values[i]=Math.Max(0,current-loss);total+=current-values[i];maximum=Math.Max(maximum,loss);}
            for(int i=0;i<CareerUnits.StatCount;i++)athlete.SetStat((CareerStat)i,values[i]);
            athlete.Age=age;return new CareerAgeingFrame(previous,age,maximum,total);
        }
    }
}
