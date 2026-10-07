using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ManualAiTrafficNeighbor
    {
        public AthleteId Athlete {get;}
        public ManualRouteProjection Course {get;}
        public ManualAiTrafficNeighbor(AthleteId athlete,ManualRouteProjection course)
        {
            if(!athlete.IsValid||!course.IsValid)throw new ArgumentException("Observed active actor required.");
            Athlete=athlete;Course=course;
        }
    }
    public readonly struct ManualAiWorldTrafficPlan
    {
        public bool IsValid {get;}
        public bool ObstructionObserved {get;}
        public bool HasPassingLine {get;}
        public AthleteId Obstructor {get;}
        public double DesiredLateralMeters {get;}
        public double UsableHalfWidthMeters {get;}
        public double ForwardGapMeters {get;}
        internal ManualAiWorldTrafficPlan(bool obstructed,bool passing,AthleteId blocker,
            double target,double halfWidth,double gap)
        {IsValid=true;ObstructionObserved=obstructed;HasPassingLine=passing;
         Obstructor=blocker;DesiredLateralMeters=target;
         UsableHalfWidthMeters=halfWidth;ForwardGapMeters=gap;}
    }

    /// <summary>Choice of an observed open passing line; never a lateral move.
    /// The AI yaw controller and shared world-body/course sweep must admit any
    /// resulting motion. All athletes, including the player, remain physical.</summary>
    public sealed class ManualAiWorldTrafficPlanner
    {
        private readonly ManualAiTrafficDefinition policy;
        private readonly OccupancyDefinition bodies;
        private readonly double tubeBodyRadius;
        public string ContextJson {get;}
        public ManualAiWorldTrafficPlanner(ManualAiTrafficDefinition policy,OccupancyDefinition bodies)
        {
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            this.bodies=bodies??throw new ArgumentNullException(nameof(bodies));
            tubeBodyRadius=.5*Math.Sqrt(bodies.LongitudinalSeparation*bodies.LongitudinalSeparation+
                bodies.LateralSeparation*bodies.LateralSeparation)+bodies.EdgeClearance;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{
                    "manual_ai_observed_world_bypass_line_v2",policy.ToJson(),bodies.ToJson(),
                    "candidate_line_only;observed_blocker_hold_until_body_clear;common_yaw_and_body_course_admission_required"});
                ContextJson=Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public ManualAiWorldTrafficPlan Plan(AthleteId self,ManualRouteProjection course,
            ReadOnlySpan<ManualAiTrafficNeighbor> others,double courseWidthMeters,
            ManualAiWorldTrafficPlan previous=default)
        {
            if(!self.IsValid||!course.IsValid)throw new ArgumentException("Observed self required.");
            PhysiologyNumbers.RequireRange(courseWidthMeters,0,PhysiologyNumbers.MaximumMagnitude,
                nameof(courseWidthMeters));
            double halfWidth=courseWidthMeters*.5-tubeBodyRadius;
            if(halfWidth<=0)return new ManualAiWorldTrafficPlan(false,false,default,
                course.LateralMeters,0,0);
            double lead=policy.ObstructionLookaheadMeters;
            if(previous.IsValid&&previous.HasPassingLine&&previous.Obstructor.IsValid&&
                Math.Abs(previous.DesiredLateralMeters)<=halfWidth)
                for(int i=0;i<others.Length;i++)if(others[i].Athlete.Equals(previous.Obstructor))
                {
                    var at=others[i].Course;
                    if(at.SectionIndex!=course.SectionIndex||at.LoopId!=course.LoopId)break;
                    double gap=(at.RouteNanometers-course.RouteNanometers)/
                        (double)SkiState.NanometersPerMeter;
                    if(gap> -bodies.LongitudinalSeparation&&gap<=lead&&
                        Math.Abs(at.LateralMeters-previous.DesiredLateralMeters)>=bodies.LateralSeparation&&
                        OpenLine(previous.DesiredLateralMeters,i,course,others,lead))
                        return new ManualAiWorldTrafficPlan(true,true,others[i].Athlete,
                            previous.DesiredLateralMeters,halfWidth,gap);
                    break;
                }
            int nearest=-1;double nearestGap=double.MaxValue;
            for(int i=0;i<others.Length;i++)
            {
                var other=others[i];if(other.Athlete.Equals(self))continue;
                var at=other.Course;
                if(at.SectionIndex!=course.SectionIndex||at.LoopId!=course.LoopId)continue;
                double gap=(at.RouteNanometers-course.RouteNanometers)/(double)SkiState.NanometersPerMeter;
                if(gap<=0||gap>lead||Math.Abs(at.LateralMeters-course.LateralMeters)>=bodies.LateralSeparation)
                    continue;
                if(gap<nearestGap||gap==nearestGap&&string.CompareOrdinal(other.Athlete.Value,
                    others[nearest].Athlete.Value)<0)
                {nearest=i;nearestGap=gap;}
            }
            if(nearest<0)return new ManualAiWorldTrafficPlan(false,false,default,
                course.LateralMeters,halfWidth,0);
            var blocker=others[nearest];
            double displacement=bodies.LateralSeparation+policy.PassLineClearanceMeters;
            double right=blocker.Course.LateralMeters+displacement;
            double left=blocker.Course.LateralMeters-displacement;
            bool canRight=right<=halfWidth&&right>=-halfWidth&&OpenLine(right,nearest,course,others,lead);
            bool canLeft=left>=-halfWidth&&left<=halfWidth&&OpenLine(left,nearest,course,others,lead);
            if(!canRight&&!canLeft)return new ManualAiWorldTrafficPlan(true,false,blocker.Athlete,
                course.LateralMeters,halfWidth,nearestGap);
            double rightMargin=PhysiologyNumbers.Quantize(halfWidth-right);
            double leftMargin=PhysiologyNumbers.Quantize(left+halfWidth);
            bool chooseRight=canRight&&(!canLeft||rightMargin>leftMargin||
                rightMargin==leftMargin&&policy.PreferRightOnTie);
            return new ManualAiWorldTrafficPlan(true,true,blocker.Athlete,
                chooseRight?right:left,halfWidth,nearestGap);
        }
        private bool OpenLine(double target,int blockedIndex,ManualRouteProjection self,
            ReadOnlySpan<ManualAiTrafficNeighbor> others,double lead)
        {
            for(int i=0;i<others.Length;i++)if(i!=blockedIndex)
            {
                var at=others[i].Course;
                if(at.SectionIndex!=self.SectionIndex||at.LoopId!=self.LoopId)continue;
                double gap=(at.RouteNanometers-self.RouteNanometers)/(double)SkiState.NanometersPerMeter;
                if(gap>0&&gap<=lead&&Math.Abs(at.LateralMeters-target)<bodies.LateralSeparation)
                    return false;
            }
            return true;
        }
    }
}
