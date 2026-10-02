using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Accepted full-body clearance of the final private mat corridor. The field
    /// must independently verify its retained physical tail before submitting this fact.</summary>
    public readonly struct PhysicalRangeRelease
    {
        public AthleteId AthleteId { get; }
        public int BoutIndex { get; }
        public int LaneOrdinal { get; }
        public SimulationTick Tick { get; }
        public int TickFrequency { get; }
        public long ClearanceRouteNanometers { get; }
        public long Fraction { get; }
        public bool IsValid => AthleteId.IsValid && Tick.Value > 0 && TickFrequency > 0 && LaneOrdinal > 0;
        private PhysicalRangeRelease(PhysicalRangeArrival receipt, int lane)
        {
            AthleteId=receipt.AthleteId;BoutIndex=receipt.BoutIndex;LaneOrdinal=lane;
            Tick=receipt.Tick;TickFrequency=receipt.TickFrequency;
            ClearanceRouteNanometers=receipt.AdmissionRouteNanometers;Fraction=receipt.Fraction;
        }
        public static bool TryFromAcceptedMotion(AthleteId athlete,int bout,int lane,SimulationTick tick,
            int frequency,long clearanceRouteNanometers,RaceMotionPoint initial,
            ReadOnlySpan<RaceMotionSegment> segments,out PhysicalRangeRelease release)
        {
            if(lane<=0)throw new ArgumentOutOfRangeException(nameof(lane));
            release=default;
            if(!PhysicalRangeArrival.TryFromAcceptedMotion(athlete,bout,tick,frequency,clearanceRouteNanometers,initial,segments,out var crossing))return false;
            release=new PhysicalRangeRelease(crossing,lane);return true;
        }
    }
    public readonly struct PhysicalRangeReservationPreview
    {
        internal PhysicalRangeReservations Owner { get; }
        internal long Revision { get; }
        public SimulationTick Tick { get; }
        internal PhysicalRangeReservationPreview(PhysicalRangeReservations owner,long revision,SimulationTick tick)
        {Owner=owner;Revision=revision;Tick=tick;}
    }
    public readonly struct PhysicalRangeReservationRecord
    {
        public PhysicalRangeArrival Arrival { get; }
        public PhysicalRangeRelease Release { get; }
        public int ArrivalRank { get; }
        public int LaneOrdinal { get; }
        public bool IsValid => LaneOrdinal>0;
        public bool IsReleased => Release.IsValid;
        internal PhysicalRangeReservationRecord(PhysicalRangeArrival arrival,int rank,int lane,PhysicalRangeRelease release=default)
        {Arrival=arrival;ArrivalRank=rank;LaneOrdinal=lane;Release=release;}
        internal PhysicalRangeReservationRecord Released(PhysicalRangeRelease release)
            =>new PhysicalRangeReservationRecord(Arrival,ArrivalRank,LaneOrdinal,release);
    }
    public readonly struct PhysicalRangeReservationChange
    {
        public AthleteId AthleteId {get;}
        public int BoutIndex {get;}
        public int LaneOrdinal {get;}
        public long Fraction {get;}
        public bool IsRelease {get;}
        internal PhysicalRangeReservationChange(AthleteId athlete,int bout,int lane,long fraction,bool release)
        {AthleteId=athlete;BoutIndex=bout;LaneOrdinal=lane;Fraction=fraction;IsRelease=release;}
    }
    /// <summary>Opt-in Sprint/Pursuit reusable private-mat ownership. Admission rank is
    /// distinct from physical lane. Scratch state is disposable until the field's joint
    /// commit; this class supplies no collision proof and emits no events itself.</summary>
    public sealed class PhysicalRangeReservations
    {
        private readonly AthleteId[] roster;
        private readonly int frequency;
        private readonly int[] owner,preparedOwner,ownerBout,preparedOwnerBout,lastBout,preparedLastBout,
            ranks,preparedRanks,arrivalIndices,releaseIndices;
        private readonly bool[] seenArrivals,seenReleases,denied;
        private readonly PhysicalRangeReservationRecord[] records,preparedRecords;
        private readonly PhysicalRangeArrival[] arrivals;
        private readonly PhysicalRangeRelease[] releases;
        private readonly string[] privateLoops;
        private readonly long[] admissionLines,releaseLines;
        private readonly PhysicalRangeReservationChange[] changes,preparedChanges;
        private int preparedChangeCount;
        private long revision;
        private bool ready;
        public int Count=>roster.Length;
        public int LaneCount=>owner.Length;
        public int BoutCount=>ranks.Length;
        public SimulationTick Tick {get;private set;}
        public string ContextJson {get;}
        public int LastChangeCount {get;private set;}
        public PhysicalRangeReservationChange GetLastChange(int index)
        {if(index<0||index>=LastChangeCount)throw new ArgumentOutOfRangeException(nameof(index));return changes[index];}
        public PhysicalRangeReservations(CompetitionProfile profile,ReadOnlySpan<AthleteId> athletes,
            PhysicalRangeFieldLayout layout,OccupancyDefinition body,int tickFrequency)
        {
            if(profile==null||layout==null||body==null)throw new ArgumentNullException();
            if((profile.Format!=CompetitionFormat.Sprint&&profile.Format!=CompetitionFormat.Pursuit)||
                tickFrequency<=0||athletes.Length<1||athletes.Length>60||athletes.Length>profile.FieldCapacity||
                layout.Route.CompetitionProfileId!=profile.Id||layout.Route.Length.Millimeters!=profile.Distance.Millimeters||layout.Route.LapCount!=profile.LapCount)
                throw new ArgumentException("A matched Sprint/Pursuit profile, bounded stable roster, layout and clock are required.");
            frequency=tickFrequency;roster=athletes.ToArray();
            for(int i=0;i<Count;i++)
            {
                if(!roster[i].IsValid)throw new ArgumentException("Valid roster identity required.");
                for(int j=0;j<i;j++)if(roster[i].Equals(roster[j]))throw new ArgumentException("Unique roster identities required.");
            }
            owner=new int[layout.Count];preparedOwner=new int[layout.Count];ownerBout=new int[layout.Count];preparedOwnerBout=new int[layout.Count];
            lastBout=new int[Count];preparedLastBout=new int[Count];for(int i=0;i<Count;i++)lastBout[i]=-1;
            ranks=new int[profile.BoutCount];preparedRanks=new int[profile.BoutCount];
            records=new PhysicalRangeReservationRecord[checked(Count*BoutCount)];preparedRecords=new PhysicalRangeReservationRecord[records.Length];
            arrivals=new PhysicalRangeArrival[Count];releases=new PhysicalRangeRelease[Count];arrivalIndices=new int[Count];releaseIndices=new int[Count];
            seenArrivals=new bool[Count];seenReleases=new bool[Count];denied=new bool[Count];
            privateLoops=new string[layout.Count];admissionLines=new long[BoutCount];releaseLines=new long[checked(layout.Count*BoutCount)];
            changes=new PhysicalRangeReservationChange[Count*2];preparedChanges=new PhysicalRangeReservationChange[Count*2];
            CapturePrivateCorridors(profile,layout,body);
            var context=new string[Count+5];context[0]="physical_range_reservations_v1";context[1]=profile.ToJson();context[2]=layout.ContextJson;context[3]=body.ToJson();
            context[4]=frequency.ToString(CultureInfo.InvariantCulture)+";accepted_fraction_order;release_before_equal_fraction_arrival;arrival_start_ordinal;lowest_free_physical_lane;strict_private_body_clearance;no_modulo;prepare_commit_only";
            for(int i=0;i<Count;i++)context[i+5]=roster[i].Value;ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }
        private void CapturePrivateCorridors(CompetitionProfile profile,PhysicalRangeFieldLayout layout,OccupancyDefinition body)
        {
            var definition=layout.Definition;var track=layout.Track;var route=layout.Route;
            var usage=new int[track.LoopCount];var seen=new bool[track.LoopCount];
            for(int p=0;p<LaneCount;p++)
            {
                Array.Clear(seen,0,seen.Length);var lane=definition.GetLane(p);
                for(int s=0;s<lane.SliceCount;s++){int index=LoopIndex(track,lane.GetSlice(s).LoopId);if(!seen[index]){usage[index]++;seen[index]=true;}}
            }
            long half=checked((long)Math.Ceiling(body.LongitudinalSeparation*SkiState.NanometersPerMeter*.5));
            for(int p=0;p<LaneCount;p++)
            {
                var lane=definition.GetLane(p);int privateIndex=-1;
                for(int s=0;s<lane.SliceCount;s++)
                {
                    var candidate=lane.GetSlice(s);
                    if(candidate.LoopId==lane.MatAnchor.LoopId)
                    {if(privateIndex>=0||usage[LoopIndex(track,candidate.LoopId)]!=1)throw new ArgumentException("Reusable mats require a distinct private mat loop used exactly once.");privateIndex=s;}
                    else if(LaneCount>1&&candidate.LoopId!=definition.MainLoopId&&usage[LoopIndex(track,candidate.LoopId)]==1)
                        throw new ArgumentException("Additional private corridors need a stronger reservation resource contract.");
                }
                if(privateIndex<=0||privateIndex>=lane.SliceCount-1)throw new ArgumentException("A shared entry, exclusive mat corridor and shared return are required.");
                var slice=lane.GetSlice(privateIndex);var loop=track.GetLoop(slice.LoopId);
                if(loop.Closed||slice.Start.Millimeters!=0||slice.End.Millimeters!=loop.Length.Millimeters||lane.MatAnchor.LoopId!=loop.Id)
                    throw new ArgumentException("The personal mat must occupy a complete distinct open private loop.");
                privateLoops[p]=loop.Id;
                for(int b=0;b<BoutCount;b++)
                {
                    int lap=profile.GetBout(b).AfterLap-1,matches=0;long origin=0;
                    for(int r=0;r<route.SectionCount;r++)
                    {var section=route.GetSection(r);if(section.LapIndex==lap&&section.HasRange&&section.LoopId==definition.MainLoopId){origin=checked((section.Start.Millimeters-section.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter);matches++;}}
                    if(matches!=1)throw new ArgumentException("One bound range occurrence per bout required.");
                    long line=checked(origin+track.Range.Entry.Distance.Millimeters*SkiState.NanometersPerMillimeter);
                    if(p==0)admissionLines[b]=line;else if(admissionLines[b]!=line)throw new ArgumentException("All mats require the same logical admission line.");
                    long clearance=checked(origin+(definition.Entry.Millimeters+slice.Offset.Millimeters+slice.Length.Millimeters)*SkiState.NanometersPerMillimeter+half+1);
                    long rangeExit=checked(origin+Math.Min(definition.Exit.Millimeters,track.Range.Exit.Distance.Millimeters)*SkiState.NanometersPerMillimeter);
                    if(clearance>=rangeExit)throw new ArgumentException("The shared return must clear the entire private body before the penalty/route exit.");
                    releaseLines[b*LaneCount+p]=clearance;
                }
            }
        }
        private static int LoopIndex(TrackDefinition track,string id)
        {for(int i=0;i<track.LoopCount;i++)if(track.GetLoop(i).Id==id)return i;throw new ArgumentException("Unknown private loop.");}
        public long AdmissionRouteNanometers(int bout)=>admissionLines[ValidateBout(bout)];
        public long ClearanceRouteNanometers(int laneOrdinal,int bout)=>releaseLines[ValidateBout(bout)*LaneCount+ValidateLane(laneOrdinal)];
        public string PrivateLoopId(int laneOrdinal)=>privateLoops[ValidateLane(laneOrdinal)];
        public int GetOwner(int laneOrdinal)=>owner[ValidateLane(laneOrdinal)]-1;
        public int GetOwnedLane(int actor){ValidateActor(actor);for(int l=0;l<LaneCount;l++)if(owner[l]==actor+1)return l+1;return 0;}
        public int GetOwnedBout(int actor){int lane=GetOwnedLane(actor);return lane==0?-1:ownerBout[lane-1];}
        public int GetLastAssignedBout(int actor)=>lastBout[ValidateActor(actor)];
        public int GetArrivalCount(int bout)=>ranks[ValidateBout(bout)];
        public PhysicalRangeReservationRecord GetRecord(int actor,int bout)=>records[Slot(actor,bout)];
        public PhysicalRangeReservationPreview Prepare(SimulationTick tick,ReadOnlySpan<PhysicalRangeRelease> acceptedReleases,ReadOnlySpan<PhysicalRangeArrival> acceptedArrivals)
        {
            ready=false;preparedChangeCount=0;
            if(!tick.Equals(Tick.Next())||acceptedReleases.Length>Count||acceptedArrivals.Length>Count)throw new ArgumentException("Consecutive clock and bounded final accepted facts required.");
            Array.Clear(seenArrivals,0,Count);Array.Clear(seenReleases,0,Count);Array.Clear(denied,0,Count);
            for(int r=0;r<acceptedReleases.Length;r++)
            {
                var row=acceptedReleases[r];int i=Index(row.AthleteId);
                if(!row.IsValid||i<0||seenReleases[i]||!row.Tick.Equals(tick)||row.TickFrequency!=frequency||row.BoutIndex<0||row.BoutIndex>=BoutCount||
                    row.LaneOrdinal<1||row.LaneOrdinal>LaneCount||row.Fraction<0||row.Fraction>JunctionAdmissionSession.FractionParts||
                    owner[row.LaneOrdinal-1]!=i+1||ownerBout[row.LaneOrdinal-1]!=row.BoutIndex||
                    row.ClearanceRouteNanometers!=ClearanceRouteNanometers(row.LaneOrdinal,row.BoutIndex))throw new ArgumentException("Foreign, stale, duplicate or unowned private clearance.");
                seenReleases[i]=true;releases[r]=row;releaseIndices[r]=i;
            }
            for(int r=0;r<acceptedArrivals.Length;r++)
            {
                var row=acceptedArrivals[r];int i=Index(row.AthleteId);
                if(!row.IsValid||i<0||seenArrivals[i]||!row.Tick.Equals(tick)||row.TickFrequency!=frequency||row.BoutIndex<0||row.BoutIndex>=BoutCount||
                    row.BoutIndex!=lastBout[i]+1||row.AdmissionRouteNanometers!=admissionLines[row.BoutIndex]||row.Fraction<0||row.Fraction>JunctionAdmissionSession.FractionParts)
                    throw new ArgumentException("Foreign, stale, duplicate or nonsequential common-line arrival.");
                seenArrivals[i]=true;arrivals[r]=row;arrivalIndices[r]=i;
            }
            Array.Copy(owner,preparedOwner,LaneCount);Array.Copy(ownerBout,preparedOwnerBout,LaneCount);Array.Copy(lastBout,preparedLastBout,Count);Array.Copy(ranks,preparedRanks,BoutCount);Array.Copy(records,preparedRecords,records.Length);
            Sort(acceptedArrivals.Length,acceptedReleases.Length);
            int release=0;
            for(int a=0;a<acceptedArrivals.Length;a++)
            {
                var row=arrivals[a];int actor=arrivalIndices[a];
                while(release<acceptedReleases.Length&&releases[release].Fraction<=row.Fraction)ApplyRelease(release++);
                for(int l=0;l<LaneCount;l++)if(preparedOwner[l]==actor+1)throw new ArgumentException("The previous private reservation must clear before the next arrival.");
                int free=0;while(free<LaneCount&&preparedOwner[free]!=0)free++;
                if(free==LaneCount){denied[actor]=true;continue;}
                preparedOwner[free]=actor+1;preparedOwnerBout[free]=row.BoutIndex;preparedLastBout[actor]=row.BoutIndex;
                preparedRecords[Slot(actor,row.BoutIndex)]=new PhysicalRangeReservationRecord(row,++preparedRanks[row.BoutIndex],free+1);
                preparedChanges[preparedChangeCount++]=new PhysicalRangeReservationChange(row.AthleteId,row.BoutIndex,free+1,row.Fraction,false);
            }
            while(release<acceptedReleases.Length)ApplyRelease(release++);
            revision=checked(revision+1);ready=true;return new PhysicalRangeReservationPreview(this,revision,tick);
        }
        private void Sort(int arrivalCount,int releaseCount)
        {
            for(int r=1;r<arrivalCount;r++)for(int j=r;j>0&&(arrivals[j].Fraction<arrivals[j-1].Fraction||arrivals[j].Fraction==arrivals[j-1].Fraction&&arrivalIndices[j]<arrivalIndices[j-1]);j--)
            {var row=arrivals[j];arrivals[j]=arrivals[j-1];arrivals[j-1]=row;int i=arrivalIndices[j];arrivalIndices[j]=arrivalIndices[j-1];arrivalIndices[j-1]=i;}
            for(int r=1;r<releaseCount;r++)for(int j=r;j>0&&(releases[j].Fraction<releases[j-1].Fraction||releases[j].Fraction==releases[j-1].Fraction&&releaseIndices[j]<releaseIndices[j-1]);j--)
            {var row=releases[j];releases[j]=releases[j-1];releases[j-1]=row;int i=releaseIndices[j];releaseIndices[j]=releaseIndices[j-1];releaseIndices[j-1]=i;}
        }
        private void ApplyRelease(int row)
        {
            var release=releases[row];int actor=releaseIndices[row],lane=release.LaneOrdinal-1;
            if(preparedOwner[lane]!=actor+1||preparedOwnerBout[lane]!=release.BoutIndex)throw new ArgumentException("Release no longer owns its private mat.");
            preparedRecords[Slot(actor,release.BoutIndex)]=preparedRecords[Slot(actor,release.BoutIndex)].Released(release);preparedOwner[lane]=0;
            preparedChanges[preparedChangeCount++]=new PhysicalRangeReservationChange(release.AthleteId,release.BoutIndex,release.LaneOrdinal,release.Fraction,true);
        }
        public void ValidatePreview(PhysicalRangeReservationPreview preview)
        {if(!ReferenceEquals(preview.Owner,this)||!ready||preview.Revision!=revision||!preview.Tick.Equals(Tick.Next()))throw new ArgumentException("Foreign, stale or consumed reservation preview.");}
        public bool WasPreparedDenied(PhysicalRangeReservationPreview preview,int actor){ValidatePreview(preview);return denied[ValidateActor(actor)];}
        public int GetPreparedOwner(PhysicalRangeReservationPreview preview,int lane){ValidatePreview(preview);return preparedOwner[ValidateLane(lane)]-1;}
        public PhysicalRangeReservationRecord GetPreparedRecord(PhysicalRangeReservationPreview preview,int actor,int bout){ValidatePreview(preview);return preparedRecords[Slot(actor,bout)];}
        public int GetPreparedArrivalCount(PhysicalRangeReservationPreview preview,int bout){ValidatePreview(preview);return preparedRanks[ValidateBout(bout)];}
        public void Commit(PhysicalRangeReservationPreview preview)
        {
            ValidatePreview(preview);ready=false;Array.Copy(preparedOwner,owner,LaneCount);Array.Copy(preparedOwnerBout,ownerBout,LaneCount);Array.Copy(preparedLastBout,lastBout,Count);Array.Copy(preparedRanks,ranks,BoutCount);Array.Copy(preparedRecords,records,records.Length);
            Array.Copy(preparedChanges,changes,preparedChangeCount);LastChangeCount=preparedChangeCount;Tick=preview.Tick;
        }
        private int Index(AthleteId id){for(int i=0;i<Count;i++)if(roster[i].Equals(id))return i;return -1;}
        private int ValidateActor(int actor){if(actor<0||actor>=Count)throw new ArgumentOutOfRangeException(nameof(actor));return actor;}
        private int ValidateBout(int bout){if(bout<0||bout>=BoutCount)throw new ArgumentOutOfRangeException(nameof(bout));return bout;}
        private int ValidateLane(int lane){if(lane<1||lane>LaneCount)throw new ArgumentOutOfRangeException(nameof(lane));return lane-1;}
        private int Slot(int actor,int bout)=>ValidateBout(bout)*Count+ValidateActor(actor);
    }
}
