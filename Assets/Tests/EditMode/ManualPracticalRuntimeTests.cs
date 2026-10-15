using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class ManualPracticalRuntimeTests
    {
        [Test] public void NearestHairpinEndpointDoesNotGrantPhysicalFiringArrival()
        {
            var f=ManualProductionFieldTests.Initial("sprint",1);var c=f.stepper.World.GetPathChoice(29);
            int firing=c.GetFiringOccurrence(0);var row=c.Get(firing);
            var pose=new ManualSkiHeadingState(-42.395011714,-1.731579243,5.6185356);
            Assert.That(c.TryProject(pose,firing,0,5,out var at),Is.True);
            // The folded curve keeps its guarded projection before the mat.
            // Phase admission also requires a separate physical plane proof.
            Assert.That(at.PhysicalArcNanometers,Is.LessThan(row.LengthNanometers));
            Assert.That(c.TrySamplePortal(firing,row.LengthNanometers,out var goal),Is.True);
            var definition=ManualMatCaptureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_mat_capture_v1.json"));
            double before=(pose.X-goal.X)*goal.ForwardX+(pose.Z-goal.Z)*goal.ForwardZ;
            Assert.That(before,Is.LessThan(-definition.PositionTolerance));
            var scheduler=new ManualRacePathBoundaryScheduler(c,ManualScopedFieldStepperTests.Work());
            var id=f.runner.Field.GetAthlete(0).AthleteId;
            var window=new ManualScopedWorldWindow(id,firing,row.LengthNanometers,5,row.LengthNanometers);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(id,pose,default,75),new SkiContactMotion(default,0),0,0,.05));
            Assert.That(scheduler.Preview(raw,window).Status,Is.Not.EqualTo(ManualRacePathBoundaryStatus.Boundary),"Nearest-point projection alone cannot claim the mat.");
            var reached=new ManualSkiHeadingState(goal.X,goal.Z,Math.Atan2(goal.ForwardX,goal.ForwardZ));
            Assert.That(c.TryProject(reached,firing,at.PhysicalArcNanometers,5,out var arrival),Is.True);
            Assert.That(arrival.PhysicalArcNanometers,Is.EqualTo(row.LengthNanometers));
            var planted=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(id,reached,default,75),new SkiContactMotion(default,0),0,0,.05));
            Assert.That(scheduler.Preview(planted,window).Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));
        }
        [Test] public void WidePrivateExitObservesBothSidesOfItsOrdinaryPortal()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var c=f.stepper.World.GetCatalog(23);
            var pose=new ManualSkiHeadingState(-22.033806371,-6.199567243,3.00393442);
            var row=c.Get(4);int next=c.GetBaseFlowSuccessor(4);
            c.TrySamplePortal(4,row.LengthNanometers,out var end);c.TrySamplePortal(next,0,out var start);
            TestContext.Progress.WriteLine("exit="+row.LoopId+" end=("+end.X+","+end.Z+") normal=("+end.ForwardX+","+end.ForwardZ+") next="+c.Get(next).LoopId+" start=("+start.X+","+start.Z+") normal=("+start.ForwardX+","+start.ForwardZ+")");
            Assert.That(c.TryProject(pose,4,row.LengthNanometers,0,out var at),Is.True);
            Assert.That(at.PhysicalArcNanometers,Is.EqualTo(row.LengthNanometers));
            Assert.That(c.TryProject(pose,next,0,0,out var successor),Is.True);
            Assert.That(successor.PhysicalArcNanometers,Is.Zero);
            Assert.That(c.TryProject(pose,next,0,.001,out var moving),Is.True);
            Assert.That(moving.PhysicalArcNanometers,Is.GreaterThan(0));
        }
        [Test] public void RelaxedMassCornerAllowsThePaidTrajectory()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var c=f.stepper.World.GetCatalog(0);
            var pose=new ManualSkiHeadingState(111.434157444,-21.915851728,2.385708864,.318030013);
            Assert.That(c.TryProject(pose,32,27163684162L,1,out var at),Is.True);
            TestContext.Progress.WriteLine("corner path="+c.Get(32).LoopId+" kind="+c.Get(32).Kind+" length="+c.Get(32).LengthNanometers);
            var velocity=new SkiPlanarVelocity(1.894313013,-2.75960547);
            var trajectory=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(f.runner.Field.GetAthlete(0).AthleteId,pose,velocity,75),new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),0,0,.05));
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            var result=sweep.Preview(trajectory,c.GetGeometryObservation(at));
            TestContext.Progress.WriteLine("corner sweep="+result.Status+" vertices="+sweep.LastVertexCount+" clearance="+sweep.LastStartClearance+" minimum="+sweep.LastMinimumSampledClearance);
            Assert.That(result.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void EncodedPursuitKnotKeepsItsConnectedSnowWindow()
        {
            var f=ManualReusableRangeTests.Initial("pursuit",1);var c=f.stepper.World.GetCatalog(0);
            var row=c.Get(2);var road=TrackVisualDefinition.FromJson(ManualScopedFieldStepperTests.Read(
                "Biathlon/Venues/woodland_basin_v1/visual"),f.runner.Field.Track).Get(row.LoopId);
            int knotIndex=-1;double nearest=double.PositiveInfinity;
            double middle=(row.LocalStartNanometers+row.LocalEndNanometers)/(2d*SkiState.NanometersPerMeter);
            for(int k=0;k<road.Count;k++){var point=road.Get(k);double n=point.Distance*SkiState.NanometersPerMeter;
                if(n<=row.LocalStartNanometers||n>=row.LocalEndNanometers||Math.Abs(point.Distance-middle)>=nearest)continue;
                nearest=Math.Abs(point.Distance-middle);knotIndex=k;}
            Assert.That(knotIndex,Is.GreaterThanOrEqualTo(0));var knot=road.Get(knotIndex);var tangent=road.Sample(knot.Distance);
            var pose=new ManualSkiHeadingState(knot.X,knot.Z,Math.Atan2(tangent.ForwardX,tangent.ForwardZ));
            long arc=(long)Math.Round(knot.Distance*SkiState.NanometersPerMeter)-row.LocalStartNanometers;
            Assert.That(c.TryProject(pose,2,arc,.001,out var at),Is.True);
            var body=f.stepper.BodyDefinition;var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(body,20).PreviewTrajectory(
                new ManualSkiContactBody(f.runner.Field.GetAthlete(0).AthleteId,pose,default,75),new SkiContactMotion(default,0),0,0,.05));
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,body,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            Assert.That(sweep.Preview(raw,c.GetGeometryObservation(at)).Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void PaidOrdinaryEntryMotionDoesNotStickAtZeroThenJumpOutsideItsPortalBand()
        {
            var f=ManualProductionFieldTests.Initial("sprint",1);var c=f.stepper.World.GetCatalog(0);
            // Reproduce the long main-to-common-range entry reported by the
            // full Sprint40 field, rather than unrelated private mat curves.
            int previous=-1;
            for(int i=0;i<c.BaseFlowCount;i++)
            {
                int successor=c.GetBaseFlowSuccessor(i);
                if(c.Get(i).Kind==ManualRacePathKind.BaseRoute&&c.Get(i).End==ManualRacePathEnd.Continue&&
                    successor>=0&&c.Get(successor).Kind==ManualRacePathKind.RangeApproach)
                {previous=i;break;}
            }
            Assert.That(previous,Is.GreaterThanOrEqualTo(0),"The regression must exercise the actual main-to-common-range seam, not the separate start spur.");
            {
                var row=c.Get(previous);
                int next=c.GetBaseFlowSuccessor(previous);
                Assert.That(row.Kind,Is.EqualTo(ManualRacePathKind.BaseRoute));
                Assert.That(c.Get(next).Kind,Is.EqualTo(ManualRacePathKind.RangeApproach));
                Assert.That(c.TrySamplePortal(next,0,out var entry),Is.True);
                var pose=new ManualSkiHeadingState(entry.X+.185*entry.ForwardX,entry.Z+.185*entry.ForwardZ,Math.Atan2(entry.ForwardX,entry.ForwardZ));
                Assert.That(c.TryProject(pose,next,0,0,out var atEntry),Is.True,row.Id);
                Assert.That(atEntry.PhysicalArcNanometers,Is.Zero,row.Id);
                Assert.That(c.TryProject(pose,next,0,.001,out var moving),Is.True,row.Id);
                Assert.That(moving.PhysicalArcNanometers,Is.GreaterThan(0),row.Id);
                var ahead=new ManualSkiHeadingState(entry.X+.201*entry.ForwardX,entry.Z+.201*entry.ForwardZ,pose.HeadingRadians);
                Assert.That(c.TryProject(ahead,next,moving.PhysicalArcNanometers,.05,out var outside),Is.True,row.Id);
                Assert.That(outside.PhysicalArcNanometers,Is.GreaterThan(moving.PhysicalArcNanometers),row.Id);
            }
        }
        [Test] public void TiedPursuitQualifiersStartOnDistinctLinesWithoutChangingTheirScheduledTicks()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var sprint=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+"sprint_profile"));
            var pursuit=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+"pursuit_profile"));
            var ids=new[]{new AthleteId("tied_0"),new AthleteId("tied_1"),new AthleteId("tied_2")};
            var book=new SprintResultBook(sprint,SprintResultsTests.Timing(),ids,20,new RaceEventLog());
            foreach(var id in ids)
            {
                var life=new CompetitionLifecycle(sprint,id,20,new RaceEventLog());
                life.Step(new SimulationTick(1),CompetitionAction.Start);
                life.Step(new SimulationTick(2),CompetitionAction.EnterRange);
                life.Step(new SimulationTick(3),CompetitionAction.CompleteBout,31,5);
                life.Step(new SimulationTick(4),CompetitionAction.ReachLapBoundary);
                life.Step(new SimulationTick(5),CompetitionAction.EnterRange);
                life.Step(new SimulationTick(6),CompetitionAction.CompleteBout,31,5);
                life.Step(new SimulationTick(7),CompetitionAction.ReachLapBoundary);
                for(long t=8;t<201;t++)life.Step(new SimulationTick(t));
                life.Step(new SimulationTick(201),CompetitionAction.Finish,finish:new FinishTime(201,0,20));book.Record(life);
            }
            var qualification=PursuitQualificationSnapshot.Create("tied_manual_start",book.Complete("tied_source"),pursuit);
            var f=ManualProductionFieldTests.Initial("pursuit",3,qualification);
            for(int i=0;i<3;i++)
            {
                Assert.That(f.runner.Field.GetAthlete(i).ScheduledStart.Value,Is.EqualTo(1));
                for(int j=0;j<i;j++)Assert.That(f.runner.Field.GetLateral(i),Is.Not.EqualTo(f.runner.Field.GetLateral(j)));
            }
            for(int t=0;t<80;t++)f.runner.Step();
            for(int i=0;i<3;i++)Assert.That(f.runner.Field.GetAthlete(i).Motion.DistanceNanometers,Is.GreaterThan(SkiState.NanometersPerMeter));
            Assert.That(f.stepper.World.Tick.Value,Is.EqualTo(80));
        }
        [Test] public void EveryReusableMatKeepsTheSelectedFootPlacementDepartureBand()
        {
            var f=ManualProductionFieldTests.Initial("sprint",1);
            for(int p=0;p<f.stepper.World.PathChoiceCount;p++)
            {
                var c=f.stepper.World.GetPathChoice(p);int firing=c.GetFiringOccurrence(0);var row=c.Get(firing);
                Assert.That(c.TrySamplePortal(firing,row.LengthNanometers,out var goal),Is.True);
                var pose=new ManualSkiHeadingState(goal.X+.15*goal.ForwardX,goal.Z+.15*goal.ForwardZ,Math.Atan2(goal.ForwardX,goal.ForwardZ));
                Assert.That(c.TryProject(pose,c.GetBaseFlowSuccessor(firing),0,0,out var at),Is.True,"mat="+p);
                Assert.That(at.PhysicalArcNanometers,Is.Zero,"Departure geometry retains the planted band without granting a phase: mat="+p);
            }
        }
        [Test] public void APlantedOffCentreDepartureImmediatelyObservesPaidForwardMotion()
        {
            var f=ManualProductionFieldTests.Initial("sprint",1);
            Assert.That(f.stepper.World.PathChoiceCount,Is.EqualTo(30));
            for(int p=0;p<f.stepper.World.PathChoiceCount;p++)
            {
                var c=f.stepper.World.GetPathChoice(p);int firing=c.GetFiringOccurrence(0);var row=c.Get(firing);
                Assert.That(c.TrySamplePortal(firing,row.LengthNanometers,out var goal),Is.True);
                var pose=new ManualSkiHeadingState(goal.X+.151*goal.ForwardX,goal.Z+.151*goal.ForwardZ,Math.Atan2(goal.ForwardX,goal.ForwardZ));
                Assert.That(c.TryProject(pose,c.GetBaseFlowSuccessor(firing),0,.001,out var at),Is.True,"mat="+p);
                Assert.That(at.PhysicalArcNanometers,Is.GreaterThan(0),"Moving bodies cannot stick at the entry band: mat="+p);
            }
        }
        [Test] public void FinalMainDepartureRetainsPracticalWholeSkiClearanceBeforeTheRangeHairpin()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var c=f.stepper.World.GetCatalog(0);
            var row=c.Get(25);
            Assert.That(row.LocalEndNanometers,Is.LessThan(f.runner.Field.Track.Range.Entry.Distance.Millimeters*
                SkiState.NanometersPerMillimeter-20*SkiState.NanometersPerMeter));
            Assert.That(c.TrySamplePortal(row.Index,row.LengthNanometers,out var fork),Is.True);
            var pose=new ManualSkiHeadingState(fork.X,fork.Z,Math.Atan2(fork.ForwardX,fork.ForwardZ));
            Assert.That(c.TryProject(pose,row.Index,row.LengthNanometers,1,out var observation),Is.True);
            var velocity=new SkiPlanarVelocity(.5*fork.ForwardX,.5*fork.ForwardZ);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(f.runner.Field.GetAthlete(0).AthleteId,pose,velocity,75),new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),0,0,.05));
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            Assert.That(sweep.Preview(raw,c.GetGeometryObservation(observation)).Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void ThirtySkierStartBypassesAnUnmovingManuallyControlledPlayer()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var field=f.runner.Field;var id=field.GetAthlete(0).AthleteId;
            for(int i=0;i<500;i++)
            {
                var tick=field.Tick.Next();f.runner.StepWithManualPlayer(0,new SprintAthleteInput(
                    new TimeTrialInput(new SimulationInput(tick,id,0),1,1,0),new WeaponAimCommand(tick,id,0,0)),0);
            }
            for(int i=1;i<30;i++)Assert.That(field.GetAthlete(i).Motion.DistanceNanometers,
                Is.GreaterThan(20*SkiState.NanometersPerMeter),"AI must steer around the neutral player: "+i);
            Assert.That(field.Tick.Value,Is.EqualTo(500));
        }
        [Test] public void PlantedMatBodyReceivesGroundSupportAndTheApproachingAthleteReceivesTheImpact()
        {
            var body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            var contact=SkiContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_gameplay_v1.json"));
            var moving=new ManualSkiContactBody(new AthleteId("approaching"),new ManualSkiHeadingState(0,0,0),new SkiPlanarVelocity(0,3),75);
            var planted=new ManualSkiContactBody(new AthleteId("planted"),new ManualSkiHeadingState(0,body.LongitudinalSeparation,0),default,75,true);
            var result=new ManualSkiContactSystem(body,contact).PreviewAngularContact(moving,planted);
            Assert.That(result.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(result.Contact.SecondVelocity,Is.EqualTo(default(SkiPlanarVelocity)));Assert.That(result.SecondYawRate,Is.Zero);
            Assert.That(result.Contact.FirstVelocity.Z,Is.Zero.Within(1e-8));
            Assert.That(result.Contact.NormalImpulseNewtonSeconds,Is.EqualTo(225).Within(1e-8));
            Assert.That(planted.Pose.Z,Is.EqualTo(body.LongitudinalSeparation));
        }
        [Test] public void CapturedPrivateCurveKeepsTheConnectedWideArrivalSnow()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var catalog=f.stepper.World.GetCatalog(20);
            var pose=new ManualSkiHeadingState(-15.603452287,-.219344066,5.356863169);
            Assert.That(catalog.TryProject(pose,3,2176825583L,1,out var at),Is.True);
            var trajectory=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(f.runner.Field.GetAthlete(0).AthleteId,pose,default,75),new SkiContactMotion(default,0),0,0,.05));
            var sweep=new ManualRouteRibbonSweep(catalog.ProjectionGeometry,f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            Assert.That(sweep.Preview(trajectory,catalog.GetGeometryObservation(at)).Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void PenaltyEntryKeepsTheWideIncomingSnowUnderTheRearSkis()
        {
            var f=ManualProductionFieldTests.Initial("sprint",1);var c=f.stepper.World.GetPathChoice(24);
            int row=c.GetPenaltyOccurrence(0,1);
            var pose=new ManualSkiHeadingState(88.5,-10.291651563,1.880578773);
            Assert.That(c.TryProject(pose,row,0,0,out var at),Is.True);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(
                new ManualSkiContactBody(f.runner.Field.GetAthlete(0).AthleteId,pose,new SkiPlanarVelocity(3.103293372,-.112568097),75),
                new SkiContactMotion(new SkiPlanarVelocity(3.103293372,-.112568097),0),0,0,.01));
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            Assert.That(sweep.Preview(raw,c.GetGeometryObservation(at)).Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(c.Get(row).Kind,Is.EqualTo(ManualRacePathKind.PenaltyLoop));
        }
        [Test] public void OffCentreMatArrivalFitsTheExplicitFootPlacementArea()
        {
            var f=new ManualRacePathCatalogTests.Fixture("sprint",1);var c=f.Catalog;
            int row=c.GetFiringOccurrence(1);var pose=new ManualSkiHeadingState(40.442955222,-1.040496215,.459420096,0);
            Assert.That(c.TrySample(row,c.Get(row).LengthNanometers,out var end),Is.True);
            var definition=ManualMatCaptureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_mat_capture_v1.json"));
            Assert.That(Math.Sqrt(Math.Pow(pose.X-end.X,2)+Math.Pow(pose.Z-end.Z,2)),Is.LessThan(definition.PositionTolerance));
            Assert.That(definition.PositionTolerance,Is.LessThanOrEqualTo(.8));
            Assert.That(c.TryProject(pose,row,c.Get(row).LengthNanometers,.5,out var before),Is.True);
            var id=new AthleteId("off_centre_mat_plant");
            var cursor=new ManualRacePathProgression(c).Reconstitute(id,row,before.PhysicalArcNanometers,before.PhysicalArcNanometers,before.MappedRouteNanometers);
            var world=new ManualScopedWorldRoster(new[]{c},f.Venue.Occupancy,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),
                new[]{cursor},new[]{pose},new[]{default(SkiPlanarVelocity)},new[]{new SkiState(0,0)},20,
                responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{75d},matCapture:definition,
                numericalTolerance:ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            // Captured real endpoint after foot planting: departure geometry
            // must accept this unchanged resting body. A moving trajectory
            // beyond an already closed firing phase is intentionally forbidden.
            var actual=world.GetActor(0);
            Assert.That(actual.Pose,Is.EqualTo(pose));
            Assert.That(actual.Velocity,Is.EqualTo(default(SkiPlanarVelocity)));
            Assert.That(c.TryProject(actual.Pose,c.GetBaseFlowSuccessor(row),0,0,out var departure),Is.True);
            Assert.That(departure.PhysicalArcNanometers,Is.Zero,"The selected mat's exit must be ready without a paid waiting tick.");
        }
        [TestCase(false,.5,.4,false,0)][TestCase(true,.5,.4,true,0)][TestCase(true,2.3,0,false,0)][TestCase(true,.5,.4,true,.7)]
        public void MatFootPlacementIsExplicitBoundedAndRetainsTheReachedPoseAndPaidTravel(bool selected,double speed,double yaw,bool captured,double lateral)
        {
            var f=new ManualRacePathCatalogTests.Fixture("sprint");var c=f.Catalog;
            if(lateral!=0)c=ManualProductionFieldTests.Initial("sprint",1).stepper.World.GetCatalog(0);
            // Production includes the shared approach and separate start spur;
            // resolve the actual selected catalog after replacing the fixture.
            int occurrence=c.GetFiringOccurrence(0);var row=c.Get(occurrence);
            Assert.That(row.End,Is.EqualTo(ManualRacePathEnd.FiringLine));
            Assert.That(c.TrySamplePortal(occurrence,row.LengthNanometers,out var end),Is.True);
            var velocity=new SkiPlanarVelocity(speed*end.ForwardX,speed*end.ForwardZ);
            var pose=new ManualSkiHeadingState(end.X-velocity.X*.05+end.ForwardZ*lateral,end.Z-velocity.Z*.05-end.ForwardX*lateral,Math.Atan2(end.ForwardX,end.ForwardZ)-yaw*.05,yaw);
            Assert.That(c.TryProject(pose,occurrence,row.LengthNanometers,1,out var at),Is.True);
            var id=new AthleteId("explicit_mat_plant");var cursor=new ManualRacePathProgression(c).Reconstitute(id,occurrence,at.PhysicalArcNanometers,at.PhysicalArcNanometers,at.MappedRouteNanometers);
            var world=new ManualScopedWorldRoster(new[]{c},f.Venue.Occupancy,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),
                new[]{cursor},new[]{pose},new[]{velocity},new[]{new SkiState(0,speed)},20,responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{75d},
                matCapture:selected?ManualMatCaptureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_mat_capture_v1.json")):null,
                numericalTolerance:lateral!=0?ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")):null);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.Venue.Occupancy,20).PreviewTrajectory(
                new ManualSkiContactBody(id,pose,velocity,75),new SkiContactMotion(velocity,yaw),0,0,.05));
            Assert.That(world.TryPrepare(new[]{new SkiWorldMotionCandidate(id,raw)},new[]{new ManualScopedWorldWindow(id,occurrence,at.PhysicalArcNanometers,1,row.LengthNanometers)},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{0d},out var proof),Is.True);
            var actual=world.GetProposedActor(proof,0);
            Assert.That(actual.Pose.X,Is.EqualTo(raw.Sample(1).X));Assert.That(actual.Pose.Z,Is.EqualTo(raw.Sample(1).Z));
            Assert.That(actual.Pose.HeadingNanoradians,Is.EqualTo(raw.Sample(1).HeadingNanoradians));
            Assert.That(actual.Velocity.X,Is.EqualTo(captured?0:velocity.X));Assert.That(actual.Velocity.Z,Is.EqualTo(captured?0:velocity.Z));
            Assert.That(actual.Pose.YawRateRadiansPerSecond,Is.EqualTo(captured?0:yaw));
            Assert.That(world.MatCaptures,Is.EqualTo(captured?1:0));Assert.That(world.NumericalRestCanonicalizations,Is.Zero);
            Assert.That(world.MatCaptureDissipatedJoules,Is.EqualTo(captured?.5*75*speed*speed+.5*75*(1.5*1.5+.7*.7)/12*yaw*yaw:0).Within(1e-6));
            Assert.That(world.GetProposedMotionPiece(proof,0,0).EndSeconds,Is.EqualTo(.05));
            Assert.That(world.GetProposedPhysicalMotion(proof,0).DistanceNanometers,Is.EqualTo((long)Math.Round(raw.PhysicalTravelDistanceAt(1,0)*SkiState.NanometersPerMeter)));
        }
        [Test] public void ManualPlayerCanTouchTheCourseEdgeAndContinueWithoutFreezingTheField()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var field=f.runner.Field;var id=field.GetAthlete(0).AthleteId;int contacts=0;
            for(int i=0;i<200;i++)
            {
                var tick=field.Tick.Next();try{f.runner.StepWithManualPlayer(0,new SprintAthleteInput(
                    new TimeTrialInput(new SimulationInput(tick,id,650),1,1,0),new WeaponAimCommand(tick,id,0,0)),i<90?0:500);}
                catch(Exception e){Assert.Fail("tick="+tick.Value+" "+ManualScopedFieldStepperTests.Explain(f.stepper)+" "+e);}
                contacts+=f.stepper.World.CourseEdgeResponses;
            }
            Assert.That(field.Tick.Value,Is.EqualTo(200));Assert.That(f.stepper.World.Tick,Is.EqualTo(field.Tick));
            Assert.That(contacts,Is.GreaterThan(0));
        }
        [TestCase(5,.5,.1)][TestCase(1e-11,-.32998538969884758,-1000000)][TestCase(0,.3,0)]
        public void NearRestSpinCannotOverflowThePostureTravelMetadata(double speed,double spin,double expected)
        {
            var f=new ManualRacePathCatalogTests.Fixture();var sample=new TrackSampler(f.Venue.Track.GetLoop("main")).Sample(new TrackDistance(0));
            var observed=sample.WithWorldTravelGeometry(.1,speed,spin);
            Assert.That(observed.CurvaturePerMeter,Is.EqualTo(expected).Within(1e-12));
            Assert.That(observed.Grade,Is.EqualTo(.1));Assert.That(observed.WidthMeters,Is.EqualTo(sample.WidthMeters));
            Assert.That(observed.SnowProfileId,Is.EqualTo(sample.SnowProfileId));
        }
    }
}
