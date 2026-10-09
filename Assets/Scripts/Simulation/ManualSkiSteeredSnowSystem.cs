using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ManualSkiSteeredSnowPreview
    {
        public bool IsValid=>Request.IsValid&&Snow.IsValid;
        public ManualSkiSteeringRequest Request {get;}
        public SkiSnowContactPreview Snow {get;}
        public double FrontDeflectionRadians {get;}
        public double AngularResponseScale {get;}
        internal ManualSkiSteeredSnowPreview(ManualSkiSteeringRequest request,SkiSnowContactPreview snow,double angle,double scale)
        {Request=request;Snow=snow;FrontDeflectionRadians=angle;AngularResponseScale=scale;}
    }

    /// <summary>Passive two-support snow constraint with a commanded front
    /// deflection. Converts existing travel into yaw through bounded snow forces;
    /// it neither assigns a heading nor creates rotational kinetic energy.</summary>
    public sealed class ManualSkiSteeredSnowSystem
    {
        private readonly ManualSkiSteeringSystem steering;
        private readonly double arm,inertiaPerMass,duration,minimumForwardSpeed;
        public string ContextJson {get;}
        public ManualSkiSteeredSnowSystem(OccupancyDefinition body,ManualSkiSteeringDefinition definition,int frequency)
        {
            if(body==null||definition==null)throw new ArgumentNullException();
            steering=new ManualSkiSteeringSystem(definition,frequency);duration=1.0/frequency;
            arm=body.BodyLength*.5;
            inertiaPerMass=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            minimumForwardSpeed=definition.MinimumSpeedForGripLimit;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_steered_snow_constraint_v2_exact_neutral_constraint",body.ToJson(),definition.ToJson(),
                frequency.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                "frozen_front_deflection_atan_2arm_target_over_signed_forward;rear_ski_axis;equal_support_grip_box;"+
                "minimum_total_kinetic_energy;shared_angular_acceleration_scale;raw_slip_spin;no_rest_pivot;no_heading_or_velocity_snap"});
        }
        public ManualSkiSteeredSnowPreview Preview(ManualSkiContactBody body,SkiContactMotion motion,
            int steeringInput,double lateralAccelerationLimit,double seconds,double postureResponse=1)
        {
            if(!body.IsValid)throw new ArgumentException("Initialized physical body required.",nameof(body));
            PhysiologyNumbers.RequireRange(seconds,double.Epsilon,duration,nameof(seconds));
            double speed=Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z);
            var request=steering.GetRequest(steeringInput,speed,lateralAccelerationLimit,postureResponse);
            double h=body.Pose.HeadingRadians,rx=Math.Cos(h),rz=-Math.Sin(h),fx=-rz,fz=rx;
            double forward=motion.Velocity.X*fx+motion.Velocity.Z*fz;
            double denominator=(forward<0?-1:1)*Math.Max(Math.Abs(forward),minimumForwardSpeed);
            double angle=Math.Atan(2*arm*request.TargetYawRate/denominator),c=Math.Cos(angle),s=Math.Sin(angle);
            double nx=rx*c-fx*s,nz=rz*c-fz*s,frontLever=arm*c,rearLever=-arm;
            double frontSpeed=motion.Velocity.X*nx+motion.Velocity.Z*nz+motion.YawRate*frontLever;
            double rearSpeed=motion.Velocity.X*rx+motion.Velocity.Z*rz+motion.YawRate*rearLever;
            double ratio=arm*arm/inertiaPerMass;
            double qff=1+c*c*ratio,qrr=1+ratio,qfr=c*(1-ratio);
            // Algebraically identical positive determinant, avoiding cancellation
            // for short supports, large inertia or almost parallel constraints.
            double det=s*s+ratio*(1+3*c*c);
            double limit=lateralAccelerationLimit*seconds*.5;
            double bestFront=0,bestRear=0,bestEnergy=0;bool freeChosen=false;
            if(limit>0)
            {
                double freeFront=(qfr*rearSpeed-qrr*frontSpeed)/det;
                double freeRear=(qfr*frontSpeed-qff*rearSpeed)/det;
                if(Math.Abs(freeFront)<=limit&&Math.Abs(freeRear)<=limit)Candidate(freeFront,freeRear,true);
                Candidate(-limit,Clamp(-(rearSpeed-qfr*limit)/qrr));
                Candidate(limit,Clamp(-(rearSpeed+qfr*limit)/qrr));
                Candidate(Clamp(-(frontSpeed-qfr*limit)/qff),-limit);
                Candidate(Clamp(-(frontSpeed+qfr*limit)/qff),limit);
            }
            // Scaling a dissipative impulse towards zero remains dissipative.
            // The command's acceleration limit acts on forces, never clamps a
            // pre-existing physical spin to the requested yaw-rate ceiling.
            double yawChange=(frontLever*bestFront+rearLever*bestRear)/inertiaPerMass;
            double scale=yawChange==0?1:Math.Min(1,request.AngularAccelerationLimit*seconds/Math.Abs(yawChange));
            bestFront*=scale;bestRear*=scale;
            double dx=nx*bestFront+rx*bestRear,dz=nz*bestFront+rz*bestRear;
            double dw=(frontLever*bestFront+rearLever*bestRear)/inertiaPerMass;
            double energy=frontSpeed*bestFront+rearSpeed*bestRear+
                .5*qff*bestFront*bestFront+.5*qrr*bestRear*bestRear+qfr*bestFront*bestRear;
            var finalMotion=new SkiContactMotion(new SkiPlanarVelocity(motion.Velocity.X+dx,motion.Velocity.Z+dz),motion.YawRate+dw);
            if(angle==0&&freeChosen&&scale==1)
                finalMotion=new SkiContactMotion(new SkiPlanarVelocity(fx*forward,fz*forward),0);
            var result=new SkiSnowContactPreview(finalMotion,lateralAccelerationLimit,
                bestFront*body.MassKilograms,bestRear*body.MassKilograms,-energy*body.MassKilograms);
            return new ManualSkiSteeredSnowPreview(request,result,angle,scale);

            double Clamp(double value)=>Math.Max(-limit,Math.Min(limit,value));
            void Candidate(double front,double rear,bool free=false)
            {
                double value=frontSpeed*front+rearSpeed*rear+.5*qff*front*front+.5*qrr*rear*rear+qfr*front*rear;
                if(value>=bestEnergy)return;
                bestEnergy=value;bestFront=front;bestRear=rear;freeChosen=free;
            }
        }
    }
}
