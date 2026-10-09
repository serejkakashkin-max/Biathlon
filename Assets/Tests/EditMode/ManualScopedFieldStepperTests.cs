using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScopedFieldStepperTests
    {
        internal static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"stepper_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        internal static SkiContactSweepDefinition Work()=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"stepper_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
        internal static SkiContactGroupDefinition Group()=>SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"stepper_group\",\"maximum_passes\":4096,\"closing_speed_tolerance_mps\":0.00000001}");
        internal static ManualSkiSteeringDefinition Steering()=>ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/manual_ski_steering_v2.json"));
        internal static string Read(string resource)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+resource+".json");
        internal sealed class Fixture
        {
            internal readonly ManualScopedFieldPublicationTests.Fixture Source;
            internal readonly SkiModelDefinitions Models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            internal readonly ManualScopedWorldRoster World;
            internal readonly ManualScopedFieldStepper Stepper;
            internal Fixture(string phase="base")
            {
                Source=new ManualScopedFieldPublicationTests.Fixture(phase);
                int n=Source.Pack.Count;
                var c=new ManualRacePathCursor[n];var poses=new ManualSkiHeadingState[n];var velocity=new SkiPlanarVelocity[n];
                var motion=new SkiState[n];var masses=new double[n];
                for(int i=0;i<n;i++)
                {c[i]=Source.World.GetCursor(i);poses[i]=Source.World.GetActor(i).Pose;velocity[i]=Source.World.GetActor(i).Velocity;
                    motion[i]=Source.World.GetPhysicalMotion(i);masses[i]=Models.Physics.MassKilograms;}
                World=new ManualScopedWorldRoster(Source.Catalogs,Source.Source.Venue.Occupancy,Contact(),Work(),c,poses,velocity,motion,
                    Source.Pack.TickFrequency,Source.Pack.Tick,Group(),masses);
                Stepper=new ManualScopedFieldStepper(Source.Pack,World,Models,Source.Source.Venue.Occupancy,Steering(),Work());
            }
            internal void Commands(int effort=600)
            {
                for(int i=0;i<Source.Pack.Count;i++)
                {
                    var a=Source.Pack.GetAthlete(i);var tick=a.Tick.Next();
                    Source.Inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,effort),1,1,0),
                        new WeaponAimCommand(tick,a.AthleteId,0,0));
                }
            }
        }
        private static string Explain(ManualScopedFieldStepper s)
        {
            var rejected=s.LastRejectedWorkingActor;
            var forces=s.World.LastContactForces;
            string source=FormattableString.Invariant($"working x={rejected.Pose.X:R} z={rejected.Pose.Z:R} yaw={rejected.Pose.HeadingRadians:R} spin={rejected.Pose.YawRateRadiansPerSecond:R} vx={rejected.Velocity.X:R} vz={rejected.Velocity.Z:R} occurrence={rejected.Observation.OccurrenceIndex} arc={rejected.Observation.PhysicalArcNanometers} lateral={rejected.Observation.LateralMeters:R}; gate={s.LastStep.WorldAttempt.Gate.Status}; ");
            for(int i=0;i<s.World.Count;i++)
            {
                var actor=s.World.GetActor(i);
                if(!actor.Athlete.Equals(s.LastStep.WorldAttempt.Athlete))continue;
                var cursor=s.World.GetCursor(i);var velocity=actor.Velocity;
                var ribbon=s.World.GetRibbonDiagnostics(i);
                source+=FormattableString.Invariant($"source={i} occurrence={cursor.OccurrenceIndex} arc={cursor.ObservedArcNanometers} x={actor.Pose.X:R} z={actor.Pose.Z:R} yaw={actor.Pose.HeadingRadians:R} spin={actor.Pose.YawRateRadiansPerSecond:R} vx={velocity.X:R} vz={velocity.Z:R} lateral={actor.Observation.LateralMeters:R} vertices={ribbon.LastVertexCount} clearance={ribbon.LastStartClearance:R} minimum={ribbon.LastMinimumSampledClearance:R}");
            }
            return FormattableString.Invariant($"{s.LastStep.Status} / {s.LastStep.WorldAttempt.Status} / {s.LastStep.WorldAttempt.Athlete} / {s.LastStep.ProvisionalSeconds:R} / pieces {s.LastStep.Intervals} / forces {forces.Status} supports={forces.SupportPoints} passes={forces.Passes} seconds={forces.DurationSeconds:R} residual={forces.MaximumReactionResidualSpeed:R} closing={forces.MaximumClosingAcceleration:R} / {s.LastStep.WorldAttempt.Course.Status} / {s.LastStep.WorldAttempt.Course.SourceEdgeId} / {s.LastStep.WorldAttempt.CourseEdge.Status} / pair {s.LastStep.WorldAttempt.BodyOrEdge.First}:{s.LastStep.WorldAttempt.BodyOrEdge.Second} {s.LastStep.WorldAttempt.BodyOrEdge.Pair.Status} clear {s.LastStep.WorldAttempt.BodyOrEdge.Pair.ClearUntilSeconds:R} end {s.LastStep.WorldAttempt.BodyOrEdge.Pair.IntervalEndSeconds:R} continuation {s.LastStep.WorldAttempt.BodyOrEdge.Pair.IsContactContinuation} / {source}");
        }
        internal static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) InitialCandidate(int count)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));var events=new RaceEventLog(10000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),count:count);
            var models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var world=ManualScopedFieldStepper.CreateInitialWorld(runner.Field,venue.Track,venue.Visual,venue.Occupancy,Contact(),Work(),Group(),models.Physics);
            var stepper=new ManualScopedFieldStepper(runner.Field,world,models,venue.Occupancy,Steering(),Work(),
                ManualMatBrakingDefinition.FromJson(Read("Biathlon/manual_mat_braking_v1")));
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(Read("Biathlon/manual_ai_steering_v2")),Steering());
            return (runner,stepper,events);
        }

        [Test] public void ManualAiAndPlayerUseSharedClockAndKeepStreamSelectionExplicit()
        {
            var f=InitialCandidate(3);
            for(int i=0;i<16;i++)
            {
                var a=f.runner.Field.GetAthlete(0);var tick=a.Tick.Next();
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,600),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0));
                Assert.Throws<InvalidOperationException>(()=>f.runner.StepWithPlayer(0,input,0));
                f.runner.StepWithManualPlayer(0,input,17);
                Assert.That(f.runner.GetLastManualSteering(0),Is.EqualTo(17));
                Assert.That(f.runner.Field.Tick,Is.EqualTo(f.stepper.World.Tick));
                Assert.That(f.runner.Field.GetPhysicalPoint(0).LocalNanometers,
                    Is.EqualTo((long)Math.Round(f.stepper.World.GetActor(0).Observation.LocalMeters*SkiState.NanometersPerMeter)).Within(2));
            }
            Assert.That(f.runner.ContextJson,Does.Contain("not_production_default"));
            Assert.That(f.runner.Field.GetAthlete(1).Motion.DistanceNanometers,Is.GreaterThan(0));
        }

        [Test] public void InitialMass30MovesOnACommonClockWithoutWaitingBehindANeutralPlayer()
        {
            var f=InitialCandidate(30);
            for(int i=0;i<20;i++)
            {
                var a=f.runner.Field.GetAthlete(0);var tick=a.Tick.Next();
                f.runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,0),1,1,0),
                    new WeaponAimCommand(tick,a.AthleteId,0,0)),0);
            }
            Assert.That(f.runner.Field.Tick.Value,Is.EqualTo(20));
            Assert.That(f.runner.Field.GetAthlete(0).Motion.DistanceNanometers,Is.Zero);
            for(int i=1;i<30;i++)Assert.That(f.runner.Field.GetAthlete(i).Motion.DistanceNanometers,Is.GreaterThan(0),"row "+i);
        }

        [Test] public void Mass30PassesStationaryPlayerAndReachesTheFirstCourseBend()
        {
            var f=InitialCandidate(30);bool seenYaw=false;
            for(int i=0;i<400;i++)
            {
                var a=f.runner.Field.GetAthlete(0);var tick=a.Tick.Next();
                try
                {
                    f.runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,0),1,1,0),
                        new WeaponAimCommand(tick,a.AthleteId,0,0)),0);
                }
                catch(Exception e)
                {Assert.Fail("tick "+tick.Value+" / "+Explain(f.stepper)+" / "+e);}
                seenYaw|=f.runner.GetLastManualSteering(3)!=0;
            }
            Assert.That(seenYaw,Is.True);
            Assert.That(f.runner.Field.GetAthlete(3).RouteNanometers,Is.GreaterThan(f.runner.Field.GetAthlete(0).RouteNanometers+3*SkiState.NanometersPerMeter));
            long leading=0;for(int i=1;i<30;i++)leading=Math.Max(leading,f.runner.Field.GetAthlete(i).RouteNanometers);
            Assert.That(leading,Is.GreaterThan(60*SkiState.NanometersPerMeter));
        }

        [Test] public void SignedPreparedRosterPublishesOneCommonFieldWorldClock()
        {
            var f=new Fixture();f.Commands();long tick=f.Source.Pack.Tick.Value;var before=f.World.GetActor(0);
            long travel=f.World.GetPhysicalMotion(0).DistanceNanometers;
            Assert.That(f.Stepper.TryStep(f.Source.Inputs,new[]{431}),Is.True,Explain(f.Stepper));
            Assert.That(f.Stepper.LastStep.Status,Is.EqualTo(ManualFieldStepStatus.Complete));
            Assert.That(f.World.Tick.Value,Is.EqualTo(tick+1));Assert.That(f.Source.Pack.Tick,Is.EqualTo(f.World.Tick));
            Assert.That(f.World.GetActor(0).Pose.HeadingNanoradians,Is.Not.EqualTo(before.Pose.HeadingNanoradians));
            Assert.That(f.Source.Pack.GetManualScopedWorldActor(0).Pose,Is.EqualTo(f.World.GetActor(0).Pose));
            Assert.That(f.Source.Pack.GetAthlete(0).Motion.DistanceNanometers,Is.GreaterThan(travel));
        }
        [Test] public void ControlledBrakePaysTheSameFieldClockWithLessPhysicalTravel()
        {
            var coast=new Fixture();var brake=new Fixture();coast.Commands(0);brake.Commands(0);
            var before=brake.World.GetActor(0);long distance=brake.World.GetPhysicalMotion(0).DistanceNanometers;
            Assert.That(coast.Stepper.TryStep(coast.Source.Inputs,new[]{0}),Is.True,Explain(coast.Stepper));
            Assert.That(brake.Stepper.TryStep(brake.Source.Inputs,new[]{0},new[]{1d}),Is.True,Explain(brake.Stepper));
            Assert.That(brake.World.Tick,Is.EqualTo(coast.World.Tick));
            Assert.That(brake.World.Tick,Is.EqualTo(brake.Source.Pack.Tick));
            Assert.That(brake.World.GetPhysicalMotion(0).SpeedMetersPerSecond,Is.LessThan(coast.World.GetPhysicalMotion(0).SpeedMetersPerSecond));
            Assert.That(brake.World.GetPhysicalMotion(0).DistanceNanometers,Is.GreaterThan(distance).And.LessThan(coast.World.GetPhysicalMotion(0).DistanceNanometers));
            Assert.That(brake.World.GetActor(0).Pose,Is.Not.EqualTo(before.Pose));
            Assert.That(brake.World.GetActor(0).Pose.HeadingNanoradians,Is.EqualTo(before.Pose.HeadingNanoradians));
        }
        [TestCase(-.01)][TestCase(1.01)][TestCase(double.NaN)]
        public void InvalidLastBrakeCannotPublishEarlierRosterState(double fraction)
        {
            var f=new Fixture("start");f.Commands();long tick=f.World.Tick.Value;int events=f.Source.Events.Count;
            var before=f.World.GetActor(0);
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.Stepper.TryStep(f.Source.Inputs,new int[4],new[]{0d,0,0,fraction}));
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.EqualTo(tick));
            Assert.That(f.Source.Pack.Tick.Value,Is.EqualTo(tick));Assert.That(f.Source.Events.Count,Is.EqualTo(events));
        }
        [Test] public void AiRosterReachesItsFirstShootingBoutOnTheManualClock()
        {
            var f=InitialCandidate(3);bool shooting=false;
            for(int i=0;i<4000&&!shooting;i++)
            {
                try {f.runner.Step();}
                catch(Exception e)
                {Assert.Fail("tick "+f.runner.Field.Tick.Next().Value+" / "+Explain(f.stepper)+" / "+e);}
                for(int k=0;k<3;k++)shooting|=f.runner.Field.GetAthlete(k).ActiveShooting!=null;
            }
            Assert.That(shooting,Is.True,"AI must complete the ski lap and prepare for its first bout on the shared manual clock.");
        }
        [Test] public void StationaryShootingPaysClockWithoutInventingTravel()
        {
            var f=new Fixture("shooting");var before=f.World.GetActor(0);var motion=f.World.GetPhysicalMotion(0);
            for(int i=0;i<4;i++)
            {
                f.Commands();Assert.That(f.Stepper.TryStep(f.Source.Inputs,new[]{1000}),Is.True,Explain(f.Stepper));
                Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(before.Pose));Assert.That(f.World.GetActor(0).Velocity,Is.EqualTo(before.Velocity));
                Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(motion));
            }
            Assert.That(f.Source.Pack.GetAthlete(0).ActiveShooting,Is.Not.Null);
        }
        [Test] public void EveryAiReachesItsFirstBoutWithAnActuallyStationaryBody()
        {
            var f=InitialCandidate(3);var arrived=new bool[3];int count=0;
            for(int i=0;i<4500&&count<3;i++)
            {
                try {f.runner.Step();}
                catch(Exception e){Assert.Fail("tick "+f.runner.Field.Tick.Next().Value+" / "+Explain(f.stepper)+" / "+e);}
                for(int k=0;k<3;k++)if(!arrived[k]&&f.runner.Field.GetAthlete(k).ActiveShooting!=null)
                {
                    var actor=f.stepper.World.GetActor(k);arrived[k]=true;count++;
                    Assert.That(actor.Velocity.X,Is.Zero);Assert.That(actor.Velocity.Z,Is.Zero);
                    Assert.That(actor.Pose.YawRateRadiansPerSecond,Is.Zero);
                    Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
                }
            }
            Assert.That(count,Is.EqualTo(3));
        }
        [Test] public void TerminalMatArrivalRepeatsWorldPhysiologyAndEventsExactly()
        {
            var a=InitialCandidate(3);var b=InitialCandidate(3);var arrived=new bool[3];int count=0;
            for(int i=0;i<4500&&count<3;i++)
            {
                a.runner.Step();b.runner.Step();
                for(int k=0;k<3;k++)if(!arrived[k]&&a.runner.Field.GetAthlete(k).ActiveShooting!=null)
                {arrived[k]=true;count++;}
            }
            Assert.That(count,Is.EqualTo(3));Assert.That(a.stepper.World.Tick,Is.EqualTo(b.stepper.World.Tick));
            for(int k=0;k<3;k++)
            {
                Assert.That(a.stepper.World.GetActor(k).Pose,Is.EqualTo(b.stepper.World.GetActor(k).Pose));
                Assert.That(a.stepper.World.GetActor(k).Velocity,Is.EqualTo(b.stepper.World.GetActor(k).Velocity));
                Assert.That(a.runner.Field.GetAthlete(k).Motion,Is.EqualTo(b.runner.Field.GetAthlete(k).Motion));
                Assert.That(a.runner.Field.GetAthlete(k).RouteNanometers,Is.EqualTo(b.runner.Field.GetAthlete(k).RouteNanometers));
                Assert.That(a.runner.Field.GetAthlete(k).PhysiologyState,Is.EqualTo(b.runner.Field.GetAthlete(k).PhysiologyState));
            }
            Assert.That(a.events.Count,Is.EqualTo(b.events.Count));
            for(int e=0;e<a.events.Count;e++)Assert.That(a.events[e],Is.EqualTo(b.events[e]));
        }
        [Test] public void EveryAiCompletesItsFirstRangeAndResumesTheManualLap()
        {
            var f=InitialCandidate(3);int returned=0;
            for(int i=0;i<6000&&returned<3;i++)
            {
                try {f.runner.Step();}
                catch(Exception e){Assert.Fail("tick "+f.runner.Field.Tick.Next().Value+" / "+Explain(f.stepper)+" / "+e);}
                returned=0;
                for(int k=0;k<3;k++)
                {
                    var cursor=f.stepper.World.GetCursor(k);var row=f.stepper.World.GetCatalog(k).Get(cursor.OccurrenceIndex);
                    if(f.runner.Field.GetAthlete(k).Lifecycle.CompletedBouts==1&&row.Kind==ManualRacePathKind.BaseRoute)returned++;
                }
            }
            Assert.That(returned,Is.EqualTo(3),"All actors must complete actual shots, departure, penalty debt and range rejoin on the manual clock.");
        }
        [Test] public void FirstRangeDepartureRepeatsActualWorldMotionAndEventsExactly()
        {
            var a=InitialCandidate(3);var b=InitialCandidate(3);int returned=0;
            for(int i=0;i<6000&&returned<3;i++)
            {
                a.runner.Step();b.runner.Step();returned=0;
                for(int k=0;k<3;k++)
                {
                    var cursor=a.stepper.World.GetCursor(k);
                    if(a.runner.Field.GetAthlete(k).Lifecycle.CompletedBouts==1&&
                        a.stepper.World.GetCatalog(k).Get(cursor.OccurrenceIndex).Kind==ManualRacePathKind.BaseRoute)returned++;
                    Assert.That(a.stepper.World.GetActor(k).Pose,Is.EqualTo(b.stepper.World.GetActor(k).Pose));
                    Assert.That(a.stepper.World.GetActor(k).Velocity,Is.EqualTo(b.stepper.World.GetActor(k).Velocity));
                    Assert.That(a.runner.Field.GetAthlete(k).Motion,Is.EqualTo(b.runner.Field.GetAthlete(k).Motion));
                    Assert.That(a.runner.Field.GetAthlete(k).PhysiologyState,Is.EqualTo(b.runner.Field.GetAthlete(k).PhysiologyState));
                }
                Assert.That(a.stepper.LastStep.Intervals,Is.EqualTo(b.stepper.LastStep.Intervals));
                Assert.That(a.stepper.World.Tick,Is.EqualTo(b.stepper.World.Tick));
            }
            Assert.That(returned,Is.EqualTo(3));Assert.That(a.events.Count,Is.EqualTo(b.events.Count));
            for(int i=0;i<a.events.Count;i++)Assert.That(a.events[i],Is.EqualTo(b.events[i]));
        }
        [Test] public void InvalidFinalIntentCannotPublishAnyFieldClockOrEvent()
        {
            var f=new Fixture("start");f.Commands();long tick=f.World.Tick.Value;int events=f.Source.Events.Count;
            var pose=f.World.GetActor(0).Pose;
            Assert.Throws<ArgumentException>(()=>f.Stepper.TryStep(f.Source.Inputs,new[]{0,0,0,1001}));
            Assert.That(f.World.Tick.Value,Is.EqualTo(tick));Assert.That(f.Source.Pack.Tick.Value,Is.EqualTo(tick));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(pose));Assert.That(f.Source.Events.Count,Is.EqualTo(events));
        }
        [Test] public void StaleLastCommandCannotPrepareAnEarlierAcceptedPhase()
        {
            var f=new Fixture("start");f.Commands();var a=f.Source.Pack.GetAthlete(3);long tick=f.World.Tick.Value;
            f.Source.Inputs[3]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(a.Tick,a.AthleteId,600),1,1,0),new WeaponAimCommand(a.Tick,a.AthleteId,0,0));
            Assert.Throws<ArgumentException>(()=>f.Stepper.TryStep(f.Source.Inputs,new int[4]));
            Assert.That(f.World.Tick.Value,Is.EqualTo(tick));Assert.That(f.Source.Pack.Tick.Value,Is.EqualTo(tick));
        }
        [Test] public void RepeatedCommandsReconstructExactly()
        {
            var a=new Fixture();var b=new Fixture();var yaw=new[]{200};
            for(int i=0;i<12;i++)
            {
                a.Commands();b.Commands();Assert.That(a.Stepper.TryStep(a.Source.Inputs,yaw),Is.True,Explain(a.Stepper));
                Assert.That(b.Stepper.TryStep(b.Source.Inputs,yaw),Is.True,Explain(b.Stepper));
                Assert.That(a.World.GetActor(0).Pose,Is.EqualTo(b.World.GetActor(0).Pose));
                Assert.That(a.World.GetActor(0).Velocity,Is.EqualTo(b.World.GetActor(0).Velocity));
                Assert.That(a.Source.Pack.GetAthlete(0).RouteNanometers,Is.EqualTo(b.Source.Pack.GetAthlete(0).RouteNanometers));
                Assert.That(a.Source.Events.Count,Is.EqualTo(b.Source.Events.Count));
                for(int e=0;e<a.Source.Events.Count;e++)Assert.That(a.Source.Events[e],Is.EqualTo(b.Source.Events[e]));
            }
        }
        [Test] public void BoundBodyAndWorkAndForcePolicyAreMandatory()
        {
            var f=new Fixture();var body=OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(f.Source.Source.Venue.Occupancy.ToJson(),"body_width_m","0.3"));
            Assert.Throws<ArgumentException>(()=>new ManualScopedFieldStepper(f.Source.Pack,f.World,f.Models,body,Steering(),Work()));
            var work=SkiContactSweepDefinition.FromJson(Work().ToJson().Replace("stepper_work","other_work"));
            Assert.Throws<ArgumentException>(()=>new ManualScopedFieldStepper(f.Source.Pack,f.World,f.Models,f.Source.Source.Venue.Occupancy,Steering(),work));
            Assert.That(f.Stepper.ContextJson,Does.Contain("all_signed_intents"));Assert.That(f.Stepper.ContextJson,Does.Contain("drafting_unqualified"));
        }
        [TestCase(1)][TestCase(30)]
        public void InitialWoodlandMassGridUsesActualUnshiftedBodyPositions(int count)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var events=new RaceEventLog(10000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),count:count);
            var models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var world=ManualScopedFieldStepper.CreateInitialWorld(runner.Field,venue.Track,venue.Visual,venue.Occupancy,Contact(),Work(),Group(),models.Physics);
            var stepper=new ManualScopedFieldStepper(runner.Field,world,models,venue.Occupancy,Steering(),Work());
            var inputs=new SprintAthleteInput[count];var yaw=new int[count];
            for(int i=0;i<count;i++)
            {
                var a=runner.Field.GetAthlete(i);var p=runner.Field.GetPhysicalPoint(i);var visual=venue.Visual.Get(p.LoopId).Sample(p.LocalNanometers/(double)SkiState.NanometersPerMeter);
                double side=runner.Field.GetLateral(i);
                Assert.That(world.GetActor(i).Pose.X,Is.EqualTo(visual.X+visual.ForwardZ*side).Within(2e-9));
                Assert.That(world.GetActor(i).Pose.Z,Is.EqualTo(visual.Z-visual.ForwardX*side).Within(2e-9));
                // The pose is unchanged. At a curved knot the canonical local
                // projection can have a different station/lateral coordinate.
                inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(a.Tick.Next(),a.AthleteId,600),1,1,0),new WeaponAimCommand(a.Tick.Next(),a.AthleteId,0,0));
            }
            Assert.That(stepper.TryStep(inputs,yaw),Is.True,Explain(stepper));
            Assert.That(world.Tick.Value,Is.EqualTo(1));Assert.That(runner.Field.Tick,Is.EqualTo(world.Tick));
        }
    }
}
