using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct MatGuidePoint
    {
        public readonly double X,Y,Z;
        internal MatGuidePoint(double x,double y,double z){X=x;Y=y;Z=z;}
    }
    // One read-only geometric trail for drawing and physical arrow capture.
    // Scene objects and renderer visibility never decide control ownership.
    public static class RangeMatGuidanceGeometry
    {
        public static long Station(RangeGuidanceDefinition guide,long route,int index)
        {
            long spacing=(long)Math.Round(guide.ChevronSpacing*SkiState.NanometersPerMeter);
            return (route/spacing+index+1)*spacing;
        }
        public static MatGuidePoint Point(VenueAuthoringPlan venue,PhysicalRangePathMap path,RangeGuidanceTrailDefinition settings,
            long at,long stop,bool pulse,int index,int count)
        {
            var range=venue.Manifest.Range;var mat=range.MatDefinition;
            var anchor=venue.MatPoint(path.LaneOrdinal-1);double fx=range.Forward.X,fz=range.Forward.Z;
            double rearX=anchor.X+fx*(mat.AthleteStopDepth-mat.Length),rearZ=anchor.Z+fz*(mat.AthleteStopDepth-mat.Length);
            double gap=settings.MatClearance+settings.ChevronFootprint;
            var end=new MatGuidePoint(rearX-fx*gap,anchor.Y,rearZ-fz*gap);
            if(pulse){double back=(count-1-index)*settings.FinalSpacing;return new MatGuidePoint(end.X-fx*back,end.Y,end.Z-fz*back);}
            long turnStart=stop-(long)Math.Round((path.MatAnchor.Distance.Meters+settings.TurnLead)*SkiState.NanometersPerMeter);
            MatGuidePoint Clear(long station)
            {
                var p=path.MapRoute(station);var sample=venue.Visual.Get(p.LoopId).Sample(p.LocalNanometers/(double)SkiState.NanometersPerMeter);
                double depth=(sample.X-rearX)*fx+(sample.Z-rearZ)*fz;
                double shift=Math.Min(0,-gap-settings.CorridorClearance-depth);
                return new MatGuidePoint(sample.X+fx*shift,sample.Y,sample.Z+fz*shift);
            }
            if(at<=turnStart)return Clear(at);
            var begin=Clear(Math.Max(0,turnStart));double along=(end.X-begin.X)*fx+(end.Z-begin.Z)*fz;
            double controlX=end.X-fx*along,controlZ=end.Z-fz*along;
            double t=Math.Max(0,Math.Min(1,(at-turnStart)/(double)(stop-turnStart))),u=1-t;
            return new MatGuidePoint(u*u*begin.X+2*u*t*controlX+t*t*end.X,u*begin.Y+t*end.Y,u*u*begin.Z+2*u*t*controlZ+t*t*end.Z);
        }
        public static bool Contains(VenueAuthoringPlan venue,PhysicalRangePathMap path,RangeGuidanceDefinition guide,RangeGuidanceTrailDefinition trail,
            ManualSkiHeadingState pose,long route,long stop,double radius)
        {
            double remaining=(stop-route)/(double)SkiState.NanometersPerMeter;
            if(remaining<=0||remaining>guide.MatShowLead)return false;
            bool pulse=remaining<=guide.MatAssistLead;int count=pulse?3:guide.ChevronCount;
            for(int i=0;i<count;i++)
            {
                long at=pulse?stop-(long)Math.Round(guide.MatAssistLead*(count-i)/(count+1.0)*SkiState.NanometersPerMeter):
                    Station(guide,route,i);
                if(!pulse&&at>=stop){if(i>0)break;at=route+(stop-route)/2;}
                var p=Point(venue,path,trail,at,stop,pulse,i,count);
                double dx=p.X-pose.X,dz=p.Z-pose.Z;
                if(dx*dx+dz*dz<=radius*radius)return true;
            }
            return false;
        }
    }
}
