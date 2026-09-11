using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Paired solo Sprint experiment. Changes commands, never authoritative physical capability.</summary>
    public sealed class BalanceRaceRunner
    {
        private readonly BalanceBatchDefinition suite;
        private readonly BalancePacingCase scenario;
        private readonly ulong seed;
        private readonly ShootingModelDefinitions shooting;
        private readonly ShootingAiDefinition aim;
        private readonly SprintAthleteInput[] input=new SprintAthleteInput[1];
        private ShootingSession previous;
        private ShootingAiController controller;
        public SprintFieldSession Field { get; }
        public string ContextJson { get; }
        public int LastEffort { get; private set; }
        public BalanceRaceRunner(BalanceBatchDefinition suite,int caseIndex,int iteration,Func<string,string> read,TrackDefinition track,IRaceEventSink events)
        {
            if(suite==null||read==null||track==null||events==null)throw new ArgumentNullException();
            this.suite=suite;scenario=suite.Get(caseIndex);seed=suite.SeedAt(iteration);
            var profile=CompetitionProfile.FromJson(read(suite.ProfileFile));
            if(profile.Format!=CompetitionFormat.Sprint)throw new ArgumentException("Solo batch requires Sprint profile; use championship for linked formats.");
            var ski=TimeTrialAthlete.FromJson(read(suite.SkiAthleteFile));
            var shooter=ShootingAthleteDefinition.FromJson(read(suite.ShootingAthleteFile));
            shooter=ShootingAthleteDefinition.FromJson(shooter.ToJson().Replace(shooter.Id,ski.Id));
            shooting=new ShootingModelDefinitions(read);aim=AiDifficultyDefinition.FromJson(read("ai_difficulty_expert_v1.json")).Apply(ShootingAiDefinition.FromJson(read("shooting_ai_v1.json")));
            var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("balance_route",track.Id,"main_long",profile.ToRouteRequirements()),profile.ToRouteRequirements());
            Field=new SprintFieldSession(new SimulationConfig(1,"balance_race",suite.Frequency,seed),profile,new SkiModelDefinitions(read),shooting,track,route,
                RaceSnowConditions.FromJson(read("race_snow_neutral_v1.json")),ResultTimingDefinition.FromJson(read("result_timing_v1.json")),
                new[]{new SprintEntrant(ski,shooter,"ski_neutral")},new SimulationTick(1),"balance_result",events);
            using(var stream=new System.IO.MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{Field.ContextJson,scenario.ToJson(),aim.ToJson(),"solo_pacing_controller_v1"});
                ContextJson=System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public void Step()
        {
            if(Field.IsComplete||Field.Tick.Value>=suite.MaximumTicks)throw new InvalidOperationException("Batch race completed or tick budget exhausted.");
            var a=Field.GetAthlete(0);var t=Field.Tick.Next();var s=a.ActiveShooting;
            var command=new WeaponAimCommand(t,a.AthleteId,0,0);bool trigger=false,hold=false;
            if(s!=null)
            {
                if(s!=previous)
                {
                    controller=new ShootingAiController(aim,shooting.Targets,shooting.Aim,shooting.Stances.Get(Field.Profile.GetBout(a.ActiveBout).Stance),a.AthleteId,suite.Frequency,
                        new SeededRandomSource(unchecked(seed+(ulong)a.ActiveBout)),a.Tick);previous=s;
                }
                var d=controller.Step(t,new ShootingAiObservation(s.Phase,s.ClosedMask,s.ShotsFired,s.Sight.YawRadians,s.Sight.PitchRadians));
                command=d.Aim;trigger=d.Trigger;hold=d.HoldBreath;
            }
            var range=a.CurrentRangeMotion;
            int effort=a.Lifecycle.CompletedBouts==Field.Profile.BoutCount?scenario.FinalEffort:scenario.Effort;
            if(range.HasValue&&range.Value.Phase==RangeTraversalPhase.Approach&&range.Value.DistanceToBoundaryNanometers<=scenario.RecoveryDistance*SkiState.NanometersPerMeter)
                effort=scenario.RecoveryEffort;
            LastEffort=effort;
            input[0]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(t,a.AthleteId,effort),1,1,0),command,trigger,hold);
            Field.Step(input);
        }
    }
}
