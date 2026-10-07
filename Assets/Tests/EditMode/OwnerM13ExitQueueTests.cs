using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class OwnerM13ExitQueueTests
    {
        private static string Read(string id) => PhysiologyContractTests.ProjectFile("Assets/Resources/" + id + ".json");

        private static PackAiRaceRunner Create(int count)
        {
            const string root = "Biathlon/Venues/woodland_basin_v1/";
            var venue = new VenueAuthoringPlan(VenueManifest.FromJson(Read(root + "manifest")), Read);
            return ProductionVenueRaceFactory.Create(venue, CompetitionProfile.FromJson(Read(root + "mass_start_profile")),
                RaceRoutePlan.FromJson(Read(root + "mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson, new RaceEventLog(500000), new AiDecisionCounts(), count:count);
        }

        [Test, Timeout(600000)]
        public void PlayerHeldOnActualExitDoesNotProduceAStationaryFollowerQueue()
            => VerifyHeldExit(30,2,12000);

#if !UNITY_EDITOR
        // Long headless stress regression. The shorter second-bout variant is
        // the Unity-native smoke check; this one reaches the third range exit.
        [Test, Timeout(600000)]
        public void PlayerHeldAfterThirdBoutDoesNotProduceAStationaryFollowerQueue()
            => VerifyHeldExit(30,3,22000,ScoutThirdBoutPlayerWithActualFollower());

        private static int ScoutThirdBoutPlayerWithActualFollower()
        {
            // A decision-policy change may reorder the field. Keep the exact
            // player-held geometry as the acceptance condition instead of
            // relying on a historical bib number that no longer has traffic.
            var scout=Create(30);var firstEligible=new bool[scout.Field.Count];int chosen=-1;
            while(scout.Field.Tick.Value<22000&&!scout.Field.IsComplete&&chosen<0)
            {
                scout.Step();
                for(int i=0;i<scout.Field.Count;i++)
                {
                    var athlete=scout.Field.GetAthlete(i);var point=scout.Field.GetPhysicalPoint(i);
                    if(firstEligible[i]||athlete.Lifecycle.CompletedBouts<3||point.LoopId!="exit_aisle"||
                        point.LocalNanometers<115*SkiState.NanometersPerMeter)continue;
                    firstEligible[i]=true;
                    for(int j=0;j<scout.Field.Count;j++)if(j!=i)
                    {
                        var other=scout.Field.GetPhysicalPoint(j);
                        if(other.LoopId=="exit_aisle"&&other.LocalNanometers<point.LocalNanometers&&
                            other.LocalNanometers>point.LocalNanometers-20*SkiState.NanometersPerMeter)
                        {chosen=i;break;}
                    }
                    if(chosen>=0)break;
                }
            }
            TestContext.WriteLine("scouted_third_bout_player="+chosen+
                " scout_tick="+scout.Field.Tick.Value);
            scout=null;GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
            Assert.That(chosen,Is.GreaterThanOrEqualTo(0),
                "A real third-bout exit follower is needed to qualify this owner regression.");
            return chosen;
        }
#endif

        private static void VerifyHeldExit(int count,int completedBouts,int maximumTicks,int player=17)
        {
            // Zero drive can still coast under accepted physics. These checks
            // establish physical passing without follower stalls, not a proof
            // that the controlled athlete stays stationary for all 240 ticks.
            Assert.That(AiRangeTrafficPolicyDefinition.FromJson(
                Read("Biathlon/Venues/woodland_basin_v1/ai_range_traffic")).RetainSharedExitLine,
                Is.True, "The shared exit must not converge everyone onto one lateral line.");
            Assert.That(AiRangeTrafficPolicyDefinition.FromJson(
                Read("Biathlon/Venues/woodland_basin_v1/ai_range_traffic")).BlockedAlignmentEffort,
                Is.Null, "Blocked alignment must not command an artificial effort cap.");
            var reference = Create(count);
            var controlled = Create(count);
            var previous = new long[count];
            var stopped = new int[count];
            bool held = false;
            int holdTicks = 0, maxFollowerStall = 0, followersPassing = 0, nearbyAtHold = 0;
            var initialFollowers = new bool[count];
            var didPass = new bool[count];
            int witnessedBodyBlocks = 0;
            long playerStation = 0;
            for (int tick = 0; tick < maximumTicks && holdTicks < 240; tick++)
            {
                for (int i = 0; i < count; i++) previous[i] = controlled.Field.GetAthlete(i).Motion.DistanceNanometers;
                reference.Step();
                var command = reference.GetLastInput(player);
                double line = reference.GetLastLateral(player);
                var prior = controlled.Field.GetAthlete(player);
                var point = controlled.Field.GetPhysicalPoint(player);
                if (!held && prior.Lifecycle.CompletedBouts >= completedBouts && point.LoopId == "exit_aisle" &&
                    point.LocalNanometers >= 115 * SkiState.NanometersPerMeter)
                {
                    held = true;
                    playerStation = point.LocalNanometers;
                    TestContext.WriteLine("held_exit_tick=" + tick + " station_m=" + playerStation / 1e9);
                    for (int i = 0; i < count; i++) if(i!=player)
                    {
                        var other = controlled.Field.GetPhysicalPoint(i);
                        if (other.LoopId == "exit_aisle" && other.LocalNanometers < playerStation &&
                            other.LocalNanometers > playerStation - 20 * SkiState.NanometersPerMeter)
                        { nearbyAtHold++; initialFollowers[i] = true; }
                    }
                }
                if (held)
                {
                    holdTicks++;
                    line = controlled.Field.GetLateral(player);
                    var ski = command.Ski;
                    command = new SprintAthleteInput(new TimeTrialInput(
                        new SimulationInput(ski.Effort.Tick, ski.Effort.AthleteId, 0), ski.Form,
                        ski.LineQuality, ski.HeadwindMetersPerSecond, false, false,
                        ski.TechniqueId, ski.MovementEfficiency), command.Aim, command.Trigger,
                        command.HoldBreath, command.Wind, command.Pressure);
                }
                controlled.StepWithPlayer(player, command, line);
                for(int i=0;i<count;i++)
                {
                    var block=controlled.Field.GetInitialMovementBlock(i);
                    if(block.Kind==PackMovementBlockKind.Athlete)
                    {Assert.That(block.Blocker.IsValid,Is.True);witnessedBodyBlocks++;}
                }
                if (!held) continue;
                var playerNow = controlled.Field.GetPhysicalPoint(player);
                for (int i = 0; i < count; i++) if (i != player)
                {
                    var athlete = controlled.Field.GetAthlete(i);
                    var location = controlled.Field.GetPhysicalPoint(i);
                    if (!initialFollowers[i] || athlete.ActiveShooting != null) continue;
                    stopped[i] = !didPass[i] && athlete.Motion.DistanceNanometers == previous[i] ? stopped[i] + 1 : 0;
                    maxFollowerStall = Math.Max(maxFollowerStall, stopped[i]);
                    bool passed = location.LoopId == playerNow.LoopId &&
                        location.LocalNanometers > playerNow.LocalNanometers + 2 * SkiState.NanometersPerMeter ||
                        playerNow.LoopId == "exit_aisle" &&
                        (location.LoopId == "penalty_150" || location.LoopId == "main") ||
                        playerNow.LoopId == "penalty_150" && location.LoopId == "main";
                    if (!didPass[i] && passed)
                    { didPass[i] = true; followersPassing++; }
                }
                if (tick % 25 == 0) MassStartFieldTests.AssertClear(controlled.Field);
            }
            var finalPlayer = controlled.Field.GetPhysicalPoint(player);
            TestContext.WriteLine("held_ticks=" + holdTicks + " nearby_at_hold=" + nearbyAtHold + " max_follower_stall=" + maxFollowerStall +
                " followers_passing=" + followersPassing + " player_final=" + finalPlayer.LoopId + ":" + finalPlayer.LocalNanometers / 1e9 +
                " speed=" + controlled.Field.GetAthlete(player).Motion.SpeedMetersPerSecond);
            for (int i = 0; i < count; i++) if (initialFollowers[i])
            {
                var other = controlled.Field.GetPhysicalPoint(i);
                TestContext.WriteLine("follower=" + i + " final=" + other.LoopId + ":" + other.LocalNanometers / 1e9 +
                    " speed=" + controlled.Field.GetAthlete(i).Motion.SpeedMetersPerSecond + " line=" + controlled.Field.GetLateral(i));
            }
            Assert.That(held, Is.True, "The scenario must reach a real range exit.");
            Assert.That(holdTicks, Is.EqualTo(240));
            Assert.That(witnessedBodyBlocks,Is.GreaterThan(0),"The dense replay must exercise physical body admission.");
            Assert.That(nearbyAtHold, Is.GreaterThan(0), "A follower must be present when the player stops.");
            Assert.That(maxFollowerStall, Is.LessThan(100), "Followers must find a physical way around a stopped athlete.");
            Assert.That(followersPassing, Is.GreaterThan(0), "At least one real follower must pass the player on the exit.");
        }
    }
}
