using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualRouteTubeSweepTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static (RaceRoute route,TrackVisualDefinition visual,ManualRouteProjectionMap map) Create()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            return (route,venue.Visual,new ManualRouteProjectionMap(route,venue.Visual));
        }
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
        private static SkiContactSweepDefinition Work(int checks=4096)=>SkiContactSweepDefinition.FromJson(
            "{\"schema_version\":1,\"id\":\"manual_route_tube_test_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":"+
            checks+",\"time_tolerance_s\":0.0000001}");
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);
        private static ManualSkiWorldTrajectory Travel(OccupancyDefinition body,ManualSkiHeadingState pose,
            double velocityX,double velocityZ,double seconds=.05)
        {
            var velocity=new SkiPlanarVelocity(velocityX,velocityZ);
            var athlete=new ManualSkiContactBody(new AthleteId("tube_probe"),pose,velocity,75);
            return new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(body,1)
                .PreviewTrajectory(athlete,new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),0,0,seconds));
        }
        [Test] public void ShortWoodlandVisualEdgesDoNotStopContinuousForwardTravelAtTheirKnots()
        {
            var f=Create();var body=Body();var sweep=new ManualRouteTubeSweep(f.map,body,Work());
            var section=f.route.GetSection(0);var loop=f.visual.Get(section.LoopId);
            int accepted=0,shortEdges=0;
            for(int k=1;k+1<loop.Count;k++)
            {
                var previous=loop.Get(k-1);var knot=loop.Get(k);var following=loop.Get(k+1);
                double dx=following.X-knot.X,dz=following.Z-knot.Z,length=Math.Sqrt(dx*dx+dz*dz);
                if(length<2)shortEdges++;
                var pose=new ManualSkiHeadingState(knot.X,knot.Z,Math.Atan2(dx,dz));
                var trajectory=Travel(body,pose,3*dx/length,3*dz/length);
                var preview=sweep.Preview(trajectory,0,Nano(knot.Distance),2,false);
                Assert.That(preview.Status,Is.EqualTo(ManualRouteTubeStatus.Clear),
                    $"knot={k} local={knot.Distance} edge_length={length} source={preview.SourceEdgeId}");
                Assert.That(preview.ClearUntilSeconds,Is.EqualTo(.05));accepted++;
            }
            Assert.That(accepted,Is.GreaterThan(300));
            Assert.That(shortEdges,Is.GreaterThan(100));
        }
        [Test] public void OutwardCourseDepartureHasAContinuousUnprovedPrefixInsteadOfAClamp()
        {
            var f=Create();var body=Body();var sweep=new ManualRouteTubeSweep(f.map,body,Work());
            var section=f.route.GetSection(0);var loop=f.visual.Get(section.LoopId);
            double local=loop.Get(20).Distance;var p=loop.Sample(local);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            var trajectory=Travel(body,pose,10*p.ForwardZ,-10*p.ForwardX,.5);
            var result=sweep.Preview(trajectory,0,Nano(local),6,false);
            Assert.That(result.Status,Is.EqualTo(ManualRouteTubeStatus.PotentialBoundary));
            Assert.That(result.ClearUntilSeconds,Is.GreaterThan(0).And.LessThan(.5));
            Assert.That(result.SourceEdgeId,Does.StartWith("0:"));
            Assert.That(trajectory.Sample(1).X,Is.Not.EqualTo(p.X),"The preview reports a limit without clamping or steering the pose.");
        }
        [Test] public void TubeQueryKeepsSelectedLapIdentityAndRejectsOutsideStart()
        {
            var f=Create();var body=Body();var sweep=new ManualRouteTubeSweep(f.map,body,Work());
            var first=f.route.GetSection(0);var repeat=f.route.GetSection(1);var p=f.visual.Get(first.LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            var trajectory=Travel(body,pose,0,0);
            var one=sweep.Preview(trajectory,0,Nano(10),1,false);
            var two=sweep.Preview(trajectory,1,Nano(repeat.Start.Meters+10),1,false);
            Assert.That(one.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(two.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(one.SourceEdgeId,Does.StartWith("0:"));
            Assert.That(two.SourceEdgeId,Does.StartWith("1:"));
            var outside=new ManualSkiHeadingState(p.X+30*p.ForwardZ,p.Z-30*p.ForwardX,pose.HeadingRadians);
            Assert.That(sweep.Preview(Travel(body,outside,0,0),0,Nano(10),1,false).Status,
                Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
            Assert.Throws<ArgumentOutOfRangeException>(()=>sweep.Preview(trajectory,0,Nano(first.Length.Meters+1),1,false));
        }
        [Test] public void RepeatedTubeSweepIsDeterministicAndWarmAllocationFree()
        {
            var f=Create();var body=Body();var sweep=new ManualRouteTubeSweep(f.map,body,Work());
            var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            var trajectory=Travel(body,pose,3*p.ForwardX,3*p.ForwardZ);
            var expected=sweep.Preview(trajectory,0,Nano(10),2,false);
            Assert.That(expected.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            Assert.That(sweep.ContextJson,Does.Contain("manual_selected_route_inner_tube_sweep_v1"));
            for(int i=0;i<100;i++)sweep.Preview(trajectory,0,Nano(10),2,false);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                var result=sweep.Preview(trajectory,0,Nano(10),2,false);
                if(result.Status!=expected.Status||result.IntervalChecks!=expected.IntervalChecks||
                    result.SourceEdgeId!=expected.SourceEdgeId)Assert.Fail("Same fixed-tick tube query changed.");
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void ExhaustedProofBudgetNeverTurnsAnOutwardDepartureIntoClearTravel()
        {
            var f=Create();var body=Body();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            var trajectory=Travel(body,pose,10*p.ForwardZ,-10*p.ForwardX,.5);
            var limited=new ManualRouteTubeSweep(f.map,body,Work(1)).Preview(trajectory,0,Nano(10),6,false);
            Assert.That(limited.Status,Is.EqualTo(ManualRouteTubeStatus.WorkLimit));
            Assert.That(limited.ClearUntilSeconds,Is.Zero);
            Assert.That(limited.IntervalChecks,Is.EqualTo(1));
        }
    }
}
