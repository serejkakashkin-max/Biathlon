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
        public string ContextJson {get;}
        public ManualAiSnowSteeringPlanner(OccupancyDefinition body,ManualSkiSteeringDefinition steering,int frequency)
        {
            snow=new ManualSkiSteeredSnowSystem(body,steering,frequency);seconds=1.0/frequency;
            ContextJson=ManualScopedAiMovement.Encode(new[]{"manual_ai_snow_yaw_intent_v2_endpoint_correction",snow.ContextJson,
                "nominal_retained_inside_signed_endpoint_envelope;unreached_spin_uses_bounded_signed_endpoint;diagnostic_two_signed_error_searches_and_exact_neutral;actual_slip_spin;closest_sampled_shared_yaw;bounded_integer_search;not_global_monotonic_inversion;intent_only;shared_force_cost"});
        }
        public int Plan(ManualSkiContactBody body,SkiContactMotion motion,double targetYawRate,double lateralAcceleration,double response=1)
        {
            PhysiologyNumbers.RequireRange(targetYawRate,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(targetYawRate));
            double neutral=At(0);if(targetYawRate==neutral)return 0;
            int best=0;double error=Math.Abs(neutral-targetYawRate);
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
                    if(Math.Abs(a-targetYawRate)<Math.Abs(b-targetYawRate))hi=right-1;
                    else lo=left+1;
                }
                for(int command=lo;command<=hi;command++)Score(command,At(command));
            }
            void Score(int command,double yaw)
            {
                double e=Math.Abs(yaw-targetYawRate);
                if(e<error||e==error&&Math.Abs(command)>Math.Abs(best)){best=command;error=e;}
            }
            double At(int command)=>snow.Preview(body,motion,command,lateralAcceleration,seconds,response).Snow.Motion.YawRate;
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
