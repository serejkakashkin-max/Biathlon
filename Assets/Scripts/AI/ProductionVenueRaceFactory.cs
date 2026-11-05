using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Explicit authored venue/profile/route binding. Existing synthetic workloads
    /// retain their separate factory and context. Static visual acceptance belongs to the
    /// venue validator; this composition never edits a course or athlete's capabilities.</summary>
    public static class ProductionVenueRaceFactory
    {
        public static PackAiRaceRunner Create(VenueAuthoringPlan venue, CompetitionProfile profile,
            RaceRoutePlan routePlan, RaceSnowConditions conditions, Func<string,string> read, IRaceEventSink events, IAiDecisionSink telemetry,
            PursuitQualificationSnapshot qualification=null, int count=30, int frequency=20, ulong seed=123,
            SprintEntrant[] suppliedEntrants=null, AiPersonalityDefinition[] suppliedPersonalities=null,
            double[] raceForms=null, ShootingPressureContext[] racePressures=null,WindVector raceWind=default,bool massStartNumberFromRight=false,bool completeStartCorridorProof=true,int startContinuationVersion=3)
        {
            if(venue==null||profile==null||routePlan==null||conditions==null||read==null||events==null||telemetry==null)
                throw new ArgumentNullException();
            bool reusableMats=venue.Formats!=null&&(profile.Format==CompetitionFormat.Sprint||profile.Format==CompetitionFormat.Pursuit);
            if(!venue.UsesPhysicalPaths||count<1||(!reusableMats&&count>venue.LaneCount)||count>profile.FieldCapacity||frequency<=0||
                (profile.Format==CompetitionFormat.Pursuit)!=(qualification!=null))
                throw new ArgumentException("Explicit paths, covered roster, clock and format dependency required.");
            if((suppliedEntrants==null)!=(suppliedPersonalities==null)||
                suppliedEntrants!=null&&(suppliedEntrants.Length!=count||suppliedPersonalities.Length!=count))
                throw new ArgumentException("Complete supplied entrant and personality roster required.");
            PhysicalDirectedPassageDefinition directedPassages=null;
            if(venue.Formats!=null)
            {
                bool bound=false;
                for(int i=0;i<venue.Formats.Count;i++)
                {
                    var item=venue.Formats.Get(i);
                    if(item.Profile.ToJson()==profile.ToJson()&&item.Route.ToJson()==routePlan.ToJson())
                    {bound=true;directedPassages=item.DirectedPassages;}
                }
                if(!bound)throw new ArgumentException("Production race must use an exact profile/route pair from the venue's format catalog.");
            }
            var track=venue.Track;var route=new RaceRoute(track,routePlan,profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var physical=new PhysicalTrackMap(track,route);
            var layout=new PhysicalRangeFieldLayout(physical,track,route,venue.Paths);
            var entrants=new SprintEntrant[count];var personalities=new AiPersonalityDefinition[count];var ids=new AthleteId[count];
            var ski=TimeTrialAthlete.FromJson(read("time_trial_athlete_v1.json"));
            var shot=ShootingAthleteDefinition.FromJson(read("shooting_athlete_v1.json"));
            for(int i=0;i<count;i++)
            {
                if(suppliedEntrants!=null)
                {entrants[i]=suppliedEntrants[i];personalities[i]=suppliedPersonalities[i];ids[i]=new AthleteId(entrants[i].SkiAthlete.Id);continue;}
                string id=qualification==null?venue.Manifest.Id+"_athlete_"+i.ToString(CultureInfo.InvariantCulture):qualification.Get(i).AthleteId;
                ids[i]=new AthleteId(id);
                entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id)),
                    ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,id)),"ski_neutral");
                personalities[i]=AiPersonalityDefinition.FromJson(read("ai_personality_"+(i%2==0?"attacker":"patient")+"_v1.json"));
            }
            MassStartGrid grid=null;StartApproachMap approach=null;MassStartRangePolicyDefinition policy=null;
            if(venue.StartApproach?.LaunchDistanceMeters.HasValue==true)
                approach=new StartApproachMap(track,route,venue.StartApproach);
            if(profile.Format==CompetitionFormat.MassStart)
            {
                if(venue.StartApproach==null)throw new ArgumentException("Mass Start requires an authored open start approach.");
                approach=approach??new StartApproachMap(track,route,venue.StartApproach);
                var corridor=track.GetLoop(approach.LoopId);double width=double.MaxValue;
                for(int i=0;i<corridor.SegmentCount;i++)width=Math.Min(width,corridor.GetSegment(i).WidthMeters);
                grid=new MassStartGrid(profile,MassStartGridDefinition.FromJson(read("mass_start_grid_provisional_v1.json")),
                    venue.Occupancy,ids,width,corridor.Length.Meters,numberFromRight:massStartNumberFromRight);
                policy=MassStartRangePolicyDefinition.FromJson(read("mass_start_range_policy_v1.json"));
            }
            string prefix=venue.Manifest.Id+"_"+profile.Format.ToString().ToLowerInvariant();
            var gameplayRead=ProductionGameplayDefinitions.Bind(read);
            var field=new PackSprintFieldSession(new SimulationConfig(1,prefix+"_simulation_v1",frequency,seed),profile,
                new SkiModelDefinitions(gameplayRead),new ShootingModelDefinitions(gameplayRead),track,route,
                conditions,ResultTimingDefinition.FromJson(read("result_timing_v1.json")),
                entrants,new SimulationTick(1),prefix+"_result_v1",venue.Occupancy,DraftDefinition.FromJson(read("draft_provisional_v1.json")),
                OvertakeDefinition.FromJson(read("overtake_provisional_v1.json")),venue.Junctions,venue.Connections,events,
                qualification:qualification,massGrid:grid,startApproach:approach,rangePolicy:policy,rangeLayout:layout,
                authoredStartApproach:approach!=null,startContinuation:approach==null?null:venue.StartContinuation,
                reusableRangeMats:reusableMats,
                routeBoundJunctions:venue.Formats!=null,directedPassages:directedPassages,
                sharedApron:venue.SharedApron,sharedApronVisual:venue.SharedApron==null?null:venue.Visual,
                sharedApronRange:venue.SharedApron==null?null:venue.Manifest.Range,completeStartCorridorProof:completeStartCorridorProof,startContinuationVersion:startContinuationVersion);
            // AI chooses tuck from downhill geometry. The shared field still
            // uses the owner's freely requested posture and identical costs.
            Func<string,string> aiRead=name=>name=="pace_controller_provisional_v1.json"&&venue.AiSkiPolicy?.PaceControllerResource!=null?read(venue.AiSkiPolicy.PaceControllerResource):
                name=="shooting_strategy_provisional_v1.json"&&venue.AiSkiPolicy?.ShootingStrategyResource!=null?read(venue.AiSkiPolicy.ShootingStrategyResource):
                name=="pre_range_recovery_provisional_v1.json"&&venue.AiSkiPolicy?.PreRangeRecoveryResource!=null?read(venue.AiSkiPolicy.PreRangeRecoveryResource):
                name=="tuck_provisional_v1.json"?read(name):gameplayRead(name);
            return new PackAiRaceRunner(field,aiRead,personalities,AiDifficultyDefinition.FromJson(read("ai_difficulty_expert_v1.json")),seed,events,telemetry,
                raceForms,racePressures,
                rangeTrafficPolicy:venue.AiRangeTraffic??AiRangeTrafficPolicyDefinition.FromJson(read("ai_range_traffic_provisional_v1.json")),skiPolicy:venue.AiSkiPolicy,raceWind:raceWind);
        }
    }
}
