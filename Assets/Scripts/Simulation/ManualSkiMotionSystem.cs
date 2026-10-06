using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Already delivered mechanical power and observed conditions. Effort,
    /// technique, equipment and tuck decisions remain with their shared systems.</summary>
    public readonly struct ManualSkiMotionInput
    {
        public bool IsValid { get; }
        public int SteeringInput { get; }
        public double DeliveredPowerWatts { get; }
        public int CorneringSkill { get; }
        public int DownhillSkill { get; }
        public double LineQuality { get; }
        public double MuscularFatigue { get; }
        public double GripMultiplier { get; }
        public double DragAreaMultiplier { get; }
        public double SnowResistanceMultiplier { get; }
        public double SteeringResponse { get; }
        public double WindX { get; }
        public double WindZ { get; }
        public ManualSkiMotionInput(int steeringInput,double deliveredPowerWatts,int corneringSkill,int downhillSkill,
            double lineQuality=1,double muscularFatigue=0,double gripMultiplier=1,double dragAreaMultiplier=1,
            double snowResistanceMultiplier=1,double steeringResponse=1,double windX=0,double windZ=0)
        {
            if(steeringInput< -ManualSkiCommand.SteeringUnits||steeringInput>ManualSkiCommand.SteeringUnits||corneringSkill<0||corneringSkill>100||downhillSkill<0||downhillSkill>100)
                throw new ArgumentOutOfRangeException();
            PhysiologyNumbers.RequireRange(deliveredPowerWatts,0,PhysiologyNumbers.MaximumMagnitude,nameof(deliveredPowerWatts));
            PhysiologyNumbers.RequireRange(lineQuality,0,1,nameof(lineQuality));
            PhysiologyNumbers.RequireRange(muscularFatigue,0,1,nameof(muscularFatigue));
            PhysiologyNumbers.RequireRange(gripMultiplier,PhysiologyNumbers.MinimumPositive,PhysiologyNumbers.MaximumMagnitude,nameof(gripMultiplier));
            PhysiologyNumbers.RequireRange(dragAreaMultiplier,0,1,nameof(dragAreaMultiplier));
            PhysiologyNumbers.RequireRange(snowResistanceMultiplier,PhysiologyNumbers.MinimumPositive,PhysiologyNumbers.MaximumMagnitude,nameof(snowResistanceMultiplier));
            PhysiologyNumbers.RequireRange(steeringResponse,0,1,nameof(steeringResponse));
            PhysiologyNumbers.RequireRange(windX,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(windX));
            PhysiologyNumbers.RequireRange(windZ,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(windZ));
            SteeringInput=steeringInput;DeliveredPowerWatts=deliveredPowerWatts;CorneringSkill=corneringSkill;DownhillSkill=downhillSkill;
            LineQuality=lineQuality;MuscularFatigue=muscularFatigue;GripMultiplier=gripMultiplier;DragAreaMultiplier=dragAreaMultiplier;
            SnowResistanceMultiplier=snowResistanceMultiplier;SteeringResponse=steeringResponse;WindX=windX;WindZ=windZ;IsValid=true;
        }
    }
    public readonly struct ManualSkiMotionFrame
    {
        public SkiForceTrajectory Travel { get; }
        public ManualSkiHeadingFrame Heading { get; }
        public CorneringOutput Cornering { get; }
        public TrackSample TravelSurface { get; }
        public double Headwind { get; }
        internal ManualSkiMotionFrame(SkiForceTrajectory travel,ManualSkiHeadingFrame heading,CorneringOutput cornering,TrackSample surface,double wind)
        { Travel=travel;Heading=heading;Cornering=cornering;TravelSurface=surface;Headwind=wind; }
    }
    /// <summary>Event-free shared manual movement preview on the currently observed
    /// surface. Does not commit race progress, admit swept bodies, or choose a branch.</summary>
    public sealed class ManualSkiMotionSystem
    {
        private readonly SkiForceSystem forces;
        private readonly ManualSkiSteeringSystem steering;
        private readonly CorneringSystem cornering;
        private readonly double duration;
        public string ContextJson { get; }
        public ManualSkiMotionSystem(SkiPhysicsDefinition physics,ManualSkiSteeringDefinition heading,CorneringDefinition corners,int frequency)
        {
            forces=new SkiForceSystem(physics,frequency);steering=new ManualSkiSteeringSystem(heading,frequency);
            cornering=new CorneringSystem(corners,physics);duration=1.0/frequency;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_force_heading_preview_v1",physics.ToJson(),heading.ToJson(),corners.ToJson(),frequency.ToString(System.Globalization.CultureInfo.InvariantCulture)});
        }
        public ManualSkiMotionFrame Preview(SkiState ski,ManualSkiHeadingState pose,ManualRouteProjection observation,TrackSample surface,ManualSkiMotionInput input)
            =>PreviewDuration(ski,pose,observation,surface,input,duration);

        public ManualSkiMotionFrame PreviewDuration(SkiState ski,ManualSkiHeadingState pose,ManualRouteProjection observation,
            TrackSample surface,ManualSkiMotionInput input,double seconds)
        {
            if(!input.IsValid||!pose.IsValid||!observation.IsValid||surface.LoopId!=observation.LoopId)
                throw new ArgumentException("Initialized motion and matching course/surface observations required.");
            // A sampled surface uses floor-to-millimetre station. Reject stale/different
            // observations; reserve only world-coordinate encoding precision at endpoints.
            if(Math.Abs(surface.Distance.Meters-observation.LocalMeters)>.001+2*PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Surface metadata must be sampled at the observed course station.");
            PhysiologyNumbers.RequireRange(seconds,double.Epsilon,duration,nameof(seconds));
            double grade=observation.GradeAlong(pose),scale=1/Math.Sqrt(1+grade*grade);
            // Route curvature is no longer a force input. Actual yaw is explicit,
            // avoiding curvature = yaw/speed singularities when starting from rest.
            var actualSurface=surface.WithTravelGeometry(grade,0);
            var turn=cornering.EvaluateWorldHeading(ski,actualSurface,pose.YawRateRadiansPerSecond,input.CorneringSkill,input.DownhillSkill,
                input.LineQuality,input.MuscularFatigue,input.GripMultiplier);
            double wind=-(input.WindX*Math.Sin(pose.HeadingRadians)+input.WindZ*Math.Cos(pose.HeadingRadians));
            var travel=forces.PreviewTrajectory(ski,input.DeliveredPowerWatts,actualSurface,seconds,wind,
                input.DragAreaMultiplier,turn.BrakingNewtons,input.SnowResistanceMultiplier);
            var heading=steering.Preview(pose,input.SteeringInput,travel,scale,turn.LateralAccelerationLimit,input.SteeringResponse);
            return new ManualSkiMotionFrame(travel,heading,turn,actualSurface,wind);
        }
    }
}
