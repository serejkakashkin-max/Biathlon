using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DirectedPassageFieldTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private const string Policy="Biathlon/Venues/production_venue_validation_v1";
        private static string Number(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
        private static string Knot(double d,double x,double y=0,double z=0)
            =>"{\"distance_m\":"+Number(d)+",\"x\":"+Number(x)+",\"y\":"+Number(y)+",\"z\":"+Number(z)+"}";
        private static string Port(string id,string loop,double anchor,double before,double after)
            =>"{\"id\":\""+id+"\",\"anchor\":{\"loop_id\":\""+loop+"\",\"distance_m\":"+Number(anchor)+"},\"guard_before_m\":"+Number(before)+",\"guard_after_m\":"+Number(after)+"}";
        private static string Region(string id,string a,string b)=>"{\"id\":\""+id+"\",\"ports\":["+a+","+b+"]}";
        private static string Connection(string id,string from,string to)
            =>"{\"id\":\""+id+"\",\"junction_id\":\""+id+"\",\"from_port_id\":\""+from+"\",\"to_port_id\":\""+to+"\",\"kind\":\"coincident_seam\"}";
        private static string Insert(string json,string array,string items)
            =>Regex.Replace(json,"(\""+array+"\"\\s*:\\s*\\[)",m=>m.Value+items+",");
        private static string Corridor(string loop,params string[] knots)=>"{\"loop_id\":\""+loop+"\",\"knots\":["+string.Join(",",knots)+"]}";
        private static string Passage(string id,string source,string target)
            =>"{\"id\":\""+id+"\",\"connection_id\":\""+id+"\",\"source\":"+source+",\"target\":"+target+"}";
        private static string Contract(string track,string route,string a,string b)
            =>"{\"schema_version\":1,\"id\":\"directed_field_test_v1\",\"track_id\":\""+track+"\",\"route_id\":\""+route+"\",\"occupancy_definition_id\":\"occupancy_provisional_v1\",\"validation_policy_resource\":\""+Policy+"\",\"passages\":["+a+","+b+"]}";
        private static string ReadResource(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");

        public static PhysicalRangePackTests.Fixture Fixture(int count=3)
        {
            var f=PhysicalRangeReservationsTests.Fixture(count,pursuit:true);
            const string branch="{\"id\":\"test_shortcut\",\"kind\":\"race\",\"closed\":false,\"segments\":[{\"id\":\"test_straight\",\"length_m\":60,\"start_grade\":0,\"end_grade\":0,\"curvature_per_m\":0,\"width_m\":6,\"snow_profile_id\":\"packed\",\"wind_exposure\":1,\"terrain_muscle_factor\":1,\"draft_allowed\":true,\"overtake_allowed\":true,\"risk_level\":0,\"semantic_zone\":\"flat\"}]}";
            f.Track=TrackDefinition.FromJson(Insert(f.Track.ToJson(),"loops",branch));
            string regions=Region("test_depart",Port("depart_main","main",140,25,25),Port("depart_branch","test_shortcut",0,0,25))+","+
                Region("test_return",Port("return_branch","test_shortcut",60,25,0),Port("return_main","main",200,25,25));
            f.Junctions=PhysicalJunctionDefinition.FromJson(Insert(f.Junctions.ToJson(),"junctions",regions),f.Track);
            string connections=Connection("test_depart","depart_main","depart_branch")+","+Connection("test_return","return_branch","return_main");
            f.Connections=PhysicalConnectionDefinition.FromJson(Insert(f.Connections.ToJson(),"connections",connections),f.Junctions);
            var laps=new string[f.Profile.LapCount];
            for(int i=0;i<laps.Length;i++)laps[i]="{\"id\":\"lap_"+(i+1)+"\",\"slices\":[{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":140},{\"loop_id\":\"test_shortcut\",\"start_m\":0,\"end_m\":60,\"enter_connection_id\":\"test_depart\"},{\"loop_id\":\"main\",\"start_m\":200,\"end_m\":300,\"enter_connection_id\":\"test_return\"}]}";
            var plan=RaceRoutePlan.FromJson("{\"schema_version\":2,\"id\":\"directed_test_route\",\"track_id\":\""+f.Track.Id+"\",\"competition_profile_id\":\""+f.Profile.Id+"\",\"laps\":["+string.Join(",",laps)+"]}");
            f.Route=new RaceRoute(f.Track,plan,f.Profile.ToRouteRequirements(),f.Junctions,f.Connections);f.Map=new PhysicalTrackMap(f.Track,f.Route);
            f.Layout=new PhysicalRangeFieldLayout(f.Map,f.Track,f.Route,PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),f.Track,f.Junctions,f.Connections));
            return f;
        }
        public static string ContractJson(PhysicalRangePackTests.Fixture f)
            =>Contract(f.Track.Id,f.Route.Id,
                Passage("test_depart",Corridor("main",Knot(110,110),Knot(140,140)),Corridor("test_shortcut",Knot(0,140),Knot(30,170))),
                Passage("test_return",Corridor("test_shortcut",Knot(30,170),Knot(60,200)),Corridor("main",Knot(200,200),Knot(230,230))));
        public static PhysicalDirectedPassageDefinition Definition(PhysicalRangePackTests.Fixture f)
            =>PhysicalDirectedPassageDefinition.FromJson(ContractJson(f),f.Track,RaceRoutePlan.FromJson(f.Route.PlanSnapshotJson),f.Junctions,f.Connections,OccupancyTests.Definition(),ReadResource);
        private static PursuitQualificationSnapshot Qualification(PhysicalRangePackTests.Fixture f)
        {
            var ids=PhysicalRangeReservationsTests.Roster(f);var source=new SprintResultBook(CompetitionProfileTests.TestProfile(),SprintResultsTests.Timing(),ids,20,new RaceEventLog(200));
            for(int i=0;i<ids.Length;i++)source.Record(SprintResultsTests.Finisher(ids[i].Value,1,200+20*i));
            return PursuitQualificationSnapshot.Create("directed_qualification",source.Complete("directed_source"),f.Profile);
        }
        public static PackSprintFieldSession Create(PhysicalRangePackTests.Fixture f,RaceEventLog log,PhysicalDirectedPassageDefinition definition)
            =>new PackSprintFieldSession(new SimulationConfig(1,"directed_field",20,123),f.Profile,new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),
                f.Track,f.Route,RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),SprintResultsTests.Timing(),f.Entrants,
                new SimulationTick(1),"directed_result",OccupancyTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),f.Junctions,f.Connections,log,
                qualification:Qualification(f),rangeLayout:f.Layout,reusableRangeMats:true,routeBoundJunctions:true,directedPassages:definition);
        private static FieldInfo Member(Type type,string name)=>type.GetField(name,Private|BindingFlags.Public)??throw new InvalidOperationException(name);
        private static object Trace(PackSprintFieldSession field,string name,int index)=>((Array)Member(typeof(PackSprintFieldSession),name).GetValue(field)).GetValue(index);
        private static void AssertPublicEqual(PackSprintFieldSession a,PackSprintFieldSession b)
        {
            Assert.That(a.Tick,Is.EqualTo(b.Tick));Assert.That(a.PrecisionFallbackTicks,Is.EqualTo(b.PrecisionFallbackTicks));
            for(int i=0;i<a.Count;i++)
            {
                Assert.That(a.GetAthlete(i).LastFrame,Is.EqualTo(b.GetAthlete(i).LastFrame));
                Assert.That(a.GetPhysicalPoint(i).LoopId,Is.EqualTo(b.GetPhysicalPoint(i).LoopId));
                Assert.That(a.GetPhysicalPoint(i).LocalNanometers,Is.EqualTo(b.GetPhysicalPoint(i).LocalNanometers));
                Assert.That(a.GetLateral(i),Is.EqualTo(b.GetLateral(i)));Assert.That(a.GetDraft(i),Is.EqualTo(b.GetDraft(i)));
                Assert.That(a.IsOnCourse(i),Is.EqualTo(b.IsOnCourse(i)));Assert.That(a.GetJunctionWaitSince(i),Is.EqualTo(b.GetJunctionWaitSince(i)));
                Assert.That(a.GetAssignedRangeLane(i),Is.EqualTo(b.GetAssignedRangeLane(i)));Assert.That(a.IsWaitingForRangeCapacity(i),Is.EqualTo(b.IsWaitingForRangeCapacity(i)));
                for(int bout=0;bout<a.Profile.BoutCount;bout++)Assert.That(a.RangeReservations.GetRecord(i,bout),Is.EqualTo(b.RangeReservations.GetRecord(i,bout)));
            }
        }
        private static bool SharedGuard(PackSprintFieldSession field,PhysicalJunctionMap guards,int region)
        {
            for(int i=0;i<field.Count;i++)if(field.IsOnCourse(i))for(int j=0;j<i;j++)if(field.IsOnCourse(j)&&
                guards.TouchesForwardSweep(region,field.GetPhysicalPoint(i),0,750000000)&&guards.TouchesForwardSweep(region,field.GetPhysicalPoint(j),0,750000000))return true;
            return false;
        }

        [Test] public void FullPursuitMatchesDirectQueriesAndPersistedReplayThroughSharedSeamsAndReservations()
        {
            var f=Fixture();var other=Fixture();var events=new RaceEventLog(500000);var directEvents=new RaceEventLog(500000);
            var field=Create(f,events,Definition(f));var direct=Create(other,directEvents,Definition(other));
            Assert.That(field.DirectedPassageCount,Is.EqualTo(2));Assert.That(field.DirectedPassageTickReserveNanometers,Is.GreaterThan(0));
            Assert.That(field.IsDirectedPassageSharingEnabled(0)&&field.IsDirectedPassageSharingEnabled(1),Is.True);
            Member(typeof(PackSprintFieldSession),"bypassJunctionTouchCache").SetValue(direct,true);
            var tape=new PackSprintRaceCommandTape("directed_commands",field,40000);var inputs=new SprintAthleteInput[field.Count];var lines=new double[field.Count];
            var guards=new PhysicalJunctionMap(f.Map,f.Junctions);bool shared=false,crossLoop=false,oldTail=false,privatePath=false;int compared=0;
            while(!field.IsComplete&&field.Tick.Value<40000)
            {
                PackSprintFieldTests.Fill(field,inputs,800);tape.StepAndRecord(field,inputs,lines);direct.Step(inputs,lines);AssertPublicEqual(field,direct);
                Assert.That(events.Count,Is.EqualTo(directEvents.Count));for(;compared<events.Count;compared++)Assert.That(events[compared],Is.EqualTo(directEvents[compared]));
                for(int i=0;i<field.Count;i++)
                {
                    privatePath|=field.GetAssignedRangeLane(i)>0;var trace=Trace(field,"traces",i);var tail=Member(trace.GetType(),"OldTail").GetValue(trace);
                    oldTail|=(bool)Member(tail.GetType(),"Active").GetValue(tail)&&(int)Member(tail.GetType(),"SourceSection").GetValue(tail)>=0&&
                        (int)Member(trace.GetType(),"SeamIndex").GetValue(trace)==-1;
                    if(!field.IsOnCourse(i))continue;
                    for(int j=0;j<i;j++)if(field.IsOnCourse(j))for(int region=0;region<2;region++)
                    {
                        var a=field.GetPhysicalPoint(i);var b=field.GetPhysicalPoint(j);
                        if(!guards.TouchesForwardSweep(region,a,0,750000000)||!guards.TouchesForwardSweep(region,b,0,750000000))continue;
                        shared=true;crossLoop|=a.LoopId!=b.LoopId;
                        double x=a.LocalNanometers/(double)M+(a.LoopId=="test_shortcut"?140:0),y=b.LocalNanometers/(double)M+(b.LoopId=="test_shortcut"?140:0);
                        Assert.That(Math.Abs(x-y)>=1.5||Math.Abs(field.GetLateral(i)-field.GetLateral(j))>=.7,Is.True,"Independent straight-world endpoints overlap.");
                    }
                }
            }
            Assert.That(field.IsComplete,Is.True);Assert.That(shared&&crossLoop&&oldTail&&privatePath,Is.True,"Every new passage/reservation branch must be exercised.");
            Assert.That(field.PursuitResult.ToJson(),Is.EqualTo(direct.PursuitResult.ToJson()));
            var rebuilt=Fixture();var replayEvents=new RaceEventLog(500000);var replayField=Create(rebuilt,replayEvents,Definition(rebuilt));
            var replay=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(replayField);
            while(replay.HasNext)replay.Step();Assert.That(replayField.IsComplete,Is.True);AssertPublicEqual(field,replayField);
            Assert.That(replayField.PursuitResult.ToJson(),Is.EqualTo(field.PursuitResult.ToJson()));Assert.That(replayEvents.Count,Is.EqualTo(events.Count));
            for(int i=0;i<events.Count;i++)Assert.That(replayEvents[i],Is.EqualTo(events[i]));
        }

        [Test] public void AcceptedIncomingTailRetainsConnectionOccurrenceAndRefusesForgedProvenance()
        {
            var f=Fixture(2);var field=Create(f,new RaceEventLog(100000),Definition(f));var inputs=new SprintAthleteInput[2];var lines=new double[2];
            bool witnessed=false;
            while(field.Tick.Value<8000&&!witnessed)
            {
                PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                for(int i=0;i<field.Count;i++)
                {
                    var trace=Trace(field,"traces",i);var tail=Member(trace.GetType(),"OldTail").GetValue(trace);var type=tail.GetType();
                    if(!(bool)Member(type,"Active").GetValue(tail)||(int)Member(type,"SourceSection").GetValue(tail)<0)continue;
                    var check=typeof(PackSprintFieldSession).GetMethod("CanContinuePassage",Private);
                    int connection=(int)Member(type,"ConnectionIndex").GetValue(tail),passage=connection==0?0:1;
                    Assert.That((bool)check.Invoke(field,new[]{trace,(object)i,passage}),Is.True);
                    foreach(string property in new[]{"ConnectionIndex","SourceSection"})
                    {
                        var forged=Member(trace.GetType(),"OldTail").GetValue(trace);Member(type,property).SetValue(forged,-1);Member(trace.GetType(),"OldTail").SetValue(trace,forged);
                        Assert.That((bool)check.Invoke(field,new[]{trace,(object)i,passage}),Is.False,property);
                        Member(trace.GetType(),"OldTail").SetValue(trace,tail);
                    }
                    witnessed=true;break;
                }
            }
            Assert.That(witnessed,Is.True,"A retained tail must survive beyond its seam tick.");
        }

        [Test] public void ForeignPolicyRejectsBeforeRosterEventsAndBadBatchPreservesTailsAndAuthority()
        {
            var f=Fixture();var wrong=Fixture();wrong.Track=TrackDefinition.FromJson(wrong.Track.ToJson().Replace("\"width_m\":6","\"width_m\":5"));
            wrong.Junctions=PhysicalJunctionDefinition.FromJson(wrong.Junctions.ToJson(),wrong.Track);
            wrong.Connections=PhysicalConnectionDefinition.FromJson(wrong.Connections.ToJson(),wrong.Junctions);
            wrong.Route=new RaceRoute(wrong.Track,RaceRoutePlan.FromJson(wrong.Route.PlanSnapshotJson),wrong.Profile.ToRouteRequirements(),wrong.Junctions,wrong.Connections);
            wrong.Map=new PhysicalTrackMap(wrong.Track,wrong.Route);
            wrong.Layout=new PhysicalRangeFieldLayout(wrong.Map,wrong.Track,wrong.Route,PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),wrong.Track,wrong.Junctions,wrong.Connections));
            var events=new RaceEventLog(100000);Assert.Throws<ArgumentException>(()=>Create(wrong,events,Definition(f)));Assert.That(events.Count,Is.Zero);
            var field=Create(f,events,Definition(f));var inputs=new SprintAthleteInput[field.Count];var lines=new double[field.Count];
            for(int i=0;i<3000;i++){PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);}
            var frames=new SprintAthleteFrame[field.Count];for(int i=0;i<frames.Length;i++)frames[i]=field.GetAthlete(i).LastFrame;
            var tails=(Array)((Array)Member(typeof(PackSprintFieldSession),"tails").GetValue(field)).Clone();int count=events.Count;var tick=field.Tick;
            PackSprintFieldTests.Fill(field,inputs,800);lines[lines.Length-1]=double.NaN;Assert.Throws<ArgumentOutOfRangeException>(()=>field.Step(inputs,lines));
            Assert.That(field.Tick,Is.EqualTo(tick));Assert.That(events.Count,Is.EqualTo(count));
            for(int i=0;i<frames.Length;i++){Assert.That(field.GetAthlete(i).LastFrame,Is.EqualTo(frames[i]));Assert.That(((Array)Member(typeof(PackSprintFieldSession),"tails").GetValue(field)).GetValue(i),Is.EqualTo(tails.GetValue(i)));}
        }

        [Test] public void WarmDirectedFieldWithPreallocatedEventsAllocatesNothing()
        {
            var f=Fixture();var field=Create(f,new RaceEventLog(200000),Definition(f));var inputs=new SprintAthleteInput[field.Count];var lines=new double[field.Count];
            var guards=new PhysicalJunctionMap(f.Map,f.Junctions);
            for(int i=0;i<100;i++){PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);}
            while(field.Tick.Value<15000&&!SharedGuard(field,guards,0)&&!SharedGuard(field,guards,1))
            {PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);}
            Assert.That(SharedGuard(field,guards,0)||SharedGuard(field,guards,1),Is.True,"Measure the active passage branch.");
            for(int i=0;i<10;i++){PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<40;i++){PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }

        [Test] public void GuardBoundaryEntryAndExitRemainCoveredWithOngoingOwners()
        {
            var f=Fixture(2);var field=Create(f,new RaceEventLog(100),Definition(f));
            var a=Trace(field,"traces",0);var b=Trace(field,"stationary",1);var type=a.GetType();
            var conflict=typeof(PackSprintFieldSession).GetMethod("Conflict",Private);
            var inputs=new SprintAthleteInput[2];PackSprintFieldTests.Fill(field,inputs,800);
            var prepare=typeof(SprintAthleteSession).GetMethod("PreparePackStep",Private,null,new[]{typeof(SprintAthleteInput),typeof(double),typeof(double),typeof(int)},null);
            var previews=(SprintStepPreview[])Member(typeof(PackSprintFieldSession),"previews").GetValue(field);
            for(int i=0;i<2;i++)previews[i]=(SprintStepPreview)prepare.Invoke(field.GetAthlete(i),new object[]{inputs[i],1.0,1.0,0});
            var projection=(PhysicalMotionProjection)typeof(PackSprintFieldSession).GetMethod("Projection",Private).Invoke(field,new object[]{0});
            // Inclusive guard entry lies strictly inside the captured, tick-reserved window.
            void Set(object trace,string loop,long local,long route)
            {
                var point=f.Map.MapLoop(loop,local);Member(type,"Start").SetValue(trace,point);Member(type,"End").SetValue(trace,point);
                Member(type,"StartRoute").SetValue(trace,route);Member(type,"EndRoute").SetValue(trace,route);
                Member(type,"Count").SetValue(trace,0);
            }
            foreach(var row in new[]{new[]{114250000000L-1,125000000000L},new[]{165750000000L,150000000000L}})
            {
                long local=row[0];string loop=local<140*M?"main":"test_shortcut";long native=local<140*M?local:local-140*M;
                Set(a,loop,native,local);Set(b,row[1]<140*M?"main":"test_shortcut",row[1]<140*M?row[1]:row[1]-140*M,row[1]);
                var movement=(RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment),Private,null,
                    new object[]{local,local+1000000,local,local+1000000,0.0,.05,false,false},null);
                var receipt=projection.Project(movement,f.Map.MapLoop(loop,native));
                ((PhysicalMotionReceipt[])Member(type,"Segments").GetValue(a))[0]=receipt;
                ((long[])Member(type,"RouteBefore").GetValue(a))[0]=local;((long[])Member(type,"RouteAfter").GetValue(a))[0]=local+1000000;
                Member(type,"Count").SetValue(a,1);Member(type,"End").SetValue(a,receipt.End);Member(type,"EndRoute").SetValue(a,local+1000000);
                type.GetMethod("ResetJunctionTouches",Private|BindingFlags.Public).Invoke(a,null);type.GetMethod("ResetJunctionTouches",Private|BindingFlags.Public).Invoke(b,null);
                Assert.That((bool)conflict.Invoke(field,new[]{a,(object)0,b,1}),Is.False);
            }
        }

        [Test] public void ExactGuardOnlyWindowsDisableSharingAndRetainDefaultExclusiveAdmission()
        {
            var f=Fixture(2);
            string json=Contract(f.Track.Id,f.Route.Id,
                Passage("test_depart",Corridor("main",Knot(114.25,114.25),Knot(140,140)),Corridor("test_shortcut",Knot(0,140),Knot(25.75,165.75))),
                Passage("test_return",Corridor("test_shortcut",Knot(34.25,174.25),Knot(60,200)),Corridor("main",Knot(200,200),Knot(225.75,225.75))));
            var shortPolicy=PhysicalDirectedPassageDefinition.FromJson(json,f.Track,RaceRoutePlan.FromJson(f.Route.PlanSnapshotJson),f.Junctions,f.Connections,OccupancyTests.Definition(),ReadResource);
            var field=Create(f,new RaceEventLog(200000),shortPolicy);Assert.That(field.IsDirectedPassageSharingEnabled(0),Is.False);Assert.That(field.IsDirectedPassageSharingEnabled(1),Is.False);
            var inputs=new SprintAthleteInput[2];var lines=new double[2];var guards=new PhysicalJunctionMap(f.Map,f.Junctions);bool visited=false;
            while(!field.IsComplete&&field.Tick.Value<30000)
            {
                PackSprintFieldTests.Fill(field,inputs,800);field.Step(inputs,lines);
                Assert.That(SharedGuard(field,guards,0)||SharedGuard(field,guards,1),Is.False);
                visited|=field.GetPhysicalPoint(0).LoopId=="test_shortcut";
            }
            Assert.That(visited&&field.IsComplete,Is.True,"A short optional window retains conservative finite default passage behavior.");
        }

        [Test] public void DifferentLapOccurrencesShareOnlyTheirExactPassageAndSkippedThroughMainRejects()
        {
            var f=Fixture(2);var field=Create(f,new RaceEventLog(100),Definition(f));
            var a=Trace(field,"stationary",0);var b=Trace(field,"stationary",1);var type=a.GetType();
            var conflict=typeof(PackSprintFieldSession).GetMethod("Conflict",Private);
            void Point(object trace,string loop,long native,long route)
            {
                var point=f.Map.MapLoop(loop,native);Member(type,"Start").SetValue(trace,point);Member(type,"End").SetValue(trace,point);
                Member(type,"StartRoute").SetValue(trace,route);Member(type,"EndRoute").SetValue(trace,route);
                type.GetMethod("ResetJunctionTouches",Private|BindingFlags.Public).Invoke(trace,null);
            }
            // These centres are twenty world metres apart while both own the full region.
            // Their lap numbers differ, but each occurrence follows the same exact seam.
            Point(a,"main",130*M,130*M);Point(b,"test_shortcut",10*M,450*M);
            Assert.That((bool)conflict.Invoke(field,new[]{a,(object)0,b,1}),Is.False);
            Point(a,"main",130*M,130*M+1);
            Assert.That((bool)conflict.Invoke(field,new[]{a,(object)0,b,1}),Is.True,"One-nanometre stale route provenance cannot authorize sharing.");
            Point(a,"main",142*M,142*M);
            var skipped=Assert.Throws<TargetInvocationException>(()=>conflict.Invoke(field,new[]{a,(object)0,b,1}));
            Assert.That(skipped.InnerException,Is.TypeOf<ArgumentException>(),"A skipped Main interval cannot supply traffic authority.");
            Point(a,"main",10*M,10*M);Point(b,"mat_1",0,10*M);
            Assert.That((bool)conflict.Invoke(field,new[]{a,(object)0,b,1}),Is.True,"Other reachable range regions retain their exclusive rule.");
        }
    }
}
