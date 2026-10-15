using System.Text.Json;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;

internal static class SoakDiagnosis
{
    public static void Run(string root,string output,string profileFile)
    {
        Directory.CreateDirectory(output);var options=new JsonSerializerOptions{WriteIndented=true};
        var cache=new Dictionary<string,string>();string Read(string n){if(!cache.TryGetValue(n,out var text))cache[n]=text=File.ReadAllText(Path.Combine(root,"Assets/Resources/Biathlon",n));return text;}
        string Venue(string n)=>File.ReadAllText(Path.Combine(root,"Assets/Resources",n+".json"));
        var source=CareerSave.FromJson(File.ReadAllText(profileFile));var d=CareerDefinitions.ForSave(Read,source);var player=source.FindWorldAthlete(source.PlayerId);
        var plan=CareerAiTraining.Plan(player,d.Training,d.Calendar,d.Policy,source.Week,d.Growth,d.Adaptation);var kinds=Enumerable.Range(0,plan.Count).Select(plan.Get).ToArray();
        var prepared=CareerSeasonEngine.PrepareWeek(source,d,kinds,s=>new SeededRandomSource(s));
        File.WriteAllText(Path.Combine(output,"prepared-save.json"),prepared.RaceSnapshotJson);
        var p=new CareerRaceProvider(Read,d,Venue).Prepare(CareerSave.FromJson(prepared.RaceSnapshotJson),prepared.Event,prepared.RaceSeed);
        var runner=p.Runner;var field=runner.Field;var movement=ManualProductionFieldComposition.Create(field,p.Venue,p.ManualRead,p.CourseReactionPolicy);
        runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")));
        var ai=(ManualScopedAiMovement)typeof(PackAiRaceRunner).GetField("manualCandidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(runner)!;
        var still=new int[field.Count];var prior=new long[field.Count];var samples=new List<object>();
        int watched=Enumerable.Range(0,field.Count).FirstOrDefault(i=>field.GetAthlete(i).AthleteId.Value=="career_m16_soak_athlete_100");
        var beforeFailure=new Queue<object>();var transitions=new List<object>();int lastOccurrence=-1;
        using var early=new StreamWriter(Path.Combine(output,"early-penalty.jsonl"));
        object Snapshot(int index)
        {
            var a=field.GetAthlete(index);var cursor=movement.World.GetCursor(index);var frame=movement.World.GetActor(index);var path=movement.World.GetCatalog(index).Get(cursor.OccurrenceIndex);var input=runner.GetLastInput(index);
            return new{index,id=a.AthleteId.Value,phase=a.Lifecycle.Phase.ToString(),laps=a.Lifecycle.CompletedLaps,bouts=a.Lifecycle.CompletedBouts,penalties=a.Lifecycle.PendingPenaltyLoops,finished=a.HasFinished,route_nm=a.RouteNanometers,stalled_ticks=still[index],motion_speed=a.Motion.SpeedMetersPerSecond,physical_speed=Math.Sqrt(frame.Velocity.X*frame.Velocity.X+frame.Velocity.Z*frame.Velocity.Z),pose=frame.Pose,velocity=frame.Velocity,loop=frame.Observation.LoopId,local_m=frame.Observation.LocalMeters,lateral_m=frame.Observation.LateralMeters,grade=frame.Observation.GradeAlong(frame.Pose),forward_x=frame.Observation.ForwardX,forward_z=frame.Observation.ForwardZ,cursor,kind=path.Kind.ToString(),end=path.End.ToString(),effort=input.Ski.Effort.RequestedEffort,form=input.Ski.Form,power=a.LastFrame.Physiology.Effort,physiology=a.PhysiologyState,ai_brake=ai.GetBrakeFraction(index),ai_steering=ai.GetSteering(index),tuck=input.Ski.TuckRequested,range_transit=input.RangeTransit,shooting=a.ActiveShooting?.Phase.ToString(),course_responses=movement.World.CourseEdgeResponses,curved_continuations=movement.World.CurvedCourseContinuations};
        }
        while(!field.IsComplete&&field.Tick.Value<200000)
        {
            if(field.Tick.Value>=5600&&field.Tick.Value<=7000){early.WriteLine(JsonSerializer.Serialize(new{tick=field.Tick.Value,actor=Snapshot(watched)}));early.Flush();}
            if(field.Tick.Value>=14000)
            {
                beforeFailure.Enqueue(new{tick=field.Tick.Value,actor=Snapshot(watched)});if(beforeFailure.Count>80)beforeFailure.Dequeue();
            }
            int occurrence=movement.World.GetCursor(watched).OccurrenceIndex;
            if(occurrence!=lastOccurrence){transitions.Add(new{tick=field.Tick.Value,actor=Snapshot(watched)});lastOccurrence=occurrence;}
            try{runner.Step();}
            catch(Exception ex)
            {
                File.WriteAllText(Path.Combine(output,"rejection.json"),JsonSerializer.Serialize(new{error=ex.ToString(),tick=field.Tick.Value,watch=Snapshot(watched),world_last=movement.LastStep.Status.ToString(),last80=beforeFailure.ToArray(),transitions,remaining=Enumerable.Range(0,field.Count).Where(i=>!field.GetAthlete(i).HasFinished).Select(Snapshot).ToArray()},options));
                throw;
            }
            for(int i=0;i<field.Count;i++)
            {
                var a=field.GetAthlete(i);long route=a.RouteNanometers;
                still[i]=!a.HasFinished&&a.ActiveShooting==null&&route<=prior[i]?still[i]+1:0;prior[i]=route;
            }
            if(field.Tick.Value%2000!=0)continue;
            var remaining=Enumerable.Range(0,field.Count).Where(i=>!field.GetAthlete(i).HasFinished).ToArray();
            var sample=new{tick=field.Tick.Value,remaining=remaining.Length,actors=remaining.Select(Snapshot).ToArray()};samples.Add(sample);
            File.WriteAllText(Path.Combine(output,"progress.json"),JsonSerializer.Serialize(sample,options));
            Console.WriteLine("diagnose tick="+field.Tick.Value+" remaining="+remaining.Length+" max_stall="+still.Max());
            if(field.Tick.Value>=40000&&remaining.Length>0&&remaining.All(i=>still[i]>2000))break;
        }
        File.WriteAllText(Path.Combine(output,"timeline.json"),JsonSerializer.Serialize(samples,options));
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{status=field.IsComplete?"PASS":"STUCK",season=source.Season,week=source.Week,event_id=prepared.Event.Id,seed=prepared.RaceSeed,tick=field.Tick.Value,source_sha256=BalanceRegression.Hash(source.ToJson()),prepared_sha256=BalanceRegression.Hash(prepared.RaceSnapshotJson),remaining=Enumerable.Range(0,field.Count).Where(i=>!field.GetAthlete(i).HasFinished).Select(Snapshot).ToArray()},options));
    }
}
