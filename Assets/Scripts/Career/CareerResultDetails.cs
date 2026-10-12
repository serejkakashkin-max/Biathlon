using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerResultSplit
    {
        [DataMember(Name="checkpoint_id",IsRequired=true)] public string CheckpointId { get; private set; }
        [DataMember(Name="lap_id",IsRequired=true)] public string LapId { get; private set; }
        [DataMember(Name="elapsed_ms",IsRequired=true)] public long ElapsedMilliseconds { get; private set; }
        [DataMember(Name="clock_ms",IsRequired=true)] public long ClockMilliseconds { get; private set; }
        public CareerResultSplit(string checkpointId,string lapId,long elapsedMilliseconds,long clockMilliseconds)
        {CheckpointId=checkpointId;LapId=lapId;ElapsedMilliseconds=elapsedMilliseconds;ClockMilliseconds=clockMilliseconds;Validate();}
        internal void Validate()
        {
            CareerUnits.Id(CheckpointId);CareerUnits.Id(LapId);
            if(ElapsedMilliseconds<0||ClockMilliseconds<ElapsedMilliseconds||ClockMilliseconds>86400000)
                throw new SerializationException("Invalid result split clock.");
        }
    }

    /// <summary>Detached observations collected once after the field completes; never a second race authority.</summary>
    [DataContract] public sealed class CareerResultDetails
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="closed_masks",IsRequired=true)] private int[] masks { get; set; }
        [DataMember(Name="stances",IsRequired=true)] private ShootingStance[] stances { get; set; }
        [DataMember(Name="splits",IsRequired=true)] private CareerResultSplit[] splits { get; set; }
        public int BoutCount=>masks.Length;
        public int SplitCount=>splits.Length;
        public int ClosedMask(int bout)=>masks[bout];
        public ShootingStance Stance(int bout)=>stances[bout];
        public CareerResultSplit Split(int index)=>splits[index];
        public int Hits(int bout)
        {int count=0;for(int bit=0;bit<5;bit++)if((masks[bout]&(1<<bit))!=0)count++;return count;}
        public int TotalHits {get{int count=0;for(int i=0;i<BoutCount;i++)count+=Hits(i);return count;}}
        public CareerResultDetails(string id,IReadOnlyList<int> closedMasks,IReadOnlyList<ShootingStance> shootingStances,IReadOnlyList<CareerResultSplit> checkpoints)
        {
            if(closedMasks==null||shootingStances==null||checkpoints==null)throw new ArgumentNullException();
            SchemaVersion=1;Id=id;masks=new int[closedMasks.Count];stances=new ShootingStance[shootingStances.Count];splits=new CareerResultSplit[checkpoints.Count];
            for(int i=0;i<masks.Length;i++)masks[i]=closedMasks[i];
            for(int i=0;i<stances.Length;i++)stances[i]=shootingStances[i];
            for(int i=0;i<splits.Length;i++)splits[i]=checkpoints[i];Validate();
        }
        internal void Validate()
        {
            CareerUnits.Id(Id);
            if(SchemaVersion!=1||masks==null||stances==null||splits==null||masks.Length<1||masks.Length>16||stances.Length!=masks.Length||splits.Length>4096)
                throw new SerializationException("Invalid result detail schema/count.");
            for(int i=0;i<masks.Length;i++)if(masks[i]<0||masks[i]>31||(stances[i]!=ShootingStance.Prone&&stances[i]!=ShootingStance.Standing))
                throw new SerializationException("Invalid result shooting observation.");
            long elapsed=0,clock=0;
            foreach(var split in splits)
            {
                if(split==null)throw new SerializationException("Missing result split.");split.Validate();
                if(split.ElapsedMilliseconds<elapsed||split.ClockMilliseconds<clock)throw new SerializationException("Unordered result splits.");
                elapsed=split.ElapsedMilliseconds;clock=split.ClockMilliseconds;
            }
        }
    }
}
