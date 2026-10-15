using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
static class Program
{
    static readonly string Root=Path.GetFullPath(".");
    static readonly Dictionary<string,string> Resources=new Dictionary<string,string>();
    static string Read(string n){string p="Assets/Resources/Biathlon/"+n;return Resource(p);}
    static string Venue(string n)=>Resource("Assets/Resources/"+n+".json");
    static string Resource(string n){string s=File.ReadAllText(Path.Combine(Root,n));Resources[n]=Hash(Encoding.UTF8.GetBytes(s));return s;}
    static string Hash(byte[] b){using(var h=SHA256.Create())return Convert.ToHexString(h.ComputeHash(b)).ToLowerInvariant();}
    static void Main()
    {
        const string venueRoot="Biathlon/Venues/woodland_basin_v1/";
        var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Venue(venueRoot+"manifest")),Venue);
        var events=new RaceEventLog(100000);
        var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Venue(venueRoot+"mass_start_profile")),
            RaceRoutePlan.FromJson(Venue(venueRoot+"mass_start_route")),RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")),Read,events,new AiDecisionCounts(),count:1,seed:123);
        var stepper=ManualProductionFieldComposition.Create(runner.Field,venue,Read);
        runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
        string stepperHash=ManualContextIdentity.Sha256(stepper.ContextJson),worldHash=ManualContextIdentity.Sha256(stepper.World.ContextJson);
        string contextRoot=Path.Combine(Root,"artifacts/m15-6/replay-compat");
        File.WriteAllText(Path.Combine(contextRoot,"original-stepper-context.json"),stepper.ContextJson,new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(contextRoot,"original-world-context.json"),stepper.World.ContextJson,new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(contextRoot,"original-field-context.json"),stepper.Field.ContextJson,new UTF8Encoding(false));
        byte[] bytes;int edgeResponses=0;
        using(var memory=new MemoryStream())
        {
            using(var writer=new ManualRaceJournalWriter(memory,stepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            {
                stepper.CommandObserver=writer;
                for(int tick=0;tick<2000;tick++){runner.Step();edgeResponses+=stepper.World.CourseEdgeResponses;}
                writer.Complete();if(writer.Failure!=null)throw writer.Failure;bytes=memory.ToArray();
            }
        }
        string output=Path.Combine(Root,"artifacts/m15-6/replay-compat");
        using(var f=File.Create(Path.Combine(output,"v25-original.brp")))using(var gzip=new GZipStream(f,CompressionLevel.Optimal))gzip.Write(bytes,0,bytes.Length);
        var body=stepper.World.GetActor(0);var cursor=stepper.World.GetCursor(0);var actor=runner.Field.GetAthlete(0);
        var state=new { body.Pose.X,body.Pose.Z,Heading=body.Pose.HeadingRadians,Spin=body.Pose.YawRateRadiansPerSecond,Vx=body.Velocity.X,Vz=body.Velocity.Z,
            cursor.OccurrenceIndex,cursor.ObservedArcNanometers,actor.RouteNanometers,actor.Motion.DistanceNanometers,actor.Motion.SpeedMetersPerSecond };
        var reactions=new List<object>();double[][] inputs={new[]{.6,.8,-.3,-3.0,2.0,.5},new[]{-.8,.6,.2,4.0,-5.0,-.7},new[]{.6,.8,.3,1.0,2.0,.5}};
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        foreach(var v in inputs)
        {
            ((bool[])typeof(ManualScopedWorldRoster).GetField("courseSupports",flags).GetValue(stepper.World))[0]=true;
            ((double[])typeof(ManualScopedWorldRoster).GetField("courseNormalsX",flags).GetValue(stepper.World))[0]=v[0];
            ((double[])typeof(ManualScopedWorldRoster).GetField("courseNormalsZ",flags).GetValue(stepper.World))[0]=v[1];
            ((double[])typeof(ManualScopedWorldRoster).GetField("courseLevers",flags).GetValue(stepper.World))[0]=v[2];
            var free=new SkiForceCoupledTrajectory(body.Pose,new SkiContactMotion(new SkiPlanarVelocity(4,3),.1),new SkiContactAcceleration(v[3],v[4],v[5]),.05);
            var result=(SkiForceCoupledTrajectory)typeof(ManualScopedWorldRoster).GetMethod("ApplyCourseReaction",flags).Invoke(stepper.World,new object[]{0,free});
            reactions.Add(new{input=v,result=new[]{result.Acceleration.X,result.Acceleration.Z,result.Acceleration.Yaw}});
        }
        string metadataJson=new RaceReplayInfo{Id="policy_test",RecordedUtc="2026-10-15T00:00:00Z",CareerSnapshotJson="{\"save_version\":2, \"note\":\"keep whitespace\" }",EventId="event",VenueId="woodland_basin_v1",PlayerId="player",PlayerName="Alex",TickFrequency=20,TickCount=1200,CommandSha256=new string('a',64),ResultSha256=new string('b',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()}.ToJson();
        string fixture=JsonSerializer.Serialize(new{schema_version=1,id="m15_5_original_v25_journal",baseline_commit="9867b24bfcaed10dc354267477eb9d665fe50d14",
            generator="Isolated compile of original Domain/Simulation/AI/Career/Replay Git blobs; no compatibility code compiled.",ticks=2000,count=1,seed=123,
            stepper_sha256=stepperHash,world_sha256=worldHash,commands_sha256=Hash(bytes),edge_responses=edgeResponses,resource_sha256=Resources,state,reactions,metadata_json=metadataJson},new JsonSerializerOptions{WriteIndented=true});
        File.WriteAllText(Path.Combine(output,"v25-original.json"),fixture,new UTF8Encoding(false));Console.WriteLine(fixture);
    }
}
