using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiSnowContactPreview
    {
        public bool IsValid {get;}
        public SkiContactMotion Motion {get;}
        public double AvailableLateralAcceleration {get;}
        public double FrontImpulseNewtonSeconds {get;}
        public double RearImpulseNewtonSeconds {get;}
        public double DissipatedEnergyJoules {get;}
        internal SkiSnowContactPreview(SkiContactMotion motion,double available,double front,double rear,double energy)
        {IsValid=true;Motion=motion;AvailableLateralAcceleration=available;FrontImpulseNewtonSeconds=front;RearImpulseNewtonSeconds=rear;DissipatedEnergyJoules=energy;}
    }

    /// <summary>Passive snow impulse at a frozen pose, retaining contact side-slip
    /// and spin. This velocity operator alone neither moves nor admits the body.</summary>
    public sealed class ManualSkiSnowContactSystem
    {
        private readonly double arm,inertiaPerMass,duration;
        public string ContextJson {get;}
        public string TrajectoryContextJson {get;}
        public ManualSkiSnowContactSystem(OccupancyDefinition body,int frequency)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(frequency<=0)throw new ArgumentOutOfRangeException(nameof(frequency));
            arm=body.BodyLength*.5;
            // Match the pair solver's rigid occupied-body inertia. Snow support uses
            // physical body length; personal clearance is not an extra ski contact.
            inertiaPerMass=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            duration=1.0/frequency;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_snow_slip_impulse_v2_exact_free_constraint",body.ToJson(),frequency.ToString(CultureInfo.InvariantCulture)});
            TrajectoryContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_contact_frozen_velocity_ramp_v1",ContextJson});
        }

        /// <summary>Spread this interval's passive impulse as constant acceleration.
        /// Retain world side-slip and physical spin, without heading realignment.
        /// Caller must still admit the trajectory against other bodies and edges.</summary>
        public SkiContactTrajectory PreviewTrajectory(ManualSkiContactBody body,SkiContactMotion motion,
            double lateralAccelerationLimit,double reservedTurningAcceleration,double seconds)
            =>new SkiContactTrajectory(body.Pose,motion,Preview(body,motion,lateralAccelerationLimit,reservedTurningAcceleration,seconds),seconds);

        /// <param name="lateralAccelerationLimit">Existing skill/snow/equipment cornering capability.</param>
        /// <param name="reservedTurningAcceleration">Grip already consumed by the admitted turn; cannot be spent twice on slip.</param>
        public SkiSnowContactPreview Preview(ManualSkiContactBody body,SkiContactMotion motion,
            double lateralAccelerationLimit,double reservedTurningAcceleration,double seconds)
        {
            if(!body.IsValid)throw new ArgumentException("Initialized body required.",nameof(body));
            PhysiologyNumbers.RequireRange(lateralAccelerationLimit,0,PhysiologyNumbers.MaximumMagnitude,nameof(lateralAccelerationLimit));
            PhysiologyNumbers.RequireRange(reservedTurningAcceleration,0,PhysiologyNumbers.MaximumMagnitude,nameof(reservedTurningAcceleration));
            PhysiologyNumbers.RequireRange(seconds,double.Epsilon,duration,nameof(seconds));
            double available=Math.Max(0,lateralAccelerationLimit-reservedTurningAcceleration);
            double limit=available*seconds*.5;
            if(limit==0)return new SkiSnowContactPreview(motion,available,0,0,0);
            double rightX=Math.Cos(body.Pose.HeadingRadians),rightZ=-Math.Sin(body.Pose.HeadingRadians);
            double side=motion.Velocity.X*rightX+motion.Velocity.Z*rightZ,w=motion.YawRate;
            double frontSpeed=side+w*arm,rearSpeed=side-w*arm;
            double ratio=arm*arm/inertiaPerMass,diagonal=1+ratio,cross=1-ratio;
            double bestFront=0,bestRear=0,bestEnergy=0;bool freeChosen=false;

            // Minimum kinetic energy subject to equal front/rear normal-load impulse
            // budgets. Solve the unconstrained point and the four box edges exactly;
            // sequential front/rear friction would invent spin from pure side slip.
            double freeFront=-side*.5-inertiaPerMass*w/(2*arm);
            double freeRear=-side*.5+inertiaPerMass*w/(2*arm);
            if(Math.Abs(freeFront)<=limit&&Math.Abs(freeRear)<=limit)Candidate(freeFront,freeRear,true);
            Candidate(-limit,Clamp(-(rearSpeed-cross*limit)/diagonal));
            Candidate(limit,Clamp(-(rearSpeed+cross*limit)/diagonal));
            Candidate(Clamp(-(frontSpeed-cross*limit)/diagonal),-limit);
            Candidate(Clamp(-(frontSpeed+cross*limit)/diagonal),limit);

            double impulse=bestFront+bestRear;
            var velocity=new SkiPlanarVelocity(motion.Velocity.X+rightX*impulse,motion.Velocity.Z+rightZ*impulse);
            double yaw=w+arm*(bestFront-bestRear)/inertiaPerMass;
            if(freeChosen)
            {
                // The unconstrained minimizer has exactly zero side velocity
                // and spin. Construct its null-space value directly, rather
                // than leaving cancellation residue from summed impulses.
                double fx=-rightZ,fz=rightX,forward=motion.Velocity.X*fx+motion.Velocity.Z*fz;
                velocity=new SkiPlanarVelocity(fx*forward,fz*forward);yaw=0;
            }
            return new SkiSnowContactPreview(new SkiContactMotion(velocity,yaw),available,
                bestFront*body.MassKilograms,bestRear*body.MassKilograms,-bestEnergy*body.MassKilograms);

            double Clamp(double value)=>Math.Max(-limit,Math.Min(limit,value));
            void Candidate(double front,double rear,bool free=false)
            {
                double energy=frontSpeed*front+rearSpeed*rear+.5*diagonal*(front*front+rear*rear)+cross*front*rear;
                if(energy>=bestEnergy)return;
                bestEnergy=energy;bestFront=front;bestRear=rear;freeChosen=free;
            }
        }
    }
}
