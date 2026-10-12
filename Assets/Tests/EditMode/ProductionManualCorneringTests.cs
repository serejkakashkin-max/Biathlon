using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ProductionManualCorneringTests
    {
        private static string Read(string id) => PhysiologyContractTests.DefinitionJson(id + ".json");

        private static int FindOccurrence(ManualRacePathCatalogTests.Fixture fixture, string loopId,
            double fromMeters, double untilMeters)
        {
            for (int i = 0; i < fixture.Catalog.BaseFlowCount; i++)
            {
                var row = fixture.Catalog.Get(i);
                if (row.LoopId == loopId && row.LocalStartNanometers <= fromMeters * 1e9 &&
                    row.LocalEndNanometers >= untilMeters * 1e9) return i;
            }
            Assert.Fail("Authored production bend is missing: " + loopId);
            return -1;
        }

        [TestCase(40, 8.5)] [TestCase(70, 9.0)] [TestCase(40, 10.0)] [TestCase(70, 10.5)]
        public void HeldManualKeyHasEnoughActualTravelHeadingAuthorityAtRaceSpeed(int skill, double initialSpeed)
        {
            var fixture = new ManualRacePathCatalogTests.Fixture("sprint");
            int occurrence = FindOccurrence(fixture, "main", 62.559, 94);
            var row = fixture.Catalog.Get(occurrence);
            long arc = (long)(62.559e9) - row.LocalStartNanometers;
            var pose = fixture.At(occurrence, arc);
            var velocity = new SkiPlanarVelocity(initialSpeed * Math.Sin(pose.HeadingRadians), initialSpeed * Math.Cos(pose.HeadingRadians));
            var physics = SkiPhysicsDefinition.FromJson(Read("ski_physics_provisional_v1"));
            var drive = new ManualSkiControlledDriveSystem(physics, fixture.Venue.Occupancy,
                ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2")), CorneringDefinition.FromJson(Read("cornering_gameplay_v2")), 20);
            double largestLateral = 0, lastGrip = 0, firstHeading = Math.Atan2(velocity.X, velocity.Z), velocityTurn = 0, odometer = 0;
            for (int tick = 0; tick < 60; tick++)
            {
                Assert.That(fixture.Catalog.TryProject(pose, occurrence, arc, 12, out var observation), Is.True);
                arc = observation.PhysicalArcNanometers;
                largestLateral = Math.Max(largestLateral, Math.Abs(observation.LateralMeters));
                var body = new ManualSkiContactBody(new AthleteId("manual_corner_witness"), pose, velocity, physics.MassKilograms);
                var frame = drive.Preview(body, new SkiContactMotion(velocity, pose.YawRateRadiansPerSecond), observation,
                    fixture.Catalog.SampleSurface(observation), new ManualSkiMotionInput(1000, 300, skill, skill, muscularFatigue: .05), .05);
                lastGrip = frame.Cornering.LateralAccelerationLimit;
                var priorPose = pose;
                pose = frame.Drive.State;
                velocity = frame.Drive.MotionAt(1).Velocity;
                velocityTurn = Wrap(Math.Atan2(velocity.X, velocity.Z) - firstHeading);
                odometer += Math.Sqrt((pose.X - priorPose.X) * (pose.X - priorPose.X) +
                    (pose.Z - priorPose.Z) * (pose.Z - priorPose.Z));
            }
            TestContext.WriteLine($"skill={skill}; start_mps={initialSpeed}; grip={lastGrip:F3}; velocity_turn_rad={velocityTurn:F3}; max_lateral_m={largestLateral:F3}; actual_travel_m={odometer:F3}");
            // The authored opening bend has a radius of about 25 m; this 25 m interval
            // needs at least 0.8 rad of travel-heading correction.
            Assert.That(velocityTurn, Is.GreaterThan(.8), "Full A/D must change actual travel direction, not merely turn the visible ski heading while sliding outwards.");
            // Holding a hard turn forever is intentionally allowed to cross the inside edge. Corridor
            // acceptance belongs to the deliberate-key scenario below; this probe measures actuator capacity.
            Assert.That(double.IsFinite(largestLateral), Is.True);
        }
        private static double Wrap(double value) => value - 2 * Math.PI * Math.Floor((value + Math.PI) / (2 * Math.PI));
        [TestCase("main", 62.559, 92.0, 40, 8.5, 8.0)]
        [TestCase("main", 62.559, 92.0, 40, 10.0, 8.0)]
        [TestCase("finish_spur", 0.5, 28.0, 40, 6.5, 5.0)]
        public void DeliberateManualKeysCanKeepNoviceOnProductionBends(string loopId, double from,
            double until, int skill, double speed, double lookahead)
        {
            var f = new ManualRacePathCatalogTests.Fixture("sprint");
            int occurrence = FindOccurrence(f, loopId, from, until);
            var row = f.Catalog.Get(occurrence);
            long arc = (long)(from * 1e9) - row.LocalStartNanometers;
            var pose = f.At(occurrence, arc);
            var velocity = new SkiPlanarVelocity(speed * Math.Sin(pose.HeadingRadians),
                speed * Math.Cos(pose.HeadingRadians));
            var physics = SkiPhysicsDefinition.FromJson(Read("ski_physics_provisional_v1"));
            var steering = ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2"));
            var system = new ManualSkiControlledDriveSystem(physics, f.Venue.Occupancy, steering,
                CorneringDefinition.FromJson(Read("cornering_gameplay_v2")), 20);
            var occupancy = f.Venue.Occupancy;
            double bodyRadius = .5 * Math.Sqrt(occupancy.LongitudinalSeparation * occupancy.LongitudinalSeparation +
                occupancy.LateralSeparation * occupancy.LateralSeparation) + occupancy.EdgeClearance;
            double maxLateral = 0, lastGrip = 0, local = from, smallestMargin = double.PositiveInfinity;
            int ticks = 0;
            // A deterministic stand-in for a player looking ahead and tapping A/D. This is test-only intent,
            // not a route-following component or a correction applied to accepted physics.
            for (; ticks < 300 && local < until; ticks++)
            {
                Assert.That(f.Catalog.TryProject(pose, occurrence, arc, 12, out var at), Is.True,
                    $"Projection lost loop={loopId},tick={ticks},local={local},maxLateral={maxLateral},pose={pose.X}:{pose.Z},yaw={pose.HeadingRadians}");
                arc = at.PhysicalArcNanometers;
                local = at.LocalMeters;
                var surface = f.Catalog.SampleSurface(at);
                maxLateral = Math.Max(maxLateral, Math.Abs(at.LateralMeters));
                smallestMargin = Math.Min(smallestMargin,
                    surface.WidthMeters * .5 - Math.Abs(at.LateralMeters) - bodyRadius);
                var target = f.Venue.Visual.Get(loopId).Sample(Math.Min(row.LocalEndNanometers / 1e9, local + lookahead));
                double desired = Math.Atan2(target.X - pose.X, target.Z - pose.Z);
                double error = Wrap(desired - pose.HeadingRadians);
                int key = Math.Abs(error) < .035 ? 0 : Math.Sign(error) * 1000;
                var body = new ManualSkiContactBody(new AthleteId("manual_keys_witness"), pose, velocity, physics.MassKilograms);
                var frame = system.Preview(body, new SkiContactMotion(velocity, pose.YawRateRadiansPerSecond), at, surface,
                    new ManualSkiMotionInput(key, 250, skill, skill, muscularFatigue: .05), .05);
                pose = frame.Drive.State;
                velocity = frame.Drive.MotionAt(1).Velocity;
                lastGrip = frame.Cornering.LateralAccelerationLimit;
            }
            TestContext.WriteLine($"manual_keys loop={loopId}; novice={skill}; start_speed={speed}; grip={lastGrip:F3}; max_lateral={maxLateral:F3}; footprint_margin={smallestMargin:F3}; local={local:F3}; ticks={ticks}");
            Assert.That(smallestMargin, Is.GreaterThan(0),
                "The complete footprint must fit the authored width under deliberate manual steering.");
            Assert.That(local, Is.GreaterThanOrEqualTo(until));
        }
        [Test]
        public void SharedGameplayGripRemainsPassiveBoundedAndSensitiveToAthleteSkill()
        {
            var f = new ManualRacePathCatalogTests.Fixture("sprint");
            var bodyDefinition = f.Venue.Occupancy;
            var physics = SkiPhysicsDefinition.FromJson(Read("ski_physics_provisional_v1"));
            var turns = new CorneringSystem(CorneringDefinition.FromJson(Read("cornering_gameplay_v2")), physics);
            var snow = new ManualSkiSteeredSnowSystem(bodyDefinition,
                ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2")), 20);
            double inertia = (bodyDefinition.LongitudinalSeparation * bodyDefinition.LongitudinalSeparation +
                bodyDefinition.LateralSeparation * bodyDefinition.LateralSeparation) / 12;
            Assert.That(f.Catalog.TryProject(f.At(0, 10_000_000_000), 0, 10_000_000_000, 0, out var at), Is.True);
            var surface = f.Catalog.SampleSurface(at).WithTravelGeometry(0, 0);
            double totalDissipated = 0;
            foreach (int skill in new[] { 0, 40, 100 })
            foreach (double speed in new[] { 0d, 2, 6.5, 10, 18 })
            foreach (double side in new[] { -1d, 0, 1 })
            foreach (int key in new[] { -1000, 0, 1000 })
            {
                var pose = new ManualSkiHeadingState(0, 0, .37, .4);
                var velocity = new SkiPlanarVelocity(speed * Math.Sin(.37) + side * Math.Cos(.37),
                    speed * Math.Cos(.37) - side * Math.Sin(.37));
                var body = new ManualSkiContactBody(new AthleteId("shared_grip_cost"), pose, velocity, physics.MassKilograms);
                var motion = new SkiContactMotion(velocity, .4);
                double grip = turns.EvaluateWorldHeading(new SkiState(0, Math.Sqrt(speed * speed + side * side)),
                    surface, .4, skill, skill, 1, .05).LateralAccelerationLimit;
                var result = snow.Preview(body, motion, key, grip, .05).Snow;
                double before = .5 * physics.MassKilograms *
                    (velocity.X * velocity.X + velocity.Z * velocity.Z + inertia * .4 * .4);
                var after = result.Motion;
                double energy = .5 * physics.MassKilograms * (after.Velocity.X * after.Velocity.X +
                    after.Velocity.Z * after.Velocity.Z + inertia * after.YawRate * after.YawRate);
                Assert.That(energy, Is.LessThanOrEqualTo(before + 1e-8));
                Assert.That(before - energy, Is.EqualTo(result.DissipatedEnergyJoules).Within(1e-8));
                double cap = physics.MassKilograms * grip * .05 * .5;
                Assert.That(Math.Abs(result.FrontImpulseNewtonSeconds), Is.LessThanOrEqualTo(cap + 1e-12));
                Assert.That(Math.Abs(result.RearImpulseNewtonSeconds), Is.LessThanOrEqualTo(cap + 1e-12));
                totalDissipated += result.DissipatedEnergyJoules;
            }
            Assert.That(totalDissipated, Is.GreaterThan(0), "Turning/sliding must still pay the shared physical loss.");
            var novice = turns.EvaluateWorldHeading(new SkiState(0, 8), surface, .5, 0, 0, 1, 0);
            var expert = turns.EvaluateWorldHeading(new SkiState(0, 8), surface, .5, 100, 100, 1, 0);
            Assert.That(expert.LateralAccelerationLimit, Is.GreaterThan(novice.LateralAccelerationLimit));
            var stopped = new ManualSkiContactBody(new AthleteId("no_free_pivot"),
                new ManualSkiHeadingState(0, 0, 0), default, physics.MassKilograms);
            foreach (int key in new[] { -1000, 0, 1000 })
                Assert.That(snow.Preview(stopped, default, key, expert.LateralAccelerationLimit, .05).Snow.Motion,
                    Is.EqualTo(default(SkiContactMotion)), "Grip cannot create free turning energy at rest.");
        }
        [Test]
        public void ExtraGripCannotTurnNeutralInputOrChangeItsLongitudinalForceLaw()
        {
            var f = new ManualRacePathCatalogTests.Fixture("sprint");
            var physics = SkiPhysicsDefinition.FromJson(Read("ski_physics_provisional_v1"));
            var steering = ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2"));
            string json = Read("cornering_gameplay_v2");
            var current = new ManualSkiControlledDriveSystem(physics, f.Venue.Occupancy, steering,
                CorneringDefinition.FromJson(json), 20);
            var former = new ManualSkiControlledDriveSystem(physics, f.Venue.Occupancy, steering,
                CorneringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(
                    PhysiologyContractTests.ReplaceNumber(json, "novice_lateral_acceleration_mps2", "1.5"),
                    "expert_lateral_acceleration_mps2", "4")), 20);
            var pose = f.At(0, 10_000_000_000, 1.5);
            var velocity = new SkiPlanarVelocity(6 * Math.Sin(pose.HeadingRadians), 6 * Math.Cos(pose.HeadingRadians));
            Assert.That(f.Catalog.TryProject(pose, 0, 10_000_000_000, 0, out var at), Is.True);
            var body = new ManualSkiContactBody(new AthleteId("neutral_grip"), pose, velocity, physics.MassKilograms);
            var input = new ManualSkiMotionInput(0, 350, 40, 40);
            var motion = new SkiContactMotion(velocity, 0);
            var surface = f.Catalog.SampleSurface(at);
            var a = current.Preview(body, motion, at, surface, input, .05);
            var b = former.Preview(body, motion, at, surface, input, .05);
            Assert.That(a.Drive.State, Is.EqualTo(b.Drive.State));
            Assert.That(a.Drive.MotionAt(1), Is.EqualTo(b.Drive.MotionAt(1)));
            Assert.That(a.Drive.FirstDrive.Output.PropulsiveNewtons, Is.EqualTo(b.Drive.FirstDrive.Output.PropulsiveNewtons));
            Assert.That(a.Drive.State.HeadingRadians, Is.EqualTo(pose.HeadingRadians),
                "Extra snow support has no course-tangent or centre-following input.");
            Assert.That(a.Cornering.BrakingNewtons, Is.Zero, "Production A/D still has no automatic longitudinal brake.");
        }
    }
}
