using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class RangeWorldTrafficTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static PackAiRaceRunner Create(bool enabled,int count=6,string format="mass_start")
        {
            const string v="Biathlon/Venues/woodland_basin_v1/";
            string Resource(string id)
            {
                string json=Read(id);
                if(id==v+"ai_ski_policy")json=json.Replace("true","false");
                if(id==v+"shared_apron")
                {
                    json=Regex.Replace(json,",?\\s*\"parallel_post_shoot_departures\"\\s*:\\s*(true|false)","");
                    json=Regex.Replace(json,",?\\s*\"penalty_world_merge_junctions\"\\s*:\\s*\\[[^\\]]*\\]","");
                    if(enabled)json=json.Insert(json.IndexOf('{')+1,"\"parallel_post_shoot_departures\":true,\"penalty_world_merge_junctions\":[\"range_rejoin\"],");
                }
                if(id==v+"ai_range_traffic")
                {
                    json=Regex.Replace(json,",?\\s*\"(exit|penalty)_lateral_m\"\\s*:\\s*-?[0-9.]+","");
                    if(enabled)json=json.Insert(json.IndexOf('{')+1,"\"exit_lateral_m\":1.3,\"penalty_lateral_m\":-1.3,");
                }
                return json;
            }
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Resource(v+"manifest")),Resource);
            // Freeze the independent tuck policy in this traffic fixture so its fixed-seed
            // first-bout misses exercise the paid penalty merge. Full venue races enable tuck.
            string Core(string name)
            {
                string json=PhysiologyContractTests.DefinitionJson(name);
                if(name=="ai_race_policy_provisional_v1.json")
                {
                    json=Regex.Replace(json,",?\\s*\"downhill_tuck_enabled\"\\s*:\\s*(true|false)","");
                    json=json.Insert(json.IndexOf('{')+1,"\"downhill_tuck_enabled\":false,");
                }
                return json;
            }
            return ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Resource(v+format+"_profile")),
                RaceRoutePlan.FromJson(Resource(v+format+"_route")),RaceSnowConditions.FromJson(Resource("Biathlon/race_snow_neutral_v1")),
                Core,new RaceEventLog(100000),new AiDecisionCounts(),count:count);
        }
        private static PhysicalRangeWorldTrafficMap Map(PackAiRaceRunner runner)=>(PhysicalRangeWorldTrafficMap)typeof(PackSprintFieldSession).GetField("rangeWorldTraffic",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner.Field);
        private static PhysicalTrackMap Physical(PackAiRaceRunner runner)=>(PhysicalTrackMap)typeof(PackSprintFieldSession).GetField("map",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner.Field);
        [Test] public void BypassMustRejectSubNanometerGoalsThatCommitAsNoMovement()
        {
            var runner=Create(true,1);for(int i=0;i<1000;i++)runner.Step();
            var field=runner.Field;double line=field.GetLateral(0);
            Assert.That(field.IsRangeWorldLateralRequestClear(0,line+.3,.3),Is.True,"Control: an actual paid sidestep is available.");
            Assert.That(field.IsRangeWorldLateralRequestClear(0,line+1e-12,.3),Is.False,"A floating-point residue is not an escape and must not renew its goal hold.");
            Assert.That(field.IsRangeWorldLateralRequestClear(0,line-1e-12,.3),Is.False);
            Assert.That(field.IsRangeWorldLateralRequestClear(0,line,.3),Is.False);
        }
        [Test] public void PaidWorldEscapeScopeRequiresAnExplicitPolicyAndExactRouteGeometry()
        {
            string json=Read("Biathlon/Venues/woodland_basin_v1/ai_range_traffic");
            var enabled=AiRangeTrafficPolicyDefinition.FromJson(json);
            Assert.That(enabled.WorldBypassPaidRouteLoops,Is.True);
            Assert.That(AiRangeTrafficPolicyDefinition.FromJson(enabled.ToJson()).WorldBypassPaidRouteLoops,Is.True);
            string absent=Regex.Replace(json,",?\\s*\"world_bypass_paid_route_loops\"\\s*:\\s*true","");
            var legacy=AiRangeTrafficPolicyDefinition.FromJson(absent);
            Assert.That(legacy.WorldBypassPaidRouteLoops,Is.False);
            Assert.That(legacy.ToJson(),Does.Not.Contain("world_bypass_paid_route_loops"));
            Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson("{\"schema_version\":1,\"id\":\"incomplete_world_scope\",\"align_physical_range_paths\":true,\"alignment_lead_m\":75,\"world_bypass_paid_route_loops\":true}"));
            var runner=Create(true,1,"sprint");var field=runner.Field;var physical=Physical(runner);
            var main=physical.MapLoop("main",170*SkiState.NanometersPerMeter);
            var sprint=physical.MapLoop("sprint_extension",20*SkiState.NanometersPerMeter);
            Assert.That(field.HasPaidRouteWorldGeometry(main),Is.True);
            Assert.That(field.HasPaidRouteWorldGeometry(sprint),Is.True);
            foreach(string loop in new[]{"pursuit_shortcut","start_approach","mat_01","penalty_150"})
                Assert.That(field.HasPaidRouteWorldGeometry(physical.MapLoop(loop,0)),Is.False,loop);
            var mass=Create(true,1);Assert.That(mass.Field.HasPaidRouteWorldGeometry(Physical(mass).MapLoop("sprint_extension",20*SkiState.NanometersPerMeter)),Is.False);
            for(int i=0;i<100;i++)field.HasPaidRouteWorldGeometry(sprint);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)field.HasPaidRouteWorldGeometry(sprint);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void SeparatedSprintBranchBodiesMustNotClaimAnExclusivePointGuard()
        {
            var runner=Create(true,2,"sprint");var field=runner.Field;var physical=Physical(runner);
            var traces=(Array)typeof(PackSprintFieldSession).GetField("stationary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
            var type=traces.GetValue(0).GetType();
            var conflict=typeof(PackSprintFieldSession).GetMethod("Conflict",BindingFlags.Instance|BindingFlags.NonPublic);
            void Place(int index,string loop,double metres,double line)
            {
                object trace=traces.GetValue(index);var point=physical.MapLoop(loop,(long)Math.Round(metres*SkiState.NanometersPerMeter));
                foreach(string key in new[]{"Start","End"})type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trace,point);
                foreach(string key in new[]{"LateralStart","LateralEnd"})type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trace,line);
                type.GetMethod("ResetJunctionTouches",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(trace,null);
            }
            bool Blocked()=>(bool)conflict.Invoke(field,new object[]{traces.GetValue(0),0,traces.GetValue(1),1});
            Place(0,"main",170,0);Place(1,"sprint_extension",20,0);
            Assert.That(Blocked(),Is.False,"Separate real bodies inside a branch point guard must not take a whole-junction lease.");
            Assert.That(Map(runner).Contains(physical.MapLoop("sprint_extension",0)),Is.True,"All paid route loops need captured body geometry.");
            Assert.That(Map(runner).Contains(physical.MapLoop("pursuit_shortcut",0)),Is.False,"A different format's unbound route must not grant compatibility.");
            Place(0,"main",177.357,0);Place(1,"sprint_extension",0,0);
            Assert.That(Blocked(),Is.True,"Coincident bodies at the authored branch seam retain physical contact protection.");
            Place(0,"main",170,0);Place(1,"sprint_extension",20,0);
            runner=Create(false,2,"sprint");field=runner.Field;physical=Physical(runner);
            traces=(Array)typeof(PackSprintFieldSession).GetField("stationary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
            Place(0,"main",170,0);Place(1,"sprint_extension",20,0);
            Assert.That(Blocked(),Is.True,"Policy-absent legacy admission remains conservative.");
        }
        [Test,Timeout(600000)] public void ActualSprintFortyPrefixDoesNotTrapTheFieldBeforeItsBranchSeam()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Read(root+"sprint_profile")),
                RaceRoutePlan.FromJson(Read(root+"sprint_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(200000),new AiDecisionCounts(),count:40);
            var field=runner.Field;var previous=new long[40];var stopped=new int[40];int longest=0,branchFrames=0;
            for(int t=0;t<12000;t++)
            {
                for(int i=0;i<40;i++)previous[i]=field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();if(t%25==0)MassStartFieldTests.AssertClear(field);
                for(int i=0;i<40;i++)
                {
                    var actor=field.GetAthlete(i);
                    stopped[i]=field.IsOnCourse(i)&&!actor.HasFinished&&actor.ActiveShooting==null&&actor.Motion.DistanceNanometers==previous[i]?stopped[i]+1:0;
                    longest=Math.Max(longest,stopped[i]);
                    if(field.IsOnCourse(i)&&field.GetPhysicalPoint(i).LoopId=="sprint_extension")branchFrames++;
                }
            }
            TestContext.Out.WriteLine("sprint40_branch_frames="+branchFrames+" maximum_stationary_ticks="+longest);
            Assert.That(branchFrames,Is.GreaterThan(0));
            Assert.That(longest,Is.LessThan(1000),"The observed 5795-tick branch trap must not recur. This prefix does not establish full-race completion.");
        }
        [Test] public void LaterLapMainPassageMustNotWaitForAnotherMainBodyAtTheStartJoin()
        {
            var runner=Create(true,2);var field=runner.Field;var physical=Physical(runner);
            var traces=(Array)typeof(PackSprintFieldSession).GetField("stationary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
            var type=traces.GetValue(0).GetType();
            var conflict=typeof(PackSprintFieldSession).GetMethod("Conflict",BindingFlags.Instance|BindingFlags.NonPublic);
            void Place(int index,double metres,double line)
            {
                object trace=traces.GetValue(index);var point=physical.MapLoop("main",(long)Math.Round(metres*SkiState.NanometersPerMeter));
                foreach(string key in new[]{"Start","End"})type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trace,point);
                foreach(string key in new[]{"LateralStart","LateralEnd"})type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trace,line);
                type.GetMethod("ResetJunctionTouches",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(trace,null);
            }
            bool Blocked()=>(bool)conflict.Invoke(field,new object[]{traces.GetValue(0),0,traces.GetValue(1),1});
            Place(0,1183,-1.3);Place(1,1185,1.3);
            Assert.That(Blocked(),Is.False,"Two separated Main bodies must not claim the unused start branch exclusively on later laps.");
            Place(0,1199.9,-1.3);Place(1,.1,1.3);
            Assert.That(Blocked(),Is.False,"A split closed-loop endpoint is still the same incident Main passage.");
            Place(0,1199.9,0);Place(1,.1,0);
            Assert.That(Blocked(),Is.True,"Periodic physical body contact remains blocked by the current collision policy.");
            Place(0,1183,0);Place(1,1183.1,0);
            Assert.That(Blocked(),Is.True);
            Place(0,1183,-1.3);Place(1,1185,1.3);
            runner=Create(false,2);field=runner.Field;physical=Physical(runner);
            traces=(Array)typeof(PackSprintFieldSession).GetField("stationary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
            Place(0,1183,-1.3);Place(1,1185,1.3);
            Assert.That(Blocked(),Is.True,"Absent complete world geometry preserves the legacy guard.");
        }
        [Test] public void SingleIncidentPortProofRejectsSelfCrossingsAndStaysAllocationFree()
        {
            var runner=Create(true,1);var proof=Map(runner);var physical=Physical(runner);
            var junctions=PhysicalJunctionDefinition.FromJson(Read("Biathlon/Venues/woodland_basin_v1/junctions"),runner.Field.Track);
            int start=-1,rejoin=-1;
            for(int j=0;j<junctions.Count;j++){if(junctions.Get(j).Id=="start_join")start=j;if(junctions.Get(j).Id=="range_rejoin")rejoin=j;}
            var point=physical.MapLoop("main",1183*SkiState.NanometersPerMeter);
            Assert.That(proof.CanShareSingleLoopIncidentPort(start,point),Is.True);
            Assert.That(proof.CanShareSingleLoopIncidentPort(rejoin,point),Is.False,"Multiple Main ports cannot be treated as one pass-through port.");
            Assert.That(proof.CanShareCapturedBranch(rejoin),Is.False,"An independently qualified range self-crossing is not a simple route branch.");
            Assert.That(proof.CanShareCapturedBranch(start),Is.False,"Uncaptured start geometry cannot grant a branch proof.");
            Assert.That(proof.CanShareSingleLoopIncidentPort(start,physical.MapLoop("start_approach",39*SkiState.NanometersPerMeter)),Is.False,"The unbound source body still requires start-continuation admission.");
            Assert.That(proof.ContextJson,Does.Contain("single_incident_same_loop_junction"));
            for(int i=0;i<100;i++)proof.CanShareSingleLoopIncidentPort(start,point);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)proof.CanShareSingleLoopIncidentPort(start,point);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void IndexedWorldBoundsExactlyMatchExhaustiveProjectionAtKnotsAndInsideEdges()
        {
            var runner=Create(true,1);var proof=Map(runner);var physical=Physical(runner);
            var corridors=(PhysicalPassageCorridor[])typeof(PhysicalRangeWorldTrafficMap).GetField("corridors",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(proof);
            var type=typeof(PhysicalRangeWorldTrafficMap).Assembly.GetType("Biathlon.Simulation.PhysicalApronBodySupportDraft");
            var constructor=type.GetConstructors()[0];var bounds=type.GetMethod("Bounds",BindingFlags.Instance|BindingFlags.NonPublic);
            var body=OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            var policy=VenueValidationPolicy.FromJson(Read("Biathlon/Venues/production_venue_validation_v1"));
            int checkedAxes=0;
            foreach(var corridor in corridors)
            {
                if(corridor==null)continue;
                object exhaustive=constructor.Invoke(new object[]{corridor,corridor,body,policy,true,false});
                object indexed=constructor.Invoke(new object[]{corridor,corridor,body,policy,true,true});
                for(int k=0;k<corridor.Count;k+=Math.Max(1,corridor.Count/12))
                for(int shift=-1;shift<=1;shift++)
                {
                    long at=Math.Max(0,Math.Min((long)Math.Round(corridor.End*SkiState.NanometersPerMeter),
                        (long)Math.Round(corridor.Get(k).Distance*SkiState.NanometersPerMeter)+shift));
                    long travel=Math.Min(SkiState.NanometersPerMeter/10,(long)Math.Round(corridor.End*SkiState.NanometersPerMeter)-at);
                    var sweep=new PhysicalOccupancySweep(new AthleteId("projection_reference"),physical.MapLoop(corridor.LoopId,at),travel,-.5,.5);
                    foreach(var axis in new[]{new[]{1d,0d},new[]{0d,1d},new[]{.6,.8},new[]{-.8,.6}})
                    {
                        object[] oldArgs={sweep,axis[0],axis[1],0d,0d},newArgs={sweep,axis[0],axis[1],0d,0d};
                        bounds.Invoke(exhaustive,oldArgs);bounds.Invoke(indexed,newArgs);
                        Assert.That(newArgs[3],Is.EqualTo(oldArgs[3]));Assert.That(newArgs[4],Is.EqualTo(oldArgs[4]));checkedAxes++;
                    }
                }
            }
            Assert.That(checkedAxes,Is.GreaterThan(1000));
        }
        [Test] public void ExactPenaltyAndExitSweepsAllowSeparatedLinesButContactWrapAndUnknownGeometryBlock()
        {
            var runner=Create(true);var proof=Map(runner);var physical=Physical(runner);
            PhysicalOccupancySweep Sweep(string id,string loop,double at,double line,double travel=0)=>new PhysicalOccupancySweep(new AthleteId(id),physical.MapLoop(loop,(long)Math.Round(at*SkiState.NanometersPerMeter)),(long)Math.Round(travel*SkiState.NanometersPerMeter),line,line);
            Assert.That(proof,Is.Not.Null);
            Assert.That(proof.Conflicts(Sweep("a","exit_aisle",130,-1.7,.1),Sweep("b","exit_aisle",130,1.7,.1)),Is.False);
            Assert.That(proof.Conflicts(Sweep("a","exit_aisle",130,0,.1),Sweep("b","exit_aisle",130,0,.1)),Is.True);
            Assert.That(proof.Conflicts(Sweep("a","penalty_150",149,0,2),Sweep("b","exit_aisle",130,0)),Is.True,"Unsplit wrap cannot grant compatibility.");
            Assert.That(Map(Create(false)),Is.Null,"Absent policy retains legacy field semantics.");
            for(int i=0;i<100;i++)proof.Conflicts(Sweep("a","exit_aisle",130,-1.7,.1),Sweep("b","exit_aisle",130,1.7,.1));
            var a=Sweep("a","exit_aisle",130,-1.7,.1);var b=Sweep("b","exit_aisle",130,1.7,.1);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)proof.Conflicts(a,b);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        // This acceptance includes three complete physical contexts and a persisted
        // 7,000-tick replay. Mono needs more than NUnit's default 180-second budget.
        [Test,Timeout(600000)] public void ActualSixAthleteExitPrefixKeepsPaidBodiesAndReplaysWithoutAi()
        {
            var runner=Create(true);var target=Create(true).Field;var tape=new PackSprintRaceCommandTape("actual_range_world_paid_prefix",target,6*7000);
            var inputs=new SprintAthleteInput[6];var lines=new double[6];int exit=0,penalty=0;var stopped=new int[6];int longest=0;
            for(int t=0;t<7000;t++)
            {
                runner.Step();for(int i=0;i<6;i++){inputs[i]=runner.GetLastInput(i);lines[i]=runner.GetLastLateral(i);}
                tape.StepAndRecord(target,inputs,lines);
                if(t%10==0)MassStartFieldTests.AssertClear(runner.Field);
                for(int i=0;i<6;i++)
                {
                    var actor=runner.Field.GetAthlete(i);var phase=actor.CurrentRangeMotion?.Phase;
                    if(phase==RangeTraversalPhase.RangeExit)exit++;
                    if(phase==RangeTraversalPhase.PenaltyLoop)penalty++;
                    bool departure=phase==RangeTraversalPhase.RangeExit||phase==RangeTraversalPhase.PenaltyApproach||phase==RangeTraversalPhase.Rejoin;
                    stopped[i]=departure&&actor.Motion.SpeedMetersPerSecond==0?stopped[i]+1:0;longest=Math.Max(longest,stopped[i]);
                    Assert.That(target.GetAthlete(i).LastFrame,Is.EqualTo(actor.LastFrame));Assert.That(target.GetLateral(i),Is.EqualTo(runner.Field.GetLateral(i)));
                }
            }
            TestContext.Out.WriteLine("paid_exit_frames="+exit+" penalty_frames="+penalty+" longest_stopped_exit_ticks="+longest);
            Assert.That(exit,Is.GreaterThan(0));Assert.That(penalty,Is.GreaterThan(0));
            Assert.That(longest,Is.LessThan(200),"A six-body range departure must not form a long exclusive queue.");
            var restored=Create(true).Field;var cursor=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(restored);while(cursor.HasNext)cursor.Step();
            for(int i=0;i<6;i++)Assert.That(restored.GetAthlete(i).LastFrame,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame));
        }
        [Test,Timeout(600000)] public void ActualThirtyBodyRepeatedPenaltyMergeDoesNotDeadlockOrEscapeAwayFromAMat()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Read(root+"mass_start_profile")),
                RaceRoutePlan.FromJson(Read(root+"mass_start_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(300000),new AiDecisionCounts(),count:30);
            var stopped=new int[30];var wasOnMat=new bool[30];int longest=0,departures=0,penalties=0;bool escape=false;
            for(int t=0;t<9000;t++)
            {
                for(int i=0;i<30;i++)wasOnMat[i]=runner.Field.GetPhysicalPoint(i).LoopId==runner.Field.GetAthlete(i).RangePath.MatAnchor.LoopId;
                runner.Step();if(t%25==0)MassStartFieldTests.AssertClear(runner.Field);
                for(int i=0;i<30;i++)
                {
                    var actor=runner.Field.GetAthlete(i);var phase=actor.CurrentRangeMotion?.Phase;var point=runner.Field.GetPhysicalPoint(i);
                    bool departing=phase==RangeTraversalPhase.RangeExit||phase==RangeTraversalPhase.PenaltyApproach||phase==RangeTraversalPhase.Rejoin;
                    if(departing)departures++;if(phase==RangeTraversalPhase.PenaltyLoop)penalties++;
                    stopped[i]=(departing||phase==RangeTraversalPhase.PenaltyLoop)&&actor.Motion.SpeedMetersPerSecond==0?stopped[i]+1:0;
                    longest=Math.Max(longest,stopped[i]);
                    var plan=runner.GetLastRangeTrafficPlan(i);
                    if(wasOnMat[i]&&point.LoopId==actor.RangePath.MatAnchor.LoopId&&phase==RangeTraversalPhase.Approach&&plan.Active&&!plan.AlignmentBlocked)
                        Assert.That(plan.DesiredLateralMeters,Is.Zero,"Shared-corridor escape must not steer a private mat approach away from its support.");
                    escape|=plan.Active&&point.LoopId=="exit_aisle"&&Math.Abs(runner.GetLastLateral(i)-1.3)>.2;
                }
            }
            TestContext.Out.WriteLine("thirty_paid_departures="+departures+" penalty_frames="+penalties+" longest_stopped_departure_ticks="+longest);
            Assert.That(departures,Is.GreaterThan(0));Assert.That(penalties,Is.GreaterThan(0));Assert.That(escape,Is.True);
            Assert.That(longest,Is.LessThan(100),"Repeated paid penalty/range merges must recover promptly, rather than form an exclusive queue.");
            for(int i=0;i<30;i++)Assert.That(runner.Field.GetAthlete(i).Lifecycle.CompletedBouts,Is.GreaterThanOrEqualTo(1));
        }
        [Test] public void WorldEscapePolicyIsBoundedCompleteAndQueriesAreReadOnlyWithoutAllocations()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string json=Read(root+"ai_range_traffic");var policy=AiRangeTrafficPolicyDefinition.FromJson(json);
            Assert.That(AiRangeTrafficPolicyDefinition.FromJson(policy.ToJson()).ToJson(),Is.EqualTo(policy.ToJson()));
            foreach(string key in new[]{"world_bypass_after_seconds","world_bypass_lateral_step_m","world_bypass_preview_m","world_bypass_hold_seconds"})
            {
                Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(Regex.Replace(json,",?\\s*\""+key+"\"\\s*:\\s*[0-9.]+","")));
                foreach(string bad in new[]{"0","-1","1001","\"NaN\""})
                    Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(Regex.Replace(json,"\""+key+"\"\\s*:\\s*[0-9.]+","\""+key+"\":"+bad)));
            }
            foreach(string bad in new[]{"0","-1","100.1","\"NaN\""})
                Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(Regex.Replace(json,"\"world_bypass_lookahead_m\"\\s*:\\s*[0-9.]+","\"world_bypass_lookahead_m\":"+bad)));
            var legacy=AiRangeTrafficPolicyDefinition.FromJson(Regex.Replace(json,",?\\s*\"world_bypass_lookahead_m\"\\s*:\\s*[0-9.]+",""));
            Assert.That(legacy.WorldBypassLookaheadMeters,Is.Null);
            Assert.That(legacy.ToJson(),Does.Not.Contain("world_bypass_lookahead_m"));
            var runner=Create(true,1);for(int i=0;i<1000;i++)runner.Step();var field=runner.Field;
            Assert.Throws<ArgumentOutOfRangeException>(()=>field.IsRangeWorldLateralRequestClear(-1,0,.3));
            Assert.Catch(()=>field.IsRangeWorldLateralRequestClear(0,double.NaN,.3));
            Assert.Catch(()=>field.IsRangeWorldLateralRequestClear(0,0,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>field.IsRangeWorldForwardLaneClear(-1,0,5));
            Assert.Catch(()=>field.IsRangeWorldForwardLaneClear(0,double.NaN,5));
            Assert.Catch(()=>field.IsRangeWorldForwardLaneClear(0,0,0));
            var frame=field.GetAthlete(0).LastFrame;long tick=field.Tick.Value;double line=field.GetLateral(0);
            for(int i=0;i<100;i++){field.IsRangeWorldLateralRequestClear(0,.3,.3);field.IsRangeWorldForwardLaneClear(0,.3,5);}
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++){field.IsRangeWorldLateralRequestClear(0,.3,.3);field.IsRangeWorldForwardLaneClear(0,.3,5);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.That(field.Tick.Value,Is.EqualTo(tick));Assert.That(field.GetLateral(0),Is.EqualTo(line));Assert.That(field.GetAthlete(0).LastFrame,Is.EqualTo(frame));
        }
        [Test] public void ExitBypassLookaheadRespectsNarrowPenaltyEntryBeforeTheCentreReachesIt()
        {
            var runner=Create(true);var field=runner.Field;
            var fits=typeof(PackSprintFieldSession).GetMethod("FitsBypassPathWidth",BindingFlags.Instance|BindingFlags.NonPublic);
            bool witnessed=false;
            for(int t=0;t<7000&&!witnessed;t++)
            {
                runner.Step();
                for(int i=0;i<field.Count;i++)
                {
                    var actor=field.GetAthlete(i);var point=field.GetPhysicalPoint(i);
                    if(point.LoopId!="exit_aisle"||actor.CurrentRangeMotion?.Phase!=RangeTraversalPhase.PenaltyApproach||
                        actor.PreviewCurrentRangeMotion(5*SkiState.NanometersPerMeter)?.Phase!=RangeTraversalPhase.PenaltyLoop)continue;
                    bool current=(bool)fits.Invoke(field,new object[]{i,point,2d,0L});
                    if(!current)continue;
                    witnessed=true;
                    Assert.That((bool)fits.Invoke(field,new object[]{i,point,2d,5*SkiState.NanometersPerMeter}),Is.False,
                        "A line that fits the wide exit must be rejected when its lookahead enters the narrower penalty lane.");
                    var frame=actor.LastFrame;var motion=actor.CurrentRangeMotion;long tick=field.Tick.Value;
                    Assert.That(field.IsRangeWorldForwardLaneClear(i,2,5),Is.False);
                    Assert.That(actor.LastFrame,Is.EqualTo(frame));Assert.That(actor.CurrentRangeMotion,Is.EqualTo(motion));
                    Assert.That(field.Tick.Value,Is.EqualTo(tick));break;
                }
            }
            Assert.That(witnessed,Is.True,"Exercise the real paid approach to a penalty caused by an actual AI miss.");
        }
        [Test,Timeout(600000)] public void ActualQualifiedPursuitFortyClearsTheMainAndPenaltyReturnWithoutCircularWait()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"pursuit_profile"));
            // Actual completed Sprint40 source retained to reproduce the observed traffic
            // schedule. This is an input witness, not a current Sprint acceptance claim.
            var source=SprintResultSnapshot.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/races/woodland_sprint40_traffic_source_v1.json"));
            var qualification=PursuitQualificationSnapshot.Create("traffic_pursuit_actual_sprint",source,profile);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(Read(root+"pursuit_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),PhysiologyContractTests.DefinitionJson,
                new RaceEventLog(500000),new AiDecisionCounts(),qualification,count:40);
            double frontageMinimum=AiRangeTrafficPolicyDefinition.FromJson(Read(root+"ai_range_traffic")).EntryLateralMeters.Value;
            var field=runner.Field;var previous=new long[40];var stopped=new int[40];int longest=0,frontageSamples=0;bool mainEscape=false;
            for(int t=0;t<40000&&!field.IsComplete;t++)
            {
                for(int i=0;i<40;i++)previous[i]=field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();if(t%25==0)MassStartFieldTests.AssertClear(field);
                for(int i=0;i<40;i++)
                {
                    var actor=field.GetAthlete(i);
                    var location=field.GetPhysicalPoint(i);
                    if(actor.CurrentRangeMotion==null&&location.LoopId==field.Track.Range.Entry.LoopId&&
                        location.LocalNanometers>=field.Track.Range.Exit.Distance.Millimeters*SkiState.NanometersPerMillimeter+
                            2*SkiState.NanometersPerMeter&&
                        location.LocalNanometers<=field.Track.Penalty.Entry.Distance.Millimeters*SkiState.NanometersPerMillimeter)
                    {
                        frontageSamples++;
                        Assert.That(runner.GetLastLateral(i),Is.GreaterThanOrEqualTo(frontageMinimum),
                            "AI must plan the paid safe side through the physical penalty merge.");
                    }
                    stopped[i]=field.IsOnCourse(i)&&!actor.HasFinished&&actor.ActiveShooting==null&&previous[i]==actor.Motion.DistanceNanometers?stopped[i]+1:0;
                    longest=Math.Max(longest,stopped[i]);
                    if(stopped[i]>=200)
                    {
                        var blocked=field.GetPhysicalPoint(i);
                        var traces=(Array)typeof(PackSprintFieldSession).GetField("stationary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
                        var conflict=typeof(PackSprintFieldSession).GetMethod("Conflict",BindingFlags.Instance|BindingFlags.NonPublic);
                        TestContext.Out.WriteLine("blocked tick="+field.Tick.Value+" athlete="+i+" loop="+blocked.LoopId+
                            " local_m="+blocked.LocalNanometers/1e9+" line="+field.GetLateral(i)+
                            " route_m="+actor.RouteNanometers/1e9+" range_phase="+actor.CurrentRangeMotion?.Phase+
                            " requested_line="+runner.GetLastLateral(i)+" plan_blocked="+runner.GetLastRangeTrafficPlan(i).AlignmentBlocked+
                            " effort="+runner.GetLastInput(i).Ski.Effort.RequestedEffort+
                            " speed="+actor.Motion.SpeedMetersPerSecond+
                            " constrained="+((bool[])typeof(PackSprintFieldSession).GetField("constrained",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field))[i]+
                            " initial_block="+field.GetInitialMovementBlock(i).Kind+
                            " blocker="+field.GetInitialMovementBlock(i).Blocker.Value+
                            " precision_fallback="+field.PrecisionFallbackTicks+
                            " junction_wait_since="+field.GetJunctionWaitSince(i));
                        for(int j=0;j<40;j++)if(j!=i)
                        {
                            var other=field.GetPhysicalPoint(j);
                            bool conflictNow=(bool)conflict.Invoke(field,new object[]{traces.GetValue(i),i,traces.GetValue(j),j});
                            if(conflictNow || field.GetAthlete(j).AthleteId.Equals(field.GetInitialMovementBlock(i).Blocker) ||
                                other.LoopId==blocked.LoopId&&Math.Abs(other.LocalNanometers-blocked.LocalNanometers)<20*SkiState.NanometersPerMeter)
                                TestContext.Out.WriteLine("near athlete="+j+" conflict="+conflictNow+" loop="+other.LoopId+
                                    " local_m="+other.LocalNanometers/1e9+" line="+field.GetLateral(j)+
                                    " phase="+field.GetAthlete(j).CurrentRangeMotion?.Phase+
                                    " distance_m="+field.GetAthlete(j).Motion.DistanceNanometers/1e9);
                        }
                        Assert.Fail("A circular wait must fail immediately instead of consuming the entire race timeout.");
                    }
                    mainEscape|=actor.CurrentRangeMotion==null&&field.GetPhysicalPoint(i).LoopId==venue.Track.Range.Entry.LoopId&&runner.GetLastRangeTrafficPlan(i).Active;
                }
            }
            Assert.That(mainEscape,Is.True,"An actor beyond the range planner must still find paid clear space on Main.");
            Assert.That(frontageSamples,Is.GreaterThan(0),"The real range-frontage merge must be exercised.");
            Assert.That(field.IsComplete,Is.True,"All forty must finish rather than form the observed circular Main/penalty wait.");
            Assert.That(longest,Is.LessThan(1000),"A transient body-safe yield must not become a permanent traffic queue.");
            TestContext.Out.WriteLine("qualified_pursuit_complete_tick="+field.Tick.Value+" maximum_stationary_ticks="+longest);
        }
    }
}
