using System;
using System.Globalization;
using System.IO;
using Biathlon.Domain;
namespace Biathlon.AI
{
    public readonly struct AiUtilitySnapshot
    {
        private readonly TacticalUtilityScore a,b,c,d,e,f,g,h;
        public AiUtilitySnapshot(ReadOnlySpan<TacticalUtilityScore> scores)
        { if(scores.Length!=8) throw new ArgumentException("Eight utility scores required."); a=scores[0];b=scores[1];c=scores[2];d=scores[3];e=scores[4];f=scores[5];g=scores[6];h=scores[7]; }
        public TacticalUtilityScore Get(int index)
        { switch(index) { case 0:return a;case 1:return b;case 2:return c;case 3:return d;case 4:return e;case 5:return f;case 6:return g;case 7:return h;default:throw new ArgumentOutOfRangeException(nameof(index)); } }
    }
    public enum AiTraceKind { Tactical, Shooting }
    public readonly struct AiDecisionTrace
    {
        public readonly SimulationTick Tick;
        public readonly AthleteId Athlete;
        public readonly AiTraceKind Kind;
        public readonly TacticalIntentDecision Intent;
        public readonly AiUtilitySnapshot Utilities;
        public readonly DraftSelectionFrame Draft;
        public readonly OvertakeDecisionFrame Overtake;
        public readonly PreRangeRecoveryPlan Recovery;
        public readonly ShootingStrategyPlan Shooting;
        public readonly PaceControllerFrame Pace;
        public readonly ShootingAiFrame Aim;
        public readonly double Lateral;
        public AiDecisionTrace(SimulationTick tick,AthleteId athlete,AiTraceKind kind,TacticalIntentDecision intent,AiUtilitySnapshot utilities,
            DraftSelectionFrame draft,OvertakeDecisionFrame overtake,PreRangeRecoveryPlan recovery,ShootingStrategyPlan shooting,PaceControllerFrame pace,ShootingAiFrame aim,double lateral)
        { Tick=tick;Athlete=athlete;Kind=kind;Intent=intent;Utilities=utilities;Draft=draft;Overtake=overtake;Recovery=recovery;Shooting=shooting;Pace=pace;Aim=aim;Lateral=lateral; }
    }
    /// <summary>Sinks must not throw or reenter the runner. Observation only.</summary>
    public interface IAiDecisionSink { void Record(AiDecisionTrace trace); }
    public sealed class AiDecisionBuffer : IAiDecisionSink
    {
        private readonly AiDecisionTrace[] rows;
        public long Total { get; private set; }
        public int Count => (int)Math.Min(Total,rows.Length);
        public long Overwritten => Total-Count;
        public AiDecisionBuffer(int capacity)
        { if(capacity<=0) throw new ArgumentOutOfRangeException(nameof(capacity)); rows=new AiDecisionTrace[capacity]; }
        public void Record(AiDecisionTrace trace) { rows[(int)(Total%rows.Length)]=trace; Total++; }
        public AiDecisionTrace Get(int index)
        { if(index<0 || index>=Count) throw new ArgumentOutOfRangeException(nameof(index)); return rows[(int)((Total-Count+index)%rows.Length)]; }
        public void WriteCsv(TextWriter writer)
        {
            if(writer==null) throw new ArgumentNullException(nameof(writer));
            writer.WriteLine("schema_version,tick,athlete,kind,intent,target,effort,nominal,control_error,adjustment,lateral,draft_reason,draft_target,draft_strength,overtake_reason,recovery_reason,recovery_reduction,shooting_risk,tolerance,stable_seconds,aim_limit,trigger,aim_x,aim_y,score_index,eligible,bias,features,personality,total");
            for(int i=0;i<Count;i++)
            {
                var r=Get(i);
                for(int score=0;score<8;score++)
                {
                    var s=r.Utilities.Get(score);
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "1,{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:R},{10},{11},{12:R},{13},{14},{15},{16:R},{17:R},{18:R},{19:R},{20},{21},{22},{23},{24},{25:R},{26:R},{27:R},{28:R}",
                        r.Tick.Value,r.Athlete.Value,(int)r.Kind,(int)r.Intent.Kind,r.Intent.TargetId.Value,r.Pace.Input.RequestedEffort,r.Pace.NominalEffort,r.Pace.ControlError,r.Pace.PlannedEffortAdjustment,r.Lateral,
                        (int)r.Draft.Reason,r.Draft.Selected.LeaderId.Value,r.Draft.Selected.Strength,(int)r.Overtake.Reason,(int)r.Recovery.Reason,r.Recovery.EffortReduction,
                        r.Shooting.Risk,r.Shooting.FireTolerance,r.Shooting.StableSeconds,r.Shooting.MaximumAimSeconds,r.Aim.Trigger?1:0,r.Aim.Aim.Horizontal,r.Aim.Aim.Vertical,score,s.Eligible?1:0,s.Bias,s.Features,s.Personality,s.Total));
                }
            }
        }
    }
}
