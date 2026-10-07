#if !UNITY_EDITOR
using System;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Bounded headless baseline for actual full-field paid stops.
    /// Deliberately separate from the player-held exit regression: ordinary AI
    /// traffic alone cannot prove that a player will never queue.</summary>
    public sealed class WoodlandTrafficStopBaselineTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");

        [Test,Timeout(600000)]
        public void FullMassStartRecordsWhereMovingIntentWasStationary()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var audit=new BalanceEventAudit(5);
            var runner=ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(root+"mass_start_profile")),
                RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,audit,new AiDecisionCounts(),count:30,seed:123);
            var prior=new long[runner.Field.Count];var streak=new int[runner.Field.Count];
            var stopsByLoopAndBout=new long[5][];
            var blockByLoopAndKind=new long[5][];
            for(int loop=0;loop<5;loop++)
            {stopsByLoopAndBout[loop]=new long[5];blockByLoopAndKind[loop]=new long[3];}
            long start=0,main=0,exit=0,penalty=0,mat=0,other=0;
            long exitAthleteBlock=0,exitWidthBlock=0,exitUnclassified=0;
            long matCapacityWait=0,matBodyBlock=0,matUnclassified=0;
            long draftingActorTicks=0,overtakeIntentActorTicks=0,stopEpisodeStarts=0;
            double draftReductionSum=0;
            int maximumConcurrentStops=0;long maximumConcurrentTick=0;
            int longest=0,longestExit=0,longestMat=0;string longestExitAt="",longestMatAt="";
            string matBlockChain="";
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<50000)
            {
                for(int i=0;i<prior.Length;i++)prior[i]=runner.Field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();
                int concurrentStops=0;
                for(int i=0;i<prior.Length;i++)
                {
                    var athlete=runner.Field.GetAthlete(i);
                    var draft=runner.Field.GetDraft(i);
                    if(draft.Strength>0){draftingActorTicks++;draftReductionSum+=draft.DragReduction;}
                    if(runner.GetLastIntent(i).Kind==TacticalIntentKind.Overtake)
                        overtakeIntentActorTicks++;
                    if(athlete.HasFinished||athlete.ActiveShooting!=null||
                        runner.Field.Tick.Value<=athlete.ScheduledStart.Value||
                        runner.GetLastInput(i).Ski.Effort.RequestedEffort==0||
                        athlete.Motion.DistanceNanometers!=prior[i])
                    {streak[i]=0;continue;}
                    var loop=runner.Field.GetPhysicalPoint(i).LoopId;
                    int loopIndex=loop=="start_approach"?0:loop=="main"?1:
                        loop=="exit_aisle"?2:loop=="penalty_150"?3:4;
                    int bout=Math.Max(0,Math.Min(4,athlete.ActiveBout));
                    stopsByLoopAndBout[loopIndex][bout]++;
                    var firstBlock=runner.Field.GetInitialMovementBlock(i);
                    int blockIndex=firstBlock.Kind==PackMovementBlockKind.Athlete?0:
                        firstBlock.Kind==PackMovementBlockKind.Width?1:2;
                    blockByLoopAndKind[loopIndex][blockIndex]++;
                    concurrentStops++;
                    streak[i]++;if(streak[i]>longest)longest=streak[i];
                    if(streak[i]==1)stopEpisodeStarts++;
                    switch(loop)
                    {
                        case "start_approach":start++;break;
                        case "main":main++;break;
                        case "exit_aisle":
                            exit++;
                            var block=runner.Field.GetInitialMovementBlock(i);
                            if(block.Kind==PackMovementBlockKind.Athlete)exitAthleteBlock++;
                            else if(block.Kind==PackMovementBlockKind.Width)exitWidthBlock++;
                            else exitUnclassified++;
                            if(streak[i]>longestExit)
                            {
                                longestExit=streak[i];longestExitAt="athlete="+i+
                                " tick="+runner.Field.Tick.Value+
                                " station_m="+runner.Field.GetPhysicalPoint(i).LocalNanometers/1e9+
                                " line="+runner.Field.GetLateral(i)+
                                " requested_line="+runner.GetLastLateral(i)+
                                " block="+block.Kind+" blocker="+block.Blocker.Value;
                                for(int j=0;j<runner.Field.Count;j++)
                                    if(runner.Field.GetAthlete(j).AthleteId.Equals(block.Blocker))
                                    {
                                        var blockingPoint=runner.Field.GetPhysicalPoint(j);
                                        longestExitAt+=" blocker_index="+j+
                                            " blocker_loop="+blockingPoint.LoopId+
                                            " blocker_station_m="+blockingPoint.LocalNanometers/1e9+
                                            " blocker_line="+runner.Field.GetLateral(j);
                                        break;
                                    }
                            }
                            break;
                        case "penalty_150":penalty++;break;
                        default:
                            if(loop.StartsWith("mat_",System.StringComparison.Ordinal))
                            {
                                mat++;
                                if(runner.Field.IsWaitingForRangeCapacity(i))matCapacityWait++;
                                if(firstBlock.Kind==PackMovementBlockKind.Athlete)matBodyBlock++;
                                else if(firstBlock.Kind==PackMovementBlockKind.None)matUnclassified++;
                                if(streak[i]>longestMat)
                                {
                                    longestMat=streak[i];longestMatAt="athlete="+i+
                                        " tick="+runner.Field.Tick.Value+
                                        " loop="+loop+
                                        " station_m="+runner.Field.GetPhysicalPoint(i).LocalNanometers/1e9+
                                        " lane="+runner.Field.GetAssignedRangeLane(i)+
                                        " waiting_capacity="+runner.Field.IsWaitingForRangeCapacity(i)+
                                        " phase="+athlete.CurrentRangeMotion?.Phase+
                                        " block="+firstBlock.Kind+" blocker="+firstBlock.Blocker.Value;
                                    for(int j=0;j<runner.Field.Count;j++)
                                        if(runner.Field.GetAthlete(j).AthleteId.Equals(firstBlock.Blocker))
                                        {
                                            var blockingAthlete=runner.Field.GetAthlete(j);
                                            var blockingPoint=runner.Field.GetPhysicalPoint(j);
                                            longestMatAt+=" blocker_index="+j+
                                                " blocker_loop="+blockingPoint.LoopId+
                                                " blocker_station_m="+blockingPoint.LocalNanometers/1e9+
                                                " blocker_line="+runner.Field.GetLateral(j)+
                                                " blocker_phase="+blockingAthlete.CurrentRangeMotion?.Phase+
                                                " blocker_shooting="+(blockingAthlete.ActiveShooting!=null)+
                                                " blocker_lane="+runner.Field.GetAssignedRangeLane(j);
                                            break;
                                        }
                                    matBlockChain=CaptureBlockChain(runner,i);
                                }
                            }
                            else other++;
                            break;
                    }
                }
                if(concurrentStops>maximumConcurrentStops)
                {maximumConcurrentStops=concurrentStops;maximumConcurrentTick=runner.Field.Tick.Value;}
            }
            TestContext.WriteLine("wp177_mass30_stop_ticks="+runner.Field.Tick.Value+
                " start="+start+" main="+main+" exit="+exit+" penalty="+penalty+
                " mat="+mat+" other="+other+" longest="+longest+
                " longest_exit="+longestExit+" exit_athlete_block="+exitAthleteBlock+
                " exit_width_block="+exitWidthBlock+" exit_unclassified="+exitUnclassified+
                " longest_exit_at="+longestExitAt+
                " longest_mat="+longestMat+" mat_capacity_wait="+matCapacityWait+
                " mat_body_block="+matBodyBlock+" mat_unclassified="+matUnclassified+
                " longest_mat_at="+longestMatAt);
            TestContext.WriteLine("wp177_mat_block_chain="+matBlockChain);
            TestContext.WriteLine("wp177_traffic_diagnostic_v2="+JsonSerializer.Serialize(new
            {
                tick=runner.Field.Tick.Value,athletes=runner.Field.Count,event_hash=audit.Hash,
                loop_order=new[]{"start_approach","main","exit_aisle","penalty_150","mat_or_other"},
                block_order=new[]{"athlete","width","unclassified"},
                stops_by_loop_and_active_bout=stopsByLoopAndBout,
                blocks_by_loop_and_kind=blockByLoopAndKind,
                stop_episode_starts=stopEpisodeStarts,
                maximum_concurrent_moving_intent_stops=maximumConcurrentStops,
                maximum_concurrent_tick=maximumConcurrentTick,
                mat_capacity_wait_actor_ticks=matCapacityWait,
                longest_mat_streak_ticks=longestMat,
                drafting_actor_ticks=draftingActorTicks,
                draft_reduction_sum=draftReductionSum,
                overtake_intent_actor_ticks=overtakeIntentActorTicks
            }));
            Assert.That(runner.Field.IsComplete,Is.True,
                "A reproducible full Mass Start is required for phase-level stopped-time evidence.");
            Assert.That(runner.Field.MassStartResult.Count,Is.EqualTo(30));
        }
        private static string CaptureBlockChain(PackAiRaceRunner runner,int start)
        {
            string chain="";int current=start;var seen=new bool[runner.Field.Count];
            for(int depth=0;depth<runner.Field.Count&&!seen[current];depth++)
            {
                seen[current]=true;
                var point=runner.Field.GetPhysicalPoint(current);
                var block=runner.Field.GetInitialMovementBlock(current);
                chain+=" ["+current+" "+point.LoopId+":"+point.LocalNanometers/1e9+
                    " line="+runner.Field.GetLateral(current)+" requested="+runner.GetLastLateral(current)+
                    " speed="+runner.Field.GetAthlete(current).Motion.SpeedMetersPerSecond+
                    " block="+block.Kind+":"+block.Blocker.Value+"]";
                if(block.Kind!=PackMovementBlockKind.Athlete)break;
                int next=-1;
                for(int j=0;j<runner.Field.Count;j++)
                    if(runner.Field.GetAthlete(j).AthleteId.Equals(block.Blocker)){next=j;break;}
                if(next<0)break;current=next;
            }
            return chain;
        }
    }
}
#endif
