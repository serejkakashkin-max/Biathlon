using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>AI-only bounded intent from the same two-support snow operator.
    /// A preview changes no accepted body and supplies no movement admission.</summary>
    public sealed class ManualAiSnowSteeringPlanner
    {
        private readonly ManualSkiSteeredSnowSystem snow;
        private readonly double seconds;
        private readonly bool completeSearch;
        private readonly ManualSkiSteeringDefinition steering;
        public int SearchVersion=>completeSearch?1:0;
        public int LastPreviewCount {get;private set;}
        public string ContextJson {get;}
        public ManualAiSnowSteeringPlanner(OccupancyDefinition body,ManualSkiSteeringDefinition steering,int frequency,bool completeSearch=true)
        {
            this.completeSearch=completeSearch;this.steering=steering;
            snow=new ManualSkiSteeredSnowSystem(body,steering,frequency);seconds=1.0/frequency;
            ContextJson=ManualScopedAiMovement.Encode(new[]{completeSearch?"manual_ai_snow_yaw_intent_v4_complete_integer_domain":"manual_ai_snow_yaw_intent_v3_minimum_equal_deflection",snow.ContextJson,
                completeSearch?"nominal_retained_inside_signed_endpoint_envelope;actual_slip_spin;complete_signed_integer_domain;minimum_error_then_absolute_deflection;negative_before_positive_tie;intent_only;shared_force_cost":
                "nominal_retained_inside_signed_endpoint_envelope;unreached_spin_uses_bounded_signed_endpoint;diagnostic_two_signed_error_searches_and_exact_neutral;actual_slip_spin;closest_sampled_shared_yaw;bounded_integer_search;not_global_monotonic_inversion;intent_only;shared_force_cost"});
        }
        public int Plan(ManualSkiContactBody body,SkiContactMotion motion,double targetYawRate,double lateralAcceleration,double response=1)
        {
            PhysiologyNumbers.RequireRange(targetYawRate,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(targetYawRate));
            LastPreviewCount=0;
            double speed=Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z);
            ManualSkiSteeringRequest negativeRequest=default,positiveRequest=default;
            double negativeYaw=0,positiveYaw=0;bool negativeValid=false,positiveValid=false;
            double neutral=At(0);if(targetYawRate==neutral)return 0;
            int best=0;double error=Math.Abs(neutral-targetYawRate);
            if(completeSearch)
            {
                // Shared slip/spin response can have several extrema even on
                // one signed half. Enumerate the existing finite actuator domain
                // in increasing deflection; the first exact pair is minimal.
                for(int magnitude=1;magnitude<=ManualSkiCommand.SteeringUnits;magnitude++)
                {Score(-magnitude,At(-magnitude));Score(magnitude,At(magnitude));if(error==0)return best;}
                return best;
            }
            Search(-ManualSkiCommand.SteeringUnits,-1);
            Search(1,ManualSkiCommand.SteeringUnits);
            return best;
            void Search(int lo,int hi)
            {
                Score(lo,At(lo));Score(hi,At(hi));
                while(hi-lo>3&&error!=0)
                {
                    int left=lo+(hi-lo)/3,right=hi-(hi-lo)/3;
                    double a=At(left),b=At(right);Score(left,a);Score(right,b);
                    if(Math.Abs(a-targetYawRate)<Math.Abs(b-targetYawRate)||
                        Math.Abs(a-targetYawRate)==Math.Abs(b-targetYawRate)&&Math.Abs(left)<Math.Abs(right))hi=right-1;
                    else lo=left+1;
                }
                for(int command=lo;command<=hi;command++)Score(command,At(command));
            }
            void Score(int command,double yaw)
            {
                double e=Math.Abs(yaw-targetYawRate);
                if(e<error||e==error&&Math.Abs(command)<Math.Abs(best)){best=command;error=e;}
            }
            double At(int command)
            {
                // Preserve the complete scoring order. A signed grip plateau
                // has the same target and angular force budget; unbounded yaw
                // is diagnostic only and cannot change the snow response.
                if(completeSearch&&command!=0)
                {
                    var request=ManualSkiSteeringSystem.GetRequest(steering,command,speed,lateralAcceleration,response);
                    var previous=command<0?negativeRequest:positiveRequest;
                    if((command<0?negativeValid:positiveValid)&&request.TargetYawRate==previous.TargetYawRate&&request.AngularAccelerationLimit==previous.AngularAccelerationLimit)
                        return command<0?negativeYaw:positiveYaw;
                    LastPreviewCount++;double yaw=snow.Preview(body,motion,command,lateralAcceleration,seconds,response).Snow.Motion.YawRate;
                    if(command<0){negativeRequest=request;negativeYaw=yaw;negativeValid=true;}else{positiveRequest=request;positiveYaw=yaw;positiveValid=true;}
                    return yaw;
                }
                LastPreviewCount++;return snow.Preview(body,motion,command,lateralAcceleration,seconds,response).Snow.Motion.YawRate;
            }
        }
        public int CorrectUnreachedYaw(ManualSkiContactBody body,SkiContactMotion motion,int nominalCommand,
            double targetYawRate,double lateralAcceleration,double response=1)
        {
            PhysiologyNumbers.RequireRange(targetYawRate,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(targetYawRate));
            snow.Preview(body,motion,nominalCommand,lateralAcceleration,seconds,response);
            double left=snow.Preview(body,motion,-ManualSkiCommand.SteeringUnits,lateralAcceleration,seconds,response).Snow.Motion.YawRate;
            double right=snow.Preview(body,motion,ManualSkiCommand.SteeringUnits,lateralAcceleration,seconds,response).Snow.Motion.YawRate;
            if(targetYawRate<Math.Min(left,right))return -ManualSkiCommand.SteeringUnits;
            if(targetYawRate>Math.Max(left,right))return ManualSkiCommand.SteeringUnits;
            return nominalCommand;
        }
    }
}
