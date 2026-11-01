using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    /// <summary>Official discipline distance over the approved compressed route.
    /// Penalty loops do not advance this race-distance coordinate.</summary>
    public sealed class RaceDistanceView
    {
        public double TotalMeters {get;}
        public double PhysicalMeters {get;}
        private readonly double[] lapStarts,lapLengths;
        public RaceDistanceView(CompetitionProfile physical,CompetitionProfile official,RaceRoute route=null)
        {
            if(physical==null||official==null)throw new ArgumentNullException();
            if(physical.Format!=official.Format||physical.Category!=official.Category)throw new ArgumentException("Matching discipline/category required.");
            TotalMeters=official.DistanceMeters;PhysicalMeters=route?.Length.Meters??physical.DistanceMeters;
            if(PhysicalMeters<=0)throw new ArgumentException("Positive physical race length required.");
            if(route!=null)
            {
                if(route.LapCount!=physical.LapCount)throw new ArgumentException("Matching lap structure required.");
                lapStarts=new double[route.LapCount];lapLengths=new double[route.LapCount];
                for(int i=0;i<route.LapCount;i++){lapStarts[i]=route.GetLapStart(i).Meters;lapLengths[i]=route.GetLapLength(i).Meters;}
            }
        }
        public double AtRouteMeters(double meters)
        {
            if(double.IsNaN(meters)||double.IsInfinity(meters))throw new ArgumentOutOfRangeException(nameof(meters));
            meters=Math.Max(0,Math.Min(PhysicalMeters,meters));
            if(lapStarts!=null)for(int i=lapStarts.Length-1;i>=0;i--)if(meters>=lapStarts[i])return (i+Math.Min(1,(meters-lapStarts[i])/lapLengths[i]))*TotalMeters/lapStarts.Length;
            return meters/PhysicalMeters*TotalMeters;
        }
        public string Kilometers(double routeMeters,CultureInfo culture)=>
            (AtRouteMeters(routeMeters)/1000).ToString("0.00",culture??throw new ArgumentNullException(nameof(culture)));
        public static string OfficialProfileResource(CompetitionProfile physical)
        {
            if(physical==null)throw new ArgumentNullException(nameof(physical));if(physical.Category==CompetitionCategory.Test)return null;
            string discipline=physical.Format==CompetitionFormat.Sprint?"sprint":physical.Format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
            return discipline+"_standard_"+(physical.Category==CompetitionCategory.Women?"women":"men")+"_v1";
        }
    }
}
