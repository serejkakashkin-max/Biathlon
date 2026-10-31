using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerWeatherTests
    {
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Venue(string n)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json");
        private static RaceWeatherCatalog Catalog()=>RaceWeatherCatalog.FromJson(Read("career_weather_v1.json"));
        private static CareerSave Save(CareerDefinitions d)=>CareerFactory.Create(d.Roster,d.Calendar,"weather_test","Alex","NOR","default",new SeededRandomSource(71000));
        [Test] public void CareerCalendarConnectsFogLightSnowAndEveryDevelopedTimeOfDay()
        {
            var catalog=Catalog();var visuals=new HashSet<string>();var lighting=new HashSet<string>();
            for(int i=0;i<catalog.Count;i++){visuals.Add(catalog.Select(i,1).VisualPreset);lighting.Add(catalog.Select(i,1).LightingPreset);}
            Assert.That(visuals,Does.Contain("fog"));Assert.That(visuals,Does.Contain("light_snow"));
            foreach(string id in new[]{"clear_cold","overcast","dawn","late_day","evening"})Assert.That(lighting,Does.Contain(id));
        }
        [Test] public void CatalogHasDistinctPhysicalSnowAndRoundTripsRecordedConditions()
        {
            var catalog=Catalog();var snow=SnowCatalog.FromJson(Read("snow_catalog_provisional_v1.json"));
            var profiles=new HashSet<string>();var visuals=new HashSet<string>();
            for(int i=0;i<catalog.Count;i++)
            {
                var p=catalog.Get(i);profiles.Add(p.SnowProfile);visuals.Add(p.VisualPreset);
                Assert.That(RaceWeatherPreset.FromJson(p.ToJson()).ToJson(),Is.EqualTo(p.ToJson()));
                Assert.That(snow.GetProfile(p.SnowProfile),Is.Not.Null);
                var conditions=p.SnowConditions(RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")));
                Assert.That(conditions.GetSurface(0).ProfileId,Is.EqualTo(p.SnowProfile));
            }
            Assert.That(profiles.Count,Is.EqualTo(4));Assert.That(visuals.Count,Is.EqualTo(5));
        }
        [Test] public void ForecastVariesAcrossExistingCareerEventsAndDoesNotConsumeSavedRandomness()
        {
            var d=new CareerDefinitions(Read,"woodland_career_calendar_v1.json");var s=Save(d);string before=s.ToJson();
            var provider=new CareerRaceProvider(Read,d,Venue);var ids=new HashSet<string>();
            for(int i=0;i<d.Calendar.Count;i++)
            {
                var a=provider.Forecast(s,d.Calendar.Get(i));var b=provider.Forecast(CareerSave.FromJson(before),d.Calendar.Get(i));
                Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));ids.Add(a.Id);
            }
            Assert.That(ids.Count,Is.EqualTo(Catalog().Count));Assert.That(s.ToJson(),Is.EqualTo(before));
            Assert.That(Catalog().Select(0,2).Id,Is.Not.EqualTo(Catalog().Select(0,1).Id));
        }
        [Test] public void PreparedCareerUsesForecastSnowAndSameWindForEveryEntrant()
        {
            var d=new CareerDefinitions(Read,"woodland_career_calendar_v1.json");var s=Save(d);var provider=new CareerRaceProvider(Read,d,Venue);
            var p=provider.Prepare(s,d.Calendar.Get(0),123);
            Assert.That(p.Weather.ToJson(),Is.EqualTo(provider.Forecast(s,d.Calendar.Get(0)).ToJson()));
            var player=p.Runner.Field.GetAthlete(p.PlayerIndex);var tick=p.Runner.Field.Tick.Next();
            p.Runner.StepWithPlayer(p.PlayerIndex,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,player.AthleteId,700),1,1,0),
                new WeaponAimCommand(tick,player.AthleteId,0,0)),0);
            for(int i=0;i<p.Runner.Field.Count;i++)
            {
                Assert.That(p.Runner.Field.GetAthlete(i).SnowConditionsId,Is.EqualTo(p.Weather.Id+"_snow"));
                Assert.That(p.Runner.GetLastInput(i).Wind.EastMetersPerSecond,Is.EqualTo(p.Weather.WindEast));
                Assert.That(p.Runner.GetLastInput(i).Wind.NorthMetersPerSecond,Is.EqualTo(p.Weather.WindNorth));
            }
        }
        [Test] public void RecordedOverrideSurvivesFutureForecastAndLegacyReplayRemainsNeutral()
        {
            var d=new CareerDefinitions(Read,"woodland_career_calendar_v1.json");var s=Save(d);var provider=new CareerRaceProvider(Read,d,Venue);
            var recorded=Catalog().Get(3);var p=provider.Prepare(s,d.Calendar.Get(0),123,recordedWeather:RaceWeatherPreset.FromJson(recorded.ToJson()));
            Assert.That(p.Weather.Id,Is.EqualTo(recorded.Id));p.Runner.Step();
            Assert.That(p.Runner.Field.GetAthlete(0).SnowConditionsId,Is.EqualTo(recorded.Id+"_snow"));
            var legacy=provider.Prepare(s,d.Calendar.Get(0),123,legacyNeutralWeather:true);Assert.That(legacy.Weather,Is.Null);legacy.Runner.Step();
            Assert.That(legacy.Runner.Field.GetAthlete(0).SnowConditionsId,Is.EqualTo("race_snow_neutral_v1"));
            Assert.That(legacy.Runner.RaceWind.EastMetersPerSecond,Is.Zero);
        }
        [Test] public void ReplayMetadataAcceptsOldFilesAndValidatesNewWeather()
        {
            var info=new RaceReplayInfo{Id="weather_replay",RecordedUtc="2026-10-12T12:00:00Z",CareerSnapshotJson="{}",EventId="event",VenueId="woodland_basin_v1",PlayerId="player",PlayerName="Alex",TickFrequency=20,TickCount=20,CommandSha256=new string('a',64),ResultSha256=new string('b',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()};
            Assert.That(RaceReplayInfo.FromJson(info.ToJson()).RaceWeatherJson,Is.Null);
            info.RaceWeatherJson=Catalog().Get(0).ToJson();Assert.That(RaceReplayInfo.FromJson(info.ToJson()).RaceWeatherJson,Is.EqualTo(info.RaceWeatherJson));
            info.RaceWeatherJson="{}";Assert.Throws<SerializationException>(()=>info.ToJson());
        }
        [Test] public void InvalidWeatherAndDuplicateIdsFailBeforeRaceComposition()
        {
            string json=Catalog().Get(0).ToJson();
            Assert.Catch(()=>RaceWeatherPreset.FromJson(json.Replace("1.8","100")));
            Assert.Catch(()=>RaceWeatherPreset.FromJson(json.Replace("\"cold\"","\"unknown\"")));
            Assert.Catch(()=>RaceWeatherCatalog.FromJson(Read("career_weather_v1.json").Replace("clear_frost_v1","snowfall_fresh_v1")));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog().Select(-1,1));
        }
        [TestCase(4),TestCase(5),TestCase(6),TestCase(9)] public void FogAndEveningSnapshotsReplayExactAcceptedManualFrames(int index)
        {
            string Small(string name)=>name=="career_roster_v1.json"?Read(name).Replace("\"population\": 30","\"population\": 2"):Read(name);
            var definitions=new CareerDefinitions(Small,"woodland_career_calendar_v1.json");var save=Save(definitions);string json=save.ToJson();
            var weather=Catalog().Get(index);var provider=new CareerRaceProvider(Small,definitions,Venue);
            CareerPreparedRace Prepare()=>provider.Prepare(CareerSave.FromJson(json),definitions.Calendar.Get(0),123,recordedWeather:RaceWeatherPreset.FromJson(weather.ToJson()));
            ManualScopedFieldStepper Bind(CareerPreparedRace p)
            {
                var stepper=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,p.ManualRead,p.CourseReactionPolicy);
                p.Runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")));
                return stepper;
            }
            var source=Prepare();var stepper=Bind(source);byte[] journal;
            using(var stream=new System.IO.MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(stream,stepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
                {stepper.CommandObserver=writer;for(int i=0;i<120;i++)source.Runner.Step();writer.Complete();Assert.That(writer.Failure,Is.Null);journal=stream.ToArray();}
            }
            var target=Prepare();var playback=Bind(target);
            using(var stream=new System.IO.MemoryStream(journal))using(var reader=new ManualRaceJournalReader(stream,playback,120))while(reader.HasNext)reader.Step();
            for(int i=0;i<source.Runner.Field.Count;i++)
            {
                Assert.That(playback.World.GetActor(i).Pose,Is.EqualTo(stepper.World.GetActor(i).Pose));
                Assert.That(target.Runner.Field.GetAthlete(i).LastFrame,Is.EqualTo(source.Runner.Field.GetAthlete(i).LastFrame));
            }
            Assert.That(save.ToJson(),Is.EqualTo(json));
        }
#if !UNITY_EDITOR
        [Test,Timeout(900000)] public void CompleteSnowfallSprintIsDeterministicAcrossSavedCareerReload()
        {
            var d=new CareerDefinitions(Read,"woodland_career_calendar_v1.json");var save=Save(d);string json=save.ToJson();
            var first=new CareerRaceProvider(Read,d,Venue);var second=new CareerRaceProvider(Read,d,Venue);
            var a=first.Run(save,d.Calendar.Get(0),123);var b=second.Run(CareerSave.FromJson(json),d.Calendar.Get(0),123);
            Assert.That(a.AuthorityResultJson,Is.EqualTo(b.AuthorityResultJson));Assert.That(a.AuthoritySha256,Is.EqualTo(b.AuthoritySha256));
            Assert.That(first.LastEventHash,Is.EqualTo(second.LastEventHash));Assert.That(first.LastTicks,Is.EqualTo(second.LastTicks));
            Assert.That(first.LastShots,Is.EqualTo(save.AthleteCount*10));
            TestContext.WriteLine("weather_sprint_regression hash="+a.AuthoritySha256+" ticks="+first.LastTicks+" shots="+first.LastShots+" event_hash="+first.LastEventHash);
        }
#endif
    }
}
