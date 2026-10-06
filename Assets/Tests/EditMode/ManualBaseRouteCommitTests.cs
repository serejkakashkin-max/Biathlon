using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualBaseRouteCommitTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        [TestCase(false)]
        [TestCase(true)]
        public void FullClearWorldProposalCanReplaceOnlyBaseRouteMovement(bool invalidateWorldFirst)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"sprint_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"sprint_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var ski=TimeTrialTests.Athlete();
            var skiModel=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var shooting=ShootingSessionTests.Athlete();
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            var session=new SprintAthleteSession(new SimulationConfig(1,"manual_base_bridge",20,123),
                profile,skiModel,
                ShootingSessionTests.Model(),ski,shooting,venue.Track,route,
                new CheckpointLayout(venue.Track,route),SnowConditionTests.Conditions(),
                "ski_neutral",new SimulationTick(1),
                new ShootingLaneAssignment(venue.Track.Range.Id,"lane_1",1),new RaceEventLog(256));
            session.Step(SprintAthleteSessionTests.Script(session));
            while(session.RouteNanometers<10*SkiState.NanometersPerMeter)
                session.Step(SprintAthleteSessionTests.Script(session));
            var projection=new ManualRouteProjectionMap(route,venue.Visual);
            var section=route.GetSection(0);
            Assert.That(session.RouteNanometers,Is.LessThan(section.Length.Millimeters*SkiState.NanometersPerMillimeter));
            Assert.That(projection.TrySampleAhead(0,0,session.RouteNanometers/(double)SkiState.NanometersPerMeter,
                false,out var target),Is.True);
            var pose=new ManualSkiHeadingState(target.X,target.Z,Math.Atan2(target.ForwardX,target.ForwardZ));
            Assert.That(projection.TryProject(pose,0,session.RouteNanometers,.001,false,out var atStart),Is.True,
                "The sampled initial world pose must project onto the selected base route.");
            double grade=atStart.GradeAlong(pose),planarSpeed=session.Motion.SpeedMetersPerSecond/Math.Sqrt(1+grade*grade);
            var velocity=new SkiPlanarVelocity(planarSpeed*Math.Sin(pose.HeadingRadians),
                planarSpeed*Math.Cos(pose.HeadingRadians));
            var body=venue.Occupancy;
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"bridge_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"bridge_sweep\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var progress=new ManualRouteProgression(route);
            var ledger=new ManualWorldRosterLedger(route,projection,
                new ManualRouteJointSweep(projection,body,contact,work),progress,
                new[]{progress.Reconstitute(session.AthleteId,0,session.RouteNanometers,session.RouteNanometers)},
                new[]{pose},new[]{velocity},20,new[]{session.Motion},session.Tick);
            var pending=session.PrepareManualBaseStep(SprintAthleteSessionTests.Script(session));
            Assert.That(pending.ManualWorldRequired,Is.True);
            Assert.That(pending.Movement.SegmentCount,Is.Zero);
            Assert.Throws<InvalidOperationException>(()=>session.CommitStep(pending));
            var accepted=ledger.GetActor(0);
            var surface=new TrackSampler(venue.Track.GetLoop(accepted.Projection.LoopId))
                .Sample(new TrackDistance((long)Math.Floor(accepted.Projection.LocalMeters*1000)));
            var input=session.GetPreparedManualMotionInput(pending,accepted,surface,500);
            Assert.That(input.IsValid,Is.True);
            Assert.That(input.SteeringInput,Is.EqualTo(500));
            Assert.That(input.DeliveredPowerWatts,Is.GreaterThan(0));
            var expectedForm=new SkiTechniqueSystem(skiModel.Technique).Evaluate(
                ski.TechniqueStats,
                surface.WithTravelGeometry(accepted.Projection.GradeAlong(accepted.Pose),0),
                session.PhysiologyState.MuscularFatigue,
                pending.Physiology.Effort.DeliveredPowerWatts);
            Assert.That(input.DeliveredPowerWatts,
                Is.EqualTo(expectedForm.PropulsivePowerWatts).Within(1e-9));
            var snow=new SnowConditionSystem(skiModel.Snow,SnowConditionTests.Conditions(),venue.Track);
            var gear=new SkiEquipmentSystem(skiModel.Equipment.GetSki("ski_neutral"),skiModel.Snow)
                .Evaluate(snow.Sample(surface).Id);
            Assert.That(input.SnowResistanceMultiplier,
                Is.EqualTo(gear.SnowResistanceMultiplier));
            Assert.That(input.GripMultiplier,Is.EqualTo(gear.CorneringGripMultiplier));
            var solver=new ManualSkiMotionSystem(skiModel.Physics,
                ManualSkiForceTrajectoryTests.HeadingDefinition(),skiModel.Cornering,20);
            var steering=solver.Preview(session.Motion,accepted.Pose,accepted.Projection,surface,input);
            Assert.That(steering.Heading.State.HeadingNanoradians,
                Is.Not.EqualTo(accepted.Pose.HeadingNanoradians));
            var actors=new[]{new SkiWorldMotionCandidate(session.AthleteId,
                new ManualSkiWorldTrajectory(steering.Heading))};
            var windows=new[]{new ManualRouteActorWindow(session.AthleteId,0,session.RouteNanometers,2,false)};
            Assert.That(ledger.TryPrepareWithOdometer(actors,windows,
                new[]{session.RouteNanometers+2*SkiState.NanometersPerMeter},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{grade},out var world),Is.True,
                "The shared force trajectory must clear the base route tube and world roster.");
            Assert.That(ledger.GetProposedPhysicalMotion(world,0).DistanceNanometers-session.Motion.DistanceNanometers,
                Is.EqualTo((long)Math.Round(steering.Heading.TravelDistanceAt(1)*SkiState.NanometersPerMeter,
                    MidpointRounding.ToEven)));
            var manual=session.PrepareManualBaseRouteCommit(pending,world,0);
            Assert.Throws<ArgumentException>(()=>session.CommitStep(pending));
            Assert.That(manual.Movement.SegmentCount,Is.EqualTo(1));
            Assert.That(manual.Movement.Motion.DistanceNanometers,
                Is.EqualTo(ledger.GetProposedPhysicalMotion(world,0).DistanceNanometers));
            if(invalidateWorldFirst)
            {
                ledger.Commit(world);
                Assert.Throws<ArgumentException>(()=>session.CommitStep(manual));
                Assert.That(session.Tick.Value,Is.EqualTo(world.Tick.Value-1));
                return;
            }
            session.CommitStep(manual);
            ledger.Commit(world);
            Assert.That(session.RouteNanometers,Is.EqualTo(ledger.GetCursor(0).CreditedStationNanometers));
            Assert.That(session.Motion,Is.EqualTo(ledger.GetPhysicalMotion(0)));
        }

        [Test] public void TwoAthletesCommitOneWorldTickOrNeither()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"sprint_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"sprint_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var skiModel=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var baseSki=TimeTrialTests.Athlete();var baseShot=ShootingSessionTests.Athlete();
            var sessions=new SprintAthleteSession[2];
            for(int i=0;i<2;i++)
            {
                string id="manual_roster_"+i;
                var ski=TimeTrialAthlete.FromJson(baseSki.ToJson().Replace(baseSki.Id,id));
                var shooting=ShootingAthleteDefinition.FromJson(baseShot.ToJson().Replace(baseShot.Id,id));
                sessions[i]=new SprintAthleteSession(new SimulationConfig(1,"manual_roster",20,(ulong)(123+i)),
                    profile,skiModel,ShootingSessionTests.Model(),ski,shooting,venue.Track,route,
                    new CheckpointLayout(venue.Track,route),SnowConditionTests.Conditions(),
                    "ski_neutral",new SimulationTick(i==0?1:41),
                    new ShootingLaneAssignment(venue.Track.Range.Id,"lane_"+(i+1),i+1),new RaceEventLog(256));
            }
            while(sessions[0].RouteNanometers<40*SkiState.NanometersPerMeter)
                for(int i=0;i<2;i++)sessions[i].Step(SprintAthleteSessionTests.Script(sessions[i]));
            Assert.That(sessions[1].RouteNanometers,Is.GreaterThan(0));
            Assert.That(sessions[0].RouteNanometers-sessions[1].RouteNanometers,
                Is.GreaterThan(3*SkiState.NanometersPerMeter));
            var projection=new ManualRouteProjectionMap(route,venue.Visual);
            var progress=new ManualRouteProgression(route);
            var cursors=new ManualRouteProgressCursor[2];var poses=new ManualSkiHeadingState[2];
            var velocities=new SkiPlanarVelocity[2];var motion=new SkiState[2];
            for(int i=0;i<2;i++)
            {
                var session=sessions[i];
                Assert.That(projection.TrySampleAhead(0,0,
                    session.RouteNanometers/(double)SkiState.NanometersPerMeter,false,out var target),Is.True);
                var pose=new ManualSkiHeadingState(target.X,target.Z,Math.Atan2(target.ForwardX,target.ForwardZ));
                Assert.That(projection.TryProject(pose,0,session.RouteNanometers,0,false,out var observed),Is.True);
                double grade=observed.GradeAlong(pose);
                double speed=session.Motion.SpeedMetersPerSecond/Math.Sqrt(1+grade*grade);
                poses[i]=pose;velocities[i]=new SkiPlanarVelocity(
                    speed*Math.Sin(pose.HeadingRadians),speed*Math.Cos(pose.HeadingRadians));
                cursors[i]=progress.Reconstitute(session.AthleteId,0,session.RouteNanometers,session.RouteNanometers);
                motion[i]=session.Motion;
            }
            var body=venue.Occupancy;
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"roster_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"roster_sweep\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var ledger=new ManualWorldRosterLedger(route,projection,
                new ManualRouteJointSweep(projection,body,contact,work),progress,
                cursors,poses,velocities,20,motion,sessions[0].Tick);
            var wrongClock=new ManualWorldRosterLedger(route,projection,
                new ManualRouteJointSweep(projection,body,contact,work),progress,
                cursors,poses,velocities,30,motion,sessions[0].Tick);
            Assert.Throws<ArgumentException>(()=>new ManualBaseRosterSessionCommit(route,sessions,wrongClock));
            var pending=new SprintStepPreview[2];var actors=new SkiWorldMotionCandidate[2];
            var windows=new ManualRouteActorWindow[2];var maximum=new long[2];var grades=new double[2];
            var solver=new ManualSkiMotionSystem(skiModel.Physics,
                ManualSkiForceTrajectoryTests.HeadingDefinition(),skiModel.Cornering,20);
            for(int i=0;i<2;i++)
            {
                var session=sessions[i];pending[i]=session.PrepareManualBaseStep(SprintAthleteSessionTests.Script(session));
                var accepted=ledger.GetActor(i);
                var surface=new TrackSampler(venue.Track.GetLoop(accepted.Projection.LoopId))
                    .Sample(new TrackDistance((long)Math.Floor(accepted.Projection.LocalMeters*1000)));
                var input=session.GetPreparedManualMotionInput(pending[i],accepted,surface,i==0?500:-500);
                var heading=solver.Preview(session.Motion,accepted.Pose,accepted.Projection,surface,input).Heading;
                actors[i]=new SkiWorldMotionCandidate(session.AthleteId,new ManualSkiWorldTrajectory(heading));
                windows[i]=new ManualRouteActorWindow(session.AthleteId,0,session.RouteNanometers,2,false);
                maximum[i]=session.RouteNanometers+2*SkiState.NanometersPerMeter;
                grades[i]=accepted.Projection.GradeAlong(accepted.Pose);
            }
            Assert.That(ledger.TryPrepareWithOdometer(actors,windows,maximum,
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                grades,out var world),Is.True);
            var transaction=new ManualBaseRosterSessionCommit(route,sessions,ledger);
            var priorTick=ledger.Tick;
            Assert.Throws<ArgumentException>(()=>transaction.Commit(new[]{pending[0]},world));
            Assert.That(ledger.Tick,Is.EqualTo(priorTick));
            Assert.That(sessions[0].Tick,Is.EqualTo(priorTick));
            Assert.That(sessions[1].Tick,Is.EqualTo(priorTick));
            sessions[1].PrepareManualBaseStep(SprintAthleteSessionTests.Script(sessions[1]));
            Assert.Throws<ArgumentException>(()=>transaction.Commit(pending,world));
            Assert.That(ledger.Tick,Is.EqualTo(priorTick));
            Assert.That(sessions[0].Tick,Is.EqualTo(priorTick));
            Assert.That(sessions[1].Tick,Is.EqualTo(priorTick));
            for(int i=0;i<2;i++)pending[i]=sessions[i].PrepareManualBaseStep(
                SprintAthleteSessionTests.Script(sessions[i]));
            transaction.Commit(pending,world);
            Assert.That(ledger.Tick,Is.EqualTo(priorTick.Next()));
            for(int i=0;i<2;i++)
            {
                Assert.That(sessions[i].Tick,Is.EqualTo(ledger.Tick));
                Assert.That(sessions[i].RouteNanometers,Is.EqualTo(ledger.GetCursor(i).CreditedStationNanometers));
                Assert.That(sessions[i].Motion,Is.EqualTo(ledger.GetPhysicalMotion(i)));
            }
        }

        [Test] public void ManualWorldTickPublishesThroughSprintFieldClockAndCheckpoints()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"sprint_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"sprint_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var ski=TimeTrialTests.Athlete();var shooting=ShootingSessionTests.Athlete();
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            var skiModel=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var events=new RaceEventLog(512);
            var field=new SprintFieldSession(new SimulationConfig(1,"manual_field_bridge",20,123),
                profile,skiModel,ShootingSessionTests.Model(),venue.Track,route,
                SnowConditionTests.Conditions(),SprintResultsTests.Timing(),
                new[]{new SprintEntrant(ski,shooting,"ski_neutral")},new SimulationTick(1),
                "manual_field_result",events);
            var session=field.GetAthlete(0);
            while(session.RouteNanometers<10*SkiState.NanometersPerMeter)
                field.Step(new[]{SprintAthleteSessionTests.Script(session)});
            var projection=new ManualRouteProjectionMap(route,venue.Visual);
            Assert.That(projection.TrySampleAhead(0,0,
                session.RouteNanometers/(double)SkiState.NanometersPerMeter,false,out var target),Is.True);
            var pose=new ManualSkiHeadingState(target.X,target.Z,Math.Atan2(target.ForwardX,target.ForwardZ));
            Assert.That(projection.TryProject(pose,0,session.RouteNanometers,0,false,out var observed),Is.True);
            double grade=observed.GradeAlong(pose);
            double planarSpeed=session.Motion.SpeedMetersPerSecond/Math.Sqrt(1+grade*grade);
            var velocity=new SkiPlanarVelocity(planarSpeed*Math.Sin(pose.HeadingRadians),
                planarSpeed*Math.Cos(pose.HeadingRadians));
            var progress=new ManualRouteProgression(route);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"field_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"field_sweep\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var ledger=new ManualWorldRosterLedger(route,projection,
                new ManualRouteJointSweep(projection,venue.Occupancy,contact,work),progress,
                new[]{progress.Reconstitute(session.AthleteId,0,session.RouteNanometers,session.RouteNanometers)},
                new[]{pose},new[]{velocity},20,new[]{session.Motion},field.Tick);
            var command=SprintAthleteSessionTests.Script(session);
            var pending=session.PrepareManualBaseStep(command);
            var accepted=ledger.GetActor(0);
            var surface=new TrackSampler(venue.Track.GetLoop(accepted.Projection.LoopId))
                .Sample(new TrackDistance((long)Math.Floor(accepted.Projection.LocalMeters*1000)));
            var input=session.GetPreparedManualMotionInput(pending,accepted,surface,250);
            var heading=new ManualSkiMotionSystem(skiModel.Physics,
                ManualSkiForceTrajectoryTests.HeadingDefinition(),skiModel.Cornering,20)
                .Preview(session.Motion,accepted.Pose,accepted.Projection,surface,input).Heading;
            Assert.That(ledger.TryPrepareWithOdometer(new[]{new SkiWorldMotionCandidate(session.AthleteId,
                    new ManualSkiWorldTrajectory(heading))},
                new[]{new ManualRouteActorWindow(session.AthleteId,0,session.RouteNanometers,2,false)},
                new[]{session.RouteNanometers+2*SkiState.NanometersPerMeter},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{grade},out var world),Is.True);
            var transaction=new ManualBaseRosterSessionCommit(route,new[]{session},ledger);
            long previous=field.Tick.Value;
            transaction.CommitField(field,new[]{command},new[]{pending},world);
            Assert.That(field.Tick.Value,Is.EqualTo(previous+1));
            Assert.That(session.Tick,Is.EqualTo(field.Tick));
            Assert.That(ledger.Tick,Is.EqualTo(field.Tick));
            Assert.That(session.Checkpoints.Tick,Is.EqualTo(field.Tick));
            Assert.That(session.Motion,Is.EqualTo(ledger.GetPhysicalMotion(0)));
            Assert.That(session.RouteNanometers,Is.EqualTo(ledger.GetCursor(0).CreditedStationNanometers));
            Assert.That(events.Count,Is.GreaterThan(0));
        }
    }
}
