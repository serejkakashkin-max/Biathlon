using System;
using System.IO;
using System.Text;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class M166WeatherTests
    {
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Small(string n)=>n=="career_roster_v1.json"?Read(n).Replace("\"population\": 30","\"population\": 2"):Read(n);
        private static string Venue(string n)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json");
        public static RaceWeatherPolicy Policy()=>RaceWeatherPolicy.FromJson(Read("race_weather_policy_v1.json"));
        public static RaceWeatherPreset Preset(int index=0)=>RaceWeatherCatalog.FromJson(Read("career_weather_v1.json")).Get(index);
        [Test] public void FrontsAreGradualSnowLagsAirAndSamplingIsIndependentOfOrderOrAthleteClock()
        {
            var timeline=Policy().CreateScenario(Preset(3),"cooling");var a=timeline.Sample(180);var b=timeline.Sample(300);var c=timeline.Sample(500);var d=timeline.Sample(1200);
            Assert.That(b.AirTemperatureC,Is.LessThan(a.AirTemperatureC));Assert.That(b.SnowTemperatureC,Is.EqualTo(a.SnowTemperatureC));Assert.That(b.SnowBlend,Is.Zero);
            Assert.That(c.SnowTemperatureC,Is.LessThan(a.SnowTemperatureC));Assert.That(d.SnowProfile,Is.EqualTo("icy_fast"));
            string json=timeline.ToJson();var restored=RaceWeatherTimeline.FromJson(json);Assert.That(restored.Sample(300),Is.EqualTo(b));Assert.That(restored.Sample(180),Is.EqualTo(a));Assert.That(restored.ToJson(),Is.EqualTo(json));
            double maxAir=0,maxSnow=0;var prior=timeline.Sample(0);for(int i=1;i<1800;i++){var next=timeline.Sample(i);maxAir=Math.Max(maxAir,Math.Abs(next.AirTemperatureC-prior.AirTemperatureC));maxSnow=Math.Max(maxSnow,Math.Abs(next.SnowTemperatureC-prior.SnowTemperatureC));prior=next;}
            Assert.That(maxAir,Is.LessThan(.04));Assert.That(maxSnow,Is.LessThan(.02));Assert.Catch(()=>timeline.Sample(-1));
        }
        [Test] public void MostSeedsRemainStaticAndNoRaceHasMoreThanTwoFrontsOrAnAllocationPerSample()
        {
            int stable=0;var policy=Policy();for(ulong seed=0;seed<1000;seed++){var a=policy.Create(Preset(),seed);Assert.That(a.ToJson(),Is.EqualTo(policy.Create(Preset(),seed).ToJson()));Assert.That(a.FrontCount,Is.InRange(0,2));if(a.FrontCount==0)stable++;}
            Assert.That(stable,Is.InRange(540,660));var timeline=policy.CreateScenario(Preset(),"two_fronts");var gear=new SkiEquipmentSystem(M166EquipmentTests.Gear().Skis.GetSki("ski_fresh_float"),SnowConditionTests.Catalog());
            gear.Evaluate(timeline.Sample(500));long before=GC.GetAllocatedBytesForCurrentThread();double sum=0;for(int i=0;i<10000;i++)sum+=gear.Evaluate(timeline.Sample(i*.2)).SnowResistanceMultiplier;
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(sum,Is.GreaterThan(0));
        }
        [Test] public void ExistingSnowEffectsComposeOnceAndSkiSpecializationChangesAsSnowEvolves()
        {
            var timeline=Policy().CreateScenario(Preset(2),"warming");var catalog=M166EquipmentTests.Gear().Skis;var snow=SnowConditionTests.Catalog();
            var cold=new SkiEquipmentSystem(catalog.GetSki("ski_cold_race"),snow);var wet=new SkiEquipmentSystem(catalog.GetSki("ski_wet_channel"),snow);
            var begin=timeline.Sample(0);var end=timeline.Sample(1200);Assert.That(end.SnowProfile,Is.EqualTo("wet_slow"));
            Assert.That(cold.Evaluate(begin).SnowResistanceMultiplier,Is.LessThan(wet.Evaluate(begin).SnowResistanceMultiplier));Assert.That(wet.Evaluate(end).SnowResistanceMultiplier,Is.LessThan(cold.Evaluate(end).SnowResistanceMultiplier));
            Assert.That(wet.Evaluate(end).SnowResistanceMultiplier,Is.EqualTo(wet.Evaluate("wet_slow").SnowResistanceMultiplier));
            var middle=timeline.Sample(600);double expected=wet.Evaluate(middle.SnowFrom).SnowResistanceMultiplier*(1-middle.SnowBlend)+wet.Evaluate(middle.SnowTo).SnowResistanceMultiplier*middle.SnowBlend;
            Assert.That(wet.Evaluate(middle).SnowResistanceMultiplier,Is.EqualTo(expected).Within(1e-12));
        }
        [Test] public void ForecastUsesActualDrawnPersonalStartAndOwnedSelectionDoesNotRerollPreparedWeek()
        {
            var d=new CareerDefinitions(Small,"woodland_development_calendar_v1.json");var save=CareerFactory.Create(d.Roster,d.Calendar,"weather_start","Alex","NOR","default",new SeededRandomSource(71000));var provider=new CareerRaceProvider(Small,d,Venue);var e=d.Calendar.Get(0);string before=save.ToJson();
            var prepared=provider.Prepare(save,e,123);Assert.That(provider.PlayerStartSeconds(save,e,123),Is.EqualTo(prepared.Runner.Field.GetAthlete(prepared.PlayerIndex).ScheduledStart.Value/20.0));Assert.That(save.ToJson(),Is.EqualTo(before));
            while(save.Week<e.Week)save=CareerSeasonEngine.AdvanceWeek(save,d,Array.Empty<CareerTrainingKind>(),nullProvider,s=>new SeededRandomSource(s));
            var week=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));var selected=week.SelectOwnedSkis(d.Equipment,"ski_neutral");
            Assert.That(selected.RaceSeed,Is.EqualTo(week.RaceSeed));Assert.That(selected.SourceJson,Is.EqualTo(week.SourceJson));Assert.That(selected.RaceSnapshotJson,Is.EqualTo(week.RaceSnapshotJson));Assert.Catch(()=>week.SelectOwnedSkis(d.Equipment,"ski_wet_channel"));
        }
        private sealed class NoRaces:ICareerRaceProvider {public CareerRaceRecord Run(CareerSave s,CareerCalendarEvent e,ulong seed)=>throw new InvalidOperationException();}private static readonly ICareerRaceProvider nullProvider=new NoRaces();
        [Test] public void DynamicSignedWorldJournalReplaysExactlyWhileLegacyMetadataUsesFixedWeather()
        {
            var d=new CareerDefinitions(Small,"woodland_development_calendar_v1.json");var save=CareerFactory.Create(d.Roster,d.Calendar,"weather_replay","Alex","NOR","default",new SeededRandomSource(71000));var e=d.Calendar.Get(0);var provider=new CareerRaceProvider(Small,d,Venue);
            string fast=Policy().CreateScenario(provider.Forecast(save,e),"wind_front").ToJson().Replace("\"start_seconds\":180","\"start_seconds\":1");var log=new RaceEventLog(1000);
            CareerPreparedRace Prepare(IRaceEventSink sink=null)=>provider.Prepare(save,e,123,sink,recordedWeather:provider.Forecast(save,e),recordedTimelineJson:fast);
            var source=Prepare(log);var a=Bind(source);byte[] bytes;
            using(var stream=new MemoryStream()){using(var writer=new ManualRaceJournalWriter(stream,a,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json")))){a.CommandObserver=writer;for(int tick=0;tick<100;tick++)source.Runner.Step();writer.Complete();Assert.That(writer.Failure,Is.Null);bytes=stream.ToArray();}}
            var target=Prepare();var b=Bind(target);using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,b,100))while(reader.HasNext)reader.Step();
            Assert.That(b.ContextJson,Is.EqualTo(a.ContextJson));for(int i=0;i<2;i++)Assert.That(b.World.GetActor(i).Pose,Is.EqualTo(a.World.GetActor(i).Pose));
            bool front=false;for(int i=0;i<log.Count;i++)front|=log[i].EventType==RaceEventType.WeatherFrontStarted;Assert.That(front,Is.True);
            var old=provider.Prepare(save,e,123,legacyFixedWeather:true);Assert.That(old.Timeline,Is.Null);Assert.That(old.Runner.RaceWind.EastMetersPerSecond,Is.EqualTo(old.Weather.WindEast));
            ManualScopedFieldStepper Bind(CareerPreparedRace p){var s=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,p.ManualRead,p.CourseReactionPolicy);p.Runner.BindManualScopedCandidate(s,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")));return s;}
        }
        [Test] public void MeasuredTimeTrialsReportStaticWarmingCoolingAndSnowfallOnEachSpecialist()
        {
            var gear=M166EquipmentTests.Gear();var output=new StringBuilder("scenario,ski,seconds\n");
            foreach(string scenario in new[]{"stable","warming","cooling","snow_arrives"})foreach(string id in new[]{"ski_neutral","ski_cold_race","ski_fresh_float","ski_wet_channel","ski_ice_edge"})
            {
                var timeline=Policy().CreateScenario(Preset(scenario=="cooling"?3:2),scenario);var track=SyntheticTrackTests.Generated("synthetic_rolling_v1");var req=CompetitionRouteRequirements.TimeTrial("trial_profile",track.GetLoop("main_long").Length,8);var plan=RaceRoutePlan.TimeTrial("trial_route",track.Id,"main_long",req);
                string Reader(string n)=>n=="ski_equipment_provisional_v1.json"?gear.Skis.ToJson():Read(n);
                var trial=new TimeTrialSession(new SimulationConfig(1,"trial_sim",20,12345),new SkiModelDefinitions(Reader),TimeTrialTests.Athlete(),track,new RaceRoute(track,plan,req),Preset(2).SnowConditions(SnowConditionTests.Conditions()),id,new RaceEventLog(1000));trial.BindWeather(timeline);
                while(!trial.HasFinished&&trial.Clock.Tick.Value<100000)trial.Step(TimeTrialTests.Input(trial,800,tuck:true,sprint:true));Assert.That(trial.HasFinished,Is.True);output.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F3}\n",scenario,id,trial.FinishTime.Seconds);
            }
            var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"AGENTS.md")))root=root.Parent;Assert.That(root,Is.Not.Null);string folder=Path.Combine(root.FullName,"artifacts/m16-6/weather");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"measured-weather-matrix.csv"),output.ToString());
        }
    }
}
