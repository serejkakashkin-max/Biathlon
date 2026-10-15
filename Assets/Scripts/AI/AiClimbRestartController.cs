using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public enum AiClimbRestartPhase { Ordinary, Restart, Recover }
    /// <summary>Requests paid pole effort after an observed uphill stall. Recovery
    /// restores the same ordinary reserve before another attempt; no velocity,
    /// force, actor statistics or physiology state can be written here.</summary>
    public sealed class AiClimbRestartController
    {
        private readonly AiClimbRestartDefinition policy;
        private readonly int triggerTicks;
        private int slowTicks;
        public AiClimbRestartPhase Phase {get;private set;}
        public AiClimbRestartController(AiClimbRestartDefinition policy,int frequency)
        {
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            if(frequency<1||frequency>10000)throw new ArgumentOutOfRangeException(nameof(frequency));
            triggerTicks=(int)Math.Ceiling(policy.StalledSeconds*frequency);
        }
        public int Step(bool eligible,double grade,double speed,double reserve,int ordinaryEffort)
        {
            AthleteState.ValidateEffort(ordinaryEffort);PhysiologyNumbers.RequireRange(grade,-1,1,nameof(grade));
            PhysiologyNumbers.RequireRange(speed,0,1000,nameof(speed));PhysiologyNumbers.RequireRange(reserve,0,1,nameof(reserve));
            if(!eligible||grade<policy.MinimumGrade||speed>=policy.ReleaseAboveSpeed)
            {slowTicks=0;Phase=AiClimbRestartPhase.Ordinary;return ordinaryEffort;}
            if(Phase==AiClimbRestartPhase.Ordinary)
            {
                slowTicks=speed<policy.TriggerBelowSpeed?Math.Min(triggerTicks,slowTicks+1):0;
                if(slowTicks<triggerTicks)return ordinaryEffort;
                Phase=reserve<=policy.RecoverBelowReserve?AiClimbRestartPhase.Recover:AiClimbRestartPhase.Restart;
            }
            if(Phase==AiClimbRestartPhase.Restart&&reserve<=policy.RecoverBelowReserve)Phase=AiClimbRestartPhase.Recover;
            if(Phase==AiClimbRestartPhase.Recover&&reserve>=policy.RestartAboveReserve)Phase=AiClimbRestartPhase.Restart;
            if(Phase==AiClimbRestartPhase.Recover)return Math.Min(ordinaryEffort,policy.RecoveryEffort);
            return Math.Max(ordinaryEffort,policy.RestartEffort);
        }
    }
}
