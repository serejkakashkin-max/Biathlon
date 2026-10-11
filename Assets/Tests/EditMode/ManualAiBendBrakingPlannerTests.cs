using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualAiBendBrakingPlannerTests
    {
        private static ManualAiBendBrakingPlanner Create(ManualRacePathCatalogTests.Fixture f)
        {
            var models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            return new ManualAiBendBrakingPlanner(f.Venue.Track,f.Catalog,models.Physics,models.Cornering,
                ManualScopedFieldStepperTests.Steering(),ManualAiSteeringDefinition.FromJson(
                    ManualScopedFieldStepperTests.Read("Biathlon/manual_ai_steering_v2")),f.Venue.Occupancy);
        }
        private static ManualRacePathObservation At(ManualRacePathCatalogTests.Fixture f,int occurrence,double arc)
        {
            long nm=(long)Math.Round(arc*SkiState.NanometersPerMeter);
            Assert.That(f.Catalog.TryProject(f.At(occurrence,nm),occurrence,nm,.001,out var at),Is.True);
            return at;
        }
        [Test] public void AiRequestsAnExplicitSharedBrakeOnlyForDemandingSteering()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            var at=At(f,1,10);var surface=f.Catalog.SampleSurface(at).WithTravelGeometry(0,0);
            Assert.That(planner.SteeringBrake(0,14,0,surface,70,70,0),Is.Zero);
            double left=planner.SteeringBrake(-1000,14,0,surface,70,70,0);
            double right=planner.SteeringBrake(1000,14,0,surface,70,70,0);
            Assert.That(left,Is.GreaterThan(0).And.LessThanOrEqualTo(1));
            Assert.That(right,Is.EqualTo(left),"AI changes an explicit intent, never the shared athlete speed or power.");
        }
        [Test] public void DistantDepartureTurnAllowsAccelerationAlongTheStraight()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var catalog=f.stepper.World.GetCatalog(0);
            int exit=catalog.GetFiringOccurrence(0)+1;
            catalog.TrySample(exit,3*SkiState.NanometersPerMeter,out var p);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            Assert.That(catalog.TryProject(pose,exit,3*SkiState.NanometersPerMeter,.001,out var at),Is.True);
            var planner=new ManualAiBendBrakingPlanner(f.runner.Field.Track,catalog,f.stepper.PhysicsDefinition,f.stepper.CorneringDefinition,
                ManualScopedFieldStepperTests.Steering(),ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),f.stepper.BodyDefinition);
            var plan=planner.Plan(at,1.1,70,70,0);
            Assert.That(plan.SpeedLimit,Is.GreaterThan(1.1));Assert.That(plan.BrakeFraction,Is.Zero);
            Assert.That(plan.ReduceEffort,Is.False,"The future bend must not cancel propulsion throughout the straight exit.");
        }
        [Test] public void OrderedSuccessorBendRequestsBrakingAndShorterChordBeforeEntry()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            int before=f.Catalog.GetFiringOccurrence(0)-1;
            var at=At(f,before,f.Catalog.Get(before).LengthNanometers/(double)SkiState.NanometersPerMeter-2);
            var plan=planner.Plan(at,8,70,70,0);
            Assert.That(plan.SpeedLimit,Is.LessThan(8));Assert.That(plan.BrakeFraction,Is.EqualTo(1));
            Assert.That(plan.ShortLookaheadMeters,Is.GreaterThan(2).And.LessThan(4));
            Assert.That(at.OccurrenceIndex,Is.EqualTo(before),"A target query must not hand off an occurrence.");
        }
        [Test] public void TightBendUsesTheSharedYawAndGripLimits()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            int turn=f.Catalog.GetFiringOccurrence(0);var at=At(f,turn,1);
            var standard=planner.Plan(at,5,70,70,0);
            var weakerResponse=planner.Plan(at,5,70,70,0,steeringResponse:.5);
            var lowGrip=planner.Plan(at,5,70,70,0,grip:.01);
            // The folded physical turn is not the loop's uniform scalar
            // curvature. It still respects response and snow grip, without
            // imposing the high-speed yaw penalty at walking speed.
            Assert.That(standard.SpeedLimit,Is.GreaterThan(0).And.LessThan(1.5));
            Assert.That(standard.BrakeFraction,Is.EqualTo(1));
            Assert.That(weakerResponse.SpeedLimit,Is.EqualTo(standard.SpeedLimit*.5).Within(1e-12));
            Assert.That(lowGrip.SpeedLimit,Is.LessThan(standard.SpeedLimit));
        }
        [Test] public void FuturePrivateWidthIsObservedBeforeForkButClosedFiringGateStopsTheQuery()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            int privateApproach=f.Catalog.GetFiringOccurrence(0);
            int common=privateApproach-1;
            var before=At(f,common,f.Catalog.Get(common).LengthNanometers/(double)SkiState.NanometersPerMeter-2);
            var inside=At(f,privateApproach,1);
            double privateWidth=f.Catalog.SampleSurface(inside).WidthMeters;
            Assert.That(f.Catalog.SampleSurface(before).WidthMeters,Is.GreaterThan(privateWidth));
            Assert.That(planner.Plan(before,4,70,70,0).MinimumWidthMeters,Is.EqualTo(privateWidth));
            var firing=At(f,privateApproach,f.Catalog.Get(privateApproach).LengthNanometers/(double)SkiState.NanometersPerMeter-.1);
            Assert.That(planner.Plan(firing,0,70,70,0).MinimumWidthMeters,
                Is.EqualTo(f.Catalog.SampleSurface(firing).WidthMeters));
            Assert.That(before.OccurrenceIndex,Is.EqualTo(common));
            Assert.That(planner.ContextJson,Does.Contain("v4_actual_mat_geometry_low_speed_yaw"));
        }
        [Test] public void StraightOwnedStartDoesNotInventBrakingOrChangeObservation()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            var at=At(f,f.Catalog.StartApproachIndex,5);long arc=at.PhysicalArcNanometers;
            var plan=planner.Plan(at,0,50,50,0);
            Assert.That(plan.BrakeFraction,Is.Zero);Assert.That(plan.ShortLookaheadMeters,Is.Zero);
            Assert.That(at.PhysicalArcNanometers,Is.EqualTo(arc));
            Assert.That(planner.ContextJson.Contains("closed_phase_stops_lookahead"),Is.True);
        }
        [Test] public void SameOrderedQueryIsDeterministicAndAllocationFree()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            var at=At(f,f.Catalog.GetFiringOccurrence(0),1);var expected=planner.Plan(at,5,70,70,.1);
            for(int i=0;i<100;i++)planner.Plan(at,5,70,70,.1);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                var actual=planner.Plan(at,5,70,70,.1);
                if(actual.SpeedLimit!=expected.SpeedLimit||actual.BrakeFraction!=expected.BrakeFraction||
                    actual.ShortLookaheadMeters!=expected.ShortLookaheadMeters||
                    actual.MinimumWidthMeters!=expected.MinimumWidthMeters)Assert.Fail("Ordered intent changed.");
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void InvalidSourceAndNumbersCannotBecomeIntent()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var other=new ManualRacePathCatalogTests.Fixture();var planner=Create(f);
            var at=At(f,f.Catalog.StartApproachIndex,5);
            Assert.Throws<ArgumentException>(()=>planner.Plan(At(other,other.Catalog.StartApproachIndex,5),1,50,50,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,double.NaN,50,50,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,1,101,50,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,1,50,-1,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,1,50,50,-.1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,1,50,50,0,grip:0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(at,1,50,50,0,steeringResponse:0));
        }
    }
}
