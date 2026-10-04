using System;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ProneMatSupportFieldIntegrationTests
    {
        private sealed class Fixture
        {
            public string Root;
            public CompetitionProfile Profile;
            public TrackDefinition Track;
            public TrackVisualDefinition Visual;
            public RaceRoute Route;
            public PhysicalTrackMap Map;
            public PhysicalRangeFieldLayout Layout;
            public PhysicalJunctionDefinition Junctions;
            public PhysicalConnectionDefinition Connections;
            public PhysicalSharedApronDefinition Apron;
            public VenueRangeLayout Range;
            public SprintEntrant[] Entrants;
            public string Read(string id)=>File.ReadAllText(Path.Combine(Root,"candidate/resources",id+".json"));
        }
        private static Fixture Load(int count)
        {
            DirectoryInfo dir=null;
            foreach(string start in new[]{TestContext.CurrentContext.TestDirectory,Environment.CurrentDirectory})
            {
                var candidate=new DirectoryInfo(start);
                while(candidate!=null&&dir==null)
                {
                    string fixture=Path.Combine(candidate.FullName,"tests/fixtures/venues/shared_apron_support_v1");
                    if(File.Exists(Path.Combine(fixture,"candidate/apron.json")))dir=new DirectoryInfo(fixture);
                    candidate=candidate.Parent;
                }
                if(dir!=null)break;
            }
            Assert.That(dir,Is.Not.Null,"The permanent support fixture is required.");var f=new Fixture{Root=dir.FullName};const string V="Biathlon/Venues/woodland_basin_v1/";
            f.Track=TrackDefinition.FromJson(f.Read(V+"track"));f.Visual=TrackVisualDefinition.FromJson(f.Read(V+"visual"),f.Track);
            // A bounded full three-lap fixture retains actual production compact range geometry.
            // It is not a certified production Sprint40 qualification.
            string profile=PhysiologyContractTests.ReplaceNumber(f.Read(V+"sprint_profile"),"distance_m","9000");
            profile=PhysiologyContractTests.ReplaceNumber(profile,"start_interval_s","0.05");f.Profile=CompetitionProfile.FromJson(profile);
            f.Junctions=PhysicalJunctionDefinition.FromJson(f.Read(V+"junctions"),f.Track);
            f.Connections=PhysicalConnectionDefinition.FromJson(f.Read(V+"connections"),f.Junctions);
            f.Route=new RaceRoute(f.Track,RaceRoutePlan.TimeTrial("compact_field_test_route",f.Track.Id,"main",f.Profile.ToRouteRequirements()),f.Profile.ToRouteRequirements());
            f.Map=new PhysicalTrackMap(f.Track,f.Route);
            var paths=PhysicalRangePathDefinition.FromJson(f.Read(V+"paths"),f.Track,f.Junctions,f.Connections);
            f.Layout=new PhysicalRangeFieldLayout(f.Map,f.Track,f.Route,paths);
            f.Range=VenueManifest.FromJson(f.Read(V+"manifest")).Range;
            var body=OccupancyTests.Definition();
            f.Apron=PhysicalSharedApronDefinition.FromJson(File.ReadAllText(Path.Combine(f.Root,"candidate/apron.json")),f.Track,f.Visual,paths,f.Junctions,f.Connections,body,f.Read);
            f.Entrants=new SprintEntrant[count];var ski=TimeTrialTests.Athlete();var shoot=ShootingSessionTests.Athlete();
            for(int i=0;i<count;i++)
            {
                string id="compact_field_"+i;
                f.Entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id)),
                    ShootingAthleteDefinition.FromJson(shoot.ToJson().Replace(shoot.Id,id)),"ski_neutral");
            }
            return f;
        }
        private static PackSprintFieldSession Create(Fixture f,RaceEventLog events,bool enabled=true,PhysicalSharedApronDefinition definition=null)
            =>new PackSprintFieldSession(new SimulationConfig(1,"compact_field_sim",20,123),f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),f.Track,f.Route,
                RaceSnowConditions.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/race_snow_neutral_v1.json")),SprintResultsTests.Timing(),f.Entrants,
                new SimulationTick(1),"compact_field_result",OccupancyTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),
                f.Junctions,f.Connections,events,rangeLayout:f.Layout,reusableRangeMats:true,routeBoundJunctions:true,
                sharedApron:enabled?definition??f.Apron:null,sharedApronVisual:enabled?f.Visual:null,sharedApronRange:enabled?f.Range:null);
        private static FieldInfo Member(Type type,string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        private static void Equal(PackSprintFieldSession a,PackSprintFieldSession b)
        {
            Assert.That(a.Tick,Is.EqualTo(b.Tick));Assert.That(a.PrecisionFallbackTicks,Is.EqualTo(b.PrecisionFallbackTicks));
            Assert.That(a.SharedRangeApronPairProofs,Is.EqualTo(b.SharedRangeApronPairProofs));
            for(int i=0;i<a.Count;i++)
            {
                Assert.That(a.GetAthlete(i).LastFrame,Is.EqualTo(b.GetAthlete(i).LastFrame));
                var p=a.GetPhysicalPoint(i);var q=b.GetPhysicalPoint(i);
                Assert.That(p.LoopId,Is.EqualTo(q.LoopId));Assert.That(p.LocalNanometers,Is.EqualTo(q.LocalNanometers));
                Assert.That(p.LoopLengthNanometers,Is.EqualTo(q.LoopLengthNanometers));Assert.That(p.Closed,Is.EqualTo(q.Closed));
                Assert.That(a.GetLateral(i),Is.EqualTo(b.GetLateral(i)));
                Assert.That(a.GetJunctionWaitSince(i),Is.EqualTo(b.GetJunctionWaitSince(i)));Assert.That(a.IsOnCourse(i),Is.EqualTo(b.IsOnCourse(i)));
                Assert.That(a.GetDraft(i),Is.EqualTo(b.GetDraft(i)));Assert.That(a.GetAssignedRangeLane(i),Is.EqualTo(b.GetAssignedRangeLane(i)));
                Assert.That(a.IsWaitingForRangeCapacity(i),Is.EqualTo(b.IsWaitingForRangeCapacity(i)));
                for(int bout=0;bout<a.Profile.BoutCount;bout++)Assert.That(a.RangeReservations.GetRecord(i,bout),Is.EqualTo(b.RangeReservations.GetRecord(i,bout)));
            }
        }
        [TestCase(2)] [TestCase(3)]
        public void ActualCompactBodiesCompleteRepeatAndPersistedReplay(int count)
        {
            var f=Load(count);var events=new RaceEventLog(250000);var source=Create(f,events);
            var repeated=Create(Load(count),new RaceEventLog(250000));var inputs=new SprintAthleteInput[count];var lines=new double[count];
            var tape=new PackSprintRaceCommandTape("compact_field_tape",source,60000);bool mat=false,exit=false,oldTail=false;
            while(!source.IsComplete&&source.Tick.Value<60000)
            {
                PackSprintFieldTests.Fill(source,inputs,800);tape.StepAndRecord(source,inputs,lines);repeated.Step(inputs,lines);Equal(source,repeated);
                for(int i=0;i<count;i++)
                {
                    var p=source.GetPhysicalPoint(i);mat|=p.LoopId.StartsWith("mat_",StringComparison.Ordinal);exit|=p.LoopId=="exit_aisle";
                    var tails=(Array)Member(typeof(PackSprintFieldSession),"tails").GetValue(source);var tail=tails.GetValue(i);
                    oldTail|=(bool)Member(tail.GetType(),"Active").GetValue(tail)&&(int)Member(tail.GetType(),"ApronOccurrence").GetValue(tail)>=0;
                }
            }
            if(!source.IsComplete)
            {
                var regionMap=new PhysicalJunctionMap(f.Map,f.Junctions,PhysicalJunctionMap.CaptureUsage(f.Track,f.Route,f.Layout,null));
                var apronMap=new PhysicalSharedApronMapV2(f.Map,f.Track,f.Route,f.Profile,f.Layout,f.Visual,f.Junctions,f.Connections,OccupancyTests.Definition(),f.Apron,.05,2*SkiState.NanometersPerMeter);
                for(int i=0;i<count;i++)TestContext.Out.WriteLine(i+": "+source.GetPhysicalPoint(i).LoopId+" "+source.GetPhysicalPoint(i).LocalNanometers+" "+source.GetAthlete(i).CurrentRangeMotion?.Phase);
                for(int r=0;r<f.Junctions.Count;r++)if(regionMap.TouchesForwardSweep(r,source.GetPhysicalPoint(0),0,750000000)&&regionMap.TouchesForwardSweep(r,source.GetPhysicalPoint(1),0,750000000))
                {
                    int candidate=apronMap.FindRegionForJunction(r);
                    var a=new PhysicalOccupancySweep(source.GetAthlete(0).AthleteId,source.GetPhysicalPoint(0),0,0,0);
                    var b=new PhysicalOccupancySweep(source.GetAthlete(1).AthleteId,source.GetPhysicalPoint(1),0,0,0);
                    TestContext.Out.WriteLine(f.Junctions.Get(r).Id+" candidate="+candidate+" stationaryBodyConflict="+(candidate<0||apronMap.Conflicts(candidate,a,b)));
                }
            }
            Assert.That(source.IsComplete,Is.True);Assert.That(mat&&exit&&oldTail,Is.True);Assert.That(source.SharedRangeApronPairProofs,Is.GreaterThan(0));
            Assert.That(source.Result.ToJson(),Is.EqualTo(repeated.Result.ToJson()));
            var targetLog=new RaceEventLog(250000);var target=Create(Load(count),targetLog);
            var persisted=PackSprintRaceCommandTape.FromJson(tape.ToJson());var replay=persisted.CreateReplay(target);
            var replayRepeat=Create(Load(count),new RaceEventLog(250000));
            while(replay.HasNext){PackSprintFieldTests.Fill(replayRepeat,inputs,800);replayRepeat.Step(inputs,lines);replay.Step();Equal(replayRepeat,target);}
            Equal(source,target);Assert.That(source.Result.ToJson(),Is.EqualTo(target.Result.ToJson()));Assert.That(events.Count,Is.EqualTo(targetLog.Count));
            for(int i=0;i<events.Count;i++)Assert.That(events[i],Is.EqualTo(targetLog[i]));
            Assert.Throws<ArgumentException>(()=>persisted.CreateReplay(Create(Load(count),new RaceEventLog(100),enabled:false)));
        }
        [Test] public void FailedLastCommandPreservesAllCommittedRangeAndBodyState()
        {
            var f=Load(3);var events=new RaceEventLog(100000);var source=Create(f,events);var other=Create(Load(3),new RaceEventLog(100000));
            var inputs=new SprintAthleteInput[3];var lines=new double[3];
            for(int n=0;n<15000&&source.SharedRangeApronPairProofs==0;n++){PackSprintFieldTests.Fill(source,inputs,800);source.Step(inputs,lines);other.Step(inputs,lines);}
            Assert.That(source.SharedRangeApronPairProofs,Is.GreaterThan(0));var tick=source.Tick;int count=events.Count;
            var tails=(Array)((Array)Member(typeof(PackSprintFieldSession),"tails").GetValue(source)).Clone();
            PackSprintFieldTests.Fill(source,inputs,800);lines[2]=double.NaN;Assert.Throws<ArgumentOutOfRangeException>(()=>source.Step(inputs,lines));
            Assert.That(source.Tick,Is.EqualTo(tick));Assert.That(events.Count,Is.EqualTo(count));Equal(source,other);
            for(int i=0;i<3;i++)Assert.That(((Array)Member(typeof(PackSprintFieldSession),"tails").GetValue(source)).GetValue(i),Is.EqualTo(tails.GetValue(i)));
            lines[2]=0;PackSprintFieldTests.Fill(source,inputs,800);source.Step(inputs,lines);other.Step(inputs,lines);Equal(source,other);
        }
        [Test] public void SameIdChangedBodyRejectsOptionalAuthorityBeforeAnyRosterEvent()
        {
            var f=Load(2);var events=new RaceEventLog(100);var changed=OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(OccupancyTests.Definition().ToJson(),"body_length_m","1.6"));
            Assert.Throws<ArgumentException>(()=>new PackSprintFieldSession(new SimulationConfig(1,"bad_compact",20,123),f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),f.Track,f.Route,
                RaceSnowConditions.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/race_snow_neutral_v1.json")),SprintResultsTests.Timing(),f.Entrants,
                new SimulationTick(1),"bad_result",changed,DraftingTests.Definition(),OvertakeTests.Definition(),f.Junctions,f.Connections,events,
                rangeLayout:f.Layout,reusableRangeMats:true,routeBoundJunctions:true,sharedApron:f.Apron,sharedApronVisual:f.Visual));
            Assert.That(events.Count,Is.Zero);
        }
        [TestCase(-.30,-.14)] [TestCase(.12,.02)]
        public void ProneSignedSupportFailureRetainsPaidStopUntilOrdinarySteeringFits(double requested,double accepted)
        {
            var f=Load(1);var field=Create(f,new RaceEventLog(100000));
            var gate=new PhysicalRangeMatAdmissionMap(f.Track,f.Route,f.Profile,f.Layout,f.Visual,OccupancyTests.Definition(),
                f.Range.MatDefinition,f.Range.Forward.X,f.Range.Forward.Z,.05);
            var inputs=new SprintAthleteInput[1];var lines=new[]{requested};bool held=false;
            for(int n=0;n<25000&&!held;n++)
            {
                PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                var actor=field.GetAthlete(0);int path=actor.RangePath.LaneOrdinal-1;long stop=gate.FiringRouteNanometers(path,0);
                held=actor.RouteNanometers>=stop-SkiState.NanometersPerMillimeter&&actor.RouteNanometers<stop&&actor.Motion.SpeedMetersPerSecond==0;
                if(!gate.ContainsBodyAndSupports(path,0,field.GetLateral(0)))
                    Assert.That(actor.CurrentRangeMotion?.Phase,Is.Not.EqualTo(RangeTraversalPhase.Shooting));
            }
            Assert.That(held,Is.True);Assert.That(field.GetLateral(0),Is.EqualTo(requested));
            int chosen=field.GetAthlete(0).RangePath.LaneOrdinal-1;
            Assert.That(gate.ContainsBody(chosen,requested),Is.True,"The unchanged body fits; actual prone supports reject.");
            Assert.That(gate.ContainsBodyAndSupports(chosen,0,requested),Is.False);
            lines[0]=accepted;bool reached=false;double step=OvertakeTests.Definition().LateralSpeed/field.TickFrequency;
            for(int n=0;n<1000&&!reached;n++)
            {
                double previous=field.GetLateral(0);PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                Assert.That(Math.Abs(field.GetLateral(0)-previous),Is.LessThanOrEqualTo(step+1e-9));
                reached=field.GetAthlete(0).CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                if(reached)Assert.That(gate.ContainsBodyAndSupports(chosen,0,field.GetLateral(0)),Is.True);
            }
            Assert.That(reached,Is.True);Assert.That(field.GetLateral(0),Is.Not.EqualTo(requested));
        }
        [TestCase(-.30)] [TestCase(.12)]
        public void ActualStandingArrivalPreservesTheExistingSignedBodyPositions(double requested)
        {
            var f=Load(1);var field=Create(f,new RaceEventLog(100000));
            var gate=new PhysicalRangeMatAdmissionMap(f.Track,f.Route,f.Profile,f.Layout,f.Visual,OccupancyTests.Definition(),
                f.Range.MatDefinition,f.Range.Forward.X,f.Range.Forward.Z,.05);
            var inputs=new SprintAthleteInput[1];var lines=new double[1];bool standing=false;
            for(int n=0;n<30000&&!standing;n++)
            {
                var actor=field.GetAthlete(0);lines[0]=actor.ActiveBout==1?requested:0;
                PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                standing=actor.ActiveBout==1&&actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                if(standing)
                {
                    int path=actor.RangePath.LaneOrdinal-1;Assert.That(field.GetLateral(0),Is.EqualTo(requested));
                    Assert.That(gate.ContainsBody(path,requested),Is.True);
                    Assert.That(gate.ContainsBodyAndSupports(path,1,requested),Is.True);
                    Assert.That(gate.ContainsBodyAndSupports(path,0,requested),Is.False);
                }
            }
            Assert.That(standing,Is.True,"Standing must retain the physically valid old body footprint.");
        }
        [Test] public void OffMatPlayerStopsBeforeShootingThenPaysGradualCentering()
        {
            var f=Load(1);var log=new RaceEventLog(100000);var field=Create(f,log);
            var gate=new PhysicalRangeMatAdmissionMap(f.Track,f.Route,f.Profile,f.Layout,f.Visual,OccupancyTests.Definition(),
                f.Range.MatDefinition,f.Range.Forward.X,f.Range.Forward.Z,.05);
            var inputs=new SprintAthleteInput[1];var lines=new[]{.8};bool held=false;
            for(int n=0;n<25000&&!held;n++)
            {
                PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                var actor=field.GetAthlete(0);int path=actor.RangePath.LaneOrdinal-1;
                long stop=gate.FiringRouteNanometers(path,0);
                held=actor.RouteNanometers>=stop-SkiState.NanometersPerMillimeter&&actor.RouteNanometers<stop&&actor.Motion.SpeedMetersPerSecond==0;
                if(!gate.ContainsBody(path,field.GetLateral(0)))Assert.That(actor.CurrentRangeMotion?.Phase,Is.Not.EqualTo(RangeTraversalPhase.Shooting));
            }
            Assert.That(held,Is.True,"The actual off-mat athlete must stop before shooting publication.");
            lines[0]=0;bool reached=false;double step=OvertakeTests.Definition().LateralSpeed/field.TickFrequency;
            for(int n=0;n<1000&&!reached;n++)
            {
                double previous=field.GetLateral(0);PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                Assert.That(Math.Abs(field.GetLateral(0)-previous),Is.LessThanOrEqualTo(step+1e-9));
                var actor=field.GetAthlete(0);reached=actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                if(reached)Assert.That(gate.ContainsBody(actor.RangePath.LaneOrdinal-1,field.GetLateral(0)),Is.True);
            }
            Assert.That(reached,Is.True,"Ordinary paid steering must allow a valid physical shooting position.");
        }
    }
}


