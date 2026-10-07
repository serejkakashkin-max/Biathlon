using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ManualAiCornerEffortPlan
    {
        public bool IsValid {get;}
        public bool BendObserved {get;}
        public double BendAngleRadians {get;}
        public double RequestedPowerWatts {get;}
        public long LookaheadStationNanometers {get;}
        internal ManualAiCornerEffortPlan(bool bend,double angle,double power,long station)
        {IsValid=true;BendObserved=bend;BendAngleRadians=angle;
         RequestedPowerWatts=power;LookaheadStationNanometers=station;}
    }

    /// <summary>Observes an ordered route bend early enough to request less
    /// delivered power through ordinary effort control. It cannot brake an
    /// already unsafe skier, steer, move or admit a route branch.</summary>
    public sealed class ManualAiCornerEffortPlanner
    {
        private readonly ManualRouteProjectionMap route;
        private readonly ManualAiCornerEffortDefinition policy;
        public string ContextJson {get;}
        public ManualAiCornerEffortPlanner(ManualRouteProjectionMap route,ManualAiCornerEffortDefinition policy)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{
                    "manual_ai_route_bend_power_request_v1",route.ContextJson,policy.ToJson()});
                ContextJson=Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public ManualAiCornerEffortPlan Plan(ManualRouteProjection observation,
            double nominalDeliveredPowerWatts,bool admitNextSection)
        {
            if(!observation.IsValid)throw new ArgumentException("Local route observation required.",nameof(observation));
            PhysiologyNumbers.RequireRange(nominalDeliveredPowerWatts,0,
                PhysiologyNumbers.MaximumMagnitude,nameof(nominalDeliveredPowerWatts));
            if(!route.TrySampleAhead(observation.SectionIndex,observation.RouteNanometers,
                policy.BendLookaheadMeters,admitNextSection,out var target))
                return default;
            double cross=observation.ForwardX*target.ForwardZ-
                observation.ForwardZ*target.ForwardX;
            double dot=observation.ForwardX*target.ForwardX+
                observation.ForwardZ*target.ForwardZ;
            double angle=Math.Atan2(cross,dot);
            bool bend=Math.Abs(angle)>policy.BendAngleThresholdRadians;
            double requested=bend?Math.Min(nominalDeliveredPowerWatts,policy.BendPowerCapWatts):
                nominalDeliveredPowerWatts;
            return new ManualAiCornerEffortPlan(bend,angle,requested,target.RouteNanometers);
        }
    }
}
