using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class OwnerCourseClockRejectionTests
    {
        [TestCase(5, 3)]
        [TestCase(0, 0)]
        public void RoundedBendKeepsAnAdmittedSkinWithoutExhaustingTheTick(int normalColumns, int normalRows)
        {
            var source = ManualProductionFieldTests.Initial("mass_start", 1);
            var catalog = source.stepper.World.GetCatalog(0);
            int occurrence = -1;
            for (int i = 0; i < catalog.BaseFlowCount; i++)
            {
                var row = catalog.Get(i);
                if (row.LoopId == "main" && row.LocalStartNanometers <= 85.706e9 && row.LocalEndNanometers >= 85.706e9) { occurrence = i; break; }
            }
            var path = catalog.Get(occurrence);
            long arc = 85_706_000_000L - path.LocalStartNanometers;
            Assert.That(catalog.TrySample(occurrence, arc, out var center), Is.True);
            var initial = new ManualSkiHeadingState(215.131683788, -23.674832777, 2.685201267, .579);
            var velocity = new SkiPlanarVelocity(9.614 * center.ForwardX + center.ForwardZ, 9.614 * center.ForwardZ - center.ForwardX);
            var id = new AthleteId("owner_course_minimized");
            Assert.That(catalog.TryProject(initial, occurrence, arc, 3, out var at), Is.True);
            var cursor = new ManualRacePathProgression(catalog).Reconstitute(id, occurrence, at.PhysicalArcNanometers, at.PhysicalArcNanometers, at.MappedRouteNanometers);
            var body = source.stepper.BodyDefinition; var physics = source.stepper.PhysicsDefinition;
            var work = SkiContactSweepDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_sweep_v1.json"));
            var json = PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.DefinitionJson("manual_course_contact_gameplay_v2.json"), "normal_sample_columns", normalColumns.ToString(System.Globalization.CultureInfo.InvariantCulture)), "normal_sample_rows", normalRows.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var world = new ManualScopedWorldRoster(new[] { catalog }, body,
                SkiContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_gameplay_v1.json")), work,
                new[] { cursor }, new[] { initial }, new[] { velocity }, new[] { new SkiState(0, PhysicalSpeed(velocity, at)) }, 20,
                responseDefinition: SkiContactGroupDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_group_gameplay_v1.json")), actorMasses: new[] { physics.MassKilograms },
                numericalTolerance: ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")),
                courseContact: ManualCourseContactDefinition.FromJson(json));
            var system = new ManualSkiControlledDriveSystem(physics, body, ManualScopedFieldStepperTests.Steering(), source.stepper.CorneringDefinition, 20);
            var respond = typeof(ManualScopedWorldRoster).GetMethod("TryRespondToCourseEdge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var tick = world.BeginTick();
            double maximumReactionEnergy = 0;
            while (world.GetRemainingSeconds(tick) > 0)
            {
                var actor = world.GetWorkingActor(tick, 0); var current = world.GetWorkingCursor(tick, 0); double seconds = world.GetRemainingSeconds(tick);
                var raw = system.Preview(new ManualSkiContactBody(id, actor.Pose, actor.Velocity, physics.MassKilograms), new SkiContactMotion(actor.Velocity, actor.Pose.YawRateRadiansPerSecond),
                    actor.Observation, catalog.SampleSurface(actor.Observation), new ManualSkiMotionInput(1000, 350, 70, 70), seconds).Drive;
                var candidates = new SkiWorldMotionCandidate[1];
                Assert.That(world.TryPreviewContactDrivenMotion(tick, new[] { raw }, candidates), Is.True);
                var unconstrained = raw.MotionAt(1); var supported = candidates[0].Trajectory.VelocityAt(1);
                maximumReactionEnergy = Math.Max(maximumReactionEnergy, .5 * physics.MassKilograms *
                    (supported.X * supported.X + supported.Z * supported.Z - unconstrained.Velocity.X * unconstrained.Velocity.X - unconstrained.Velocity.Z * unconstrained.Velocity.Z));
                var windows = new[] { new ManualScopedWorldWindow(id, occurrence, current.ObservedArcNanometers, physics.MaximumSpeedMetersPerSecond * seconds * actor.Observation.StationMetersPerPlanarMeter, path.LengthNanometers) };
                if (world.TryAppendToOwnedFirstEvent(tick, candidates, windows, Array.Empty<SkiActorCourseEdge>(), 0, new[] { actor.Observation.GradeAlong(actor.Pose) }, out _)) continue;
                TestContext.WriteLine(FormattableString.Invariant($"response={world.CourseEdgeResponses} before_v={actor.Velocity.X:R},{actor.Velocity.Z:R} before_spin={actor.Pose.YawRateRadiansPerSecond:R} source_gap={world.GetRibbonDiagnostics(0).LastStartClearance:R} threatened_gap={world.GetRibbonDiagnostics(0).LastMinimumSampledClearance:R} normal={world.LastCourseNormalX:R},{world.LastCourseNormalZ:R} lever={world.LastCourseAngularLever:R}"));
                Assert.That((bool)respond.Invoke(world, new object[] { tick }), Is.True, "Admitted snow support exhausted: " + world.LastAttempt.Status);
            }
            Assert.That(world.TryFinalizeTick(tick, out var proof), Is.True); world.Commit(proof);
            Assert.That(world.Tick.Value, Is.EqualTo(1));
            Assert.That(world.CurvedCourseContinuations, Is.GreaterThan(0), "The reduced rejection must exercise the new curvature continuation, not only historical normal support.");
            TestContext.WriteLine("maximum_reaction_energy_j=" + maximumReactionEnergy);
            Assert.That(maximumReactionEnergy, Is.LessThanOrEqualTo(1e-8), "A course support reaction must not supply propulsion energy beyond the actual paid drive.");
        }
        [Test]
        public void PaidSnowSupportAtAuthoredBendsDoesNotExhaustItsResponses()
        {
            var source = ManualProductionFieldTests.Initial("mass_start", 1);
            var catalog = source.stepper.World.GetCatalog(0);
            var occupancy = source.stepper.BodyDefinition;
            var work = SkiContactSweepDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_sweep_v1.json"));
            var tolerance = ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json"));
            var contact = SkiContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_gameplay_v1.json"));
            var group = SkiContactGroupDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_group_gameplay_v1.json"));
            var edge = ManualCourseContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_course_contact_gameplay_v2.json"));
            var physics = source.stepper.PhysicsDefinition;
            var drive = new ManualSkiControlledDriveSystem(physics, occupancy, ManualScopedFieldStepperTests.Steering(), source.stepper.CorneringDefinition, 20);
            var snow = new ManualSkiSnowContactSystem(occupancy, 20);
            var respond = typeof(ManualScopedWorldRoster).GetMethod("TryRespondToCourseEdge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var id = new AthleteId("owner_course_edge_witness");
            var random = new SeededRandomSource(5156);
            int responses = 0, curvedContinuations = 0;
            var hashInput = new System.Text.StringBuilder();
            for (int sample = 0; sample < 400; sample++)
            {
                double local = 10 + random.NextUInt64() % 720000 / 1000.0;
                int occurrence = -1;
                for (int i = 0; i < catalog.BaseFlowCount; i++)
                {
                    var row = catalog.Get(i);
                    if (row.LoopId == "main" && row.LocalStartNanometers <= local * 1e9 && row.LocalEndNanometers >= local * 1e9) { occurrence = i; break; }
                }
                Assert.That(occurrence, Is.GreaterThanOrEqualTo(0));
                var path = catalog.Get(occurrence);
                long arc = (long)(local * 1e9) - path.LocalStartNanometers;
                Assert.That(catalog.TrySample(occurrence, arc, out var center), Is.True);
                int sign = sample % 2 == 0 ? 1 : -1;
                double angle = (random.NextUInt64() % 1201 - 600.0) / 1000;
                double heading = Math.Atan2(center.ForwardX, center.ForwardZ) + angle;
                var ribbon = new ManualRouteRibbonSweep(catalog.ProjectionGeometry, occupancy, work, tolerance);
                ManualSkiHeadingState At(double lateral) => new ManualSkiHeadingState(center.X + center.ForwardZ * lateral * sign, center.Z - center.ForwardX * lateral * sign, heading);
                bool Fits(double lateral)
                {
                    var pose = At(lateral);
                    if (!catalog.TryProject(pose, occurrence, arc, 3, out var observation)) return false;
                    var still = new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(id, pose, default, physics.MassKilograms), default, 0, 0, .05));
                    return ribbon.Preview(still, catalog.GetGeometryObservation(observation)).Status == ManualRouteTubeStatus.Clear;
                }
                double low = 0, high = 10;
                Assert.That(Fits(low), Is.True);
                for (int k = 0; k < 16; k++) { double mid = (low + high) * .5; if (Fits(mid)) low = mid; else high = mid; }
                double side = low - .01;
                double spin = (random.NextUInt64() % 1201 - 600.0) / 1000;
                var initial = At(side);
                initial = new ManualSkiHeadingState(initial.X, initial.Z, initial.HeadingRadians, spin);
                double speed = 3 + random.NextUInt64() % 8000 / 1000.0;
                var velocity = new SkiPlanarVelocity(speed * center.ForwardX + sign * center.ForwardZ, speed * center.ForwardZ - sign * center.ForwardX);
                Assert.That(catalog.TryProject(initial, occurrence, arc, 3, out var at), Is.True);
                var cursor = new ManualRacePathProgression(catalog).Reconstitute(id, occurrence, at.PhysicalArcNanometers, at.PhysicalArcNanometers, at.MappedRouteNanometers);
                var world = new ManualScopedWorldRoster(new[] { catalog }, occupancy, contact, work, new[] { cursor }, new[] { initial }, new[] { velocity }, new[] { new SkiState(0, PhysicalSpeed(velocity, at)) }, 20,
                    responseDefinition: group, actorMasses: new[] { physics.MassKilograms }, numericalTolerance: tolerance, courseContact: edge);
                var tick = world.BeginTick(); bool accepted = false;
                while (world.GetRemainingSeconds(tick) > 0)
                {
                    var actor = world.GetWorkingActor(tick, 0); var current = world.GetWorkingCursor(tick, 0); double seconds = world.GetRemainingSeconds(tick);
                    var raw = drive.Preview(new ManualSkiContactBody(id, actor.Pose, actor.Velocity, physics.MassKilograms), new SkiContactMotion(actor.Velocity, actor.Pose.YawRateRadiansPerSecond),
                        actor.Observation, catalog.SampleSurface(actor.Observation), new ManualSkiMotionInput(sign * 1000, 350, 70, 70), seconds).Drive;
                    var candidates = new SkiWorldMotionCandidate[1];
                    int previousCurves = world.CurvedCourseContinuations;
                    Assert.That(world.TryPreviewContactDrivenMotion(tick, new[] { raw }, candidates), Is.True);
                    // Historical normal-only support is preserved. This guard
                    // specifically constrains the new curvature continuation.
                    for (int k = 1; world.CurvedCourseContinuations > previousCurves && k <= 4; k++)
                    {
                        var actual = candidates[0].Trajectory.VelocityAt(k / 4.0);
                        var paid = raw.MotionAt(candidates[0].Trajectory.DurationSeconds / raw.DurationSeconds * k / 4.0).Velocity;
                        double extra = .5 * physics.MassKilograms * (actual.X * actual.X + actual.Z * actual.Z - paid.X * paid.X - paid.Z * paid.Z);
                        Assert.That(extra, Is.LessThanOrEqualTo(1e-8), "sample=" + sample + " reaction supplies unpaid translation energy at fraction=" + k + "/4");
                    }
                    var windows = new[] { new ManualScopedWorldWindow(id, occurrence, current.ObservedArcNanometers, physics.MaximumSpeedMetersPerSecond * seconds * actor.Observation.StationMetersPerPlanarMeter, path.LengthNanometers) };
                    if (world.TryAppendToOwnedFirstEvent(tick, candidates, windows, Array.Empty<SkiActorCourseEdge>(), 0, new[] { actor.Observation.GradeAlong(actor.Pose) }, out _)) { accepted = true; continue; }
                    if ((bool)respond.Invoke(world, new object[] { tick })) { responses++; continue; }
                    Assert.Fail(FormattableString.Invariant($"sample={sample} local={local:R} side={side:R} heading_offset={angle:R} speed={speed:R} spin={spin:R} status={world.LastAttempt.Status} responses={world.CourseEdgeResponses} start_clearance={world.GetRibbonDiagnostics(0).LastStartClearance:R} minimum={world.GetRibbonDiagnostics(0).LastMinimumSampledClearance:R} source={initial.X:R},{initial.Z:R},{initial.HeadingRadians:R}"));
                }
                Assert.That(accepted, Is.True); Assert.That(world.TryFinalizeTick(tick, out var proof), Is.True); world.Commit(proof);
                curvedContinuations += world.CurvedCourseContinuations;
                var final = world.GetActor(0);
                hashInput.Append(FormattableString.Invariant($"{sample}:{final.Pose.X:R}:{final.Pose.Z:R}:{final.Pose.HeadingRadians:R}:{final.Velocity.X:R}:{final.Velocity.Z:R}:{world.GetPhysicalMotion(0).DistanceNanometers};"));
            }
            Assert.That(responses, Is.GreaterThan(0));
            Assert.That(curvedContinuations, Is.GreaterThan(0));
            TestContext.WriteLine("course_edge_reaction_sha256=" + ManualContextIdentity.Sha256(hashInput.ToString()) + " responses=" + responses + " curved_continuations=" + curvedContinuations);
        }
        private static double PhysicalSpeed(SkiPlanarVelocity velocity, ManualRacePathObservation observation)
        {
            double planar = Math.Sqrt(velocity.X * velocity.X + velocity.Z * velocity.Z);
            double grade = planar == 0 ? 0 : observation.GeometricGrade *
                (velocity.X * observation.ForwardX + velocity.Z * observation.ForwardZ) / planar;
            return planar * Math.Sqrt(1 + grade * grade);
        }
        [TestCase(-1000, 80)]
        [TestCase(1000, 80)]
        [TestCase(-500, 160)]
        [TestCase(500, 160)]
        [TestCase(0, 0)]
        [TestCase(-1000, 700)]
        [TestCase(1000, 700)]
        [TestCase(-500, 700)]
        [TestCase(500, 700)]
        public void SustainedPlayerSteeringAtTheCourseEdgeKeepsTheSharedClock(int steering, int turnAt)
        {
            var f = ManualProductionFieldTests.Initial("mass_start", 1);
            for (int t = 0; t < 1800; t++)
            {
                var athlete = f.runner.Field.GetAthlete(0);
                var tick = athlete.Tick.Next();
                var input = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, athlete.AthleteId, 1000), 1, 1, 0),
                    new WeaponAimCommand(tick, athlete.AthleteId, 0, 0));
                try { f.runner.StepWithManualPlayer(0, input, t < turnAt ? 0 : steering); }
                catch (Exception error)
                {
                    Assert.Fail("turn_at=" + turnAt + " steering=" + steering + " tick=" + tick.Value + " " +
                        ManualScopedFieldStepperTests.Explain(f.stepper) + " " + error);
                }
                Assert.That(f.stepper.World.Tick, Is.EqualTo(f.runner.Field.Tick));
            }
            var actor = f.stepper.World.GetActor(0);
            TestContext.WriteLine("final_pose=" + actor.Pose.X + "," + actor.Pose.Z + "," + actor.Pose.HeadingRadians + " lateral=" + actor.Observation.LateralMeters);
        }
    }
}
