using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenuePathWorldValidationTests
    {
        [TestCase(true,true)][TestCase(false,true)][TestCase(true,false)]
        public void ProductionRangeOverlapRequiresEitherCompleteWorldAdmissionOrWholeLocalGuards(bool completeWorld,bool capturedStart)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)
            {
                string json=PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
                if(!capturedStart&&id==root+"manifest")json=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"start_continuation_resource\"\\s*:\\s*\"[^\"]*\"","");
                return !completeWorld&&id==root+"shared_apron"?System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"penalty_world_merge_junctions\"\\s*:\\s*\\[[^\\]]*\\]",""):json;
            }
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var report=VenueValidator.Validate(plan,VenueValidationPolicy.FromJson(Read("Biathlon/Venues/production_venue_validation_v1")),_=>true);
            Assert.That(report.Passed,Is.EqualTo(completeWorld&&capturedStart));
            if(!completeWorld||!capturedStart){bool extent=false;for(int i=0;i<report.Count;i++)extent|=report.Get(i).Code=="junction.guard_extent";Assert.That(extent,Is.True,"Absent captured world-body admission must retain full guard requirements.");}
        }
        private static VenueValidationReport Validate(Dictionary<string,string> resources=null,bool production=false,bool assets=true)
            =>VenueValidator.Validate(VenuePathAuthoringTests.Plan(resources),VenueValidatorTests.Policy(production),_=>assets);
        private static bool Has(VenueValidationReport report,string code)
        {for(int i=0;i<report.Count;i++)if(report.Get(i).Code==code)return true;return false;}
        private static void Expect(VenueValidationReport report,string code)=>Assert.That(Has(report,code),Is.True,report.ToJson());

        [Test] public void AuthoredPathWorldSeamsMatsCrossingsAndPenaltyPassWithoutProductionClaim()
        {var report=Validate();Assert.That(report.Passed,Is.True,report.ToJson());Expect(Validate(production:true),"range.capacity");}
        [Test] public void MatUsesAuthoredAnchorInsteadOfLegacyScalarFiringLine()
        {
            var resources=VenuePathAuthoringTests.Resources();resources["track"]=resources["track"].Replace("\"firing_line\":{\"loop_id\":\"main\",\"distance_m\":30}","\"firing_line\":{\"loop_id\":\"main\",\"distance_m\":45}");
            Assert.That(Validate(resources).Passed,Is.True,Validate(resources).ToJson());
            resources["paths"]=resources["paths"].Replace("\"mat_anchor\":{\"loop_id\":\"mat\",\"distance_m\":10}","\"mat_anchor\":{\"loop_id\":\"mat\",\"distance_m\":12}");Expect(Validate(resources),"range.mat_alignment");
        }
        [Test] public void SeamGapWrongLayoutMissingAssetsAndPenaltyConnectivityAreReported()
        {
            var resources=VenuePathAuthoringTests.Resources();resources["visual"]=resources["visual"].Replace("\"loop_id\":\"mat\",\"knots\":[{\"distance_m\":0,\"x\":0","\"loop_id\":\"mat\",\"knots\":[{\"distance_m\":0,\"x\":0.2");Expect(Validate(resources),"connection.world_gap");
            resources=VenuePathAuthoringTests.Resources();resources["manifest"]=resources["manifest"].Replace("\"distance_m\": 60","\"distance_m\": 90");Expect(Validate(resources),"range.layout_anchor_mismatch");
            Expect(Validate(assets:false),"asset.missing");resources=VenuePathAuthoringTests.Resources();resources["connections"]=resources["connections"].Replace(","+VenuePathAuthoringTests.Connection("penalty_enter","exit_out","penalty"),"");Expect(Validate(resources),"penalty.disconnected");
        }
        [Test] public void ActualWidthAndConfiguredBodyMarginRequireCompleteGuards()
        {
            var resources=VenuePathAuthoringTests.Resources();resources["junctions"]=Regex.Replace(resources["junctions"],"(\\\"guard_(?:before|after)_m\\\":)([0-9.]+)",m=>m.Groups[1].Value+(double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture)>0?"0.1":"0"));
            Expect(Validate(resources),"junction.guard_extent");
            var original=VenuePathAuthoringTests.Resources();original["occupancy"]=original["occupancy"].Replace("\"body_length_m\": 1.4","\"body_length_m\": 80");Expect(Validate(original),"junction.guard_extent");
        }
        [Test] public void CrossLoopTrafficCannotHideBehindDifferentPhysicalIdentifiers()
        {var resources=WithCrossing(false,0);Expect(Validate(resources),"junction.unprotected_crossing");Assert.That(Validate(WithCrossing(true,0)).Passed,Is.True,Validate(WithCrossing(true,0)).ToJson());}
        [Test] public void ParallelOverlappingCorridorsNeedProtectionEvenWithoutCentrelineIntersection()
        {
            var resources=WithCrossing(false,0);
            resources["visual"]=resources["visual"].Replace(VenuePathAuthoringTests.Visual("crossing",CrossingKnots(0)),VenuePathAuthoringTests.Visual("crossing",new[]{new[]{0d,0,0,-18},new[]{10d,-10,0,-18},new[]{11d,-10,0,-19},new[]{21d,0,0,-19},new[]{22d,0,0,-18}}));
            resources["track"]=resources["track"].Replace(VenuePathAuthoringTests.Loop("crossing",true,6,10,10,10,10),VenuePathAuthoringTests.Loop("crossing",true,6,10,1,10,1));
            Expect(Validate(resources),"junction.unprotected_crossing");
        }
        [Test] public void PhysicallySeparatedBridgeDoesNotNeedFalseCrossingGuard()
        {var report=Validate(WithCrossing(false,10));Assert.That(report.Passed,Is.True,report.ToJson());}
        [Test] public void NonAdjacentSelfCrossingNeedsSharedGuardAtBothCoordinates()
        {
            var resources=VenuePathAuthoringTests.Resources();
            resources["track"]=resources["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("self_crossing",true,6,28.284,20,28.284,20)+",");
            resources["visual"]=resources["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("self_crossing",new[]{new[]{0d,-50,0,-50},new[]{28.284,-30,0,-30},new[]{48.284,-50,0,-30},new[]{76.568,-30,0,-50},new[]{96.568,-50,0,-50}})+",");
            Expect(Validate(resources),"junction.unprotected_crossing");
            resources["junctions"]=resources["junctions"].Replace("\"ports\":[","\"ports\":["+VenuePathAuthoringTests.Port("self_0","self_crossing",0,48,48)+","+VenuePathAuthoringTests.Port("self_middle","self_crossing",48.284,48,48)+",");
            var report=Validate(resources);Assert.That(report.Passed,Is.True,report.ToJson());
        }
        [Test] public void OneExclusiveRegionCannotCertifyTwoIndependentMatForks()
        {var report=Validate(WithSecondLane());Expect(report,"range.shared_entry_gate");}
        [Test] public void SharedExitAisleRetainsMainCorridorMinimumWidth()
        {
            var resources=WithSecondLane();resources["track"]=resources["track"].Replace(VenuePathAuthoringTests.Loop("exit_aisle",false,6,20),VenuePathAuthoringTests.Loop("exit_aisle",false,2.5,20));
            var report=Validate(resources);Expect(report,"width.minimum");bool exit=false;for(int i=0;i<report.Count;i++)exit|=report.Get(i).Code=="width.minimum"&&report.Get(i).Path.StartsWith("loops/exit_aisle/",StringComparison.Ordinal);Assert.That(exit,Is.True,report.ToJson());
        }
        [Test] public void RepeatedValidationIsCultureIndependentAndDoesNotMutateBindings()
        {
            var plan=VenuePathAuthoringTests.Plan(WithCrossing(false,0));string track=plan.Track.ToJson(),paths=plan.Paths.ToJson(),junctions=plan.Junctions.ToJson(),occupancy=plan.Occupancy.ToJson();var policy=VenueValidatorTests.Policy();string expected=VenueValidator.Validate(plan,policy,_=>true).ToJson();
            var previous=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=new CultureInfo("ru-RU");Assert.That(VenueValidator.Validate(plan,policy,_=>true).ToJson(),Is.EqualTo(expected));}finally{CultureInfo.CurrentCulture=previous;}
            Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Paths.ToJson(),Is.EqualTo(paths));Assert.That(plan.Junctions.ToJson(),Is.EqualTo(junctions));Assert.That(plan.Occupancy.ToJson(),Is.EqualTo(occupancy));
        }
        private static double[][] CrossingKnots(double height)=>new[]{new[]{0d,-8,height,-5},new[]{10d,-8,height,-15},new[]{20d,-18,height,-15},new[]{30d,-18,height,-5},new[]{40d,-8,height,-5}};
        private static Dictionary<string,string> WithCrossing(bool guarded,double height)
        {
            var resources=VenuePathAuthoringTests.Resources();resources["track"]=resources["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("crossing",true,6,10,10,10,10)+",");resources["visual"]=resources["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("crossing",CrossingKnots(height))+",");
            if(guarded)resources["junctions"]=resources["junctions"].Replace("\"ports\":[","\"ports\":["+VenuePathAuthoringTests.Port("crossing_0","crossing",0,19.999,19.999)+","+VenuePathAuthoringTests.Port("crossing_20","crossing",20,19.999,19.999)+",");return resources;
        }
        private static Dictionary<string,string> WithSecondLane()
        {
            var resources=VenuePathAuthoringTests.Resources();resources["track"]=resources["track"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Loop("mat_2",false,2.5,10,10,20)+",");resources["visual"]=resources["visual"].Replace("\"loops\":[","\"loops\":["+VenuePathAuthoringTests.Visual("mat_2",new[]{new[]{0d,0,0,-10},new[]{10d,0,0,0},new[]{20d,-10,0,0},new[]{40d,-10,0,-20}})+",");
            resources["paths"]=resources["paths"].Replace("\"lanes\":[","\"lanes\":["+VenuePathAuthoringTests.Lane("lane_2","mat_2")+",");resources["junctions"]=resources["junctions"].Replace("\"ports\":[","\"ports\":["+VenuePathAuthoringTests.Port("mat_2_in","mat_2",0,0,39.999)+","+VenuePathAuthoringTests.Port("mat_2_out","mat_2",40,39.999,0)+",");resources["connections"]=resources["connections"].Replace("\"connections\":[","\"connections\":["+VenuePathAuthoringTests.Connection("enter_2","fork","mat_2_in")+","+VenuePathAuthoringTests.Connection("leave_2","mat_2_out","exit_in")+",");return resources;
        }
    }
}
