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
        public double BrakeFraction { get; }
        public ManualSkiMotionInput(int steeringInput,double deliveredPowerWatts,int corneringSkill,int downhillSkill,
            double lineQuality=1,double muscularFatigue=0,double gripMultiplier=1,double dragAreaMultiplier=1,
            double snowResistanceMultiplier=1,double steeringResponse=1,double windX=0,double windZ=0,double brakeFraction=0)
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
            PhysiologyNumbers.RequireRange(brakeFraction,0,1,nameof(brakeFraction));
            SteeringInput=steeringInput;DeliveredPowerWatts=deliveredPowerWatts;CorneringSkill=corneringSkill;DownhillSkill=downhillSkill;
            LineQuality=lineQuality;MuscularFatigue=muscularFatigue;GripMultiplier=gripMultiplier;DragAreaMultiplier=dragAreaMultiplier;
            SnowResistanceMultiplier=snowResistanceMultiplier;SteeringResponse=steeringResponse;WindX=windX;WindZ=windZ;IsValid=true;
            BrakeFraction=brakeFraction;
        }
        public ManualSkiMotionInput WithBrakeFraction(double fraction)
            =>new ManualSkiMotionInput(SteeringInput,DeliveredPowerWatts,CorneringSkill,DownhillSkill,
                LineQuality,MuscularFatigue,GripMultiplier,DragAreaMultiplier,SnowResistanceMultiplier,
                SteeringResponse,WindX,WindZ,fraction);
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
        private readonly double maximumScalarSpeed;
        public string ContextJson { get; }
        public string AcceptedVelocityContextJson {get;}
        public ManualSkiMotionSystem(SkiPhysicsDefinition physics,ManualSkiSteeringDefinition heading,CorneringDefinition corners,int frequency)
        {
            forces=new SkiForceSystem(physics,frequency);steering=new ManualSkiSteeringSystem(heading,frequency);
            cornering=new CorneringSystem(corners,physics);duration=1.0/frequency;
            maximumScalarSpeed=physics.MaximumSpeedMetersPerSecond;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_force_heading_preview_v2_explicit_braking",physics.ToJson(),heading.ToJson(),corners.ToJson(),frequency.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "controlled_brake_fraction;existing_shared_maximum_braking;max_not_sum;zero_default"});
            AcceptedVelocityContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_accepted_planar_velocity_force_v1",ContextJson,
                "planar_velocity_authority;directional_grade_scalar_rebase;ceil_nmps;initial_basis_rotation;heading_aligned_only;scalar_cap_reject;roundoff_scale_and_cap_16ulp"});
        }
        public ManualSkiMotionFrame Preview(SkiState ski,ManualSkiHeadingState pose,ManualRouteProjection observation,TrackSample surface,ManualSkiMotionInput input)
            =>PreviewDuration(ski,pose,observation,surface,input,duration);

        public ManualSkiMotionFrame PreviewAcceptedVelocity(SkiState ski,ManualSkiHeadingState pose,
            SkiPlanarVelocity velocity,ManualRouteProjection observation,TrackSample surface,ManualSkiMotionInput input)
            =>PreviewAcceptedVelocityDuration(ski,pose,velocity,observation,surface,input,duration);

        public ManualSkiMotionFrame PreviewAcceptedVelocity(SkiState ski,ManualSkiHeadingState pose,
            SkiPlanarVelocity velocity,ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input)
            =>PreviewAcceptedVelocityDuration(ski,pose,velocity,observation,surface,input,duration);

        public ManualSkiMotionFrame PreviewAcceptedVelocityDuration(SkiState ski,ManualSkiHeadingState pose,
            SkiPlanarVelocity velocity,ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,double seconds)
        {
            if(!observation.IsValid)throw new ArgumentException("Initialized physical occurrence required.");
            return PreviewAcceptedVelocityDuration(ski,pose,velocity,observation.GeometryProjection,surface,input,seconds);
        }

        public ManualSkiMotionFrame PreviewAcceptedVelocityDuration(SkiState ski,ManualSkiHeadingState pose,
            SkiPlanarVelocity velocity,ManualRouteProjection observation,TrackSample surface,ManualSkiMotionInput input,double seconds)
        {
            if(!pose.IsValid||!observation.IsValid)throw new ArgumentException("Initialized world observation required.");
            double grade=observation.GradeAlong(pose),planar=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double scalar=planar*Math.Sqrt(1+grade*grade);
            // Preserve the accepted horizontal velocity; only the scalar force
            // coordinate is rebased to the currently observed tangent plane.
            // Rounding upward keeps the effective planar scale at most one,
            // apart from the explicitly bounded floating encoding reserve.
            double encoded=Math.Ceiling(scalar/PhysiologyNumbers.MinimumPositive)*PhysiologyNumbers.MinimumPositive;
            // A sin/cos velocity norm at the encoded cap may round a few ulps
            // above that cap. Repair only this scalar-coordinate encoding;
            // the accepted world velocity is retained exactly. A real excess
            // still fails in the common force solver.
            if(encoded>maximumScalarSpeed&&scalar<=maximumScalarSpeed*(1+16*2.2204460492503131e-16))
                encoded=maximumScalarSpeed;
            var forceStart=new SkiState(ski.DistanceNanometers,encoded);
            double scale=planar==0?1/Math.Sqrt(1+grade*grade):planar/forceStart.SpeedMetersPerSecond;
            return PreviewCore(forceStart,pose,observation,surface,input,seconds,true,velocity,scale);
        }

        public ManualSkiMotionFrame PreviewDuration(SkiState ski,ManualSkiHeadingState pose,ManualRouteProjection observation,
            TrackSample surface,ManualSkiMotionInput input,double seconds)
            =>PreviewCore(ski,pose,observation,surface,input,seconds,false,default,0);

        private ManualSkiMotionFrame PreviewCore(SkiState ski,ManualSkiHeadingState pose,ManualRouteProjection observation,
            TrackSample surface,ManualSkiMotionInput input,double seconds,bool accepted,SkiPlanarVelocity velocity,double acceptedScale)
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
                input.DragAreaMultiplier,cornering.ControlledBrakingNewtons(turn.BrakingNewtons,input.BrakeFraction),input.SnowResistanceMultiplier);
            var heading=accepted?steering.PreviewAcceptedVelocity(pose,velocity,input.SteeringInput,travel,acceptedScale,
                    turn.LateralAccelerationLimit,input.SteeringResponse):
                steering.Preview(pose,input.SteeringInput,travel,scale,turn.LateralAccelerationLimit,input.SteeringResponse);
            return new ManualSkiMotionFrame(travel,heading,turn,actualSurface,wind);
        }
    }
}
