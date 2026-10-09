using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScheduledPresenceTests
    {
        private static (PackAiRaceRunner runner,ManualScopedWorldRoster world,VenueAuthoringPlan venue,SkiModelDefinitions models) Initial()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+"sprint_profile")),
                RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+"sprint_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(1000),new AiDecisionCounts(),count:2);
            var models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var world=ManualScopedFieldStepper.CreateInitialWorld(runner.Field,venue.Track,venue.Visual,venue.Occupancy,
                ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),ManualScopedFieldStepperTests.Group(),models.Physics);
            return(runner,world,venue,models);
        }

        private static bool AppendStationary(ManualScopedWorldRoster world,VenueAuthoringPlan venue,ManualScopedWorldTick tick,SkiPhysicsDefinition physics,bool moveWaiting=false)
        {
            var snow=new ManualSkiSnowContactSystem(venue.Occupancy,1);
            var drive=new ManualSkiContactDriveSystem(physics,venue.Occupancy,20);
            var actors=new SkiWorldMotionCandidate[world.Count];var windows=new ManualScopedWorldWindow[world.Count];
            for(int i=0;i<world.Count;i++)
            {
                var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);
                var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,physics.MassKilograms);
                var motion=new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond);
                var trajectory=moveWaiting&&i==1?new ManualSkiWorldTrajectory(drive.Preview(body,motion,actor.Observation,
                    world.GetCatalog(i).SampleSurface(actor.Observation),400,0,0,.05)):
                    new ManualSkiWorldTrajectory(snow.PreviewTrajectory(body,motion,0,0,.05));
                actors[i]=new SkiWorldMotionCandidate(actor.Athlete,trajectory);
                windows[i]=new ManualScopedWorldWindow(actor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,1,world.GetCatalog(i).Get(cursor.OccurrenceIndex).LengthNanometers);
            }
            return world.TryAppendToOwnedFirstEvent(tick,actors,windows,Array.Empty<SkiActorCourseEdge>(),0,new double[world.Count],out _);
        }

        [Test] public void WaitingSprintBodiesDoNotCollideWithTheStartedAthlete()
        {
            var f=Initial();var tick=f.world.BeginTick();
            Assert.That(f.world.TryResolveOwnedCurrentContacts(tick),Is.True);
            Assert.That(AppendStationary(f.world,f.venue,tick,f.models.Physics),Is.True);
            Assert.That(f.world.TryFinalizeTick(tick,out var proposal),Is.True);
            var before=f.world.GetActor(1);f.world.Commit(proposal);
            Assert.That(f.world.GetActor(1).Pose,Is.EqualTo(before.Pose));
            Assert.That(f.world.GetPhysicalMotion(1).DistanceNanometers,Is.Zero);
            Assert.That(f.world.LastOwnedContacts.Count,Is.Zero);
        }

        [Test] public void WaitingBodyCannotAcquireHiddenMotion()
        {
            var f=Initial();var tick=f.world.BeginTick();
            Assert.That(f.world.TryResolveOwnedCurrentContacts(tick),Is.True);
            Assert.That(AppendStationary(f.world,f.venue,tick,f.models.Physics,moveWaiting:true),Is.False);
            Assert.That(f.world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.SourceMismatch));
            f.world.AbortTick(tick);Assert.That(f.world.Tick.Value,Is.Zero);
        }

        [Test] public void InitialPresenceBindingDoesNotBypassReusableMatOwnership()
        {
            var f=Initial();
            Assert.Throws<NotSupportedException>(()=>new ManualScopedFieldStepper(f.runner.Field,f.world,f.models,f.venue.Occupancy,
                ManualScopedFieldStepperTests.Steering(),ManualScopedFieldStepperTests.Work()));
        }

        [Test] public void ArrivalOfTheScheduledStartRestoresRealCollisionAdmission()
        {
            var f=Initial();long start=f.runner.Field.GetAthlete(1).ScheduledStart.Value;
            Assert.That(f.world.IsPhysicalParticipant(1,new SimulationTick(start-1)),Is.False);
            Assert.That(f.world.IsPhysicalParticipant(1,new SimulationTick(start)),Is.True);
            // Isolate presence on the world clock. These static proposals do
            // not claim full field/session or reusable-resource publication.
            while(f.world.Tick.Next().Value<start)
            {
                var tick=f.world.BeginTick();Assert.That(f.world.TryResolveOwnedCurrentContacts(tick),Is.True);
                Assert.That(AppendStationary(f.world,f.venue,tick,f.models.Physics),Is.True);
                Assert.That(f.world.TryFinalizeTick(tick,out var proposal),Is.True);f.world.Commit(proposal);
            }
            var active=f.world.BeginTick();
            Assert.That(f.world.TryResolveOwnedCurrentContacts(active),Is.False,"Two now-present bodies at the same pose cannot bypass collision checks.");
            Assert.That(f.world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactGeometry));
            f.world.AbortTick(active);Assert.That(f.world.Tick.Value,Is.EqualTo(start-1));
        }

        [Test] public void AWorldWithoutAttestedScheduleKeepsEveryBodyPresent()
        {
            var f=Initial();var catalogs=new ManualRacePathCatalog[2];var cursors=new ManualRacePathCursor[2];
            var poses=new ManualSkiHeadingState[2];var velocities=new SkiPlanarVelocity[2];var motion=new SkiState[2];
            for(int i=0;i<2;i++)
            {catalogs[i]=f.world.GetCatalog(i);cursors[i]=f.world.GetCursor(i);poses[i]=f.world.GetActor(i).Pose;velocities[i]=f.world.GetActor(i).Velocity;motion[i]=f.world.GetPhysicalMotion(i);}
            var world=new ManualScopedWorldRoster(catalogs,f.venue.Occupancy,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),
                cursors,poses,velocities,motion,20,responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{f.models.Physics.MassKilograms,f.models.Physics.MassKilograms});
            Assert.That(world.IsPhysicalParticipant(1,default),Is.True);
            Assert.That(world.ContextJson,Is.Not.EqualTo(f.world.ContextJson));
            var tick=world.BeginTick();Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.False);
            world.AbortTick(tick);
        }
    }
}
