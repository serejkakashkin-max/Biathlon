using System;
using System.IO;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    /// <summary>Streaming accepted commands. Memory is bounded by one roster; recording never runs AI or advances authority.</summary>
    public sealed class ManualRaceJournalWriter:IManualRaceCommandObserver,IDisposable
    {
        internal const int Magic=0x42525031,Version=1;
        private readonly BinaryWriter output;
        private readonly AthleteId[] athletes;
        private readonly string[] techniques;
        private bool complete,disposed;
        public long TickCount {get;private set;}
        public Exception Failure {get;private set;}
        public ManualRaceJournalWriter(Stream stream,ManualScopedFieldStepper initial,SkiTechniqueDefinition technique)
        {
            RequireFresh(initial);if(stream==null||!stream.CanWrite||technique==null)throw new ArgumentException("Writable replay stream and techniques required.");
            output=new BinaryWriter(stream,Encoding.UTF8,true);athletes=new AthleteId[initial.Field.Count];
            techniques=new string[technique.ProfileCount+1];for(int i=1;i<techniques.Length;i++)techniques[i]=technique.GetProfile(i-1).Id;
            output.Write(Magic);output.Write(Version);output.Write(initial.Field.TickFrequency);
            output.Write(FieldIdentity(initial));output.Write(StepperIdentity(initial));output.Write(WorldIdentity(initial));
            output.Write(athletes.Length);for(int i=0;i<athletes.Length;i++){athletes[i]=initial.Field.GetAthlete(i).AthleteId;output.Write(athletes[i].Value);}
            output.Write(techniques.Length);for(int i=1;i<techniques.Length;i++)output.Write(techniques[i]);
        }
        internal static void RequireFresh(ManualScopedFieldStepper s)
        {if(s==null||s.Field.Tick.Value!=0||s.World.Tick.Value!=0)throw new ArgumentException("Fresh manual authority required.");}
        internal static string FieldIdentity(ManualScopedFieldStepper s)=>ManualContextIdentity.Sha256(s.Field.ContextJson);
        internal static string StepperIdentity(ManualScopedFieldStepper s)=>ManualContextIdentity.Sha256(s.ContextJson);
        internal static string WorldIdentity(ManualScopedFieldStepper s)=>ManualContextIdentity.Sha256(s.World.InitialStateContextJson);
        public void RecordAccepted(SimulationTick tick,ReadOnlySpan<SprintAthleteInput> inputs,ReadOnlySpan<int> steering,ReadOnlySpan<double> brakes)
        {
            if(Failure!=null||disposed||complete)return;
            try
            {
                if(tick.Value!=TickCount+1||inputs.Length!=athletes.Length||steering.Length!=athletes.Length||brakes.Length!=0&&brakes.Length!=athletes.Length)
                    throw new ArgumentException("Replay requires complete consecutive accepted command batches.");
                output.Write(tick.Value);
                for(int i=0;i<athletes.Length;i++)
                {
                    var v=inputs[i];if(v.Ski.Effort.Tick.Value!=tick.Value||!v.Ski.Effort.AthleteId.Equals(athletes[i]))throw new ArgumentException("Replay roster mismatch.");
                    int flags=(v.Ski.TuckRequested?1:0)|(v.Ski.SprintRequested?2:0)|(v.Aim.FineControl?4:0)|(v.Trigger?8:0)|(v.HoldBreath?16:0)|
                        (v.CycleBolt?32:0)|(v.ManualBoltCycle?64:0)|(v.RangeTransit?128:0)|(v.Pressure.FinalStanding?256:0)|(v.Pressure.FirstPodium?512:0);
                    int technique=Array.IndexOf(techniques,v.Ski.TechniqueId);if(technique<0)throw new ArgumentException("Unknown replay technique.");
                    output.Write(v.Ski.Effort.RequestedEffort);output.Write(flags);output.Write(technique);
                    output.Write(v.Ski.Form);output.Write(v.Ski.LineQuality);output.Write(v.Ski.HeadwindMetersPerSecond);output.Write(v.Ski.MovementEfficiency);
                    output.Write(v.Aim.Horizontal);output.Write(v.Aim.Vertical);output.Write(v.Wind.EastMetersPerSecond);output.Write(v.Wind.NorthMetersPerSecond);
                    output.Write(v.Pressure.RaceImportance);output.Write(v.Pressure.PositionImportance);output.Write(v.Pressure.RivalProximity);output.Write(v.Pressure.Confidence);
                    output.Write(steering[i]);output.Write(brakes.Length==0?0:brakes[i]);
                }
                TickCount=tick.Value;
            }
            // A failed optional replay must never fault or rewind a successfully committed race.
            catch(Exception failure){Failure=failure;}
        }
        public void Complete()
        {if(Failure!=null)throw new IOException("Replay recording failed.",Failure);if(disposed||complete)throw new InvalidOperationException("Replay already closed.");output.Write(-1L);output.Write(TickCount);output.Flush();complete=true;}
        public void Dispose(){if(disposed)return;disposed=true;output.Dispose();}
    }
    /// <summary>AI-free playback into a fresh field, bound to exact initial definitions/world/roster.</summary>
    public sealed class ManualRaceJournalReader:IDisposable
    {
        private readonly BinaryReader input;
        private readonly ManualScopedFieldStepper stepper;
        private readonly AthleteId[] athletes;
        private readonly string[] techniques;
        private readonly SprintAthleteInput[] commands;
        private readonly int[] steering;
        private readonly double[] brakes;
        private readonly long length;
        public long TickCount {get;private set;}
        public bool HasNext=>TickCount<length;
        public ManualRaceJournalReader(Stream stream,ManualScopedFieldStepper initial,long ticks)
        {
            ManualRaceJournalWriter.RequireFresh(initial);if(stream==null||!stream.CanRead||ticks<1)throw new ArgumentException("Readable completed replay required.");
            stepper=initial;length=ticks;input=new BinaryReader(stream,Encoding.UTF8,true);
            if(input.ReadInt32()!=ManualRaceJournalWriter.Magic||input.ReadInt32()!=ManualRaceJournalWriter.Version||input.ReadInt32()!=initial.Field.TickFrequency)
                throw new InvalidDataException("Unsupported replay command version.");
            if(input.ReadString()!=ManualRaceJournalWriter.FieldIdentity(initial)||input.ReadString()!=ManualRaceJournalWriter.StepperIdentity(initial)||input.ReadString()!=ManualRaceJournalWriter.WorldIdentity(initial))
                throw new InvalidDataException("Replay definitions differ from the recorded race.");
            int count=input.ReadInt32();if(count!=initial.Field.Count)throw new InvalidDataException("Replay roster differs.");
            athletes=new AthleteId[count];commands=new SprintAthleteInput[count];steering=new int[count];brakes=new double[count];
            for(int i=0;i<count;i++){athletes[i]=new AthleteId(input.ReadString());if(!athletes[i].Equals(initial.Field.GetAthlete(i).AthleteId))throw new InvalidDataException("Replay roster order differs.");}
            int n=input.ReadInt32();if(n<1||n>64)throw new InvalidDataException("Invalid replay techniques.");techniques=new string[n];for(int i=1;i<n;i++)techniques[i]=input.ReadString();
        }
        public void Step()
        {
            if(!HasNext||stepper.Field.Tick.Value!=TickCount||stepper.World.Tick.Value!=TickCount)throw new InvalidOperationException("Replay clock differs or is exhausted.");
            long tick=input.ReadInt64();if(tick!=TickCount+1)throw new InvalidDataException("Nonconsecutive replay tick.");
            for(int i=0;i<athletes.Length;i++)
            {
                int effort=input.ReadInt32(),flags=input.ReadInt32(),technique=input.ReadInt32();
                if(flags<0||flags>1023||technique<0||technique>=techniques.Length)throw new InvalidDataException("Invalid replay control flags.");
                var ski=new TimeTrialInput(new SimulationInput(new SimulationTick(tick),athletes[i],effort),input.ReadDouble(),input.ReadDouble(),input.ReadDouble(),
                    (flags&1)!=0,(flags&2)!=0,techniques[technique],input.ReadDouble());
                var aim=new WeaponAimCommand(new SimulationTick(tick),athletes[i],input.ReadInt32(),input.ReadInt32(),(flags&4)!=0);
                var wind=new WindVector(input.ReadDouble(),input.ReadDouble());var pressure=new ShootingPressureContext(input.ReadDouble(),input.ReadDouble(),input.ReadDouble(),input.ReadDouble(),(flags&256)!=0,(flags&512)!=0);
                commands[i]=new SprintAthleteInput(ski,aim,(flags&8)!=0,(flags&16)!=0,wind,pressure,(flags&32)!=0,(flags&64)!=0,(flags&128)!=0);
                steering[i]=input.ReadInt32();brakes[i]=input.ReadDouble();
            }
            if(!stepper.TryStep(commands,steering,brakes))throw new InvalidOperationException("Recorded movement rejected: "+stepper.LastStep.Status);
            TickCount=tick;
            if(!HasNext&&(input.ReadInt64()!=-1||input.ReadInt64()!=length||input.BaseStream.ReadByte()!=-1))throw new InvalidDataException("Invalid replay completion/footer.");
        }
        public void Dispose()=>input.Dispose();
    }
}
