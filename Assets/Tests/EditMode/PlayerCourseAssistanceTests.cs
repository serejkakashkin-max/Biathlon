using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class PlayerCourseAssistanceTests
    {
        private static PlayerCourseAssistanceDefinition Definition()=>PlayerCourseAssistanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("player_course_assistance_v1.json"));
        [Test] public void ReleaseStraightensAtTheExistingPositionAndDoesNotFollowLaterBends()
        {
            var s=new PlayerCourseReturnSystem(Definition(),20);
            var at=new ManualSkiHeadingState(2,40,.4);
            Assert.That(s.Evaluate(0,at,0,true),Is.Zero,"No steering gesture means no automatic course following.");
            Assert.That(s.Evaluate(800,at,0,true),Is.EqualTo(800));
            for(int i=0;i<20;i++)Assert.That(s.Evaluate(0,at,i==0?0:1.5,true),Is.LessThanOrEqualTo(0));
            Assert.That(s.ReleaseHeadingRadians,Is.Zero,"The tangent is captured once at release, rather than following the bend.");
            // Aligned on the same offset line: future course curvature cannot
            // steer us or choose a centreline position.
            var aligned=new ManualSkiHeadingState(2,45,0);
            for(int i=0;i<20;i++)s.Evaluate(0,aligned,1.5,true);
            Assert.That(s.Evaluate(0,aligned,1.5,true),Is.Zero);
            Assert.That(s.Evaluate(-800,aligned,1.5,true),Is.EqualTo(-800));
            s.Reset();
            Assert.That(s.ReleaseHeadingRadians,Is.Null);
            Assert.That(s.Evaluate(0,aligned,1.5,true),Is.Zero,"Range handback cannot resume a stale steering target.");
        }
        [Test] public void ReleasedSteeringReturnsGentlyAndHeldSteeringOverridesImmediately()
        {
            var d=Definition();var s=new PlayerCourseReturnSystem(d,20);
            Assert.That(s.Evaluate(-600,1000,true),Is.EqualTo(-600));
            for(int i=0;i<5;i++)Assert.That(s.Evaluate(0,1000,true),Is.Zero);
            int previous=0;
            for(int i=0;i<40;i++)
            {int now=s.Evaluate(0,1000,true);Assert.That(now-previous,Is.InRange(0,(int)(d.ReturnInputPerSecond/20)));Assert.That(now,Is.LessThanOrEqualTo(d.MaximumReturnInput));previous=now;}
            Assert.That(previous,Is.EqualTo(d.MaximumReturnInput));
            Assert.That(s.Evaluate(-1000,1000,true),Is.EqualTo(-1000));
            Assert.That(s.Evaluate(0,1000,false),Is.Zero);
        }
        [Test] public void ArrowCaptureLatchesUntilShootingAndDepartureReturnsAfterTheTurn()
        {
            var guide=RangeGuidanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("range_guidance_v1.json"));
            var s=new PlayerRangeAssistanceSystem(guide);
            Assert.That(s.EvaluateManual(RangeTraversalPhase.Approach,0,false,false,800,1,6).Active,Is.False);
            Assert.That(s.EvaluateManual(RangeTraversalPhase.Approach,0,true,false,800,1,6).Mode,Is.EqualTo(PlayerRangeAssistMode.FinalApproach));
            Assert.That(s.EvaluateManual(RangeTraversalPhase.Approach,0,false,false,800,1,6).Active,Is.True,"A flashing arrow cannot release captured assistance.");
            Assert.That(s.EvaluateManual(RangeTraversalPhase.Shooting,0,false,false,800,1,0).Active,Is.False);
            Assert.That(s.EvaluateManual(RangeTraversalPhase.RangeExit,0,false,false,0,1,0).Mode,Is.EqualTo(PlayerRangeAssistMode.InitialDeparture));
            var returned=s.EvaluateManual(RangeTraversalPhase.RangeExit,0,false,true,800,1,5);
            Assert.That(returned.Active,Is.False);Assert.That(returned.RequestedEffort,Is.EqualTo(800));
            Assert.That(s.EvaluateManual(RangeTraversalPhase.Approach,1,false,false,800,1,6).Active,Is.False);
        }
        [Test] public void FixedWorldArrowStationsCanBeRiddenOntoAndStayAwayFromOtherMats()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+"mass_start_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var path=new PhysicalRangeFieldLayout(new PhysicalTrackMap(venue.Track,route),venue.Track,route,venue.Paths).GetPath(10);
            var guide=RangeGuidanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("range_guidance_v1.json"));
            var trail=RangeGuidanceTrailDefinition.FromJson(PhysiologyContractTests.DefinitionJson("range_guidance_visual_v1.json"));
            long stop=path.FiringRouteNanometers(0),at=stop-40*SkiState.NanometersPerMeter;
            long station=RangeMatGuidanceGeometry.Station(guide,at,0);
            var p=RangeMatGuidanceGeometry.Point(venue,path,trail,station,stop,false,0,guide.ChevronCount);
            Assert.That(RangeMatGuidanceGeometry.Station(guide,at+SkiState.NanometersPerMeter/2,0),Is.EqualTo(station));
            Assert.That(RangeMatGuidanceGeometry.Contains(venue,path,guide,trail,new ManualSkiHeadingState(p.X,p.Z,0),at,stop,1.2),Is.True);
            Assert.That(RangeMatGuidanceGeometry.Contains(venue,path,guide,trail,new ManualSkiHeadingState(p.X,p.Z+4,0),at,stop,1.2),Is.False);
            var mat=venue.MatPoint(path.LaneOrdinal-1);
            Assert.That(p.Z,Is.LessThan(mat.Z+venue.Manifest.Range.MatDefinition.AthleteStopDepth-venue.Manifest.Range.MatDefinition.Length-trail.MatClearance));
        }
        [Test] public void SlowFramesHaveBoundedCatchUpWithoutChangingTheFixedSimulationTick()
        {
            var d=FixedTickFrameBudgetDefinition.FromJson(PhysiologyContractTests.DefinitionJson("fixed_tick_frame_budget_v1.json"));
            var clock=new FixedTickFrameClock(d,20);
            Assert.That(clock.BeginFrame(3),Is.EqualTo(4));Assert.That(clock.DiscardedWallSeconds,Is.EqualTo(2.8).Within(1e-12));
            for(int i=0;i<4;i++)clock.ConsumeTick();
            Assert.That(clock.BeginFrame(.025),Is.Zero);Assert.That(clock.BeginFrame(.025),Is.EqualTo(1));clock.ConsumeTick();
            Assert.That(clock.BacklogSeconds,Is.LessThan(1e-12));
        }
    }
}
