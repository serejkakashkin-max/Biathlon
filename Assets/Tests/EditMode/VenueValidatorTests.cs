using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class VenueValidatorTests
    {
        public static string Read(string id)=>PhysiologyContractTests.ProjectFile("tests/fixtures/venues/validator_valid/"+id+".json");
        public static string PolicyJson()=>PhysiologyContractTests.DefinitionJson("Venues/production_venue_validation_v1.json");
        public static VenueValidationPolicy Policy(bool production=false)=>VenueValidationPolicy.FromJson(production?PolicyJson():PolicyJson().Replace("\"required_lanes\": 30","\"required_lanes\": 1"));
        public static VenueAuthoringPlan Plan(Func<string,string,string> change=null)
        {
            string Get(string id){string json=Read(id);return change==null?json:change(id,json);}
            return new VenueAuthoringPlan(VenueManifest.FromJson(Get("manifest")),Get);
        }
        private static void Has(VenueValidationReport report,string code)
        {for(int i=0;i<report.Count;i++)if(report.Get(i).Code==code)return;Assert.Fail("Missing "+code+": "+report.ToJson());}
        [Test] public void GeometricFixturePassesWithoutBeingMislabelledThirtyLaneProduction()
        {
            var plan=Plan();var report=VenueValidator.Validate(plan,Policy(),_=>true);
            Assert.That(report.Passed,Is.True,report.ToJson());Assert.That(report.Count,Is.Zero);
            Has(VenueValidator.Validate(plan,Policy(true),_=>true),"range.capacity");
        }
        [TestCase("distance.world")][TestCase("width.minimum")][TestCase("grade.world_mismatch")][TestCase("connection.world_gap")]
        public void IndependentWorldAndAuthoritativeDefectsAreDetected(string expected)
        {
            var plan=Plan((id,json)=>{
                if(expected=="distance.world"&&id=="visual")return json.Replace("50.249","51.249");
                if(expected=="width.minimum"&&id=="track")return json.Replace("\"width_m\": 6","\"width_m\": 2");
                if(expected=="grade.world_mismatch"&&id=="visual")return Regex.Replace(json,"(\"x\": 0,\\s*\"y\": )0","${1}10");
                if(expected=="connection.world_gap"&&id=="visual")return Regex.Replace(json,"\"loop_id\": \"lane_1\"[\\s\\S]*?\\]",m=>m.Value.Replace("\"x\": -50","\"x\": -49"));
                return json;});
            Has(VenueValidator.Validate(plan,Policy(),_=>true),expected);
        }
        [Test] public void MissingPrefabFrontalApproachAndMisalignedMatsFail()
        {
            Has(VenueValidator.Validate(Plan(),Policy(),_=>false),"asset.missing");
            var frontal=Plan((id,json)=>id=="manifest"?json.Replace("\"distance_m\": 0","\"distance_m\": 50.249"):json);
            Has(VenueValidator.Validate(frontal,Policy(),_=>true),"range.side_approach");
            var shifted=Plan((id,json)=>id=="manifest"?Regex.Replace(json,"(\"firing_origin\": \\{\\s*\"x\": )0","${1}1"):json);
            Has(VenueValidator.Validate(shifted,Policy(),_=>true),"range.mat_alignment");
        }
        [Test] public void WrongPenaltyLengthAndRoutesAcrossFiringCorridorFail()
        {
            Has(VenueValidator.Validate(Plan(),VenueValidationPolicy.FromJson(Policy().ToJson().Replace("\"penalty_length_m\":150","\"penalty_length_m\":100")),_=>true),"penalty.length");
            var across=Plan((id,json)=>id=="manifest"?Regex.Replace(json,"(\"firing_origin\": \\{\\s*\"x\": )0","${1}50"):json);
            Has(VenueValidator.Validate(across,Policy(),_=>true),"penalty.firing_corridor");
        }
        [Test] public void ExistingGreyboxCannotPassProductionByDeclaringSideEntry()
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(VenueAuthoringTests.ManifestJson()),VenueAuthoringTests.Read);
            var report=VenueValidator.Validate(plan,Policy(true),_=>true);
            Has(report,"distance.world");Has(report,"range.side_approach");Has(report,"range.shared_entry_gate");
        }
        [Test] public void ReportsAreStableCultureIndependentAndDoNotMutateInput()
        {
            var plan=Plan();string before=plan.Track.ToJson();var policy=Policy(true);string expected=VenueValidator.Validate(plan,policy,_=>false).ToJson();
            var old=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=new CultureInfo("ru-RU");Assert.That(VenueValidator.Validate(plan,policy,_=>false).ToJson(),Is.EqualTo(expected));}finally{CultureInfo.CurrentCulture=old;}
            Assert.That(plan.Track.ToJson(),Is.EqualTo(before));Assert.That(expected,Does.Contain("\"schema_version\":1"));
        }
        [TestCase("distance_abs_tolerance_m","-1")][TestCase("grade_tolerance","1")][TestCase("required_lanes","0")][TestCase("firing_corridor_depth_m","0")]
        public void InvalidPolicyCannotSilentlyRelaxChecks(string field,string value)
        {Assert.Catch(()=>VenueValidationPolicy.FromJson(PhysiologyContractTests.ReplaceNumber(PolicyJson(),field,value)));}
        [Test] public void BrokenClosedLoopIsRejectedBeforeGeometricValidation()
        {
            Assert.Catch(()=>Plan((id,json)=>id=="visual"?Regex.Replace(json,"(\"distance_m\": 400.499,\\s*\"x\": )-50","${1}-49"):json));
        }
        [Test] public void MissingDirectedAdmissionAndPenaltySeamsCannotPass()
        {
            var plan=Plan((id,json)=>id=="connections"?Regex.Replace(json,"\"connections\": \\[[\\s\\S]*\\]","\"connections\": []"):json);
            var report=VenueValidator.Validate(plan,Policy(),_=>true);Has(report,"range.lane_disconnected");Has(report,"penalty.disconnected");
        }
        [Test] public void DecorativeAnchorsAndMidLanePenaltySeamsCannotConcealActualRoutes()
        {
            var wrong=Plan((id,json)=>id=="manifest"?json.Replace("\"distance_m\": 100.499","\"distance_m\": 300.499"):json);
            Has(VenueValidator.Validate(wrong,Policy(),_=>true),"range.layout_anchor_mismatch");
            var mid=Plan((id,json)=>id=="junctions"?Regex.Replace(json,"(\"id\": \"lane_out\"[\\s\\S]*?\"distance_m\": )100.499","${1}50"):json);
            Has(VenueValidator.Validate(mid,Policy(),_=>true),"penalty.disconnected");
        }
        [Test] public void RenamingSharedEntryRegionsDoesNotCertifyIndependentLaneAccess()
        {
            var original=new VenueAuthoringPlan(VenueManifest.FromJson(VenueAuthoringTests.ManifestJson()),VenueAuthoringTests.Read);
            PhysicalJunction entry=null;for(int i=0;i<original.Junctions.Count;i++)if(original.Junctions.Get(i).Id=="range_access")entry=original.Junctions.Get(i);
            Assert.That(entry,Is.Not.Null);var groups=new List<string>();
            for(int i=1;i<entry.PortCount;i++)groups.Add("{\"id\":\"split_"+i+"\",\"ports\":["+Port(entry.GetPort(0),"range_main_entry_"+i)+","+Port(entry.GetPort(i),entry.GetPort(i).Id)+"]}");
            string junctionJson=Regex.Replace(VenueAuthoringTests.Read(original.Manifest.JunctionsResource),"\\{\\s*\"id\": \"range_access\",[\\s\\S]*?\\]\\s*\\}",string.Join(",",groups));
            string connectionJson=Regex.Replace(VenueAuthoringTests.Read(original.Manifest.ConnectionsResource),"\\{\\s*\"id\": \"range_lane_([0-9]+)_enter\"[\\s\\S]*?\\}",m=>m.Value.Replace("\"range_access\"","\"split_"+m.Groups[1].Value+"\"").Replace("\"range_main_entry\"","\"range_main_entry_"+m.Groups[1].Value+"\""));
            var plan=new VenueAuthoringPlan(original.Manifest,id=>id==original.Manifest.JunctionsResource?junctionJson:id==original.Manifest.ConnectionsResource?connectionJson:VenueAuthoringTests.Read(id));
            Has(VenueValidator.Validate(plan,Policy(true),_=>true),"range.split_entry_regions");
            string Port(PhysicalJunctionPort port,string id)=>"{\"id\":\""+id+"\",\"anchor\":{\"loop_id\":\""+port.Anchor.LoopId+"\",\"distance_m\":"+port.Anchor.Distance.Meters.ToString(CultureInfo.InvariantCulture)+"},\"guard_before_m\":"+port.GuardBefore.Meters.ToString(CultureInfo.InvariantCulture)+",\"guard_after_m\":"+port.GuardAfter.Meters.ToString(CultureInfo.InvariantCulture)+"}";
        }
    }
}
