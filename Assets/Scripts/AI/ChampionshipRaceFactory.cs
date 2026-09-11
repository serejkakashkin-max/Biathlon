using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;
namespace Biathlon.AI
{
    /// <summary>Shared synthetic three-format workload factory for CLI and Unity regression.
    /// Profiles and all physics/tactics remain data-driven; Mass Start has an independent roster.</summary>
    public static class ChampionshipRaceFactory
    {
        public static PackAiRaceRunner Create(Func<string,string> Read,Func<string,string> Fixture,CompetitionFormat format,IRaceEventSink events,IAiDecisionSink sink,PursuitQualificationSnapshot qualification=null,int count=30,int frequency=20,ulong seed=123)
        {
            if(Read==null || Fixture==null || events==null || sink==null)throw new ArgumentNullException();
            if(count<1 || count>30 || frequency<=0 || format<CompetitionFormat.Sprint || format>CompetitionFormat.MassStart ||
                (format==CompetitionFormat.Pursuit)!=(qualification!=null))throw new ArgumentException("Supported format, clock, roster and dependency required.");
            string name=format==CompetitionFormat.Sprint?"sprint":format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
            var profile=CompetitionProfile.FromJson(Read(name+"_test_v1.json"));
            string prefix=format==CompetitionFormat.MassStart?"synthetic_mass_stadium":"synthetic_stadium";
            var track=TrackDefinition.FromJson(Fixture(prefix+"_v1.json"));var requirements=profile.ToRouteRequirements();
            var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("championship_"+name+"_route_v1",track.Id,"main_long",requirements),requirements);
            var ski=TimeTrialAthlete.FromJson(Read("time_trial_athlete_v1.json"));var shot=ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
            var entrants=new SprintEntrant[count];var personalities=new AiPersonalityDefinition[count];var ids=new AthleteId[count];
            for(int i=0;i<count;i++)
            {
                string id=qualification!=null?qualification.Get(i).AthleteId:(format==CompetitionFormat.MassStart?"mass_":"athlete_")+i.ToString(CultureInfo.InvariantCulture);
                ids[i]=new AthleteId(id);int original=int.Parse(id.Substring(id.LastIndexOf('_')+1),CultureInfo.InvariantCulture);
                entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id)),ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,id)),"ski_neutral");
                personalities[i]=AiPersonalityDefinition.FromJson(Read("ai_personality_"+(original%2==0?"attacker":"patient")+"_v1.json"));
            }
            var junction=PhysicalJunctionDefinition.FromJson(Fixture(prefix+"_junctions_v1.json"),track);
            var connections=PhysicalConnectionDefinition.FromJson(Fixture(prefix+"_connections_v1.json"),junction);
            var lanes=PhysicalRangeLaneDefinition.FromJson(Fixture(prefix+"_lanes_v1.json"),track);var body=OccupancyDefinition.FromJson(Read("occupancy_provisional_v1.json"));
            MassStartGrid grid=null;StartApproachMap approach=null;MassStartRangePolicyDefinition rangePolicy=null;
            if(format==CompetitionFormat.MassStart)
            {
                approach=new StartApproachMap(track,route,StartApproachDefinition.FromJson(Read("start_approach_test_v1.json").Replace("synthetic_mass_start_v1",track.Id)));
                var corridor=track.GetLoop(approach.LoopId);
                grid=new MassStartGrid(profile,MassStartGridDefinition.FromJson(Read("mass_start_grid_provisional_v1.json")),body,ids,corridor.GetSegment(0).WidthMeters,corridor.Length.Meters);
                rangePolicy=MassStartRangePolicyDefinition.FromJson(Read("mass_start_range_policy_v1.json"));
            }
            var field=new PackSprintFieldSession(new SimulationConfig(1,"championship_"+name+"_v1",frequency,seed),profile,new SkiModelDefinitions(Read),new ShootingModelDefinitions(Read),track,route,
                RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")),ResultTimingDefinition.FromJson(Read("result_timing_v1.json")),entrants,new SimulationTick(1),"championship_"+name+"_result_v1",body,
                DraftDefinition.FromJson(Read("draft_provisional_v1.json")),OvertakeDefinition.FromJson(Read("overtake_provisional_v1.json")),junction,connections,events,lanes,qualification,grid,approach,rangePolicy);
            return new PackAiRaceRunner(field,Read,personalities,AiDifficultyDefinition.FromJson(Read("ai_difficulty_expert_v1.json")),seed,events,sink);
        }
    }
}
