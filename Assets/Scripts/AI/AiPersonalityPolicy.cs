using System;
using Biathlon.Domain;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.AI
{
    /// <summary>Immutable athlete/trait binding. Preferences never change eligibility or physical capability.</summary>
    public sealed class AiPersonalityPolicy
    {
        public AthleteId AthleteId { get; }
        public AiPersonalityDefinition Definition { get; }
        public string ContextJson { get; }
        public AiPersonalityPolicy(AthleteId athlete, AiPersonalityDefinition definition)
        {
            if(!athlete.IsValid) throw new ArgumentException("Valid personality actor required.");
            Definition=definition??throw new ArgumentNullException(nameof(definition)); AthleteId=athlete;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{"ai_personality_direct_preferences_mean_shooting_v1",athlete.Value,definition.ToJson()});
                ContextJson=Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public void CopyPreferences(bool finalLap,Span<double> preferences)
        {
            if(preferences.Length<8) throw new ArgumentException("Eight preference slots required.");
            preferences[(int)TacticalIntentKind.Recover]=Definition.Patience;
            preferences[(int)TacticalIntentKind.Hold]=Definition.Patience;
            preferences[(int)TacticalIntentKind.Follow]=Definition.DraftPreference;
            preferences[(int)TacticalIntentKind.Draft]=Definition.DraftPreference;
            preferences[(int)TacticalIntentKind.Overtake]=Definition.OvertakeConfidence;
            preferences[(int)TacticalIntentKind.Attack]=finalLap?Definition.FinalLapAggression:Definition.Aggression;
            preferences[(int)TacticalIntentKind.FinalSprint]=Definition.FinalLapAggression;
            preferences[(int)TacticalIntentKind.PreRangeRecovery]=1-Definition.ShootingRisk;
        }
        public ShootingStrategyPlan PlanShooting(ShootingStrategyPlanner planner,double timeUrgency,double stabilityNeed,double pressure)
        {
            if(planner==null) throw new ArgumentNullException(nameof(planner));
            PhysiologyNumbers.RequireRange(timeUrgency,0,1,nameof(timeUrgency));
            PhysiologyNumbers.RequireRange(stabilityNeed,0,1,nameof(stabilityNeed));
            PhysiologyNumbers.RequireRange(pressure,0,1,nameof(pressure));
            // Equal-weight mean of observed urgency and two persistent shooting preferences.
            // Resistance moderates tactical caution only; it does not alter body sway/pressure physics.
            double urgency=(timeUrgency+Definition.ShootingRisk+Definition.ShootingTempo)/3;
            double caution=Math.Max(stabilityNeed,pressure*(1-Definition.PressureResistance));
            return planner.Evaluate(urgency,caution);
        }
    }
}
