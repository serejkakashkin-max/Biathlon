using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ProductionVenueRaceFactoryTests
    {
        private static CompetitionProfile Profile()=>CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
            CompetitionProfileTests.TestProfile().ToJson(),"distance_m","480"));
        private static RaceSnowConditions Conditions()=>RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed"));
        private static RaceRoutePlan Route(VenueAuthoringPlan venue,CompetitionProfile profile)
            =>RaceRoutePlan.TimeTrial("authored_factory_fixture_route_v1",venue.Track.Id,"main",profile.ToRouteRequirements());
        private static VenueAuthoringPlan CurvedPrivateFixture()
        {
            var resources=VenuePathAuthoringTests.Resources();
            resources["track"]=Regex.Replace(resources["track"],"(\"id\":\"mat\"[\\s\\S]*?\"curvature_per_m\":)0", "${1}0.08");
            return VenuePathAuthoringTests.Plan(resources);
        }
        private static VenueAuthoringPlan ProductionPlan(bool historical=false)
        {
            // Historical60 is an explicit detached stress authority, never an imported profile.
            string Read(string id)
            {
                var value=PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
                if(historical&&(id.EndsWith("/sprint_profile",StringComparison.Ordinal)||id.EndsWith("/pursuit_profile",StringComparison.Ordinal)))
                    value=PhysiologyContractTests.ReplaceNumber(value,"field_capacity","60");
                return value;
            }
            return new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
        }
        [Test] public void VenueTrafficIsExplicitBoundAndWidensOnlyItsObservedNeighborhood()
        {
            var venue=ProductionPlan();Assert.That(venue.AiRangeTraffic,Is.Not.Null);
            var manifest=venue.Manifest.ToJson();Assert.That(VenueManifest.FromJson(manifest).ToJson(),Is.EqualTo(manifest));
            foreach(string invalid in new[]{"../invalid","Biathlon/invalid.json",""})
                Assert.Catch(()=>VenueManifest.FromJson(manifest.Replace(venue.Manifest.AiRangeTrafficResource.Replace("/","\\/"),invalid)));
            var race=venue.Formats.Get(0);
            var runner=ProductionVenueRaceFactory.Create(venue,race.Profile,race.Route,
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,
                new RaceEventLog(5000),new AiDecisionCounts(),count:1);
            Assert.That(runner.ContextJson,Does.Contain("range_traffic_observed_radius_m:75"));
            Assert.That(runner.ContextJson,Does.Contain("woodland_basin_range_traffic_v1"));
            string Missing(string id){if(id==venue.Manifest.AiRangeTrafficResource)throw new InvalidOperationException("Missing explicit traffic resource.");return PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");}
            Assert.Throws<InvalidOperationException>(()=>new VenueAuthoringPlan(venue.Manifest,Missing));
            var tooWide=AiRangeTrafficPolicyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(venue.AiRangeTraffic.ToJson(),"alignment_lead_m","76"));
            Assert.That(Assert.Throws<ArgumentException>(()=>new PackAiRaceRunner(runner.Field,PhysiologyContractTests.DefinitionJson,
                new[]{AiPersonalityTests.Definition("attacker")},AiDifficultyTests.Definition("expert"),123,new RaceEventLog(),new AiDecisionCounts(),rangeTrafficPolicy:tooWide)).Message,Does.Contain("periodic"));
        }
        private static PursuitQualificationSnapshot ContractQualification(CompetitionProfile sprint,CompetitionProfile pursuit,AthleteId[] ids)
        {
            // Synthetic completed lifecycle rows test the qualification/factory contract only.
            // No movement/shooting simulation, actual race completion or production result is claimed.
            var source=new SprintResultBook(sprint,SprintResultsTests.Timing(),ids,20,new RaceEventLog());
            for(int i=0;i<ids.Length;i++)
            {
                var life=new CompetitionLifecycle(sprint,ids[i],20,new RaceEventLog(40));
                life.Step(new SimulationTick(1),CompetitionAction.Start);
                life.Step(new SimulationTick(2),CompetitionAction.EnterRange);
                life.Step(new SimulationTick(3),CompetitionAction.CompleteBout,31,5);
                life.Step(new SimulationTick(4),CompetitionAction.ReachLapBoundary);
                life.Step(new SimulationTick(5),CompetitionAction.EnterRange);
                life.Step(new SimulationTick(6),CompetitionAction.CompleteBout,31,5);
                life.Step(new SimulationTick(7),CompetitionAction.ReachLapBoundary);
                long finish=201+i;for(long tick=8;tick<finish;tick++)life.Step(new SimulationTick(tick));
                life.Step(new SimulationTick(finish),CompetitionAction.Finish,finish:new FinishTime(finish,0,20));source.Record(life);
            }
            return PursuitQualificationSnapshot.Create("production_contract_qualification",source.Complete("production_contract_source"),pursuit);
        }

        [Test] public void ExplicitPathAiRaceCompletesAndItsPersistedCommandsReplayWithoutAi()
        {
            // A one-lane static/runtime fixture proves composition, not production capacity or art.
            var venue=CurvedPrivateFixture();var profile=Profile();var route=Route(venue,profile);
            var log=new RaceEventLog(10000);var replayLog=new RaceEventLog(10000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,route,Conditions(),PhysiologyContractTests.DefinitionJson,log,new AiDecisionCounts(),count:1);
            var rebuilt=CurvedPrivateFixture();var replay=ProductionVenueRaceFactory.Create(rebuilt,profile,Route(rebuilt,profile),Conditions(),
                PhysiologyContractTests.DefinitionJson,replayLog,new AiDecisionCounts(),count:1).Field;
            var tape=new PackSprintRaceCommandTape("authored_factory_commands_v1",replay,50000);
            bool privatePath=false,nonzeroCurvature=false;int snapshots=0;
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<50000)
            {
                runner.Step();var input=new[]{runner.GetLastInput(0)};var lateral=new[]{runner.GetLastLateral(0)};
                tape.StepAndRecord(replay,input,lateral);
                var a=runner.Field.GetAthlete(0);var b=replay.GetAthlete(0);
                Assert.That(a.LastFrame,Is.EqualTo(b.LastFrame));Assert.That(runner.Field.GetLateral(0),Is.EqualTo(replay.GetLateral(0)));
                var point=runner.Field.GetPhysicalPoint(0);privatePath|=point.LoopId=="mat";
                if(point.LoopId=="mat")
                {
                    var snapshot=RacePresentationSnapshot.Capture(runner.Field).Get(0);
                    var sample=a.RangePath.SampleRoute(a.RouteNanometers);
                    nonzeroCurvature|=snapshot.CourseCurvaturePerMeter==.08;
                    Assert.That(snapshot.CourseGrade,Is.EqualTo(sample.Grade));Assert.That(snapshot.CourseCurvaturePerMeter,Is.EqualTo(sample.CurvaturePerMeter));snapshots++;
                }
            }
            Assert.That(runner.Field.IsComplete,Is.True);Assert.That(privatePath,Is.True);Assert.That(nonzeroCurvature,Is.True);Assert.That(snapshots,Is.GreaterThan(0));
            Assert.That(runner.Field.Result.ToJson(),Is.EqualTo(replay.Result.ToJson()));
            // Persisted commands reproduce the field's event stream. Decision/controller
            // events belong to the AI layer and are not regenerated by an AI-free replay.
            var fieldEvents=new List<RaceEvent>();
            for(int i=0;i<log.Count;i++)
            {
                var type=log[i].EventType;
                if(type==RaceEventType.AiIntentChanged||type==RaceEventType.AiShootingPolicyChanged||type==RaceEventType.AiPursuitTacticsChanged||
                    type==RaceEventType.OvertakeStarted||type==RaceEventType.OvertakeCompleted||type==RaceEventType.OvertakeAborted)continue;
                fieldEvents.Add(log[i]);
            }
            Assert.That(fieldEvents.Count,Is.EqualTo(replayLog.Count));
            for(int i=0;i<fieldEvents.Count;i++)Assert.That(fieldEvents[i],Is.EqualTo(replayLog[i]));
            var decoder=PackSprintRaceCommandTape.FromJson(tape.ToJson());
            var decodedVenue=CurvedPrivateFixture();var decoded=ProductionVenueRaceFactory.Create(decodedVenue,profile,Route(decodedVenue,profile),Conditions(),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(10000),new AiDecisionCounts(),count:1).Field;
            var cursor=decoder.CreateReplay(decoded);while(cursor.HasNext)cursor.Step();
            Assert.That(decoded.IsComplete,Is.True);
            Assert.That(decoded.Result.ToJson(),Is.EqualTo(runner.Field.Result.ToJson()));
        }

        [Test] public void LegacyVenueUncoveredRosterAndForeignRouteRejectBeforeRaceCreation()
        {
            var venue=VenuePathAuthoringTests.Plan();var profile=Profile();var route=Route(venue,profile);
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(VenueValidatorTests.Plan(),profile,route,Conditions(),PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:1));
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,profile,route,Conditions(),PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:2));
            var foreign=RaceRoutePlan.FromJson(route.ToJson().Replace(venue.Track.Id,"foreign_track_v1"));
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,profile,foreign,Conditions(),PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:1));
        }

        [Test] public void OpenPrivateCorridorHasNoPeriodicRadiusAmbiguityButClosedCourseStillRejectsIt()
        {
            var fixture=PhysicalRangePackTests.CreateFixture();var field=PhysicalRangePackTests.CreatePack(fixture,new RaceEventLog(4000));
            var source=new PackPerceptionSource(field);var rows=new SkiPlacement[2];var radius=TrackDistance.FromMeters(1000);
            Assert.Throws<ArgumentException>(()=>source.CapturePlacements(0,radius,rows));Assert.That(rows[0].IsValid,Is.False);
            var inputs=new SprintAthleteInput[2];
            do{PackSprintFieldTests.Fill(field,inputs);field.Step(inputs,new double[2]);}while(field.GetPhysicalPoint(0).Closed&&field.Tick.Value<4000);
            Assert.That(field.GetPhysicalPoint(0).Closed,Is.False);Assert.That(field.GetPhysicalPoint(0).LoopId,Is.EqualTo("mat_1"));
            Assert.That(source.CapturePlacements(0,radius,rows),Is.GreaterThan(0));
            Assert.That(rows[0].AthleteId,Is.EqualTo(field.GetAthlete(0).AthleteId));
            for(int i=0;i<100;i++)source.CapturePlacements(0,radius,rows);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)source.CapturePlacements(0,radius,rows);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void ImportedCatalogRejectsAChangedProfileOrRouteBeforeConstructingTheRace()
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            var binding=venue.Formats.Get(0);var profile=binding.Profile;var route=binding.Route;
            var changed=RaceRoutePlan.FromJson(route.ToJson().Replace(route.Id,"changed_catalog_route"));
            Assert.That(Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,profile,changed,Conditions(),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:1)).Message,Does.Contain("catalog"));
            var changedProfile=CompetitionProfile.FromJson(profile.ToJson().Replace(profile.Id,"changed_catalog_profile"));
            Assert.That(Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,changedProfile,route,Conditions(),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:1)).Message,Does.Contain("catalog"));
        }

        [TestCase(40,false)] [TestCase(60,true)] public void ProductionFortyAndDetachedHistoricalSixtyUseThirtyReusableMats(int count,bool historical)
        {
            var venue=ProductionPlan(historical);
            var sprint=venue.Formats.Get(1);var pursuit=venue.Formats.Get(2);
            Assert.That(sprint.Profile.FieldCapacity,Is.EqualTo(count));Assert.That(pursuit.Profile.FieldCapacity,Is.EqualTo(count));
            var conditions=RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json"));
            Assert.That(venue.LaneCount,Is.EqualTo(30));
            var runner=ProductionVenueRaceFactory.Create(venue,sprint.Profile,sprint.Route,conditions,
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:count);
            Assert.That(runner.Field.Count,Is.EqualTo(count));Assert.That(runner.Field.RangeReservations,Is.Not.Null);
            var ids=new AthleteId[count];for(int i=0;i<ids.Length;i++)ids[i]=runner.Field.GetAthlete(i).AthleteId;
            var qualification=ContractQualification(sprint.Profile,pursuit.Profile,ids);
            var chased=ProductionVenueRaceFactory.Create(venue,pursuit.Profile,pursuit.Route,conditions,
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),qualification:qualification,count:count);
            Assert.That(chased.Field.RangeReservations,Is.Not.Null);Assert.That(chased.Field.Count,Is.EqualTo(count));
            for(int i=0;i<ids.Length;i++)Assert.That(chased.Field.GetAthlete(i).AthleteId.Value,Is.EqualTo(qualification.Get(i).AthleteId));
            var mass=venue.Formats.Get(0);
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,mass.Profile,mass.Route,conditions,
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:31));
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,sprint.Profile,sprint.Route,conditions,
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(),new AiDecisionCounts(),count:count+1));
        }
        [TestCase("sprint",41)] [TestCase("pursuit",41)] [TestCase("mass_start",31)]
        public void OversizedProductionRosterRejectsBeforeAnyCoreReadEventOrTelemetry(string format,int count)
        {
            var venue=ProductionPlan();VenueFormatBinding selected=null;
            for(int i=0;i<venue.Formats.Count;i++)if(venue.Formats.Get(i).Id==format)selected=venue.Formats.Get(i);
            Assert.That(selected,Is.Not.Null);var conditions=RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json"));
            PursuitQualificationSnapshot qualification=null;
            if(format=="pursuit")
            {
                var ids=new AthleteId[40];for(int i=0;i<ids.Length;i++)ids[i]=new AthleteId(venue.Manifest.Id+"_athlete_"+i);
                qualification=ContractQualification(venue.Formats.Get(1).Profile,selected.Profile,ids);
                Assert.That(qualification.Count,Is.EqualTo(40),"A valid40 qualification must reach the capacity guard; missing qualification would be an unrelated rejection.");
            }
            var events=new RaceEventLog();var telemetry=new AiDecisionCounts();int reads=0;
            string ForbiddenRead(string resource){reads++;throw new InvalidOperationException("Oversized roster read core resource: "+resource);}
            Assert.Throws<ArgumentException>(()=>ProductionVenueRaceFactory.Create(venue,selected.Profile,selected.Route,conditions,ForbiddenRead,events,telemetry,qualification:qualification,count:count));
            Assert.That(reads,Is.Zero);Assert.That(events.Count,Is.Zero);Assert.That(telemetry.Tactical,Is.Zero);Assert.That(telemetry.Shooting,Is.Zero);
            for(int i=0;i<8;i++)Assert.That(telemetry.IntentCount(i),Is.Zero);for(int i=0;i<=1000;i++)Assert.That(telemetry.EffortCount(i),Is.Zero);
        }
        [Test] public void CurrentCatalogAndBothAuthoritativeRecipesBindFortyMaximumWithThirtyMassLanes()
        {
            var venue=ProductionPlan();Assert.That(venue.Formats.Count,Is.EqualTo(3));Assert.That(venue.LaneCount,Is.EqualTo(30));
            for(int i=0;i<venue.Formats.Count;i++)
            {
                var profile=venue.Formats.Get(i).Profile;Assert.That(profile.FieldCapacity,Is.LessThanOrEqualTo(40));
                Assert.That(profile.FieldCapacity,Is.EqualTo(profile.Format==CompetitionFormat.MassStart?30:40));
            }
            foreach(string recipe in new[]{"woodland_basin","pine_ridge"})
            {
                var json=PhysiologyContractTests.ProjectFile("scripts/content/m13/recipes/"+recipe+"_formats_recipe_v1.json");
                var capacities=Regex.Matches(json,"\\\"field_capacity\\\"\\s*:\\s*(\\d+)");Assert.That(capacities.Count,Is.EqualTo(2));
                foreach(Match value in capacities)Assert.That(value.Groups[1].Value,Is.EqualTo("40"),"Both prepared format recipes must retain the owner limit.");
            }
        }
    }
}
