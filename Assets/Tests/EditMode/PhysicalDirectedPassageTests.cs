using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalDirectedPassageTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private const string Venue="Biathlon/Venues/woodland_basin_v1/";
        private const string Policy="Biathlon/Venues/production_venue_validation_v1";
        // Numerical witnesses belong to the accepted WP169 3km course. Keep their
        // original exact authority as a regression fixture when production geometry changes.
        public static string Read(string resource)=>PhysiologyContractTests.ProjectFile(
            resource.StartsWith(Venue,StringComparison.Ordinal)?
            "docs/quality/fixtures/directed-passages-wp169/"+resource.Substring(Venue.Length)+".json":
            "Assets/Resources/"+resource+".json");
        public sealed class Fixture
        {
            public TrackDefinition Track;
            public TrackVisualDefinition Visual;
            public RaceRoutePlan Plan;
            public CompetitionProfile Profile;
            public PhysicalJunctionDefinition Junctions;
            public PhysicalConnectionDefinition Connections;
            public OccupancyDefinition Body;
            public RaceRoute Route;
            public PhysicalTrackMap Physical;
            public string Json;
            public PhysicalDirectedPassageDefinition Definition;
            public PhysicalDirectedPassageMap Map;
        }
        private static string Number(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        private static string Knot(double distance,double x,double z)
            =>"{\"distance_m\":"+Number(distance)+",\"x\":"+Number(x)+",\"y\":0,\"z\":"+Number(z)+"}";
        private static string NativeCorridor(string loop,params string[] knots)=>"{\"loop_id\":\""+loop+"\",\"knots\":["+string.Join(",",knots)+"]}";
        public static string Corridor(TrackVisualLoop loop,double from,double to)
        {
            var points=new List<string>();Add(from);
            for(int k=0;k<loop.Count;k++){var p=loop.Get(k);if(p.Distance>from&&p.Distance<to)Add(p.Distance);}
            Add(to);
            return "{\"loop_id\":\""+loop.LoopId+"\",\"knots\":["+string.Join(",",points)+"]}";
            void Add(double distance)
            {var p=loop.Sample(distance);points.Add("{\"distance_m\":"+Number(distance)+",\"x\":"+Number(p.X)+",\"y\":"+Number(p.Y)+",\"z\":"+Number(p.Z)+"}");}
        }
        public static Fixture Production()
        {
            var f=new Fixture();f.Track=TrackDefinition.FromJson(Read(Venue+"track"));
            f.Visual=TrackVisualDefinition.FromJson(Read(Venue+"visual"),f.Track);
            f.Profile=CompetitionProfile.FromJson(Read(Venue+"pursuit_profile"));f.Plan=RaceRoutePlan.FromJson(Read(Venue+"pursuit_route"));
            f.Junctions=PhysicalJunctionDefinition.FromJson(Read(Venue+"junctions"),f.Track);
            f.Connections=PhysicalConnectionDefinition.FromJson(Read(Venue+"connections"),f.Junctions);
            f.Body=OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            f.Route=new RaceRoute(f.Track,f.Plan,f.Profile.ToRouteRequirements(),f.Junctions,f.Connections);f.Physical=new PhysicalTrackMap(f.Track,f.Route);
            f.Json="{\"schema_version\":1,\"id\":\"woodland_pursuit_directed_passages_v1\",\"track_id\":\""+f.Track.Id+"\",\"route_id\":\""+f.Plan.Id+
                "\",\"occupancy_definition_id\":\""+f.Body.Id+"\",\"validation_policy_resource\":\""+Policy+"\",\"passages\":["+
                Passage("departure","pursuit_depart_connection",Corridor(f.Visual.Get("main"),560,597.048),Corridor(f.Visual.Get("pursuit_shortcut"),0,35))+","+
                Passage("return","pursuit_return_connection",Corridor(f.Visual.Get("pursuit_shortcut"),848,883.344),Corridor(f.Visual.Get("main"),1980.392,2015))+"]}";
            f.Definition=Bind(f,f.Json);f.Map=Map(f,f.Definition);return f;
        }
        private static string Passage(string id,string connection,string source,string target)
            =>"{\"id\":\""+id+"\",\"connection_id\":\""+connection+"\",\"source\":"+source+",\"target\":"+target+"}";
        public static PhysicalDirectedPassageDefinition Bind(Fixture f,string json)
            =>PhysicalDirectedPassageDefinition.FromJson(json,f.Track,f.Plan,f.Junctions,f.Connections,f.Body,Read);
        public static PhysicalDirectedPassageMap Map(Fixture f,PhysicalDirectedPassageDefinition definition)
            =>new PhysicalDirectedPassageMap(f.Physical,f.Track,f.Route,f.Junctions,f.Connections,f.Body,definition);
        private static PhysicalOccupancySweep Sweep(Fixture f,string id,string loop,double start,double travel,double before=0,double after=0)
            =>new PhysicalOccupancySweep(new AthleteId(id),f.Physical.MapLoop(loop,(long)Math.Round(start*M)),(long)Math.Round(travel*M),before,after);

        [Test] public void CapturedProductionPairBindsCompleteGuardsAndRoundTripsExactAuthority()
        {
            var f=Production();var d=Bind(f,f.Definition.ToJson());
            Assert.That(d.ToJson(),Is.EqualTo(f.Definition.ToJson()));Assert.That(d.MatchesPresentation(f.Visual),Is.True);
            Assert.That(d.TrackSnapshotJson,Is.EqualTo(f.Track.ToJson()));Assert.That(d.RoutePlanSnapshotJson,Is.EqualTo(f.Plan.ToJson()));
            Assert.That(d.JunctionSnapshotJson,Is.EqualTo(f.Junctions.ToJson()));Assert.That(d.ConnectionSnapshotJson,Is.EqualTo(f.Connections.ToJson()));
            Assert.That(d.OccupancySnapshotJson,Is.EqualTo(f.Body.ToJson()));Assert.That(f.Map.Count,Is.EqualTo(2));
            Assert.That(f.Map.ProjectionMarginMeters(0),Is.GreaterThanOrEqualTo(.01));
            Assert.That(f.Map.ContextJson,Does.Contain("all_receipts_endpoints_and_provenanced_tails"));
        }
        [TestCase("schema_version", "2")]
        [TestCase("track_id", "foreign_track")]
        [TestCase("route_id", "foreign_route")]
        [TestCase("occupancy_definition_id", "foreign_body")]
        [TestCase("connection_id", "unknown_connection")]
        public void MalformedOrForeignContractNeverProducesAuthority(string member,string replacement)
        {
            var f=Production();string pattern="(\\\""+member+"\\\":)(?:\\\"[^\\\"]*\\\"|[0-9]+)";
            string value=member=="schema_version"?replacement:"\""+replacement+"\"";
            string json=new Regex(pattern).Replace(f.Json,"$1"+value,1);
            Assert.That(()=>Bind(f,json),Throws.Exception);
        }
        [Test] public void IncompleteIncomingOrOutgoingGuardCoverageRejectsCold()
        {
            var f=Production();
            string source=Corridor(f.Visual.Get("main"),580,597.048),target=Corridor(f.Visual.Get("pursuit_shortcut"),0,15);
            string oldSource=Corridor(f.Visual.Get("main"),560,597.048),oldTarget=Corridor(f.Visual.Get("pursuit_shortcut"),0,35);
            Assert.Throws<ArgumentException>(()=>Bind(f,f.Json.Replace(oldSource,source)));
            Assert.Throws<ArgumentException>(()=>Bind(f,f.Json.Replace(oldTarget,target)));
        }
        [Test] public void WrongSeamDirectionWorldMetricAndSubmillimetreKnotRejectCold()
        {
            var f=Production();
            Assert.That(()=>Bind(f,f.Json.Replace("\"distance_m\":560,","\"distance_m\":560.0001,")),Throws.Exception);
            string bad=new Regex("(\\\"x\\\":)([-0-9.Ee+]+)").Replace(f.Json,"${1}999999",1);
            Assert.Throws<ArgumentException>(()=>Bind(f,bad));
            string wrong=f.Json.Replace("\"connection_id\":\"pursuit_depart_connection\"","\"connection_id\":\"pursuit_return_connection\"");
            Assert.Throws<ArgumentException>(()=>Bind(f,wrong));
        }
        [Test] public void SelfCrossingAndNonincidentCorridorContactCannotOptIn()
        {
            var f=Production();string original=Corridor(f.Visual.Get("pursuit_shortcut"),0,35);
            string source=NativeCorridor("main",Knot(560,-37.048,0),Knot(597.048,0,0));
            string crossing=NativeCorridor("pursuit_shortcut",Knot(0,0,0),Knot(5,5,0),Knot(13,5,8),Knot(21,-3,8),Knot(29,-3,0),Knot(32,0,0),Knot(35,3,0));
            string backtrack=NativeCorridor("pursuit_shortcut",Knot(0,0,0),Knot(5,5,0),Knot(10,0,0),Knot(35,25,0));
            string prefix=f.Json.Replace(Corridor(f.Visual.Get("main"),560,597.048),source);
            Assert.That(()=>Bind(f,prefix.Replace(original,crossing)),Throws.ArgumentException.With.Message.Contains("cross"));
            Assert.That(()=>Bind(f,prefix.Replace(original,backtrack)),Throws.ArgumentException.With.Message.Contains("back"));
            string touching=NativeCorridor("pursuit_shortcut",Knot(0,0,0),Knot(1,1,0),Knot(6,1,5),Knot(11,-4,5),Knot(16,-4,0),Knot(35,-23,0));
            Assert.That(()=>Bind(f,prefix.Replace(original,touching)),Throws.ArgumentException.With.Message.Contains("Nonincident"));
        }
        [Test] public void PresentationInteriorKnotCannotHideBetweenMatchingContractEndpoints()
        {
            var f=Production();
            var knots=new List<string>();var loop=f.Visual.Get("main");int altered=0;
            for(int i=0;i<loop.Count;i++)
            {
                var p=loop.Get(i);
                if(altered==0&&p.Distance>580)
                {var inserted=loop.Sample(580);Add(580,inserted.X,inserted.Y,inserted.Z+.1);altered++;}
                Add(p.Distance,p.X,p.Y,p.Z);
            }
            void Add(double distance,double x,double y,double z)=>knots.Add("{\"distance_m\":"+Number(distance)+",\"x\":"+Number(x)+",\"y\":"+Number(y)+",\"z\":"+Number(z)+"}");
            string replacement="{\"loop_id\":\"main\",\"knots\":["+string.Join(",",knots)+"]}";
            var loops=new List<string>();for(int i=0;i<f.Visual.Count;i++)
            {var l=f.Visual.Get(i);if(l.LoopId=="main"){loops.Add(replacement);continue;}loops.Add(Corridor(l,0,l.Get(l.Count-1).Distance));}
            string json="{\"schema_version\":1,\"id\":\"changed_visual\",\"track_id\":\""+f.Track.Id+"\",\"sample_spacing_m\":1,\"loops\":["+string.Join(",",loops)+"]}";
            Assert.That(altered,Is.EqualTo(1));
            Assert.That(f.Definition.MatchesPresentation(TrackVisualDefinition.FromJson(json,f.Track)),Is.False);
        }
        [Test] public void ExactHalfBodyWindowEndpointsBindAndOneMillimetreShortageRejects()
        {
            var f=Production();double sourceStart=597.048-25-f.Body.LongitudinalSeparation*.5,targetEnd=25+f.Body.LongitudinalSeparation*.5;
            string originalSource=Corridor(f.Visual.Get("main"),560,597.048),originalTarget=Corridor(f.Visual.Get("pursuit_shortcut"),0,35);
            string json=f.Json.Replace(originalSource,Corridor(f.Visual.Get("main"),sourceStart,597.048)).Replace(originalTarget,Corridor(f.Visual.Get("pursuit_shortcut"),0,targetEnd));
            var map=Map(f,Bind(f,json));long start=TrackDistance.FromMeters(sourceStart).Millimeters*SkiState.NanometersPerMillimeter;
            Assert.That(map.Contains(0,f.Physical.MapLoop("main",start)),Is.True);Assert.That(map.Contains(0,f.Physical.MapLoop("main",start-1)),Is.False);
            Assert.Throws<ArgumentException>(()=>Bind(f,json.Replace(Corridor(f.Visual.Get("main"),sourceStart,597.048),Corridor(f.Visual.Get("main"),sourceStart+.001,597.048))));
            Assert.Throws<ArgumentException>(()=>Bind(f,json.Replace(Corridor(f.Visual.Get("pursuit_shortcut"),0,targetEnd),Corridor(f.Visual.Get("pursuit_shortcut"),0,targetEnd-.001))));
        }
        [TestCase(-2d)] [TestCase(0d)] [TestCase(2d)] public void AngularProjectionIncludesInteriorCriticalCornersAndOffsetNormal(double lateral)
        {
            var f=Production();string source=NativeCorridor("main",Knot(560,-37.048,0),Knot(597.048,0,0));
            double end=33/Math.Sqrt(2);string target=NativeCorridor("pursuit_shortcut",Knot(0,0,0),Knot(2,2,0),Knot(35,2+end,end));
            string json=f.Json.Replace(Corridor(f.Visual.Get("main"),560,597.048),source).Replace(Corridor(f.Visual.Get("pursuit_shortcut"),0,35),target);
            var map=Map(f,Bind(f,json));var sweep=Sweep(f,"corner","pursuit_shortcut",1.999,.002,lateral,lateral);
            var values=(Array)typeof(PhysicalDirectedPassageMap).GetField("projections",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(map);
            var projection=values.GetValue(0);var bounds=projection.GetType().GetMethod("Bounds",BindingFlags.Instance|BindingFlags.NonPublic);double violation=0;
            double halfLength=f.Body.LongitudinalSeparation*.5,halfWidth=f.Body.LateralSeparation*.5;
            for(int axis=0;axis<72;axis++)
            {
                double angle=axis*Math.PI/36,x=Math.Cos(angle),z=Math.Sin(angle);object[] args={sweep,x,z,0d,0d};bounds.Invoke(projection,args);
                double low=(double)args[3],high=(double)args[4];
                for(int sample=0;sample<=256;sample++)
                {
                    double heading=Math.PI/4*sample/256,fx=Math.Cos(heading),fz=Math.Sin(heading);
                    for(int along=-1;along<=1;along+=2)for(int across=-1;across<=1;across+=2)
                    {
                        double normal=lateral+across*halfWidth,corner=(2+along*halfLength*fx-normal*fz)*x+(along*halfLength*fz+normal*fx)*z;
                        violation=Math.Max(violation,Math.Max(low-corner,corner-high));
                    }
                }
            }
            Assert.That(violation,Is.LessThanOrEqualTo(1e-10),"Every intermediate corner must fit the complete angular support.");
        }
        [Test] public void RepeatedLapsRequireExactRouteOccurrenceAndAcceptBothPhysicalSeamSides()
        {
            var f=Production();int occurrences=0;
            for(int i=0;i<f.Route.SectionCount-1;i++)
            {
                if(f.Route.GetSection(i+1).EnterConnectionId!="pursuit_depart_connection")continue;
                long seam=f.Route.GetSection(i+1).Start.Millimeters*SkiState.NanometersPerMillimeter;
                var incoming=f.Physical.MapRoute(seam,PhysicalBoundarySide.Incoming);var outgoing=f.Physical.MapRoute(seam);
                Assert.That(f.Map.FindOccurrence(0,seam,incoming),Is.EqualTo(i));Assert.That(f.Map.FindOccurrence(0,seam,outgoing),Is.EqualTo(i));
                Assert.That(f.Map.FindOccurrence(0,seam+1,incoming),Is.EqualTo(-1));Assert.That(f.Map.Contains(0,i+1,seam,incoming),Is.False);
                Assert.That(f.Map.MatchesOccurrence(0,i),Is.True);occurrences++;
            }
            Assert.That(occurrences,Is.EqualTo(5));Assert.That(f.Map.MatchesOccurrence(0,2),Is.False);
        }
        [Test] public void ARepeatedOccurrenceWithAnotherDirectionAndExtraIncidentPortRejectCold()
        {
            var f=Production();f.Plan=RaceRoutePlan.FromJson(new Regex("pursuit_depart_connection").Replace(f.Plan.ToJson(),"pursuit_return_connection",1));
            Assert.Throws<ArgumentException>(()=>Bind(f,f.Json));
            f=Production();string port="{\"id\":\"unknown_branch\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":600},\"guard_before_m\":1,\"guard_after_m\":1},";
            string json=Regex.Replace(Read(Venue+"junctions"),"(\"id\"\\s*:\\s*\"pursuit_depart\"\\s*,\\s*\"ports\"\\s*:\\s*\\[)",m=>m.Value+port);
            Assert.That(json,Is.Not.EqualTo(Read(Venue+"junctions")));
            f.Junctions=PhysicalJunctionDefinition.FromJson(json,f.Track);f.Connections=PhysicalConnectionDefinition.FromJson(Read(Venue+"connections"),f.Junctions);
            Assert.That(()=>Bind(f,f.Json),Throws.ArgumentException.With.Message.Contains("two-port"));
        }
        [Test] public void NullDefaultPointAndChangedSafetySnapshotCannotSupplyLateAuthority()
        {
            var f=Production();Assert.Throws<ArgumentNullException>(()=>Map(f,null));Assert.Throws<ArgumentException>(()=>f.Map.Contains(0,default(PhysicalTrackPoint)));
            var other=OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(f.Body.ToJson(),"body_width_m","0.61"));
            Assert.Throws<ArgumentException>(()=>new PhysicalDirectedPassageMap(f.Physical,f.Track,f.Route,f.Junctions,f.Connections,other,f.Definition));
            Assert.That(f.Map.FindPassageForJunction(-1),Is.EqualTo(-1));Assert.That(f.Map.FindPassageForJunction(f.Map.GetJunctionIndex(0)),Is.EqualTo(0));
        }
        [Test] public void OptionalCatalogCapturesOnlyExactPursuitAuthorityAndNullSerializationIsUnchanged()
        {
            var f=Production();var manifest=VenueManifest.FromJson(Read(Venue+"manifest"));
            string legacyJson=Regex.Replace(Read(Venue+"format_bindings"),",\\s*\"directed_passages_resource\"\\s*:\\s*\"[^\"]*\"","");
            var legacy=VenueFormatBindings.FromJson(legacyJson,manifest,f.Track,f.Junctions,f.Connections,Read,f.Body);
            Assert.That(legacy.ToJson(),Does.Not.Contain("directed_passages"));for(int i=0;i<legacy.Count;i++)Assert.That(legacy.Get(i).DirectedPassages,Is.Null);
            string optedJson=legacyJson.Replace("\"route_resource\": \""+Venue+"pursuit_route\"","\"route_resource\": \""+Venue+"pursuit_route\",\"directed_passages_resource\":\"unit_directed_passages\"");
            Assert.That(optedJson,Is.Not.EqualTo(legacyJson));var reads=new HashSet<string>(StringComparer.Ordinal);
            string Reader(string id){reads.Add(id);if(id==manifest.FormatBindingsResource)return optedJson;if(id=="unit_directed_passages")return f.Json;return Read(id);}
            var venue=new VenueAuthoringPlan(manifest,Reader);int bound=0;
            for(int i=0;i<venue.Formats.Count;i++)
            {var item=venue.Formats.Get(i);if(item.Id=="pursuit"){Assert.That(item.DirectedPassages.ToJson(),Is.EqualTo(f.Definition.ToJson()));bound++;}else Assert.That(item.DirectedPassages,Is.Null);}
            Assert.That(bound,Is.EqualTo(1));Assert.That(reads,Does.Contain("unit_directed_passages"));Assert.That(reads,Does.Contain(Policy));
            string sprint=legacyJson.Replace("\"route_resource\": \""+Venue+"sprint_route\"","\"route_resource\": \""+Venue+"sprint_route\",\"directed_passages_resource\":\"unit_directed_passages\"");
            Assert.Throws<ArgumentException>(()=>VenueFormatBindings.FromJson(sprint,manifest,f.Track,f.Junctions,f.Connections,id=>id=="unit_directed_passages"?f.Json:Read(id),f.Body));
        }
        [Test] public void CompleteWindowCoverageExcludesOutsideForeignBranchesAndClosedWrap()
        {
            var f=Production();
            Assert.That(f.Map.Contains(0,f.Physical.MapLoop("main",559*M)),Is.False);
            Assert.That(f.Map.Contains(0,Sweep(f,"a","main",597,1)),Is.False);
            Assert.That(f.Map.Contains(0,Sweep(f,"a","pursuit_shortcut",34,2)),Is.False);
            Assert.That(f.Map.Contains(0,f.Physical.MapLoop("penalty_150",0)),Is.False);
            Assert.That(f.Map.Contains(0,f.Physical.MapLoop("main",2999*M)),Is.False);
            var foreign=new PhysicalTrackMap(f.Track,f.Route);
            Assert.Throws<ArgumentException>(()=>f.Map.Contains(0,foreign.MapLoop("main",580*M)));
            Assert.Throws<ArgumentException>(()=>f.Map.Conflicts(0,Sweep(f,"a","main",559,0),Sweep(f,"b","main",580,0)));
            Assert.Throws<ArgumentException>(()=>f.Map.Conflicts(0,Sweep(f,"same","main",580,0),Sweep(f,"same","main",585,0)));
        }
        [Test] public void ActualQueuedWitnessHasWorldSpaceFollowingGapWithoutGrantingAdmission()
        {
            var f=Production();
            var waiting=Sweep(f,"waiting","main",571.297999807,.001,.7,.7);
            var incumbent=Sweep(f,"incumbent","main",581.501550267,0);
            Assert.That(f.Map.Conflicts(0,waiting,incumbent),Is.False);
            Assert.That(f.Map.FindOccurrence(0,571297999807,waiting.Start),Is.EqualTo(0));
            Assert.That(f.Map.FindOccurrence(0,571297999808,waiting.Start),Is.EqualTo(-1));
        }
        [TestCase(0)] [TestCase(1)] public void BodiesRemainProtectedAcrossBothDirectedSeams(int passage)
        {
            var f=Production();var p=f.Definition.Get(passage);
            Assert.That(f.Map.Conflicts(passage,Sweep(f,"a",p.Source.LoopId,p.Source.End-.3,0),Sweep(f,"b",p.Target.LoopId,p.Target.Start+.3,0)),Is.True);
            Assert.That(f.Map.Conflicts(passage,Sweep(f,"a",p.Source.LoopId,p.Source.End-2,0),Sweep(f,"b",p.Target.LoopId,p.Target.Start+2,0)),Is.False);
            Assert.That(f.Map.Conflicts(passage,Sweep(f,"a",p.Source.LoopId,p.Source.End-1,.9),Sweep(f,"b",p.Target.LoopId,p.Target.Start+.2,0)),Is.True);
        }
        [Test] public void ZeroForwardLateralMotionAndPaidPassingSweepsRemainBodyBlocking()
        {
            var f=Production();var parked=Sweep(f,"b","main",580,0,0,0);
            Assert.That(f.Map.Conflicts(0,Sweep(f,"a","main",580,0,-2,2),parked),Is.True);
            Assert.That(f.Map.Conflicts(0,Sweep(f,"a","main",580,0,-2,-2),parked),Is.False);
            Assert.That(f.Map.Conflicts(0,Sweep(f,"a","main",578,4,-2,2),parked),Is.True);
        }
        [Test] public void WorldQueriesAndOccurrenceLookupAllocateZeroBytesAfterColdBinding()
        {
            var f=Production();var a=Sweep(f,"a","main",580,.02);var b=Sweep(f,"b","main",585,.02);
            for(int i=0;i<1000;i++){f.Map.Conflicts(0,a,b);f.Map.FindOccurrence(0,580*M,a.Start);}
            var expectedQueries=Is.EqualTo(1000);var expectedBytes=Is.Zero;
            long before=GC.GetAllocatedBytesForCurrentThread();int valid=0;
            for(int i=0;i<1000;i++)if(!f.Map.Conflicts(0,a,b)&&f.Map.FindOccurrence(0,580*M,a.Start)==0)valid++;
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(valid,expectedQueries);Assert.That(bytes,expectedBytes);
        }
    }
}
