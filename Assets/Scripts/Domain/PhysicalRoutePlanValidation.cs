using System;

namespace Biathlon.Domain
{
    /// <summary>Cold shared contract for counted route slices and their directed physical seams.
    /// Authoring and simulation use this same validation; presentation cannot grant connectivity.</summary>
    public static class PhysicalRoutePlanValidation
    {
        public static void Validate(TrackDefinition track, RaceRoutePlan plan, CompetitionRouteRequirements requirements,
            PhysicalJunctionDefinition junctions, PhysicalConnectionDefinition connections)
        {
            if(track==null||plan==null||requirements==null)throw new ArgumentNullException();
            if(plan.TrackId!=track.Id||plan.CompetitionProfileId!=requirements.Id||plan.LapCount!=requirements.LapCount)
                throw new ArgumentException("Route track/profile identity and lap count must match.");
            if(plan.SchemaVersion==2&&(junctions==null||connections==null))
                throw new ArgumentException("Schema2 physical slices require exact directed topology.");
            if(plan.SchemaVersion==2&&(junctions.TrackSnapshotJson!=track.ToJson()||connections.TrackSnapshotJson!=track.ToJson()||
                connections.JunctionSnapshotJson!=junctions.ToJson()))throw new ArgumentException("Route topology requires exact track and junction snapshots.");
            string previousLoop=null;long previousEnd=0,total=0;
            for(int lapIndex=0;lapIndex<plan.LapCount;lapIndex++)
            {
                var lap=plan.GetLap(lapIndex);int ranges=0;long rejoin=0;
                for(int i=0;i<lap.LoopCount;i++)
                {
                    var loop=track.GetLoop(lap.GetLoopId(i));
                    if(loop.Kind!=TrackLoopKind.Race)throw new ArgumentException("Penalty loops cannot contribute to base competition distance.");
                    var slice=plan.SchemaVersion==2?lap.GetSlice(i):null;
                    long start=slice?.Start.Millimeters??0,end=slice?.End.Millimeters??loop.Length.Millimeters;
                    if(end>loop.Length.Millimeters)throw new ArgumentException("Route slice exceeds its physical loop.");
                    if(plan.SchemaVersion==2)
                    {
                        if(previousLoop==null)
                        {if(slice.EnterConnectionId!=null)throw new ArgumentException("Initial route section has no counted incoming connection.");}
                        else if(previousLoop==loop.Id)
                        {
                            bool adjacent=previousEnd==start,wrap=loop.Closed&&previousEnd==loop.Length.Millimeters&&start==0;
                            if((!adjacent&&!wrap)||slice.EnterConnectionId!=null)
                                throw new ArgumentException("Same-loop route slices must be adjacent or explicitly close the physical loop without a teleport.");
                        }
                        else
                        {
                            if(slice.EnterConnectionId==null)throw new ArgumentException("Loop changes require a directed coincident seam ID.");
                            var connection=FindConnection(connections,slice.EnterConnectionId);
                            var from=FindPort(junctions,connection.FromPortId);var to=FindPort(junctions,connection.ToPortId);
                            if(from.Anchor.LoopId!=previousLoop||Canonical(track,previousLoop,from.Anchor.Distance.Millimeters)!=Canonical(track,previousLoop,previousEnd)||
                                to.Anchor.LoopId!=loop.Id||Canonical(track,loop.Id,to.Anchor.Distance.Millimeters)!=Canonical(track,loop.Id,start))
                                throw new ArgumentException("Route seam must connect the exact directed physical slice endpoints.");
                        }
                    }
                    if(loop.Id==track.Range.Entry.LoopId&&start<=track.Range.Entry.Distance.Millimeters&&
                        end>=Math.Max(track.Range.Exit.Distance.Millimeters,track.Penalty.Rejoin.Distance.Millimeters))
                    {ranges++;rejoin=checked(total+track.Penalty.Rejoin.Distance.Millimeters-start);}
                    total=checked(total+end-start);previousLoop=loop.Id;previousEnd=end;
                }
                if(requirements.RequiresRange(lapIndex)&&ranges==0)throw new ArgumentException("A required shooting lap has no range access.");
                if(plan.SchemaVersion==2&&requirements.RequiresRange(lapIndex)&&ranges!=1)
                    throw new ArgumentException("Exactly one complete range interval per shooting lap is required.");
                if(plan.SchemaVersion==2&&requirements.RequiresRange(lapIndex)&&rejoin>=total)
                    throw new ArgumentException("A shooting lap must retain real route travel after penalty rejoin before its lap boundary.");
            }
            if(total!=requirements.Distance.Millimeters)throw new ArgumentException("Composed distance does not match the competition profile.");
        }
        private static PhysicalConnection FindConnection(PhysicalConnectionDefinition definition,string id)
        {for(int i=0;i<definition.Count;i++)if(definition.Get(i).Id==id)return definition.Get(i);throw new ArgumentException("Unknown directed connection.");}
        private static PhysicalJunctionPort FindPort(PhysicalJunctionDefinition definition,string id)
        {for(int i=0;i<definition.Count;i++){var junction=definition.Get(i);for(int j=0;j<junction.PortCount;j++)if(junction.GetPort(j).Id==id)return junction.GetPort(j);}throw new ArgumentException("Unknown directed port.");}
        private static long Canonical(TrackDefinition track,string loopId,long local)
        {var loop=track.GetLoop(loopId);return loop.Closed&&local==loop.Length.Millimeters?0:local;}
    }
}
