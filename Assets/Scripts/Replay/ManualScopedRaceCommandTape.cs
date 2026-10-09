using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Biathlon.Domain;
using Biathlon.Simulation;
using Payload = Biathlon.Replay.PackSprintRaceCommandTape.Payload;

namespace Biathlon.Replay
{
    /// <summary>Complete signed field commands. Playback invokes no AI and
    /// binds the exact initial physical/session definitions and roster.</summary>
    public sealed class ManualScopedRaceCommandTape
    {
        private const string Kind="manual_scoped_race_commands_v1";
        private struct Run
        {
            internal int Athlete,Steering;
            internal long First,Last;
            internal SprintAthleteInput Input;
            internal double Brake;
        }
        private readonly Run[] runs;
        private readonly int[] next,head,tail;
        private readonly AthleteId[] athletes;
        private readonly ManualScopedFieldStepper recording;
        public string Id {get;}
        public string ContextJson {get;}
        public long TickCount {get;private set;}
        public int RunCount {get;private set;}
        public int AthleteCount=>athletes.Length;
        public ManualScopedRaceCommandTape(string id,ManualScopedFieldStepper stepper,int capacity)
            :this(id,InitialContext(stepper),FreshRoster(stepper),capacity)
        {recording=stepper;}
        private ManualScopedRaceCommandTape(string id,string context,AthleteId[] roster,int capacity)
        {
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(context)||roster==null||roster.Length==0||capacity<roster.Length)
                throw new ArgumentException("Valid manual tape identity, initial context, roster and capacity required.");
            for(int i=0;i<roster.Length;i++)
            {
                if(!roster[i].IsValid)throw new ArgumentException("Invalid manual roster.");
                for(int j=0;j<i;j++)if(roster[i].Equals(roster[j]))throw new ArgumentException("Duplicate manual roster.");
            }
            Id=id;ContextJson=context;athletes=roster;runs=new Run[capacity];next=new int[capacity];
            head=new int[roster.Length];tail=new int[roster.Length];
            for(int i=0;i<roster.Length;i++)head[i]=tail[i]=-1;
        }
        private static void RequireFresh(ManualScopedFieldStepper stepper)
        {
            if(stepper==null)throw new ArgumentNullException(nameof(stepper));
            if(stepper.Field.Tick.Value!=0||stepper.World.Tick.Value!=0)throw new ArgumentException("Fresh manual field and world required.");
        }
        private static AthleteId[] FreshRoster(ManualScopedFieldStepper stepper)
        {
            RequireFresh(stepper);var roster=new AthleteId[stepper.Field.Count];
            for(int i=0;i<roster.Length;i++)roster[i]=stepper.Field.GetAthlete(i).AthleteId;
            return roster;
        }
        private static string InitialContext(ManualScopedFieldStepper stepper)
        {
            RequireFresh(stepper);
            // Resolve these cold identities against the exact loaded definitions,
            // never a current lane context after resource/session publication.
            var values=new[]{Kind,"initial_field_sha256:"+ManualContextIdentity.Sha256(stepper.Field.ContextJson),
                "stepper_sha256:"+ManualContextIdentity.Sha256(stepper.ContextJson),
                "initial_world_sha256:"+ManualContextIdentity.Sha256(stepper.World.InitialStateContextJson)};
            using(var stream=new MemoryStream())
            {new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,values);return Encoding.UTF8.GetString(stream.ToArray());}
        }
        private void ValidateInitial(ManualScopedFieldStepper stepper)
        {
            if(InitialContext(stepper)!=ContextJson||stepper.Field.Count!=AthleteCount)throw new ArgumentException("Manual replay initial context mismatch.");
            for(int i=0;i<AthleteCount;i++)if(!stepper.Field.GetAthlete(i).AthleteId.Equals(athletes[i]))throw new ArgumentException("Manual replay roster mismatch.");
        }
        private static void ValidateControl(int steering,double brake)
        {
            if(steering<-ManualSkiCommand.SteeringUnits||steering>ManualSkiCommand.SteeringUnits)throw new ArgumentOutOfRangeException(nameof(steering));
            PhysiologyNumbers.RequireRange(brake,0,1,nameof(brake));
        }
        private static bool Same(Run a,SprintAthleteInput b,int steering,double brake)
            =>a.Steering==steering&&a.Brake==brake&&PackSprintRaceCommandTape.SameCommandIntent(a.Input,b);
        public void StepAndRecord(ManualScopedFieldStepper stepper,ReadOnlySpan<SprintAthleteInput> inputs,
            ReadOnlySpan<int> steering,ReadOnlySpan<double> brakes)
        {
            if(!ReferenceEquals(recording,stepper)||stepper.Field.Tick.Value!=TickCount||stepper.World.Tick.Value!=TickCount)
                throw new ArgumentException("Exact bound recording field and tape clock required.");
            if(stepper.Field.IsComplete||inputs.Length!=AthleteCount||steering.Length!=AthleteCount||brakes.Length!=AthleteCount)
                throw new ArgumentException("Complete active manual command batches required.");
            long tick=checked(TickCount+1);int added=0;
            for(int i=0;i<AthleteCount;i++)
            {
                ValidateControl(steering[i],brakes[i]);
                if(inputs[i].Ski.Effort.Tick.Value!=tick||inputs[i].Aim.Tick.Value!=tick||
                    !inputs[i].Ski.Effort.AthleteId.Equals(athletes[i])||!inputs[i].Aim.AthleteId.Equals(athletes[i]))
                    throw new ArgumentException("Manual command rows must match next tick and stable roster order.");
                if(tail[i]<0||!Same(runs[tail[i]],inputs[i],steering[i],brakes[i]))added++;
            }
            if(added>runs.Length-RunCount)throw new ArgumentException("Manual tape capacity exhausted before movement commit.");
            if(!stepper.TryStep(inputs,steering,brakes))throw new InvalidOperationException("Recorded manual movement rejected: "+stepper.LastStep.Status);
            TickCount=tick;
            for(int i=0;i<AthleteCount;i++)
                if(tail[i]>=0&&Same(runs[tail[i]],inputs[i],steering[i],brakes[i]))runs[tail[i]].Last=tick;
                else Append(new Run{Athlete=i,First=tick,Last=tick,Input=inputs[i],Steering=steering[i],Brake=brakes[i]});
        }
        private void Append(Run run)
        {
            int index=RunCount++;runs[index]=run;next[index]=-1;
            if(tail[run.Athlete]<0)head[run.Athlete]=index;else next[tail[run.Athlete]]=index;
            tail[run.Athlete]=index;
        }
        public ReplayCursor CreateReplay(ManualScopedFieldStepper stepper)=>new ReplayCursor(this,stepper);
        public sealed class ReplayCursor
        {
            private readonly ManualScopedRaceCommandTape tape;
            private readonly ManualScopedFieldStepper stepper;
            private readonly int[] current,steering;
            private readonly SprintAthleteInput[] inputs;
            private readonly double[] brakes;
            private readonly long length;
            private long tick;
            internal ReplayCursor(ManualScopedRaceCommandTape tape,ManualScopedFieldStepper stepper)
            {
                tape.ValidateInitial(stepper);this.tape=tape;this.stepper=stepper;length=tape.TickCount;
                current=(int[])tape.head.Clone();steering=new int[tape.AthleteCount];
                inputs=new SprintAthleteInput[tape.AthleteCount];brakes=new double[tape.AthleteCount];
            }
            public bool HasNext=>tick<length;
            public void Step()
            {
                if(!HasNext||stepper.Field.Tick.Value!=tick||stepper.World.Tick.Value!=tick)
                    throw new InvalidOperationException("Manual replay exhausted or its field was advanced externally.");
                long target=checked(tick+1);
                for(int i=0;i<current.Length;i++)
                {
                    int index=current[i];if(index>=0&&tape.runs[index].Last<target)index=tape.next[index];
                    if(index<0||tape.runs[index].First>target||tape.runs[index].Last<target)
                        throw new InvalidOperationException("Incomplete manual replay coverage.");
                    var run=tape.runs[index];inputs[i]=PackSprintRaceCommandTape.AtTick(run.Input,target);
                    steering[i]=run.Steering;brakes[i]=run.Brake;
                }
                if(!stepper.TryStep(inputs,steering,brakes))throw new InvalidOperationException("Manual replay movement rejected: "+stepper.LastStep.Status);
                for(int i=0;i<current.Length;i++)if(tape.runs[current[i]].Last<target)current[i]=tape.next[current[i]];
                tick=target;
            }
        }
        public string ToJson()
        {
            var dto=new TapeJson{Schema=1,Kind=Kind,Id=Id,Context=ContextJson,Ticks=TickCount,
                Athletes=new string[AthleteCount],Runs=new RunJson[RunCount]};
            for(int i=0;i<AthleteCount;i++)dto.Athletes[i]=athletes[i].Value;
            for(int i=0;i<RunCount;i++){var r=runs[i];dto.Runs[i]=new RunJson{Athlete=r.Athlete,First=r.First,Last=r.Last,
                Steering=r.Steering,Brake=r.Brake,Command=Payload.From(r.Input)};}
            using(var stream=new MemoryStream())
            {new DataContractJsonSerializer(typeof(TapeJson)).WriteObject(stream,dto);return Encoding.UTF8.GetString(stream.ToArray());}
        }
        public static ManualScopedRaceCommandTape FromJson(string json)
        {
            TapeJson dto;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json)))))
                dto=(TapeJson)new DataContractJsonSerializer(typeof(TapeJson)).ReadObject(stream);
            if(dto==null||dto.Schema!=1||dto.Kind!=Kind||dto.Ticks<0||dto.Athletes==null||dto.Runs==null)
                throw new SerializationException("Invalid signed manual race tape schema.");
            var roster=new AthleteId[dto.Athletes.Length];for(int i=0;i<roster.Length;i++)roster[i]=new AthleteId(dto.Athletes[i]);
            var result=new ManualScopedRaceCommandTape(dto.Id,dto.Context,roster,Math.Max(roster.Length,dto.Runs.Length));
            long first=0;int actor=-1;
            foreach(var r in dto.Runs)
            {
                if(r==null||r.Athlete<0||r.Athlete>=roster.Length||r.Command==null||r.First<=0||
                    r.Last<r.First||r.Last>dto.Ticks||r.First<first||r.First==first&&r.Athlete<=actor)
                    throw new SerializationException("Invalid ordered manual run.");
                ValidateControl(r.Steering,r.Brake);int prior=result.tail[r.Athlete];
                if(r.First!=(prior<0?1:checked(result.runs[prior].Last+1)))throw new SerializationException("Manual run gap or overlap.");
                var input=r.Command.ToInput(roster[r.Athlete],r.First);
                if(prior>=0&&Same(result.runs[prior],input,r.Steering,r.Brake))throw new SerializationException("Identical adjacent manual runs must merge.");
                result.Append(new Run{Athlete=r.Athlete,First=r.First,Last=r.Last,Input=input,Steering=r.Steering,Brake=r.Brake});
                first=r.First;actor=r.Athlete;
            }
            for(int i=0;i<roster.Length;i++)
                if(dto.Ticks==0?result.tail[i]>=0:result.tail[i]<0||result.runs[result.tail[i]].Last!=dto.Ticks)
                    throw new SerializationException("Incomplete manual command coverage.");
            result.TickCount=dto.Ticks;return result;
        }
        [DataContract] private sealed class TapeJson
        {
            [DataMember(Name="schema_version",IsRequired=true)]public int Schema;
            [DataMember(Name="kind",IsRequired=true)]public string Kind;
            [DataMember(Name="id",IsRequired=true)]public string Id;
            [DataMember(Name="initial_context_json",IsRequired=true)]public string Context;
            [DataMember(Name="tick_count",IsRequired=true)]public long Ticks;
            [DataMember(Name="athletes",IsRequired=true)]public string[] Athletes;
            [DataMember(Name="signed_athlete_runs",IsRequired=true)]public RunJson[] Runs;
        }
        [DataContract] private sealed class RunJson
        {
            [DataMember(Name="athlete_index",IsRequired=true)]public int Athlete;
            [DataMember(Name="first_tick",IsRequired=true)]public long First;
            [DataMember(Name="last_tick",IsRequired=true)]public long Last;
            [DataMember(Name="requested_yaw_units",IsRequired=true)]public int Steering;
            [DataMember(Name="brake_fraction",IsRequired=true)]public double Brake;
            [DataMember(Name="command",IsRequired=true)]public Payload Command;
        }
    }
}
