using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenueLocalRangeGuardTests
    {
        private static bool Has(VenueValidationReport report,string code)
        {for(int i=0;i<report.Count;i++)if(report.Get(i).Code==code)return true;return false;}
        private static VenueValidationReport Validate(Dictionary<string,string> resources)
            =>VenueValidator.Validate(VenuePathAuthoringTests.Plan(resources),VenueValidatorTests.Policy(),_=>true);
        private static Dictionary<string,string> ContractResources()
        {
            // This map deliberately has no valid venue world. Assert only the targeted
            // local-region rule; this does not certify these two paths as production content.
            var resources=VenuePathAuthoringTests.Resources();
            resources["track"]=PhysicalRangePathDefinitionTests.Fixture("track");
            resources["paths"]=PhysicalRangePathDefinitionTests.Fixture("paths");
            resources["junctions"]=PhysicalRangePathDefinitionTests.Fixture("junctions");
            resources["connections"]=PhysicalRangePathDefinitionTests.Fixture("connections");
            resources["manifest"]=resources["manifest"].Replace("\"distance_m\": 60","\"distance_m\": 100");
            var track=TrackDefinition.FromJson(resources["track"]);var visuals=new string[track.LoopCount];
            for(int i=0;i<track.LoopCount;i++)
            {
                var loop=track.GetLoop(i);double length=loop.Length.Meters,x=-1000-i*1000;
                visuals[i]=VenuePathAuthoringTests.Visual(loop.Id,loop.Closed?new[]{new[]{0d,x,0,-1000},new[]{length/4,x+length/4,0,-1000},
                    new[]{length/2,x+length/4,0,-1000-length/4},new[]{length*3/4,x,0,-1000-length/4},new[]{length,x,0,-1000}}:
                    new[]{new[]{0d,x,0,-1000},new[]{length,x+length,0,-1000}});
            }
            resources["visual"]="{\"schema_version\":1,\"id\":\"local_guard_visual\",\"track_id\":\""+track.Id+"\",\"sample_spacing_m\":1,\"loops\":["+string.Join(",",visuals)+"]}";
            return resources;
        }
        private static void ExtraRegion(Dictionary<string,string> resources,string ports)
            =>resources["junctions"]=Regex.Replace(resources["junctions"],"(\"junctions\"\\s*:\\s*\\[)",
                match=>match.Value+"{\"id\":\"extra_region\",\"ports\":["+ports+"]},");
        private static string Port(string id,string loop,double at,double before,double after)=>VenuePathAuthoringTests.Port(id,loop,at,before,after);

        [Test] public void IndependentLocalForksAreNotMislabelledOneGlobalEntrance()
        {
            var report=Validate(ContractResources());
            Assert.That(Has(report,"range.shared_entry_gate"),Is.False,report.ToJson());
            // Other diagnostics must remain visible rather than turning this contract fixture into acceptance.
            Assert.That(report.Passed,Is.False);
        }
        [TestCase(false)][TestCase(true)]
        public void ExtraBroadMainGuardCannotHideBehindPortsAwayFromForks(bool wrap)
        {
            var resources=ContractResources();
            ExtraRegion(resources,Port("extra_main","main",wrap?299:20,wrap?1:15,wrap?31:15)+","+Port("extra_exit","exit_aisle",50,1,1));
            var report=Validate(resources);
            Assert.That(Has(report,"range.shared_entry_gate"),Is.True,report.ToJson());
            bool extra=false;for(int i=0;i<report.Count;i++)extra|=report.Get(i).Code=="range.shared_entry_gate"&&report.Get(i).Path=="junctions/extra_region";
            Assert.That(extra,Is.True,report.ToJson());
        }
        [Test] public void PrivateStartGuardsCannotCollectTwoMatEntrancesWithoutMainForkPorts()
        {
            var resources=ContractResources();
            ExtraRegion(resources,Port("extra_private_1","mat_1",1,1,1)+","+Port("extra_private_2","mat_2",1,1,1));
            var report=Validate(resources);Assert.That(Has(report,"range.shared_entry_gate"),Is.True,report.ToJson());
        }
        [Test] public void ExtraLocalGuardCoveringOnlyOneForkRemainsLocal()
        {
            var resources=ContractResources();ExtraRegion(resources,Port("extra_main","main",10,1,1)+","+Port("extra_exit","exit_aisle",50,1,1));
            var report=Validate(resources);Assert.That(Has(report,"range.shared_entry_gate"),Is.False,report.ToJson());
        }

        private static Dictionary<string,string> WithStart(bool bound=true)
        {
            var resources=VenuePathAuthoringTests.Resources();
            if(bound)resources["manifest"]=resources["manifest"].Replace("\"schema_version\": 2","\"schema_version\": 2,\"start_approach_resource\":\"start\"");
            resources["start"]="{\"schema_version\":1,\"id\":\"venue_start_v1\",\"track_id\":\"world_paths_track_v1\",\"loop_id\":\"start_approach\",\"route_entry_loop_id\":\"main\"}";
            resources["track"]=resources["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("start_approach",false,6,10)+",");
            resources["visual"]=resources["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("start_approach",new[]{new[]{0d,20,0,-10},new[]{10d,10,0,-10}})+",");
            resources["junctions"]=resources["junctions"].Replace("\"ports\":[","\"ports\":["+Port("start_end","start_approach",10,9.999,0)+","+
                Port("start_zero","start_approach",0,0,9.999)+","+Port("start_main","main",0,4,4)+",");
            resources["connections"]=resources["connections"].Replace("\"connections\":[","\"connections\":["+VenuePathAuthoringTests.Connection("start_join","start_end","start_main")+",");
            return resources;
        }
        [Test] public void ExplicitOptionalStartApproachBindsOpenTravelAndDirectedWorldSeam()
        {
            var resources=WithStart();var plan=VenuePathAuthoringTests.Plan(resources);var report=Validate(resources);
            Assert.That(plan.StartApproach,Is.Not.Null);Assert.That(plan.StartApproach.LoopId,Is.EqualTo("start_approach"));
            Assert.That(plan.StartApproach.RouteEntryLoopId,Is.EqualTo(plan.Paths.MainLoopId));
            Assert.That(report.Passed,Is.True,report.ToJson());
            Assert.That(plan.Manifest.ToJson(),Does.Contain("start_approach_resource"));
            var rebuilt=new VenueAuthoringPlan(VenueManifest.FromJson(plan.Manifest.ToJson()),id=>resources[id]);
            Assert.That(rebuilt.StartApproach.ToJson(),Is.EqualTo(plan.StartApproach.ToJson()));
            Assert.That(rebuilt.Manifest.ToJson(),Is.EqualTo(plan.Manifest.ToJson()));
            Assert.That(Has(Validate(WithStart(false)),"loop.open"),Is.True);
        }
        [Test] public void StartRequiresDirectedConnectionAndCoincidentWorldEndpoint()
        {
            var resources=WithStart();resources["connections"]=resources["connections"].Replace(VenuePathAuthoringTests.Connection("start_join","start_end","start_main")+",","");
            Assert.That(Has(Validate(resources),"start_approach.disconnected"),Is.True);
            resources=WithStart();resources["visual"]=resources["visual"].Replace("\"distance_m\":10,\"x\":10,\"y\":0,\"z\":-10","\"distance_m\":10,\"x\":10.1,\"y\":0,\"z\":-10");
            var report=Validate(resources);Assert.That(Has(report,"start_approach.world_gap"),Is.True,report.ToJson());
            Assert.That(Has(report,"connection.world_gap"),Is.True,report.ToJson());
        }
        [Test] public void ClosedOrRangeSharedStartLoopCannotGainTheOpenStartExemption()
        {
            var resources=WithStart();resources["start"]=resources["start"].Replace("\"loop_id\":\"start_approach\"","\"loop_id\":\"penalty\"");
            Assert.That(Has(Validate(resources),"start_approach.loop"),Is.True);
            resources=WithStart();resources["start"]=resources["start"].Replace("\"loop_id\":\"start_approach\"","\"loop_id\":\"mat\"");
            Assert.That(Has(Validate(resources),"start_approach.range_overlap"),Is.True);
        }
        [TestCase("track_id","foreign_track")][TestCase("route_entry_loop_id","mat")][TestCase("loop_id","missing")][TestCase("loop_id","main")]
        public void StartDefinitionMustBindExactTrackAndMainEntryBeforeWorldValidation(string field,string value)
        {
            var resources=WithStart();resources["start"]=Regex.Replace(resources["start"],"(\""+field+"\"\\s*:\\s*)\"[^\"]*\"","${1}\""+value+"\"");
            Assert.Catch(()=>VenuePathAuthoringTests.Plan(resources));
        }
        [Test] public void VersionOneCanonicalJsonRemainsUnchangedAndRejectsStartBindings()
        {
            var legacy=VenueValidatorTests.Plan();Assert.That(legacy.StartApproach,Is.Null);
            Assert.That(legacy.Manifest.ToJson(),Does.Not.Contain("start_approach_resource"));
            Assert.Catch(()=>VenueManifest.FromJson(VenueValidatorTests.Read("manifest").Replace("\"schema_version\": 1","\"schema_version\": 1,\"start_approach_resource\":\"start\"")));
            string valid=WithStart()["manifest"];
            Assert.Catch(()=>VenueManifest.FromJson(valid.Replace("\"start_approach_resource\":\"start\"","\"start_approach_resource\":\"../outside\"")));
        }

        [Test] public void AdjacentAndClosureRetracingCannotHideAsOrdinaryPolylineCorners()
        {
            var resources=VenuePathAuthoringTests.Resources();
            resources["track"]=resources["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("backtrack",true,6,10,10)+",");
            resources["visual"]=resources["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("backtrack",new[]{new[]{0d,-1000,0,-1000},new[]{10d,-990,0,-1000},new[]{20d,-1000,0,-1000}})+",");
            var report=Validate(resources);Assert.That(Has(report,"loop.adjacent_retrace"),Is.True,report.ToJson());
            Assert.That(Has(Validate(VenuePathAuthoringTests.Resources()),"loop.adjacent_retrace"),Is.False);
            // The existing main rectangle and private right-angle turns remain valid ribbon joins.
            Assert.That(Validate(VenuePathAuthoringTests.Resources()).Passed,Is.True);
        }
    }
}
