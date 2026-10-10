using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualNumericalToleranceTests
    {
        private static ManualNumericalToleranceDefinition Policy()=>ManualNumericalToleranceDefinition.FromJson(
            PhysiologyContractTests.DefinitionJson("manual_numerical_tolerance_v1.json"));
        [Test] public void ProductionShoulderAndOrdinaryWindowAreExplicitAndBounded()
        {
            var json=PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json");
            var practical=ManualNumericalToleranceDefinition.FromJson(json);
            Assert.That(practical.SoftSnowShoulder,Is.EqualTo(2));
            Assert.That(practical.OrdinaryHandoffWindow,Is.EqualTo(4));
            Assert.That(ManualNumericalToleranceDefinition.FromJson(practical.ToJson()).ToJson(),Is.EqualTo(practical.ToJson()));
            Assert.Catch(()=>ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"soft_snow_shoulder_m","2.01")));
            Assert.Catch(()=>ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"ordinary_handoff_window_m","4.01")));
            Assert.That(Policy().SoftSnowShoulder,Is.Zero);Assert.That(Policy().OrdinaryHandoffWindow,Is.Zero);
        }

        [Test] public void VersionedPolicyRetainsARealCourseGuard()
        {
            var policy=Policy();var body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            Assert.That(policy.SchemaVersion,Is.EqualTo(1));Assert.That(policy.Id,Is.EqualTo("manual_numerical_tolerance_v1"));
            Assert.That(ManualNumericalToleranceDefinition.FromJson(policy.ToJson()).ToJson(),Is.EqualTo(policy.ToJson()));
            Assert.DoesNotThrow(()=>policy.Validate(body));
            Assert.Throws<ArgumentException>(()=>policy.Validate(OccupancyDefinition.FromJson(
                PhysiologyContractTests.ReplaceNumber(body.ToJson(),"edge_clearance_m","0.01"))));
        }
        [TestCase("terminal_rest_speed_mps","-0.1")][TestCase("terminal_rest_speed_mps","0.1")]
        [TestCase("terminal_rest_yaw_radps","0.1")][TestCase("course_clearance_allowance_m","0.1")]
        public void ExcessiveOrNegativeToleranceIsRejected(string key,string value)
            =>Assert.Catch(()=>ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Policy().ToJson(),key,value)));

        [TestCase(true,0.000000015,0,true)]
        [TestCase(false,0.000000015,0,false)]
        [TestCase(true,0.002,0,false)]
        [TestCase(true,0.000000015,0.002,false)]
        public void OnlyExplicitTerminalNumericalRestIsCanonicalizedAfterTheWholePhysicalProof(bool selected,double speed,double yaw,bool normalize)
        {
            var f=new ManualRacePathCatalogTests.Fixture("sprint");var catalog=f.Catalog;
            int occurrence=catalog.GetFiringOccurrence(0);var row=catalog.Get(occurrence);
            Assert.That(catalog.TrySample(occurrence,row.LengthNanometers,out var end),Is.True);
            // Finish exactly on the real owned plane. The raw motion still
            // pays the entire interval and odometer; only its terminal rest
            // representation may be canonicalized by the selected policy.
            var velocity=new SkiPlanarVelocity(speed*end.ForwardX,speed*end.ForwardZ);
            var pose=new ManualSkiHeadingState(end.X-velocity.X*.05,end.Z-velocity.Z*.05,
                Math.Atan2(end.ForwardX,end.ForwardZ)-yaw*.05,yaw);
            Assert.That(catalog.TryProject(pose,occurrence,row.LengthNanometers,1,out var at),Is.True);
            var id=new AthleteId("tolerance_terminal");var cursor=new ManualRacePathProgression(catalog).Reconstitute(id,occurrence,
                at.PhysicalArcNanometers,at.PhysicalArcNanometers,at.MappedRouteNanometers);
            var world=new ManualScopedWorldRoster(new[]{catalog},f.Venue.Occupancy,ManualScopedFieldStepperTests.Contact(),
                ManualScopedFieldStepperTests.Work(),new[]{cursor},new[]{pose},new[]{velocity},new[]{new SkiState(0,speed)},20,
                numericalTolerance:selected?Policy():null);
            Assert.That(world.ContextJson.Contains("manual_numerical_tolerance_v1"),Is.EqualTo(selected));
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.Venue.Occupancy,20).PreviewTrajectory(
                new ManualSkiContactBody(id,pose,velocity,75),new SkiContactMotion(velocity,yaw),0,0,.05));
            var candidate=new[]{new SkiWorldMotionCandidate(id,raw)};
            var windows=new[]{new ManualScopedWorldWindow(id,occurrence,at.PhysicalArcNanometers,1,row.LengthNanometers)};
            Assert.That(world.TryPrepare(candidate,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out var proof),Is.True,world.LastAttempt.Status.ToString());
            var observed=world.GetProposedActor(proof,0);var motion=world.GetProposedPhysicalMotion(proof,0);
            Assert.That(observed.Pose.X,Is.EqualTo(raw.Sample(1).X));Assert.That(observed.Pose.Z,Is.EqualTo(raw.Sample(1).Z));
            Assert.That(observed.Pose.HeadingNanoradians,Is.EqualTo(raw.Sample(1).HeadingNanoradians));
            Assert.That(motion.DistanceNanometers,Is.EqualTo((long)Math.Round(raw.PhysicalTravelDistanceAt(1,0)*SkiState.NanometersPerMeter)));
            Assert.That(world.GetProposedMotionPiece(proof,0,0).EndSeconds,Is.EqualTo(.05));
            Assert.That(observed.Velocity.X,Is.EqualTo(normalize?0:velocity.X));
            Assert.That(observed.Velocity.Z,Is.EqualTo(normalize?0:velocity.Z));
            Assert.That(observed.Pose.YawRateRadiansPerSecond,Is.EqualTo(normalize?0:yaw));
            Assert.That(motion.SpeedMetersPerSecond,Is.EqualTo(normalize?0:speed).Within(1e-15));
            Assert.That(world.NumericalRestCanonicalizations,Is.EqualTo(normalize?1:0));
            Assert.That(world.MaximumCanonicalizedRestSpeed,Is.EqualTo(normalize?speed:0).Within(1e-15));
            Assert.That(world.MaximumCanonicalizedRestYaw,Is.Zero);
            world.DiscardPrepared(proof);
            Assert.That(world.TryPrepare(candidate,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{0d},out _),Is.True);
            Assert.That(world.NumericalRestCanonicalizations,Is.EqualTo(normalize?1:0),"Diagnostics reset rather than accumulating rejected proposals.");
        }

        [TestCase(-.002,ManualRouteTubeStatus.Clear)]
        [TestCase(-.05,ManualRouteTubeStatus.UnresolvedStart)]
        public void CourseAllowanceOnlyUsesASmallPartOfTheExistingSafetyMargin(double shift,ManualRouteTubeStatus expected)
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);var catalog=f.Catalog;
            var pose=new ManualSkiHeadingState(146.058043433,-11.144413020+shift,1.437653133);
            int index=catalog.GetBaseFlowSuccessor(catalog.StartApproachIndex);
            Assert.That(catalog.TryProject(pose,index,7558043433L,2,out var at),Is.True);
            var body=new ManualSkiContactBody(new AthleteId("margin_probe"),pose,default,75);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.Venue.Occupancy,20).PreviewTrajectory(
                body,new SkiContactMotion(default,0),0,0,.05));
            var sweep=new ManualRouteRibbonSweep(catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work(),Policy());
            Assert.That(sweep.Preview(raw,catalog.GetGeometryObservation(at)).Status,Is.EqualTo(expected));
        }
    }
}
