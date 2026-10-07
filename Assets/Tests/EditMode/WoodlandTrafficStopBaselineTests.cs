#if !UNITY_EDITOR
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
            var runner=ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(root+"mass_start_profile")),
                RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,new BalanceEventAudit(5),new AiDecisionCounts(),count:30,seed:123);
            var prior=new long[runner.Field.Count];var streak=new int[runner.Field.Count];
            long start=0,main=0,exit=0,penalty=0,mat=0,other=0;
            long exitAthleteBlock=0,exitWidthBlock=0,exitUnclassified=0;
            int longest=0,longestExit=0;string longestExitAt="";
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<50000)
            {
                for(int i=0;i<prior.Length;i++)prior[i]=runner.Field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();
                for(int i=0;i<prior.Length;i++)
                {
                    var athlete=runner.Field.GetAthlete(i);
                    if(athlete.HasFinished||athlete.ActiveShooting!=null||
                        runner.Field.Tick.Value<=athlete.ScheduledStart.Value||
                        runner.GetLastInput(i).Ski.Effort.RequestedEffort==0||
                        athlete.Motion.DistanceNanometers!=prior[i])
                    {streak[i]=0;continue;}
                    var loop=runner.Field.GetPhysicalPoint(i).LoopId;
                    streak[i]++;if(streak[i]>longest)longest=streak[i];
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
                            if(loop.StartsWith("mat_",System.StringComparison.Ordinal))mat++;
                            else other++;
                            break;
                    }
                }
            }
            TestContext.WriteLine("wp177_mass30_stop_ticks="+runner.Field.Tick.Value+
                " start="+start+" main="+main+" exit="+exit+" penalty="+penalty+
                " mat="+mat+" other="+other+" longest="+longest+
                " longest_exit="+longestExit+" exit_athlete_block="+exitAthleteBlock+
                " exit_width_block="+exitWidthBlock+" exit_unclassified="+exitUnclassified+
                " longest_exit_at="+longestExitAt);
            Assert.That(runner.Field.IsComplete,Is.True,
                "A reproducible full Mass Start is required for phase-level stopped-time evidence.");
            Assert.That(runner.Field.MassStartResult.Count,Is.EqualTo(30));
        }
    }
}
#endif
