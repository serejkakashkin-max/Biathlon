using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;

internal static class Program
{
    // Headless production composition/traffic probe. A zero tick limit verifies construction
    // only; it never claims a completed race. CLI counters are diagnostics, not gameplay tuning.
    private static int Main(string[] args)
    {
        if(args.Length!=10||args[0]!="race")
        {
            Console.Error.WriteLine("race <venue-resource-root> <core-resource-root> <manifest> <profile> <route> <conditions> <count> <tick-limit> <output.json>");return 2;
        }
        string output=Path.GetFullPath(args[9]);
        try
        {
            string venueRoot=Path.GetFullPath(args[1]),coreRoot=Path.GetFullPath(args[2]);
            string Read(string id)
            {
                VenueManifest.ResourceId(id);string relative=id.Replace('/',Path.DirectorySeparatorChar)+".json";
                string path=Path.Combine(venueRoot,relative);if(!File.Exists(path))path=Path.Combine(coreRoot,relative);return File.ReadAllText(path);
            }
            string Core(string name)=>Read("Biathlon/"+Path.GetFileNameWithoutExtension(name));
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(args[3])),Read);
            var profile=CompetitionProfile.FromJson(Read(args[4]));var route=RaceRoutePlan.FromJson(Read(args[5]));
            var conditions=RaceSnowConditions.FromJson(Read(args[6]));
            int count=int.Parse(args[7],CultureInfo.InvariantCulture);long limit=long.Parse(args[8],CultureInfo.InvariantCulture);
            if(limit<0)throw new ArgumentOutOfRangeException("tick-limit");
            var events=new Audit();var runner=ProductionVenueRaceFactory.Create(venue,profile,route,conditions,Core,events,new AiDecisionCounts(),count:count);
            var field=runner.Field;var initial=new List<object>();
            for(int i=0;i<field.Count;i++)
            {
                var point=field.GetPhysicalPoint(i);initial.Add(new{athlete=field.GetAthlete(i).AthleteId.Value,loop=point.LoopId,
                    local_nanometers=point.LocalNanometers,lateral_m=field.GetLateral(i),remaining_start_nanometers=field.GetAthlete(i).RemainingStartApproachNanometers});
            }
            int peakShooting=0;long maximumStationaryTicks=0;var stationary=new long[count];var previous=new long[count];
            while(!field.IsComplete&&field.Tick.Value<limit)
            {
                for(int i=0;i<count;i++)previous[i]=field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();int shooting=0;
                for(int i=0;i<count;i++)
                {
                    var actor=field.GetAthlete(i);
                    if(actor.ActiveShooting!=null)shooting++;
                    stationary[i]=field.IsOnCourse(i)&&actor.ActiveShooting==null&&actor.Motion.DistanceNanometers==previous[i]?stationary[i]+1:0;
                    maximumStationaryTicks=Math.Max(maximumStationaryTicks,stationary[i]);
                }
                peakShooting=Math.Max(peakShooting,shooting);
            }
            var state=new{schema_version=1,id="production_venue_race_probe_v1",status=field.IsComplete?"RACE_COMPLETE":limit==0?"CONSTRUCTION_PASS":"PREFIX_ONLY",
                venue=venue.Manifest.Id,profile=profile.Id,athletes=count,ticks=field.Tick.Value,distance_m=field.Route.Length.Meters,
                authored_start=field.UsesAuthoredStartApproach,peak_shooting=peakShooting,max_stationary_ticks=maximumStationaryTicks,
                event_count=events.Count,event_fingerprint=events.Fingerprint.ToString("x16",CultureInfo.InvariantCulture),initial_physical_points=initial,
                result_json=field.IsComplete?(profile.Format==CompetitionFormat.MassStart?field.MassStartResult.ToJson():profile.Format==CompetitionFormat.Sprint?field.Result.ToJson():field.PursuitResult.ToJson()):null};
            Save(output,state);Console.WriteLine(JsonSerializer.Serialize(state));return 0;
        }
        catch(Exception error)
        {
            Save(output,new{schema_version=1,id="production_venue_race_probe_v1",status="FAILED",error=error.ToString()});Console.Error.WriteLine(error);return 1;
        }
    }
    private static void Save(string path,object value)
    {Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);}
    private sealed class Audit:IRaceEventSink
    {
        public long Count;public ulong Fingerprint=14695981039346656037UL;
        public void Record(RaceEvent value)
        {
            Count++;Add(value.Tick.Value);Add((long)value.EventType);Add(value.Payload);foreach(char c in value.AthleteId.Value)Add(c);
        }
        private void Add(long value){unchecked{for(int i=0;i<8;i++){Fingerprint=(Fingerprint^(byte)value)*1099511628211UL;value>>=8;}}}
    }
}
