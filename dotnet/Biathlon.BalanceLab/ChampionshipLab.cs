using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

internal static class ChampionshipLab
{
    // Versioned headless workload, not gameplay tuning. Profiles/physics remain authoritative data.
    private const int Count=30, Frequency=20, MaximumTicks=120000;
    private const ulong Seed=123;
    private static readonly JsonSerializerOptions Json=new JsonSerializerOptions{WriteIndented=true};
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private static void Write(string output,string name,object value)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,Json));
    public static int Execute(string data,string tracks,string output)
    {
        Directory.CreateDirectory(output);
        Write(output,"summary.json",new{schema_version=1,id="three_format_championship_v1",status="incomplete",athletes=Count,tick_frequency=Frequency,seed=Seed});
        var stages=new List<object>();var clock=Stopwatch.StartNew();
        var sprintEvents=new RaceEventLog(300000);var sprint=Create(data,tracks,CompetitionFormat.Sprint,sprintEvents);
        Run(sprint,sprintEvents,output,"sprint",stages);
        var source=SprintResultSnapshot.FromJson(sprint.Field.Result.ToJson());
        var pursuitProfile=CompetitionProfile.FromJson(File.ReadAllText(Path.Combine(data,"pursuit_test_v1.json")));
        var qualification=PursuitQualificationSnapshot.Create("championship_qualification_v1",source,pursuitProfile);
        File.WriteAllText(Path.Combine(output,"qualification.json"),qualification.ToJson());
        qualification=PursuitQualificationSnapshot.FromJson(File.ReadAllText(Path.Combine(output,"qualification.json")),source.Id,pursuitProfile);
        var pursuitEvents=new RaceEventLog(300000);var pursuit=Create(data,tracks,CompetitionFormat.Pursuit,pursuitEvents,qualification);
        Run(pursuit,pursuitEvents,output,"pursuit",stages);
        PursuitResultSnapshot.FromJson(pursuit.Field.PursuitResult.ToJson(),qualification,Frequency,new SimulationTick(1));
        var massEvents=new RaceEventLog(300000);var mass=Create(data,tracks,CompetitionFormat.MassStart,massEvents);
        Run(mass,massEvents,output,"mass_start",stages);
        MassStartResultSnapshot.FromJson(mass.Field.MassStartResult.ToJson(),mass.Field.StartGrid,Frequency,new SimulationTick(1));
        Write(output,"summary.json",new{schema_version=1,id="three_format_championship_v1",status="completed",athletes=Count,tick_frequency=Frequency,seed=Seed,
            sprint_source_id=source.Id,qualification_sha256=Hash(qualification.ToJson()),mass_start_dependency="independent_roster_and_common_grid",stages,
            wall_seconds=clock.Elapsed.TotalSeconds,scope="Synthetic headless shared physics; not rendering or career standings."});
        return 0;
    }
    private static void Run(PackAiRaceRunner runner,RaceEventLog events,string output,string name,List<object> stages)
    {
        var field=runner.Field;string folder=Path.Combine(output,name);Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"context.json"),runner.ContextJson);
        while(!field.IsComplete&&field.Tick.Value<MaximumTicks)runner.Step();
        if(!field.IsComplete)throw new InvalidOperationException(name+" exceeded the race completion bound.");
        int shots=0,penalties=0,starts=0;
        var csv=new StringBuilder("tick,athlete_id,event_type,payload\n");
        for(int i=0;i<events.Count;i++)
        {
            var e=events[i];if(e.EventType==RaceEventType.ShotAccepted)shots++;if(e.EventType==RaceEventType.PenaltyLoopCompleted)penalties++;if(e.EventType==RaceEventType.ScheduledStartDue)starts++;
            csv.Append(e.Tick.Value).Append(',').Append(e.AthleteId.Value).Append(',').Append((int)e.EventType).Append(',').Append(e.Payload).Append('\n');
        }
        if(starts!=Count||shots!=Count*field.Profile.BoutCount*field.Profile.ShotsPerBout)throw new InvalidOperationException("Incomplete start/shooting event accounting.");
        int expectedPenalties=0;
        for(int i=0;i<Count;i++)
        {
            var a=field.GetAthlete(i);expectedPenalties+=a.TotalPenaltyLoops;
            long expected=field.Route.Length.Millimeters*SkiState.NanometersPerMillimeter+
                field.Track.GetLoop(field.Track.Penalty.LoopId).Length.Millimeters*SkiState.NanometersPerMillimeter*a.TotalPenaltyLoops+a.InitialStartApproachNanometers;
            if(a.Lifecycle.CompletedLaps!=field.Profile.LapCount||a.Lifecycle.CompletedBouts!=field.Profile.BoutCount||a.Motion.DistanceNanometers!=expected)
                throw new InvalidOperationException("Incomplete lifecycle or unaccounted physical travel.");
        }
        if(penalties!=expectedPenalties)throw new InvalidOperationException("Penalty event accounting mismatch.");
        string result=field.Profile.Format==CompetitionFormat.Sprint?field.Result.ToJson():field.Profile.Format==CompetitionFormat.Pursuit?field.PursuitResult.ToJson():field.MassStartResult.ToJson();
        File.WriteAllText(Path.Combine(folder,"result.json"),result);File.WriteAllText(Path.Combine(folder,"events.csv"),csv.ToString());
        stages.Add(new{name,ticks=field.Tick.Value,starts,accepted_shots=shots,penalties,precision_fallbacks=field.PrecisionFallbackTicks,result_sha256=Hash(result),events_sha256=Hash(csv.ToString())});
        Console.WriteLine(name+": ticks="+field.Tick.Value+"; shots="+shots+"; result_sha256="+Hash(result));
    }
    private static PackAiRaceRunner Create(string data,string tracks,CompetitionFormat format,RaceEventLog events,PursuitQualificationSnapshot qualification=null)
        => ChampionshipRaceFactory.Create(name=>File.ReadAllText(Path.Combine(data,name)),
            name=>File.ReadAllText(Path.Combine(tracks,name)),format,events,new AiDecisionCounts(),qualification,Count,Frequency,Seed);
}
