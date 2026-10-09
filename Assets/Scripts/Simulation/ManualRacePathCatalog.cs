using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualRacePathKind { BaseRoute, StartApproach, RangeApproach, RangeExit, PenaltyLoop }
    public enum ManualRacePathEnd { Continue, FiringLine, PenaltyDecision, PenaltyCircuit, Finish }

    /// <summary>One positive physical interval in one explicit race occurrence.
    /// Mapping a point to competition distance is not permission to award it.</summary>
    public readonly struct ManualRacePathOccurrence
    {
        public string Id {get;}
        public int Index {get;}
        public ManualRacePathKind Kind {get;}
        public ManualRacePathEnd End {get;}
        public int BaseSectionIndex {get;}
        public int LapIndex {get;}
        public int BoutIndex {get;}
        public int PenaltyOrdinal {get;}
        public string LoopId {get;}
        public long LocalStartNanometers {get;}
        public long LocalEndNanometers {get;}
        public long LengthNanometers=>LocalEndNanometers-LocalStartNanometers;
        public bool CountsTowardsRoute=>Kind!=ManualRacePathKind.StartApproach&&Kind!=ManualRacePathKind.PenaltyLoop;
        public long RouteStartNanometers {get;}
        public long RouteEndNanometers=>checked(RouteStartNanometers+(CountsTowardsRoute?LengthNanometers:0));
        internal long GeometryStartNanometers {get;}
        internal ManualRacePathOccurrence(string id,int index,ManualRacePathKind kind,ManualRacePathEnd end,
            int section,int lap,int bout,int penalty,string loop,long from,long to,long route,long geometry)
        {Id=id;Index=index;Kind=kind;End=end;BaseSectionIndex=section;LapIndex=lap;BoutIndex=bout;
            PenaltyOrdinal=penalty;LoopId=loop;LocalStartNanometers=from;LocalEndNanometers=to;
            RouteStartNanometers=route;GeometryStartNanometers=geometry;}
    }

    public readonly struct ManualRacePathObservation
    {
        internal ManualRacePathCatalog Owner {get;}
        internal ManualRouteProjection GeometryProjection {get;}
        public bool IsValid=>Owner!=null;
        public int OccurrenceIndex=>GeometryProjection.SectionIndex;
        public string LoopId=>GeometryProjection.LoopId;
        public long PhysicalArcNanometers {get;}
        /// <summary>A mapping only; no distance, shooting, lap or penalty receipt.</summary>
        public long MappedRouteNanometers {get;}
        public double LocalMeters=>GeometryProjection.LocalMeters;
        public double LateralMeters=>GeometryProjection.LateralMeters;
        public double HeightMeters=>GeometryProjection.HeightMeters;
        public double DistanceFromCentreMeters=>GeometryProjection.DistanceFromCentreMeters;
        public double ForwardX=>GeometryProjection.ForwardX;
        public double ForwardZ=>GeometryProjection.ForwardZ;
        public double GeometricGrade=>GeometryProjection.GeometricGrade;
        public double StationMetersPerPlanarMeter=>GeometryProjection.StationMetersPerPlanarMeter;
        public double GradeAlong(ManualSkiHeadingState pose)=>GeometryProjection.GradeAlong(pose);
        internal ManualRacePathObservation(ManualRacePathCatalog owner,ManualRouteProjection projection,long arc,long route)
        {Owner=owner;GeometryProjection=projection;PhysicalArcNanometers=arc;MappedRouteNanometers=route;}
    }

    public readonly struct ManualRacePathTarget
    {
        public bool IsValid=>LoopId!=null;
        public int OccurrenceIndex {get;}
        public string LoopId {get;}
        public long PhysicalArcNanometers {get;}
        public long MappedRouteNanometers {get;}
        public double X {get;} public double Y {get;} public double Z {get;}
        public double ForwardX {get;} public double ForwardZ {get;}
        internal ManualRacePathTarget(ManualRouteTarget point,long arc,long route)
        {OccurrenceIndex=point.SectionIndex;LoopId=point.LoopId;PhysicalArcNanometers=arc;MappedRouteNanometers=route;
            X=point.X;Y=point.Y;Z=point.Z;ForwardX=point.ForwardX;ForwardZ=point.ForwardZ;}
    }

    /// <summary>Immutable geometry for one exact format and selected mat path.
    /// It observes start, all counted intervals and each possible penalty circuit.
    /// It never chooses a mat, admits a transition or changes accepted race state.</summary>
    public sealed class ManualRacePathCatalog
    {
        private readonly RaceRoute route;
        private readonly CompetitionProfile profile;
        private readonly PhysicalRangePathMap rangePath;
        private readonly StartApproachMap startApproach;
        private readonly ManualRacePathOccurrence[] occurrences;
        private readonly TrackSampler[] samplers;
        private readonly int[] samplerIndexes,firingIndexes,penaltyEntryIndexes,penaltyReturnIndexes;
        private readonly int[,] penaltyIndexes;
        public int SchemaVersion=>1;
        public string Id {get;}
        public string ContextJson {get;}
        public int Count=>occurrences.Length;
        /// <summary>Start plus counted flow, excluding optional penalty occurrences.</summary>
        public int BaseFlowCount {get;}
        public int StartApproachIndex {get;}
        public string LaneId=>rangePath.LaneId;
        /// <summary>Coordinates here are a geometry namespace, never race distance.
        /// The legacy base-world ledger explicitly rejects this scoped map.</summary>
        public ManualRouteProjectionMap ProjectionGeometry {get;}
        public ManualRouteProjection GetGeometryObservation(ManualRacePathObservation observation)
            =>ReferenceEquals(observation.Owner,this)?observation.GeometryProjection:throw new ArgumentException("Exact owned physical observation required.");
        public ManualRacePathOccurrence Get(int index)=>occurrences[index];
        internal bool IsBoundTo(RaceRoute selected,PhysicalRangePathMap path)=>ReferenceEquals(route,selected)&&ReferenceEquals(rangePath,path);
        internal bool IsBoundTo(RaceRoute selected,PhysicalRangePathMap path,StartApproachMap start)
            =>IsBoundTo(selected,path)&&ReferenceEquals(startApproach,start);
        internal bool IsSameRace(ManualRacePathCatalog other)=>other!=null&&ReferenceEquals(route,other.route)&&ReferenceEquals(profile,other.profile);

        public ManualRacePathCatalog(TrackDefinition track,RaceRoute route,CompetitionProfile profile,
            TrackVisualDefinition visual,PhysicalRangePathMap rangePath,StartApproachMap startApproach=null)
        {
            if(track==null||route==null||profile==null||visual==null||rangePath==null)throw new ArgumentNullException();
            if(!rangePath.IsBoundTo(track,route)||track.ToJson()!=route.TrackSnapshotJson||
                profile.ToRouteRequirements().ToJson()!=route.RequirementsSnapshotJson||visual.TrackId!=track.Id||
                track.GetLoop(track.Penalty.LoopId).Length.Millimeters!=profile.PenaltyLoopDistance.Millimeters)
                throw new ArgumentException("Exact physical track, route, competition and assigned path required.");
            if(startApproach!=null&&(profile.Format!=CompetitionFormat.MassStart||
                startApproach.TrackJson!=route.TrackSnapshotJson||startApproach.RoutePlanJson!=route.PlanSnapshotJson||
                startApproach.RouteRequirementsJson!=route.RequirementsSnapshotJson))
                throw new ArgumentException("Start approach must match this exact simultaneous-start race.");
            this.route=route;this.profile=profile;this.rangePath=rangePath;this.startApproach=startApproach;
            Id=route.Id+":"+rangePath.LaneId+":manual_physical_paths_v1";
            firingIndexes=new int[profile.BoutCount];penaltyEntryIndexes=new int[profile.BoutCount];
            penaltyReturnIndexes=new int[profile.BoutCount];penaltyIndexes=new int[profile.BoutCount,profile.ShotsPerBout];
            for(int b=0;b<profile.BoutCount;b++)firingIndexes[b]=penaltyEntryIndexes[b]=penaltyReturnIndexes[b]=-1;
            var rows=new List<ManualRacePathOccurrence>();long geometryEnd=0;
            void Add(ManualRacePathKind kind,ManualRacePathEnd end,int section,int lap,int bout,int penalty,
                string loop,long from,long to,long canonical)
            {
                if(to<=from)throw new ArgumentException("Physical path occurrences must have positive length.");
                int index=rows.Count;
                rows.Add(new ManualRacePathOccurrence(Id+":"+index.ToString(CultureInfo.InvariantCulture),index,
                    kind,end,section,lap,bout,penalty,loop,from,to,canonical,geometryEnd));
                geometryEnd=checked(geometryEnd+to-from);
            }
            long Nano(TrackDistance value)=>checked(value.Millimeters*SkiState.NanometersPerMillimeter);
            StartApproachIndex=startApproach==null?-1:0;
            if(startApproach!=null)Add(ManualRacePathKind.StartApproach,ManualRacePathEnd.Continue,0,0,-1,0,
                startApproach.LoopId,0,startApproach.LengthNanometers,0);
            for(int s=0;s<route.SectionCount;s++)
            {
                var section=route.GetSection(s);long from=Nano(section.LoopStart),to=Nano(section.LoopEnd);
                long canonical=Nano(section.Start),origin=checked(canonical-from);
                bool overlay=rangePath.TryGetOverlayBounds(s,out long entry,out long exit);
                if(!overlay)
                {
                    Add(ManualRacePathKind.BaseRoute,s==route.SectionCount-1?ManualRacePathEnd.Finish:ManualRacePathEnd.Continue,
                        s,section.LapIndex,-1,0,section.LoopId,from,to,canonical);continue;
                }
                int bout=section.LapIndex;
                if(bout<0||bout>=profile.BoutCount||profile.GetBout(bout).AfterLap-1!=section.LapIndex)
                    throw new ArgumentException("Ordered bout occurrence required.");
                if(entry>canonical)Add(ManualRacePathKind.BaseRoute,ManualRacePathEnd.Continue,s,section.LapIndex,bout,0,
                    section.LoopId,from,entry-origin,canonical);
                for(int p=0;p<rangePath.SliceCount;p++)
                {
                    var slice=rangePath.GetSlice(p);long begin=Nano(slice.Start),end=Nano(slice.End);
                    long station=checked(entry+Nano(slice.Offset));
                    if(p==rangePath.FiringSliceIndex)
                    {
                        long mat=Nano(rangePath.MatAnchor.Distance);
                        firingIndexes[bout]=rows.Count;
                        Add(ManualRacePathKind.RangeApproach,ManualRacePathEnd.FiringLine,s,section.LapIndex,bout,0,
                            slice.LoopId,begin,mat,station);
                        Add(ManualRacePathKind.RangeExit,p==rangePath.SliceCount-1?ManualRacePathEnd.PenaltyDecision:ManualRacePathEnd.Continue,
                            s,section.LapIndex,bout,0,slice.LoopId,mat,end,checked(station+mat-begin));
                    }
                    else Add(p<rangePath.FiringSliceIndex?ManualRacePathKind.RangeApproach:ManualRacePathKind.RangeExit,
                        p==rangePath.SliceCount-1?ManualRacePathEnd.PenaltyDecision:ManualRacePathEnd.Continue,
                        s,section.LapIndex,bout,0,slice.LoopId,begin,end,station);
                }
                penaltyEntryIndexes[bout]=rows.Count-1;
                long physicalExit=checked(exit-origin),rejoin=Nano(track.Penalty.Rejoin.Distance);
                if(physicalExit!=Nano(track.Penalty.Entry.Distance)||rejoin<physicalExit||rejoin>to)
                    throw new ArgumentException("Penalty access and rejoin must belong to this exact counted section.");
                if(physicalExit<to)
                {
                    penaltyReturnIndexes[bout]=rows.Count;
                    // Expose the rejoin anchor explicitly; movement after a circuit
                    // still has to pay the original entry-to-rejoin interval.
                    if(rejoin>physicalExit)Add(ManualRacePathKind.BaseRoute,ManualRacePathEnd.Continue,s,section.LapIndex,bout,0,
                        section.LoopId,physicalExit,rejoin,exit);
                    if(rejoin<to)Add(ManualRacePathKind.BaseRoute,s==route.SectionCount-1?ManualRacePathEnd.Finish:ManualRacePathEnd.Continue,
                        s,section.LapIndex,bout,0,section.LoopId,rejoin,to,checked(origin+rejoin));
                }
                else penaltyReturnIndexes[bout]=rows.Count;
            }
            BaseFlowCount=rows.Count;
            long penaltyLength=Nano(track.GetLoop(track.Penalty.LoopId).Length);
            for(int b=0;b<profile.BoutCount;b++)
            {
                if(firingIndexes[b]<0||penaltyEntryIndexes[b]<0||penaltyReturnIndexes[b]>=BaseFlowCount)
                    throw new ArgumentException("Every bout needs explicit firing, penalty access and counted continuation.");
                long fixedRoute=rows[penaltyEntryIndexes[b]].RouteEndNanometers;
                for(int ordinal=1;ordinal<=profile.ShotsPerBout;ordinal++)
                {
                    penaltyIndexes[b,ordinal-1]=rows.Count;
                    Add(ManualRacePathKind.PenaltyLoop,ManualRacePathEnd.PenaltyCircuit,
                        rows[penaltyEntryIndexes[b]].BaseSectionIndex,profile.GetBout(b).AfterLap-1,b,ordinal,
                        track.Penalty.LoopId,0,penaltyLength,fixedRoute);
                }
            }
            occurrences=rows.ToArray();samplers=new TrackSampler[track.LoopCount];samplerIndexes=new int[Count];
            for(int i=0;i<samplers.Length;i++)samplers[i]=new TrackSampler(track.GetLoop(i));
            var scopes=new ManualProjectionScope[Count];var context=new string[Count+9];
            context[0]="manual_race_physical_occurrences_v3_shared_penalty_portals";context[1]=route.TrackSnapshotJson;
            context[2]=route.PlanSnapshotJson;context[3]=route.RequirementsSnapshotJson;context[4]=profile.ToJson();
            context[5]=rangePath.ContextJson;context[6]=startApproach?.ContextJson??"no_start_approach";
            context[7]=visual.ToJson();
            context[8]="explicit_occurrence;physical_arc_distinct_from_counted_route;no_mat_branch_or_penalty_admission;firing_departure_reuses_paid_incoming_plane_v1;penalty_entry_repeat_return_reuse_incoming_anchor_plane_v1";
            for(int i=0;i<Count;i++)
            {
                var row=occurrences[i];scopes[i]=new ManualProjectionScope(row.LoopId,row.LocalStartNanometers,row.LocalEndNanometers,row.GeometryStartNanometers);
                int loop=-1;for(int k=0;k<track.LoopCount;k++)if(track.GetLoop(k).Id==row.LoopId){loop=k;break;}
                if(loop<0)throw new ArgumentException("Unknown physical occurrence loop.");samplerIndexes[i]=loop;
                context[i+9]=ShootingModelDefinitions.EncodeContext(new[]{row.Id,((int)row.Kind).ToString(CultureInfo.InvariantCulture),
                    ((int)row.End).ToString(CultureInfo.InvariantCulture),row.BaseSectionIndex.ToString(CultureInfo.InvariantCulture),
                    row.LapIndex.ToString(CultureInfo.InvariantCulture),row.BoutIndex.ToString(CultureInfo.InvariantCulture),
                    row.PenaltyOrdinal.ToString(CultureInfo.InvariantCulture),row.LoopId,
                    row.LocalStartNanometers.ToString(CultureInfo.InvariantCulture),row.LocalEndNanometers.ToString(CultureInfo.InvariantCulture),
                    row.RouteStartNanometers.ToString(CultureInfo.InvariantCulture),row.GeometryStartNanometers.ToString(CultureInfo.InvariantCulture)});
            }
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
            var ordinaryEntries=new bool[Count];var firingDepartures=new bool[Count];
            var phaseEntries=new int[Count];var phaseExits=new int[Count];
            for(int i=0;i<Count;i++)phaseEntries[i]=phaseExits[i]=-1;
            for(int i=1;i<BaseFlowCount;i++)
            {
                ordinaryEntries[i]=occurrences[i-1].End==ManualRacePathEnd.Continue;
                firingDepartures[i]=occurrences[i-1].End==ManualRacePathEnd.FiringLine;
            }
            // All alternatives at the same penalty anchor observe one plane.
            // Selection and debt admission still belong to the race owner.
            // A changed station chart never changes the actual off-centre pose.
            for(int b=0;b<profile.BoutCount;b++)
            {
                int gate=penaltyEntryIndexes[b];phaseEntries[penaltyReturnIndexes[b]]=gate;
                for(int p=0;p<profile.ShotsPerBout;p++)
                {int index=penaltyIndexes[b,p];phaseEntries[index]=phaseExits[index]=gate;}
            }
            ProjectionGeometry=ManualRouteProjectionMap.ForPhysicalScopes(route,visual,scopes,ContextJson,ordinaryEntries,firingDepartures,phaseEntries,phaseExits);
        }

        public int GetFiringOccurrence(int bout)=>firingIndexes[bout];
        public int GetPenaltyEntryOccurrence(int bout)=>penaltyEntryIndexes[bout];
        public int GetPenaltyReturnOccurrence(int bout)=>penaltyReturnIndexes[bout];
        /// <summary>Describes an available circuit. It does not authorize penalty debt.</summary>
        public int GetPenaltyOccurrence(int bout,int ordinal)
        {
            if(bout<0||bout>=penaltyIndexes.GetLength(0)||ordinal<1||ordinal>penaltyIndexes.GetLength(1))
                throw new ArgumentOutOfRangeException();
            return penaltyIndexes[bout,ordinal-1];
        }
        /// <summary>Counted flow only. The phase owner must decide at every gate.</summary>
        public int GetBaseFlowSuccessor(int index)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            return index+1<BaseFlowCount?index+1:-1;
        }
        private ManualRacePathOccurrence ValidateArc(int index,long arc)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            var row=occurrences[index];if(arc<0||arc>row.LengthNanometers)throw new ArgumentOutOfRangeException(nameof(arc));return row;
        }
        private static long Mapped(ManualRacePathOccurrence row,long arc)=>checked(row.RouteStartNanometers+(row.CountsTowardsRoute?arc:0));
        public bool TryProject(ManualSkiHeadingState pose,int occurrenceIndex,long previousPhysicalArcNanometers,
            double maximumArcDisplacementMeters,out ManualRacePathObservation observation)
        {
            var row=ValidateArc(occurrenceIndex,previousPhysicalArcNanometers);observation=default;
            if(!ProjectionGeometry.TryProject(pose,occurrenceIndex,checked(row.GeometryStartNanometers+previousPhysicalArcNanometers),
                maximumArcDisplacementMeters,false,out var projected))return false;
            long arc=checked(projected.RouteNanometers-row.GeometryStartNanometers);
            if(arc<0||arc>row.LengthNanometers)throw new InvalidOperationException("Scoped projection escaped its physical interval.");
            observation=new ManualRacePathObservation(this,projected,arc,Mapped(row,arc));return true;
        }
        public bool TrySample(int occurrenceIndex,long physicalArcNanometers,out ManualRacePathTarget target)
        {
            var row=ValidateArc(occurrenceIndex,physicalArcNanometers);target=default;
            if(!ProjectionGeometry.TrySampleAhead(occurrenceIndex,checked(row.GeometryStartNanometers+physicalArcNanometers),0,false,out var point))return false;
            target=new ManualRacePathTarget(point,physicalArcNanometers,Mapped(row,physicalArcNanometers));return true;
        }
        internal bool TrySamplePortal(int occurrenceIndex,long physicalArcNanometers,out ManualRacePathTarget target)
        {
            var row=ValidateArc(occurrenceIndex,physicalArcNanometers);target=default;
            if(!ProjectionGeometry.TrySamplePortal(occurrenceIndex,checked(row.GeometryStartNanometers+physicalArcNanometers),out var point))return false;
            target=new ManualRacePathTarget(point,physicalArcNanometers,Mapped(row,physicalArcNanometers));return true;
        }
        internal bool WindowTouchesPortalEdge(int occurrence,long previousArc,double span,long portalArc)
        {
            var row=ValidateArc(occurrence,portalArc);ValidateArc(occurrence,previousArc);
            return ProjectionGeometry.WindowTouchesPortalEdge(occurrence,checked(row.GeometryStartNanometers+portalArc),
                checked(row.GeometryStartNanometers+previousArc),span);
        }
        public TrackSample SampleSurface(ManualRacePathObservation observation)
        {
            if(!ReferenceEquals(observation.Owner,this))throw new ArgumentException("An observation from this exact path catalog is required.");
            var row=ValidateArc(observation.OccurrenceIndex,observation.PhysicalArcNanometers);
            long local=checked(row.LocalStartNanometers+observation.PhysicalArcNanometers);
            return samplers[samplerIndexes[row.Index]].Sample(new TrackDistance(local/SkiState.NanometersPerMillimeter));
        }
    }
}
