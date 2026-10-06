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
            var shooting=ShootingSessionTests.Athlete();
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            var session=new SprintAthleteSession(new SimulationConfig(1,"manual_base_bridge",20,123),
                profile,new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),
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
            var velocity=new SkiPlanarVelocity(session.Motion.SpeedMetersPerSecond*target.ForwardX,
                session.Motion.SpeedMetersPerSecond*target.ForwardZ);
            var body=venue.Occupancy;
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"bridge_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"bridge_sweep\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var progress=new ManualRouteProgression(route);
            var ledger=new ManualWorldRosterLedger(route,projection,
                new ManualRouteJointSweep(projection,body,contact,work),progress,
                new[]{progress.Reconstitute(session.AthleteId,0,session.RouteNanometers,session.RouteNanometers)},
                new[]{pose},new[]{velocity},20,new[]{session.Motion},session.Tick);
            var trajectory=new ManualSkiSnowContactSystem(body,20).PreviewTrajectory(
                new ManualSkiContactBody(session.AthleteId,pose,velocity,75),
                new SkiContactMotion(velocity,0),0,0,.05);
            var actors=new[]{new SkiWorldMotionCandidate(session.AthleteId,
                new ManualSkiWorldTrajectory(trajectory))};
            var windows=new[]{new ManualRouteActorWindow(session.AthleteId,0,session.RouteNanometers,2,false)};
            Assert.That(ledger.TryPrepareWithOdometer(actors,windows,
                new[]{session.RouteNanometers+2*SkiState.NanometersPerMeter},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out var world),Is.True);
            var legacy=session.PrepareStep(SprintAthleteSessionTests.Script(session));
            var manual=session.PrepareManualBaseRouteCommit(legacy,world,0);
            Assert.Throws<ArgumentException>(()=>session.CommitStep(legacy));
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
    }
}
