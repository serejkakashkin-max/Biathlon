using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Event-free trajectory preview. No course tangent or ideal line can turn the athlete.</summary>
    public readonly struct ManualSkiHeadingFrame
    {
        private readonly ManualSkiHeadingState initial;
        private readonly double acceleration, reachSeconds;
        private readonly int substeps;
        private readonly SkiForceTrajectory travel;
        private readonly double planarScale;
        private readonly SkiPlanarVelocity acceptedVelocity;
        private readonly bool hasAcceptedVelocity;
        public ManualSkiHeadingState State { get; }
        public double DurationSeconds { get; }
        /// <summary>Horizontal world-plane metres, not the legacy slope-path odometer.</summary>
        public double PlanarTravelDistanceMeters { get; }
        public double RequestedYawRate { get; }
        public double TargetYawRate { get; }
        public double YawRateLimit { get; }
        public double HeadingDeltaRadians { get; }
        public double PeakYawRate { get; }
        public double LateralAccelerationDemand { get; }
        public double GripAccelerationLimit { get; }
        public bool RequestLimited => Math.Abs(RequestedYawRate) > YawRateLimit;
        public bool ExceedsGrip => LateralAccelerationDemand > GripAccelerationLimit;
        public bool IsValid => DurationSeconds > 0 && State.IsValid;
        public bool HasForceTrajectory => IsValid && travel.IsValid;
        public double PeakPlanarSpeed => travel.IsValid ? travel.PeakSpeed * planarScale : PlanarTravelDistanceMeters / DurationSeconds;
        /// <summary>Physical slope-path distance from the shared force solver.
        /// This is an odometer increment, never a projected route credit.</summary>
        public double TravelDistanceAt(double fraction)
        {
            if (!HasForceTrajectory) throw new InvalidOperationException("Force-weighted trajectory required.");
            PhysiologyNumbers.RequireRange(fraction, 0, 1, nameof(fraction));
            return travel.DistanceAt(DurationSeconds * fraction);
        }
        public double SlopeSpeedAt(double fraction)
        {
            if (!HasForceTrajectory) throw new InvalidOperationException("Force-weighted trajectory required.");
            PhysiologyNumbers.RequireRange(fraction, 0, 1, nameof(fraction));
            return travel.SpeedRaw(DurationSeconds * fraction);
        }
        /// <summary>Bound on centre acceleration, including the change of direction.</summary>
        public double PlanarAccelerationBound => Math.Abs(travel.Acceleration) * planarScale + PeakPlanarSpeed * PeakYawRate;

        /// <summary>Euclidean integration/encoding reserve for any force-weighted sample.
        /// The uniform-distance legacy overload has an unsplit angular kink and is not supported.</summary>
        public double PositionErrorBoundMeters
        {
            get
            {
                if (!HasForceTrajectory) throw new InvalidOperationException("Force-weighted trajectory required.");
                double w = PeakYawRate, alpha = Math.Abs(acceleration), a = Math.Abs(travel.Acceleration) * planarScale;
                // For f=v*exp(i*heading), |f''''| <= v*(w^4+6*w^2*alpha+3*alpha^2)
                // +4*|a|*(w^3+3*w*alpha). Each ramp kink is split by Integrate.
                double fourth = PeakPlanarSpeed * (w*w*w*w + 6*w*w*alpha + 3*alpha*alpha)
                    + 4*a*(w*w*w + 3*w*alpha);
                double panel = DurationSeconds / substeps;
                double quadrature = Math.Sqrt(2) * fourth * DurationSeconds * panel*panel*panel*panel / 2880;
                // At most three pieces; include coordinate summation, transcendental
                // arithmetic and nanometre state encoding. This is a numerical reserve,
                // not a claim of platform-independent interval arithmetic for libm.
                double roundoff = 64 * 2.2204460492503131e-16 * (3*substeps+1)
                    * (Math.Abs(initial.X)+Math.Abs(initial.Z)+PeakPlanarSpeed*DurationSeconds+1);
                return quadrature + roundoff + 1e-9;
            }
        }
        /// <summary>Includes rounding and the integer full-turn encoding's tiny difference from 2*pi.</summary>
        public double HeadingErrorBoundRadians => 2e-9 * (1 + PeakYawRate * DurationSeconds);

        public SkiPlanarVelocity VelocityAt(double fraction)
        {
            if (!HasForceTrajectory) throw new InvalidOperationException("Force-weighted trajectory required.");
            PhysiologyNumbers.RequireRange(fraction, 0, 1, nameof(fraction));
            if(fraction==0&&hasAcceptedVelocity)return acceptedVelocity;
            // The accepted endpoint must share the encoded heading and speed
            // used to initialize the next force tick. Interior samples retain
            // the continuous force path; endpoint encoding is covered by the
            // existing position/heading numerical reserves.
            double time = DurationSeconds*fraction;
            double heading = fraction==1?State.HeadingRadians:initial.HeadingRadians+AngleAt(time);
            double speed = (fraction==1?travel.Output.State.SpeedMetersPerSecond:travel.SpeedRaw(time))*planarScale;
            return fraction==1?new SkiPlanarVelocity(speed*Math.Sin(heading),speed*Math.Cos(heading)):ForceVelocityAt(time);
        }

        internal ManualSkiHeadingFrame(ManualSkiHeadingState initial, double duration, double distance,
            double requestedRate, double targetRate, double rateLimit, double acceleration, double reach,
            int substeps, double speed, double gripLimit, SkiForceTrajectory travel=default, double planarScale=1,
            bool hasAcceptedVelocity=false,SkiPlanarVelocity acceptedVelocity=default)
        {
            this.initial = initial; this.acceleration = acceleration; reachSeconds = reach; this.substeps = substeps;
            this.travel=travel;this.planarScale=planarScale;
            this.hasAcceptedVelocity=hasAcceptedVelocity;this.acceptedVelocity=acceptedVelocity;
            DurationSeconds = duration; PlanarTravelDistanceMeters = distance; RequestedYawRate = requestedRate;
            TargetYawRate = targetRate; YawRateLimit = rateLimit; GripAccelerationLimit = gripLimit;
            // Unity's C# compiler requires every struct field assigned before instance helpers.
            State = default; HeadingDeltaRadians = 0; PeakYawRate = 0; LateralAccelerationDemand = 0;
            HeadingDeltaRadians = AngleAt(duration);
            double endRate = RateAt(duration);
            PeakYawRate = Math.Max(Math.Abs(initial.YawRateRadiansPerSecond), Math.Abs(endRate));
            LateralAccelerationDemand = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(travel.IsValid?PeakDemand():speed * PeakYawRate,
                0, PhysiologyNumbers.MaximumMagnitude, "manual turning acceleration"));
            State = Integrate(duration);
        }

        /// <summary>Approximate trajectory sample; alone it grants no collision or edge admission.</summary>
        public ManualSkiHeadingState Sample(double fraction)
        {
            if (!IsValid) throw new InvalidOperationException("Cannot sample a default manual heading preview.");
            PhysiologyNumbers.RequireRange(fraction, 0, 1, nameof(fraction));
            if (fraction == 0) return initial;
            if (fraction == 1) return State;
            return Integrate(DurationSeconds * fraction);
        }

        private double AngleAt(double time)
        {
            double changing = Math.Min(time, reachSeconds);
            return initial.YawRateRadiansPerSecond * changing + acceleration * changing * changing * 0.5
                + TargetYawRate * (time - changing);
        }

        private double RateAt(double time) => time >= reachSeconds ? TargetYawRate : initial.YawRateRadiansPerSecond + acceleration * time;

        private ManualSkiHeadingState Integrate(double time)
        {
            double x = initial.X, z = initial.Z;
            if(travel.IsValid)
            {
                // Split at both ramp kinks. A stopped skier may still pivot, but cannot
                // translate during the stationary suffix of the physics step.
                double from=0;
                while(from<time)
                {
                    double to=NextBoundary(from,time);
                    double step=(to-from)/substeps;
                    for(int i=0;i<substeps;i++)
                    {
                        double a=from+i*step,b=a+step*.5,c=a+step;
                        var va=ForceVelocityAt(a);var vb=ForceVelocityAt(b);var vc=ForceVelocityAt(c);
                        x+=step/6*(va.X+4*vb.X+vc.X);
                        z+=step/6*(va.Z+4*vb.Z+vc.Z);
                    }
                    from=to;
                }
                return new ManualSkiHeadingState(x,z,initial.HeadingRadians+AngleAt(time),RateAt(time));
            }
            if (PlanarTravelDistanceMeters != 0)
            {
                // Fixed composite Simpson quadrature of continuous yaw. Distance is supplied
                // by the speed solver; this first kernel uses uniform travel density in time.
                double step = time / substeps;
                double weight = PlanarTravelDistanceMeters / DurationSeconds * step / 6;
                for (int i = 0; i < substeps; i++)
                {
                    double from = i * step, middle = from + step * 0.5, to = from + step;
                    double a = initial.HeadingRadians + AngleAt(from);
                    double b = initial.HeadingRadians + AngleAt(middle);
                    double c = initial.HeadingRadians + AngleAt(to);
                    x += weight * (Math.Sin(a) + 4 * Math.Sin(b) + Math.Sin(c));
                    z += weight * (Math.Cos(a) + 4 * Math.Cos(b) + Math.Cos(c));
                }
            }
            return new ManualSkiHeadingState(x, z, initial.HeadingRadians + AngleAt(time), RateAt(time));
        }

        private SkiPlanarVelocity ForceVelocityAt(double time)
        {
            double angle=AngleAt(time),speed=travel.SpeedRaw(time);
            if(hasAcceptedVelocity&&travel.InitialState.SpeedMetersPerSecond>0)
            {
                if(time==0)return acceptedVelocity;
                double ratio=speed/travel.InitialState.SpeedMetersPerSecond;
                double sine=Math.Sin(angle),cosine=Math.Cos(angle);
                return new SkiPlanarVelocity((acceptedVelocity.X*cosine+acceptedVelocity.Z*sine)*ratio,
                    (acceptedVelocity.Z*cosine-acceptedVelocity.X*sine)*ratio);
            }
            double heading=initial.HeadingRadians+angle;
            return new SkiPlanarVelocity(speed*planarScale*Math.Sin(heading),speed*planarScale*Math.Cos(heading));
        }

        private double NextBoundary(double from,double end)
        {
            if(reachSeconds>from&&reachSeconds<end)end=reachSeconds;
            if(travel.SpeedBoundarySeconds>from&&travel.SpeedBoundarySeconds<end)end=travel.SpeedBoundarySeconds;
            return end;
        }
        private double PeakDemand()
        {
            double from=0,peak=0;
            while(from<DurationSeconds)
            {
                double to=NextBoundary(from,DurationSeconds);
                double v=travel.SpeedRaw(from)*planarScale,w=RateAt(from);
                double a=from<travel.SpeedBoundarySeconds?travel.Acceleration*planarScale:0;
                double alpha=from<reachSeconds?acceleration:0;
                peak=Math.Max(peak,Math.Max(Math.Abs(v*w),Math.Abs(travel.SpeedRaw(to)*planarScale*RateAt(to))));
                // v(t)*yawRate(t) is quadratic on this interval; its internal extremum
                // matters when acceleration and angular braking act in opposite ways.
                double quadratic=a*alpha;
                if(quadratic!=0)
                {
                    double t=-(a*w+alpha*v)/(2*quadratic);
                    if(t>0&&t<to-from)peak=Math.Max(peak,Math.Abs((v+a*t)*(w+alpha*t)));
                }
                from=to;
            }
            return peak;
        }
    }

    /// <summary>A bounded intent, not an assignment to a body's physical yaw.</summary>
    public readonly struct ManualSkiSteeringRequest
    {
        public bool IsValid {get;}
        public double RequestedYawRate {get;}
        public double TargetYawRate {get;}
        public double YawRateLimit {get;}
        public double AngularAccelerationLimit {get;}
        internal ManualSkiSteeringRequest(double requested,double target,double limit,double acceleration)
        {IsValid=true;RequestedYawRate=requested;TargetYawRate=target;YawRateLimit=limit;AngularAccelerationLimit=acceleration;}
    }

    /// <summary>Pure fixed-tick manual heading. Callers retain route, forces, bodies and joint commit ownership.</summary>
    public sealed class ManualSkiSteeringSystem
    {
        private readonly ManualSkiSteeringDefinition definition;
        private readonly double duration;

        public ManualSkiSteeringSystem(ManualSkiSteeringDefinition definition, int frequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            duration = 1.0 / frequency;
            if (definition.YawAcceleration * duration < PhysiologyNumbers.MinimumPositive ||
                definition.NeutralYawDeceleration * duration < PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Manual angular response is below state encoding precision.");
        }

        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state, ManualSkiCommand command,
            double planarTravelDistanceMeters, double speedMetersPerSecond, double gripAccelerationLimit, double postureResponse = 1)
        {
            if (!command.IsValid) throw new ArgumentException("A valid fixed-tick manual command is required.", nameof(command));
            return Preview(state, command.SteeringInput, planarTravelDistanceMeters, speedMetersPerSecond, gripAccelerationLimit, postureResponse);
        }

        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state, int steeringInput,
            double planarTravelDistanceMeters, double speedMetersPerSecond, double gripAccelerationLimit, double postureResponse = 1)
            => Build(state,steeringInput,planarTravelDistanceMeters,speedMetersPerSecond,gripAccelerationLimit,postureResponse,duration);

        /// <summary>Integrate actual force-solver speed against continuous heading.
        /// Planar scale is cos(actual travel grade), frozen over this surface interval.
        /// The caller must split surface changes and still validate swept bodies/edges.</summary>
        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state,int steeringInput,
            SkiForceTrajectory travel,double planarScale,double gripAccelerationLimit,double postureResponse=1)
        {
            if(!travel.IsValid||travel.DurationSeconds>duration)throw new ArgumentException("Travel must fit the fixed tick.",nameof(travel));
            PhysiologyNumbers.RequireRange(planarScale,PhysiologyNumbers.MinimumPositive,1,nameof(planarScale));
            return Build(state,steeringInput,travel.DistanceAt(travel.DurationSeconds)*planarScale,travel.PeakSpeed*planarScale,
                gripAccelerationLimit,postureResponse,travel.DurationSeconds,travel,planarScale);
        }
        /// <summary>Continue an accepted heading-aligned velocity exactly. A
        /// significant side-slip needs its contact/slip continuation solver.</summary>
        public ManualSkiHeadingFrame PreviewAcceptedVelocity(ManualSkiHeadingState state,SkiPlanarVelocity velocity,
            int steeringInput,SkiForceTrajectory travel,double planarScale,double gripAccelerationLimit,double postureResponse=1)
        {
            if(!travel.IsValid||travel.DurationSeconds>duration)throw new ArgumentException("Travel must fit the fixed tick.",nameof(travel));
            // A norm of an encoded sin/cos velocity can round one ulp above
            // its encoded scalar speed. Retain that exact velocity and its
            // conservative bound; do not clamp or reconstruct it.
            const double maximumEncodedScale=1+16*2.2204460492503131e-16;
            PhysiologyNumbers.RequireRange(planarScale,double.Epsilon,maximumEncodedScale,nameof(planarScale));
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double forward=velocity.X*Math.Sin(state.HeadingRadians)+velocity.Z*Math.Cos(state.HeadingRadians);
            double side=velocity.X*Math.Cos(state.HeadingRadians)-velocity.Z*Math.Sin(state.HeadingRadians);
            double reserve=2*PhysiologyNumbers.MinimumPositive*speed;
            if(!state.IsValid||forward<0||Math.Abs(side)>reserve||
                Math.Abs(speed-travel.InitialState.SpeedMetersPerSecond*planarScale)>PhysiologyNumbers.MinimumPositive||
                travel.InitialState.SpeedMetersPerSecond==0&&speed!=0)
                throw new ArgumentException("Heading-aligned accepted velocity and matching force start required.");
            double exactScale=travel.InitialState.SpeedMetersPerSecond==0?planarScale:speed/travel.InitialState.SpeedMetersPerSecond;
            PhysiologyNumbers.RequireRange(exactScale,double.Epsilon,maximumEncodedScale,nameof(velocity));
            return Build(state,steeringInput,travel.DistanceAt(travel.DurationSeconds)*exactScale,travel.PeakSpeed*exactScale,
                gripAccelerationLimit,postureResponse,travel.DurationSeconds,travel,exactScale,true,velocity);
        }
        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state,ManualSkiCommand command,
            SkiForceTrajectory travel,double planarScale,double gripAccelerationLimit,double postureResponse=1)
        {
            if(!command.IsValid)throw new ArgumentException("A valid fixed-tick manual command is required.",nameof(command));
            return Preview(state,command.SteeringInput,travel,planarScale,gripAccelerationLimit,postureResponse);
        }
        /// <summary>Read the same data-driven command limits without requiring
        /// post-contact physical spin to fit the commanded yaw ceiling.</summary>
        public ManualSkiSteeringRequest GetRequest(int steeringInput,double speedMetersPerSecond,
            double gripAccelerationLimit,double postureResponse=1)
        {
            if(steeringInput< -ManualSkiCommand.SteeringUnits||steeringInput>ManualSkiCommand.SteeringUnits)
                throw new ArgumentOutOfRangeException(nameof(steeringInput));
            PhysiologyNumbers.RequireRange(speedMetersPerSecond,0,PhysiologyNumbers.MaximumMagnitude,nameof(speedMetersPerSecond));
            PhysiologyNumbers.RequireRange(gripAccelerationLimit,0,PhysiologyNumbers.MaximumMagnitude,nameof(gripAccelerationLimit));
            PhysiologyNumbers.RequireRange(postureResponse,0,1,nameof(postureResponse));
            double response=(1-Math.Min(1,speedMetersPerSecond/definition.HighSpeedReference)*
                (1-definition.HighSpeedResponseMultiplier))*postureResponse;
            double requested=definition.MaximumYawRate*steeringInput/ManualSkiCommand.SteeringUnits*response;
            double limit=Math.Min(definition.MaximumYawRate*response,
                gripAccelerationLimit/Math.Max(speedMetersPerSecond,definition.MinimumSpeedForGripLimit));
            double bounded=Math.Max(-limit,Math.Min(limit,requested));
            double target=Math.Sign(bounded)*Math.Floor(Math.Abs(bounded)*ManualSkiHeadingState.NanoradiansPerRadian)/
                ManualSkiHeadingState.NanoradiansPerRadian;
            return new ManualSkiSteeringRequest(requested,target,limit,
                steeringInput==0?definition.NeutralYawDeceleration:definition.YawAcceleration*response);
        }
        private ManualSkiHeadingFrame Build(ManualSkiHeadingState state,int steeringInput,double planarTravelDistanceMeters,
            double speedMetersPerSecond,double gripAccelerationLimit,double postureResponse,double frameDuration,
            SkiForceTrajectory travel=default,double planarScale=1,bool hasAcceptedVelocity=false,SkiPlanarVelocity acceptedVelocity=default)
        {
            if (!state.IsValid) throw new ArgumentException("An explicitly initialized world pose is required.", nameof(state));
            if (steeringInput < -ManualSkiCommand.SteeringUnits || steeringInput > ManualSkiCommand.SteeringUnits)
                throw new ArgumentOutOfRangeException(nameof(steeringInput));
            PhysiologyNumbers.RequireRange(planarTravelDistanceMeters, 0, PhysiologyNumbers.MaximumMagnitude, nameof(planarTravelDistanceMeters));
            PhysiologyNumbers.RequireRange(speedMetersPerSecond, 0, PhysiologyNumbers.MaximumMagnitude, nameof(speedMetersPerSecond));
            PhysiologyNumbers.RequireRange(gripAccelerationLimit, 0, PhysiologyNumbers.MaximumMagnitude, nameof(gripAccelerationLimit));
            PhysiologyNumbers.RequireRange(postureResponse, 0, 1, nameof(postureResponse));
            PhysiologyNumbers.RequireRange(state.YawRateRadiansPerSecond, -definition.MaximumYawRate, definition.MaximumYawRate, nameof(state));
            var request=GetRequest(steeringInput,speedMetersPerSecond,gripAccelerationLimit,postureResponse);
            double change = request.TargetYawRate - state.YawRateRadiansPerSecond;
            double magnitude = request.AngularAccelerationLimit;
            double acceleration = Math.Sign(change) * magnitude;
            double reach = change == 0 ? 0 : magnitude == 0 ? double.PositiveInfinity : Math.Abs(change) / magnitude;
            return new ManualSkiHeadingFrame(state, frameDuration, planarTravelDistanceMeters, request.RequestedYawRate,
                request.TargetYawRate, request.YawRateLimit, acceleration, reach, definition.PathIntegrationSubsteps, speedMetersPerSecond, gripAccelerationLimit,travel,planarScale,hasAcceptedVelocity,acceptedVelocity);
        }
    }
}
