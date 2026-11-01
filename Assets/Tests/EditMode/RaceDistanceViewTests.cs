using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Presentation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class RaceDistanceViewTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name+".json");
        [TestCase("sprint",10000,7500)][TestCase("pursuit",12500,10000)][TestCase("mass_start",15000,12500)]
        public void OfficialMenAndWomenDistancesComeFromCanonicalProfiles(string discipline,double men,double women)
        {
            foreach(var pair in new[]{(name:"men",distance:men),(name:"women",distance:women)})
            {
                var official=CompetitionProfile.FromJson(Read(discipline+"_standard_"+pair.name+"_v1"));
                var compressed=CompetitionProfile.FromJson(official.ToJson().Replace("\"distance_m\":"+pair.distance.ToString(CultureInfo.InvariantCulture),"\"distance_m\":4500"));
                string before=compressed.ToJson();var view=new RaceDistanceView(compressed,official);
                Assert.That(view.TotalMeters,Is.EqualTo(pair.distance));Assert.That(view.AtRouteMeters(2250),Is.EqualTo(pair.distance/2));
                Assert.That(view.AtRouteMeters(4500),Is.EqualTo(pair.distance));Assert.That(view.AtRouteMeters(100000),Is.EqualTo(pair.distance));Assert.That(view.AtRouteMeters(-1),Is.Zero);
                Assert.That(compressed.ToJson(),Is.EqualTo(before));Assert.That(RaceDistanceView.OfficialProfileResource(compressed),Is.EqualTo(discipline+"_standard_"+pair.name+"_v1"));
            }
        }
        [Test] public void DistanceFormattingRejectsInvalidInputAndWarmedProgressDoesNotAllocate()
        {
            var p=CompetitionProfile.FromJson(Read("mass_start_standard_men_v1"));var view=new RaceDistanceView(p,p);
            Assert.That(view.Kilometers(1250,CultureInfo.GetCultureInfo("ru-RU")),Is.EqualTo("1,25"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>view.AtRouteMeters(double.NaN));Assert.Throws<ArgumentException>(()=>new RaceDistanceView(p,CompetitionProfile.FromJson(Read("sprint_standard_men_v1"))));
            for(int i=0;i<10;i++)view.AtRouteMeters(1000);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)view.AtRouteMeters(i*10);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}
