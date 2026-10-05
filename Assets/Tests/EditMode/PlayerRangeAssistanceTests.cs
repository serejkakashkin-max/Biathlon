using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class PlayerRangeAssistanceTests
    {
        private static RangeGuidanceDefinition Definition()=>RangeGuidanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("range_guidance_v1.json"));
        [Test] public void AutomationIsBoundedToAssignedFinalThreeMetresAndFirstDepartureMetre()
        {
            var system=new PlayerRangeAssistanceSystem(Definition());
            PlayerRangeAssistPlan At(RangeTraversalPhase phase,bool mat,double distance,double speed=0)=>system.Evaluate(phase,mat,(long)Math.Round(distance*SkiState.NanometersPerMeter),800,.7,speed);
            Assert.That(At(RangeTraversalPhase.Approach,true,3).Mode,Is.EqualTo(PlayerRangeAssistMode.FinalApproach));
            Assert.That(At(RangeTraversalPhase.Approach,true,3.001).Active,Is.False);
            Assert.That(At(RangeTraversalPhase.Approach,false,2).Active,Is.False);
            Assert.That(At(RangeTraversalPhase.Approach,true,2,3).RequestedEffort,Is.Zero);
            Assert.That(At(RangeTraversalPhase.Approach,true,1).RequestedLateral,Is.Zero);
            Assert.That(At(RangeTraversalPhase.Shooting,true,0).Active,Is.False);
            Assert.That(At(RangeTraversalPhase.RangeExit,true,0).Mode,Is.EqualTo(PlayerRangeAssistMode.InitialDeparture));
            Assert.That(At(RangeTraversalPhase.RangeExit,true,-.999).Active,Is.True);
            var manual=At(RangeTraversalPhase.RangeExit,true,-1);
            Assert.That(manual.Active,Is.False);Assert.That(manual.RequestedEffort,Is.EqualTo(800));Assert.That(manual.RequestedLateral,Is.EqualTo(.7));
            Assert.That(At(RangeTraversalPhase.PenaltyLoop,true,-.5).Active,Is.False);
        }
        [Test] public void RoundTripBoundsAndHotQueriesRemainStrictAndAllocationFree()
        {
            var d=Definition();Assert.That(RangeGuidanceDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
            Assert.Catch(()=>RangeGuidanceDefinition.FromJson(d.ToJson().Replace("\"mat_assist_lead_m\":3","\"mat_assist_lead_m\":300")));
            var s=new PlayerRangeAssistanceSystem(d);for(int i=0;i<100;i++)s.Evaluate(RangeTraversalPhase.Approach,true,1000000000,800,.7,1);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)s.Evaluate(RangeTraversalPhase.Approach,true,1000000000,800,.7,1);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [TestCase("sprint")][TestCase("mass_start")][TestCase("pursuit")]
        public void ExactFormatPathGuidesAtActualBranchesAndDoesNotTreatTerrainKnotsAsForks(string format)
        {
            const string v="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(v+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(v+format+"_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(v+format+"_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var layout=new PhysicalRangeFieldLayout(new PhysicalTrackMap(venue.Track,route),venue.Track,route,venue.Paths);
            long limit=40*SkiState.NanometersPerMeter;
            Assert.That(RaceRouteGuidance.NextBranch(layout.GetPath(0),route,0,limit),Is.EqualTo(-1));
            // FirstForkRoute is the earliest fork of any mat; this player's actual
            // selected fork is its own firing stop minus its paid private approach.
            long fork=layout.GetPath(5).FiringRouteNanometers(0)-layout.GetPath(5).MatAnchor.Distance.Millimeters*SkiState.NanometersPerMillimeter;
            long found=RaceRouteGuidance.NextBranch(layout.GetPath(5),route,fork-1000000000,limit);
            Assert.That(found,Is.GreaterThanOrEqualTo(fork));
            Assert.That(layout.GetPath(5).MapRoute(found).LoopId,Is.Not.EqualTo(layout.GetPath(5).MapRoute(found,PhysicalBoundarySide.Incoming).LoopId));
        }
    }
}
