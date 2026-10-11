using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualRouteRibbonSweepTests
    {
        [Test] public void CapturedPenaltyEntryMatchesEveryFineGridLeafAcrossNormalProbes()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:22);
            var tolerance=ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json"));
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(ManualRacePathCatalog).GetMethod("BindPortalTolerance",flags).Invoke(f.Catalog,new object[]{
                tolerance.PortalPositionTolerance,tolerance.UseRoundSnowCorridor,tolerance.ConnectedSnowWindowMultiplier,tolerance.OrdinaryPortalRetreat,tolerance.OrdinaryHandoffWindow});
            var anchor=new ManualSkiHeadingState(100.548426466,-6.926235674,1.803488287);
            int occurrence=f.Catalog.GetPenaltyOccurrence(0,1);
            Assert.That(f.Catalog.TryProject(anchor,occurrence,0,150,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work(),tolerance);
            sweep.Preview(Travel(f.Venue.Occupancy,anchor),f.Catalog.GetGeometryObservation(at));
            T Field<T>(string name)=>(T)typeof(ManualRouteRibbonSweep).GetField(name,flags).GetValue(sweep);
            int columns=Field<int>("disks"),rows=Field<int>("refinedRows");
            double length=Field<double>("halfLength"),width=Field<double>("halfWidth"),radius=Field<double>("refinedRadius")+Field<double>("edgeClearance");
            var disk=typeof(ManualRouteRibbonSweep).GetMethod("DiskClearance",flags);
            var grid=typeof(ManualRouteRibbonSweep).GetMethod("RefinedGridMinimum",flags);
            var normal=typeof(ManualRouteRibbonSweep).GetMethod("CurrentNormalSupportClearance",flags);
            for(int p=0;p<24;p++)
            {
                var pose=new ManualSkiHeadingState(anchor.X+(p%3-1)*.002,anchor.Z+(p/3-3)*.002,anchor.HeadingRadians+(p%5-2)*.002);
                double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians),expected=double.PositiveInfinity;
                for(int k=0;k<columns;k++)for(int j=0;j<rows;j++)
                {
                    double along=length*(2.0*k+1-columns)/columns,side=width*(2.0*j+1-rows)/rows;
                    expected=Math.Min(expected,(double)disk.Invoke(sweep,new object[]{pose.X+fx*along+fz*side,pose.Z+fz*along-fx*side,radius}));
                }
                double actual=(double)grid.Invoke(sweep,new object[]{pose,fx,fz,0,columns-1,0,rows-1,double.PositiveInfinity,double.NegativeInfinity});
                Assert.That(actual,Is.EqualTo(expected).Within(1e-12),"The hot penalty-entry field must retain its exhaustive leaf minimum.");
                var clear=typeof(ManualRouteRibbonSweep).GetMethod("CurrentRibbonClearance",flags);
                double contact=(double)clear.Invoke(sweep,new object[]{pose});int queries=sweep.Diagnostics.LastDiskQueries;
                double repeated=(double)clear.Invoke(sweep,new object[]{new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,.5)});
                Assert.That(repeated,Is.EqualTo(contact));Assert.That(sweep.Diagnostics.LastDiskQueries,Is.EqualTo(queries),"The same pose after an impulse must not repeat its disk cover.");
                double Probe(double dx,double dz,bool bounded)
                {
                    var probe=new ManualSkiHeadingState(pose.X+dx,pose.Z+dz,pose.HeadingRadians);
                    return bounded?(double)normal.Invoke(sweep,new object[]{probe,5,3}):
                        (double)grid.Invoke(sweep,new object[]{probe,fx,fz,0,columns-1,0,rows-1,double.PositiveInfinity,double.NegativeInfinity});
                }
                double gx=Probe(.001,0,false)-Probe(-.001,0,false),gz=Probe(0,.001,false)-Probe(0,-.001,false);
                double nx=Probe(.001,0,true)-Probe(-.001,0,true),nz=Probe(0,.001,true)-Probe(0,-.001,true);
                double cosine=(gx*nx+gz*nz)/Math.Sqrt((gx*gx+gz*gz)*(nx*nx+nz*nz));
                Assert.That(cosine,Is.GreaterThan(.95),"Bounded directional probes must oppose the same captured penalty edge. Whole-body admission still uses the exhaustive cover.");
            }
        }
        [Test] public void CapturedPenaltyEdgeFindsTheExactFineGridMinimumWithBoundedQueries()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:18);
            var tolerance=ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json"));
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(ManualRacePathCatalog).GetMethod("BindPortalTolerance",flags).Invoke(f.Catalog,new object[]{
                tolerance.PortalPositionTolerance,tolerance.UseRoundSnowCorridor,tolerance.ConnectedSnowWindowMultiplier,tolerance.OrdinaryPortalRetreat,tolerance.OrdinaryHandoffWindow});
            // Actual actor18 at the first penalty exit window in the Mass30 CPU capture.
            var pose=new ManualSkiHeadingState(116.209611717,-37.739676728,2.803353678);
            int occurrence=f.Catalog.GetPenaltyOccurrence(0,1);
            Assert.That(f.Catalog.TryProject(pose,occurrence,0,150,out var observation),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work(),tolerance);
            sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(observation));
            T Field<T>(string name)=>(T)typeof(ManualRouteRibbonSweep).GetField(name,flags).GetValue(sweep);
            int columns=Field<int>("disks"),rows=Field<int>("refinedRows");
            double length=Field<double>("halfLength"),width=Field<double>("halfWidth"),radius=Field<double>("refinedRadius")+Field<double>("edgeClearance");
            var disk=typeof(ManualRouteRibbonSweep).GetMethod("DiskClearance",flags);
            double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians),expected=double.PositiveInfinity;
            for(int k=0;k<columns;k++)for(int j=0;j<rows;j++)
            {
                double along=length*(2.0*k+1-columns)/columns,side=width*(2.0*j+1-rows)/rows;
                expected=Math.Min(expected,(double)disk.Invoke(sweep,new object[]{pose.X+fx*along+fz*side,pose.Z+fz*along-fx*side,radius}));
            }
            typeof(ManualRouteRibbonSweep).GetField("diskQueries",flags).SetValue(sweep,0);
            var grid=typeof(ManualRouteRibbonSweep).GetMethod("RefinedGridMinimum",flags);
            double actual=(double)grid.Invoke(sweep,new object[]{pose,fx,fz,0,columns-1,0,rows-1,double.PositiveInfinity,double.NegativeInfinity});
            TestContext.WriteLine($"penalty_grid_queries={sweep.Diagnostics.LastDiskQueries} exact_minimum={actual:R}");
            Assert.That(actual,Is.EqualTo(expected).Within(1e-12));
            Assert.That(sweep.Diagnostics.LastDiskQueries,Is.LessThanOrEqualTo(12),"A boundary witness should seed the existing exact search before scanning interior cells.");
        }
        [Test] public void HierarchicalCoverMatchesExhaustiveDiskGridAtFoldedAndOutsidePoses()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:2,historicalCommonWidth:true);
            var anchor=new ManualSkiHeadingState(38.759850762,-1.659021024,1.180710536);
            Assert.That(f.Catalog.TryProject(anchor,3,1368532934L,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            sweep.Preview(Travel(f.Venue.Occupancy,anchor),f.Catalog.GetGeometryObservation(at));
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var disk=typeof(ManualRouteRibbonSweep).GetMethod("DiskClearance",flags);
            var grid=typeof(ManualRouteRibbonSweep).GetMethod("RefinedGridMinimum",flags);
            T Field<T>(string name)=>(T)typeof(ManualRouteRibbonSweep).GetField(name,flags).GetValue(sweep);
            int columns=Field<int>("disks"),rows=Field<int>("refinedRows");
            double length=Field<double>("halfLength"),width=Field<double>("halfWidth"),radius=Field<double>("refinedRadius")+Field<double>("edgeClearance");
            for(int p=0;p<15;p++)
            {
                var pose=new ManualSkiHeadingState(anchor.X+(p%3-1)*.4,anchor.Z+(p/3-2)*.45,anchor.HeadingRadians+(p%3-1)*.2);
                double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians),expected=double.PositiveInfinity;
                for(int k=0;k<columns;k++)for(int j=0;j<rows;j++)
                {
                    double along=length*(2.0*k+1-columns)/columns,side=width*(2.0*j+1-rows)/rows;
                    expected=Math.Min(expected,(double)disk.Invoke(sweep,new object[]{pose.X+fx*along+fz*side,pose.Z+fz*along-fx*side,radius}));
                }
                double actual=(double)grid.Invoke(sweep,new object[]{pose,fx,fz,0,columns-1,0,rows-1,double.PositiveInfinity,double.NegativeInfinity});
                Assert.That(actual,Is.EqualTo(expected).Within(1e-12),"Exact same leaf minimum at pose "+p);
            }
        }
        [Test] public void CapturedPostRangeSnowShoulderUsesABoundedRectangleCertificate()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var c=f.Catalog;
            var tolerance=ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json"));
            typeof(ManualRacePathCatalog).GetMethod("BindPortalTolerance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                .Invoke(c,new object[]{tolerance.PortalPositionTolerance,tolerance.UseRoundSnowCorridor,tolerance.ConnectedSnowWindowMultiplier,tolerance.OrdinaryPortalRetreat,tolerance.OrdinaryHandoffWindow});
            // Actual actor26 after its first range, near the existing soft snow edge.
            var pose=new ManualSkiHeadingState(144.495588034,-13.985181673,2.125655196);
            int index=c.GetBaseFlowSuccessor(c.StartApproachIndex);
            Assert.That(c.TryProject(pose,index,6*SkiState.NanometersPerMeter,10,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work(),tolerance);
            var result=sweep.Preview(Travel(f.Venue.Occupancy,pose),c.GetGeometryObservation(at));
            Assert.That(result.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(sweep.Diagnostics.LastDiskQueries,Is.Zero,"This paid stationary shoulder witness must not repeat a 512-disk body cover.");
            // The certificate retains the same shoulder/tolerance; beyond it still fails.
            var outside=new ManualSkiHeadingState(pose.X,pose.Z-1,pose.HeadingRadians);
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,outside),c.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
        }
        [Test] public void ExactRectangleProofRejectsAConcaveNotchBetweenItsInsideCorners()
        {
            double[] x={-3,3,3,.1,.1,-.1,-.1,-3},z={-3,-3,3,3,0,0,3,3};
            var method=typeof(ManualRouteRibbonSweep).GetMethod("RectangleClearance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            double clearance=(double)method.Invoke(null,new object[]{new ManualSkiHeadingState(0,.2,0),.75,.35,.1,x,z,x.Length});
            Assert.That(clearance,Is.LessThanOrEqualTo(0),"All corners fit the two wings, but the top edge crosses the notch.");
        }
        [TestCase(0d,ManualRouteTubeStatus.Clear)]
        [TestCase(-.002d,ManualRouteTubeStatus.UnresolvedStart)]
        public void CapturedMassStartRectangleKeepsItsActualEdgeClearance(double shiftZ,ManualRouteTubeStatus expected)
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(146.058043433,-11.144413020+shiftZ,1.437653133);
            int index=f.Catalog.GetBaseFlowSuccessor(f.Catalog.StartApproachIndex);
            Assert.That(f.Catalog.TryProject(pose,index,7558043433L,2,out var at),Is.True);
            // Straight authored start: all four rectangle sides, including the
            // unchanged edge clearance, fit. Circumdisks alone overestimate it.
            double extent=f.Venue.Occupancy.LongitudinalSeparation*.5*Math.Abs(Math.Cos(pose.HeadingRadians))+
                f.Venue.Occupancy.LateralSeparation*.5*Math.Abs(Math.Sin(pose.HeadingRadians));
            Assert.That(f.Catalog.ProjectionGeometry.TryGetCollinearCorridor(f.Catalog.GetGeometryObservation(at),out var cell),Is.True);
            double exact=cell.MinimumWidthMeters*.5-Math.Abs(at.LateralMeters)-extent-f.Venue.Occupancy.EdgeClearance;
            Assert.That(exact,expected==ManualRouteTubeStatus.Clear?Is.GreaterThan(0):Is.LessThan(0));
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(at)).Status,Is.EqualTo(expected));
        }
        [Test] public void TightFoldedRibbonCombinesCompleteDiskProofsWithoutAGlobalHull()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:2,historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(38.759850762,-1.659021024,1.180710536,-.566373208);
            Assert.That(f.Catalog.TryProject(pose,3,1368532934L,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void RefinedCellsDoNotShrinkAnOversizedOccupiedBody()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:2,historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(38.759850762,-1.659021024,1.180710536,-.566373208);
            Assert.That(f.Catalog.TryProject(pose,3,1368532934L,2,out var at),Is.True);
            var body=OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(f.Venue.Occupancy.ToJson(),"body_width_m","4"));
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,body,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(body,pose),f.Catalog.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
        }
        [Test] public void WarmRefinedFoldedCoverRemainsAllocationFreeAndRepeatable()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:2,historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(38.759850762,-1.659021024,1.180710536,-.566373208);
            Assert.That(f.Catalog.TryProject(pose,3,1368532934L,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            var trajectory=Travel(f.Venue.Occupancy,pose);var observation=f.Catalog.GetGeometryObservation(at);
            for(int i=0;i<128;i++)sweep.Preview(trajectory,observation);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=sweep.Preview(trajectory,observation).Status==ManualRouteTubeStatus.Clear;
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);
        }
        [Test] public void AnUpcomingNarrowMatDoesNotShrinkTheAlreadyOwnedWiderAisle()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:2,historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(37.466923758,-1.910501257,1.757996194,-.04758153);
            Assert.That(f.Catalog.TryProject(pose,3,0,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [TestCase(false)][TestCase(true)]
        public void MatHairpinAdmitsStationaryBodyButRejectsUnbrakedFastArrival(bool moving)
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);
            var pose=new ManualSkiHeadingState(44.091516134,-.76515254,1.301617099,-.352553986);
            Assert.That(f.Catalog.TryProject(pose,3,2188547085L,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            var result=sweep.Preview(Travel(f.Venue.Occupancy,pose,moving?7.985813048685882:0,moving?2.3088894868996728:0),f.Catalog.GetGeometryObservation(at));
            Assert.That(sweep.LastStartClearance,Is.GreaterThan(0));
            Assert.That(result.Status,Is.EqualTo(moving?ManualRouteTubeStatus.PotentialBoundary:ManualRouteTubeStatus.Clear));
            if(moving)Assert.That(result.ClearUntilSeconds,Is.LessThan(.05));
        }
        [TestCase(0d)][TestCase(-1.6d)]
        public void TightMatBendCanUseACompleteBodyCoverInsideExistingConservativeTubes(double side)
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);
            int index=f.Catalog.GetFiringOccurrence(0);
            f.Catalog.TrySample(index-1,f.Catalog.Get(index-1).LengthNanometers,out var a);
            f.Catalog.TrySample(index,0,out var b);
            double fx=a.ForwardX+b.ForwardX,fz=a.ForwardZ+b.ForwardZ,n=Math.Sqrt(fx*fx+fz*fz);fx/=n;fz/=n;
            var pose=new ManualSkiHeadingState(a.X+fz*side,a.Z-fx*side,1.340674625);
            Assert.That(f.Catalog.TryProject(pose,index,0,0,out var observation),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(observation)).Status,
                Is.EqualTo(ManualRouteTubeStatus.Clear),$"vertices {sweep.LastVertexCount} clearance {sweep.LastStartClearance:R}");
        }
        private static ManualSkiWorldTrajectory Travel(OccupancyDefinition body, ManualSkiHeadingState pose,
            double vx = 0, double vz = 0, double seconds = .05)
        {
            var velocity = new SkiPlanarVelocity(vx, vz);
            var skier = new ManualSkiContactBody(new AthleteId("ribbon_probe"), pose, velocity, 75);
            return new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(body, 1)
                .PreviewTrajectory(skier, new SkiContactMotion(velocity, pose.YawRateRadiansPerSecond), 0, 0, seconds));
        }

        [Test]
        public void ShortStartSeamWindowIncludesTheWholeDiskCover()
        {
            var f = new ManualRacePathCatalogTests.Fixture(historicalCapturedGeometry:true);
            // Actual accepted Mass30 pose before tick47. The old local polygon
            // ended at x=138.5: a sampling cap, not the end of skiable snow.
            var pose = new ManualSkiHeadingState(137.405557435, -10.740960171, 1.636739122);
            Assert.That(f.Catalog.TryProject(pose, f.Catalog.StartApproachIndex,
                39 * SkiState.NanometersPerMeter, 2, out var observation), Is.True);
            var sweep = new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry, f.Venue.Occupancy,
                ManualScopedFieldStepperTests.Work());
            var result = sweep.Preview(Travel(f.Venue.Occupancy, pose), f.Catalog.GetGeometryObservation(observation));
            Assert.That(result.Status, Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(sweep.LastStartClearance, Is.GreaterThan(0));
            Assert.That(result.ClearUntilSeconds, Is.EqualTo(.05));
        }

        [Test]
        public void ActualLateralDepartureIsStillRejected()
        {
            var f = new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);
            int index = f.Catalog.GetBaseFlowSuccessor(f.Catalog.StartApproachIndex);
            var pose = f.At(index, 20 * SkiState.NanometersPerMeter);
            Assert.That(f.Catalog.TryProject(pose, index, 20 * SkiState.NanometersPerMeter, 1, out var observation), Is.True);
            var sweep = new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry, f.Venue.Occupancy,
                ManualScopedFieldStepperTests.Work());
            var result = sweep.Preview(Travel(f.Venue.Occupancy, pose, 0, 20, 1), f.Catalog.GetGeometryObservation(observation));
            Assert.That(result.Status, Is.EqualTo(ManualRouteTubeStatus.PotentialBoundary));
            Assert.That(result.ClearUntilSeconds, Is.GreaterThan(0).And.LessThan(1));
        }

        [Test] public void AuthoredEndpointSurvivesCancellationWhenTheCurveCrossesZeroX()
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCapturedGeometry:true);
            int index=f.Catalog.GetBaseFlowSuccessor(f.Catalog.StartApproachIndex);
            // Actual accepted first-lap witness before tick1778. Rebuilding
            // b.X as a.X+(b.X-a.X) severed an exactly connected visual edge.
            var pose=new ManualSkiHeadingState(-.576530797,-291.041222143,4.76543527);
            Assert.That(f.Catalog.TryProject(pose,index,571917986112L,2,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,pose),f.Catalog.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.Clear));
        }
        [Test] public void ADisconnectedOrDistantPositionCannotUseTheTubeCoverAsABridge()
        {
            var f=new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);int index=f.Catalog.GetFiringOccurrence(0);
            var centre=f.At(index,0);Assert.That(f.Catalog.TryProject(centre,index,0,0,out var at),Is.True);
            var outside=new ManualSkiHeadingState(centre.X,centre.Z+10,1.34);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            Assert.That(sweep.Preview(Travel(f.Venue.Occupancy,outside),f.Catalog.GetGeometryObservation(at)).Status,
                Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
        }

        [Test]
        public void WarmRepeatedCoverQueriesAreDeterministicAndAllocateNothing()
        {
            var f = new ManualRacePathCatalogTests.Fixture(historicalCommonWidth:true);
            var pose = f.At(f.Catalog.StartApproachIndex, 39 * SkiState.NanometersPerMeter, 2);
            Assert.That(f.Catalog.TryProject(pose, f.Catalog.StartApproachIndex, 39 * SkiState.NanometersPerMeter, 1, out var observation), Is.True);
            var source = f.Catalog.GetGeometryObservation(observation);
            var sweep = new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry, f.Venue.Occupancy, ManualScopedFieldStepperTests.Work());
            var trajectory = Travel(f.Venue.Occupancy, pose, 1);
            var expected = sweep.Preview(trajectory, source);
            Assert.That(expected.Status, Is.EqualTo(ManualRouteTubeStatus.Clear));
            for (int i = 0; i < 100; i++) sweep.Preview(trajectory, source);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                var actual = sweep.Preview(trajectory, source);
                if (actual.Status != expected.Status || actual.IntervalChecks != expected.IntervalChecks)
                    Assert.Fail("Identical cover query changed its proof.");
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
