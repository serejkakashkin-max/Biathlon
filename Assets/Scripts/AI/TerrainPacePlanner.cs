using System;
using Biathlon.Domain;
namespace Biathlon.AI
{
    public readonly struct TerrainPacePlan
    {
        public double PreviewMeters { get; }
        public double ClimbDemand { get; }
        public double TurnDemand { get; }
        public int EffortAdjustment { get; }
        internal TerrainPacePlan(double preview,double climb,double turn,int adjustment)
        { PreviewMeters=preview; ClimbDemand=climb; TurnDemand=turn; EffortAdjustment=adjustment; }
    }
    public sealed class TerrainPacePlanner
    {
        private readonly TerrainPlanningDefinition definition;
        public TerrainPacePlanner(TerrainPlanningDefinition definition)
        { this.definition=definition??throw new ArgumentNullException(nameof(definition)); }
        public TerrainPacePlan Evaluate(double speed, double horizonSeconds, PhysiologyState physiology, ReadOnlySpan<AiRouteLookahead> ahead)
        {
            PhysiologyNumbers.RequireRange(speed,0,1000,nameof(speed));
            PhysiologyNumbers.RequireRange(horizonSeconds,0.000001,120,nameof(horizonSeconds));
            if(physiology.HeartRateBpm<=0 || ahead.Length==0) throw new ArgumentException("Valid physiology and route preview required.");
            double preview=Math.Max(definition.MinimumPreviewMeters,Math.Min(definition.MaximumPreviewMeters,speed*horizonSeconds));
            double climb=0,turn=0;
            for(int i=0;i<ahead.Length;i++)
            {
                var sample=ahead[i];
                if(string.IsNullOrEmpty(sample.Sample.Track.SegmentId) || (i>0 && (sample.Offset.Millimeters<ahead[i-1].Offset.Millimeters ||
                    sample.Sample.RouteDistance.Millimeters-sample.Offset.Millimeters != ahead[0].Sample.RouteDistance.Millimeters-ahead[0].Offset.Millimeters)))
                    throw new ArgumentException("Valid ordered lookahead required.");
                double weight=Math.Max(0,1-sample.Offset.Meters/preview);
                climb=Math.Max(climb,weight*Math.Min(1,Math.Max(0,sample.Sample.Track.Grade)/definition.FullClimbGrade));
                turn=Math.Max(turn,weight*Math.Min(1,Math.Abs(sample.Sample.Track.CurvaturePerMeter)/definition.FullTurnCurvature));
            }
            int adjustment=(int)Math.Round(definition.ClimbEffort*climb*physiology.AnaerobicReserve*(1-physiology.MuscularFatigue)
                -definition.TurnEffortReduction*turn,MidpointRounding.ToEven);
            return new TerrainPacePlan(preview,PhysiologyNumbers.Quantize(climb),PhysiologyNumbers.Quantize(turn),adjustment);
        }
    }
}
