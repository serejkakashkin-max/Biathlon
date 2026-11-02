using System;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.AI;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PhysicalApronExitClearanceTests
    {
        private const string V="Biathlon/Venues/woodland_basin_v1/";
        private sealed class Fixture
        {
            public string Root,Json;public VenueAuthoringPlan Venue;public PackAiRaceRunner Runner;
            public string Read(string id)=>File.ReadAllText(Path.Combine(Root,id+".json"));
        }
        private static Fixture Load(bool enabled=true)
        {
            string root=null;
            foreach(string start in new[]{Environment.CurrentDirectory,TestContext.CurrentContext.TestDirectory})
            {
                var dir=new DirectoryInfo(start);
                while(dir!=null&&root==null)
                {
                    string path=Path.Combine(dir.FullName,"tests/fixtures/venues/shared_apron_exit_v2");
                    if(File.Exists(Path.Combine(path,V+"shared_apron.json")))root=path;dir=dir.Parent;
                }
            }
            Assert.That(root,Is.Not.Null,"Frozen exact optional policy resources required.");
            var f=new Fixture{Root=root};f.Json=f.Read(V+"shared_apron");
            string Read(string id)=>id==V+"shared_apron"&&!enabled?WithoutPolicy(f.Json):f.Read(id);
            // This optional-policy witness owns frozen core definitions. The
            // production factory's aliases must resolve to those exact inputs.
            string Core(string name)
            {
                switch(name)
                {
                    case "physiology_gameplay_v2.json":name="physiology_provisional_v1.json";break;
                    case "cornering_gameplay_v2.json":name="cornering_provisional_v1.json";break;
                    case "tuck_gameplay_v2.json":name="tuck_provisional_v1.json";break;
                    case "breathing_gameplay_v2.json":name="breathing_v1.json";break;
                    case "heartbeat_gameplay_v2.json":name="heartbeat_v1.json";break;
                    case "muscular_sway_gameplay_v2.json":name="muscular_sway_v1.json";break;
                }
                return Read("Biathlon/"+Path.GetFileNameWithoutExtension(name));
            }
            f.Venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(V+"manifest")),Read);
            f.Runner=ProductionVenueRaceFactory.Create(f.Venue,CompetitionProfile.FromJson(Read(V+"sprint_profile")),
                RaceRoutePlan.FromJson(Read(V+"sprint_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                Core,new Sink(),new AiDecisionCounts(),count:2);
            return f;
        }
        private sealed class Sink:IRaceEventSink{public void Record(RaceEvent value){}}
        private static string WithoutPolicy(string json)
        {
            // Mutate the typed bound definition using the same native-compatible reflection
            // helpers as the established contract fixtures; no JSON.Nodes dependency.
            return System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(json,"\"schema_version\"\\s*:\\s*2","\"schema_version\":1",System.Text.RegularExpressions.RegexOptions.None),
                ",\\s*\"post_shoot_exit_clearance\"\\s*:\\s*\\{[^{}]*\\}","");
        }
        private static PhysicalSharedApronDefinition Bind(Fixture f,string json)=>PhysicalSharedApronDefinition.FromJson(json,
            f.Venue.Track,f.Venue.Visual,f.Venue.Paths,f.Venue.Junctions,f.Venue.Connections,f.Venue.Occupancy,f.Read);
        private static void SetProperty(object target,string name,object value)=>target.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
        private static object Member(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(target,value);
        private static object Trace(Fixture f,string loop,double from,double to)
        {
            var type=typeof(PackSprintFieldSession).GetNestedType("Trace",BindingFlags.NonPublic);
            var trace=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{1,f.Venue.Junctions.Count,false},null);
            var map=(PhysicalTrackMap)Member(f.Runner.Field,"map");
            Set(trace,"Start",map.MapLoop(loop,(long)Math.Round(from*SkiState.NanometersPerMeter)));
            Set(trace,"End",map.MapLoop(loop,(long)Math.Round(to*SkiState.NanometersPerMeter)));return trace;
        }
        private static bool Lease(Fixture f,object a,object b)=>(bool)typeof(PackSprintFieldSession).GetMethod("LocalExitLeaseConflict",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f.Runner.Field,new[]{a,b});
        private static bool Crossing(Fixture f,string id,object a,object b)
        {
            PhysicalApronExitClearance policy=null;
            for(int i=0;i<f.Venue.SharedApron.RegionCount;i++)if(f.Venue.SharedApron.GetRegion(i).JunctionId==id)policy=f.Venue.SharedApron.GetRegion(i).ExitClearance;
            Assert.That(policy,Is.Not.Null);
            return (bool)typeof(PackSprintFieldSession).GetMethod("LocalExitClearanceConflict",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{a,b,policy});
        }
        [Test] public void AllSixtyPoliciesBindExactStopsAndUnchangedCompleteGuards()
        {
            var f=Load();int count=0;
            for(int i=0;i<f.Venue.SharedApron.RegionCount;i++)
            {
                var r=f.Venue.SharedApron.GetRegion(i);var p=r.ExitClearance;if(p==null)continue;count++;
                var j=f.Venue.Junctions.Get(r.JunctionIndex);PhysicalJunctionPort through=null,priv=null;
                for(int k=0;k<j.PortCount;k++){var port=j.GetPort(k);if(port.Anchor.LoopId==p.ThroughLoopId)through=port;else priv=port;}
                PhysicalRangePathLane lane=null;for(int k=0;k<f.Venue.Paths.Count;k++)if(f.Venue.Paths.GetLane(k).MatAnchor.LoopId==p.PrivateLoopId)lane=f.Venue.Paths.GetLane(k);
                Assert.That(p.ClaimAfterStopMillimeters,Is.EqualTo(lane.MatAnchor.Distance.Millimeters));
                long half=(long)Math.Ceiling(f.Venue.Occupancy.LongitudinalSeparation*.5*1000);
                Assert.That(p.ThroughGuardStartMillimeters,Is.EqualTo(through.Anchor.Distance.Millimeters-through.GuardBefore.Millimeters-half));
                Assert.That(p.ThroughGuardEndMillimeters,Is.EqualTo(through.Anchor.Distance.Millimeters+through.GuardAfter.Millimeters+half));
                Assert.That(p.ReleaseAfterMillimeters,Is.EqualTo(p.ThroughLoopId=="main"?priv.Anchor.Distance.Millimeters+priv.GuardAfter.Millimeters+half:f.Venue.Track.GetLoop(p.PrivateLoopId).Length.Millimeters));
            }
            Assert.That(count,Is.EqualTo(60));Assert.That(f.Venue.SharedApron.SchemaVersion,Is.EqualTo(2));
            var d=Bind(f,f.Venue.SharedApron.ToJson());Assert.That(d.ToJson(),Is.EqualTo(f.Venue.SharedApron.ToJson()));
            Assert.That(f.Venue.Occupancy.BodyLength,Is.EqualTo(1.4));Assert.That(f.Venue.Occupancy.BodyWidth,Is.EqualTo(.6));
        }
        [Test] public void Schema1CannotSilentlyConsumeClearanceAndSchema2RequiresPolicy()
        {
            var f=Load();var d=Bind(f,f.Json);SetProperty(d,"SchemaVersion",1);Assert.Throws<ArgumentException>(()=>Bind(f,d.ToJson()));
            var empty=WithoutPolicy(f.Json).Replace("\"schema_version\":1","\"schema_version\":2");Assert.Throws<ArgumentException>(()=>Bind(f,empty));
        }
        [TestCase("private_loop_id","main")][TestCase("private_loop_id","penalty_150")]
        [TestCase("through_loop_id","mat_01")][TestCase("through_loop_id","exit_aisle")]
        public void MalformedOrUnrelatedCrossingBindingsReject(string field,string value)
        {
            var f=Load();var d=Bind(f,f.Json);PhysicalApronExitClearance policy=null;
            for(int i=0;i<d.RegionCount;i++)if(d.GetRegion(i).JunctionId=="outbound_cross_01")policy=d.GetRegion(i).ExitClearance;
            SetProperty(policy,field=="private_loop_id"?"PrivateLoopId":"ThroughLoopId",value);
            Assert.Throws<ArgumentException>(()=>Bind(f,d.ToJson()));
        }
        [Test] public void StationaryParallelShootersAndWaitingMatNeverClaim()
        {
            var f=Load();double stop=f.Venue.Paths.GetLane(0).MatAnchor.Distance.Meters;
            Assert.That(Lease(f,Trace(f,"mat_01",stop,stop),Trace(f,"mat_02",stop,stop)),Is.False);
            Assert.That(Crossing(f,"outbound_cross_01",Trace(f,"mat_01",stop,stop),Trace(f,"main",2892,2892.003)),Is.False);
        }
        [Test] public void PaidDepartureClaimsBeforeContactButAbsentPolicyIsExactNoop()
        {
            var f=Load();double stop=f.Venue.Paths.GetLane(0).MatAnchor.Distance.Meters;
            var a=Trace(f,"mat_01",stop,stop+.001);var b=Trace(f,"mat_02",stop,stop+.001);
            Assert.That(Lease(f,a,b),Is.True);var old=Load(false);Assert.That(Lease(old,a,b),Is.False);
            Assert.That(Crossing(f,"outbound_cross_01",a,Trace(f,"main",2892,2892.001)),Is.True);
        }
        [Test] public void OnlyPhysicallyOverlappingLocalGuardLeasesExcludeEachOther()
        {
            var f=Load();double stop=f.Venue.Paths.GetLane(0).MatAnchor.Distance.Meters;
            Assert.That(Lease(f,Trace(f,"mat_01",stop,stop+.001),Trace(f,"mat_02",stop,stop+.001)),Is.True);
            Assert.That(Lease(f,Trace(f,"mat_01",stop,stop+.001),Trace(f,"mat_10",stop,stop+.001)),Is.False);
        }
        [Test] public void CrossingLeaseReleasesOnlyAfterUnchangedPrivateGuardAndFullBody()
        {
            var f=Load();var a=Trace(f,"mat_01",9.611,9.612);var b=Trace(f,"main",2892,2892.003);
            Assert.That(Crossing(f,"outbound_cross_01",a,b),Is.False);
            Assert.That(Crossing(f,"outbound_cross_01",Trace(f,"mat_01",9.610,9.611),b),Is.True);
        }
        [Test] public void MergeLeaseRetainsAcceptedPrivateTailAndReleasesWhenTailClears()
        {
            var f=Load();var a=Trace(f,"exit_aisle",96,96.001);var b=Trace(f,"exit_aisle",95,95.001);
            var tailType=typeof(PackSprintFieldSession).GetNestedType("Tail",BindingFlags.NonPublic);
            var tail=Activator.CreateInstance(tailType);Set(tail,"Active",true);
            var map=(PhysicalTrackMap)Member(f.Runner.Field,"map");
            Set(tail,"Point",map.MapLoop("mat_01",f.Venue.Track.GetLoop("mat_01").Length.Millimeters*SkiState.NanometersPerMillimeter));
            Set(a,"OldTail",tail);
            Assert.That(Crossing(f,"merge_01",a,b),Is.True);
            Set(a,"OldTail",Activator.CreateInstance(tailType));
            Assert.That(Crossing(f,"merge_01",a,b),Is.False);
        }
        [Test] public void ActualProductionRunnerWithActiveLocalDepartureLeasesAllocatesZeroAfterWarmup()
        {
            var f=Load();bool active=false;
            for(int tick=0;tick<25000&&!active;tick++)
            {
                f.Runner.Step();
                for(int i=0;i<f.Runner.Field.Count;i++)
                {
                    var point=f.Runner.Field.GetPhysicalPoint(i);
                    var motion=f.Runner.Field.GetAthlete(i).CurrentRangeMotion;
                    if(motion.HasValue&&motion.Value.Phase==RangeTraversalPhase.RangeExit&&
                        point.LoopId==f.Venue.Paths.GetLane(0).MatAnchor.LoopId&&
                        point.LocalNanometers>f.Venue.Paths.GetLane(0).MatAnchor.Distance.Millimeters*SkiState.NanometersPerMillimeter)active=true;
                }
            }
            Assert.That(active,Is.True,"The real production runner must exercise a paid post-shoot departure before allocation measurement.");
            for(int i=0;i<16;i++)f.Runner.Step();
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<32;i++)f.Runner.Step();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);
        }
    }
}
