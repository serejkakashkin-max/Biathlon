using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenuePathAuthoringTests
    {
        // One-lane world/guard fixture only. Its deliberately broad single-lane regions
        // do not certify a 30-mat production stadium or runtime body admission.
        public static Dictionary<string,string> Resources()
        {
            var resources=new Dictionary<string,string>(StringComparer.Ordinal);
            resources["manifest"]=VenueValidatorTests.Read("manifest").Replace("\"schema_version\": 1","\"schema_version\": 2")
                .Replace("\"lanes_resource\": \"lanes\"","\"paths_resource\": \"paths\",\"occupancy_resource\":\"occupancy\"")
                .Replace("100.499","60").Replace("\"apron_depth_m\": 10","\"apron_depth_m\": 30");
            resources["occupancy"]=PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json");
            resources["track"]="{\"schema_version\":1,\"id\":\"world_paths_track_v1\",\"authoring_limits\":{\"max_abs_grade\":0.25,\"max_abs_curvature_per_m\":1,\"min_width_m\":1,\"max_width_m\":20},\"loops\":["+
                Loop("main",true,6,10,10,20,10,10,30,30,40)+","+Loop("mat",false,2.5,10,10,20)+","+Loop("exit_aisle",false,6,20)+","+Loop("penalty",true,4,37.5,37.5,37.5,37.5,true)+
                "],\"checkpoints\":[],\"range\":{\"id\":\"range\",\"entry\":"+Anchor("main",1)+",\"firing_line\":"+Anchor("main",30)+",\"exit\":"+Anchor("main",55)+"},\"penalty\":{\"loop_id\":\"penalty\",\"entry\":"+Anchor("main",60)+",\"rejoin\":"+Anchor("main",60)+"}}";
            resources["visual"]="{\"schema_version\":1,\"id\":\"world_paths_visual_v1\",\"track_id\":\"world_paths_track_v1\",\"sample_spacing_m\":1,\"loops\":["+
                Visual("main",new[]{new[]{0d,10,0,-10},new[]{10d,0,0,-10},new[]{20d,-10,0,-10},new[]{40d,-30,0,-10},new[]{50d,-30,0,-20},new[]{60d,-20,0,-20},new[]{90d,-20,0,-50},new[]{120d,10,0,-50},new[]{160d,10,0,-10}})+","+
                Visual("mat",new[]{new[]{0d,0,0,-10},new[]{10d,0,0,0},new[]{20d,-10,0,0},new[]{40d,-10,0,-20}})+","+
                Visual("exit_aisle",new[]{new[]{0d,0,0,-20},new[]{20d,-20,0,-20}})+","+
                Visual("penalty",new[]{new[]{0d,-20,0,-20},new[]{37.5,-57.5,0,-20},new[]{75d,-57.5,0,17.5},new[]{112.5,-20,0,17.5},new[]{150d,-20,0,-20}})+"]}";
            var ports=new[]{Port("fork","main",10,40,40),Port("mat_in","mat",0,0,39.999),Port("mat_out","mat",40,39.999,0),
                Port("exit_in","exit_aisle",10,10,9.999),Port("exit_out","exit_aisle",20,19.999,0),Port("exit_zero","exit_aisle",0,0,19.999),
                Port("rejoin","main",60,40,40),Port("penalty","penalty",0,74,74),Port("penalty_cross","penalty",140,74,74)};
            resources["junctions"]="{\"schema_version\":1,\"id\":\"world_paths_junctions_v1\",\"track_id\":\"world_paths_track_v1\",\"junctions\":[{\"id\":\"local_single_lane_region\",\"ports\":["+string.Join(",",ports)+"]}]}";
            resources["connections"]="{\"schema_version\":1,\"id\":\"world_paths_connections_v1\",\"junction_definition_id\":\"world_paths_junctions_v1\",\"connections\":["+
                Connection("enter","fork","mat_in")+","+Connection("leave","mat_out","exit_in")+","+Connection("rejoin","exit_out","rejoin")+","+Connection("penalty_enter","exit_out","penalty")+","+Connection("penalty_leave","penalty","rejoin")+"]}";
            resources["paths"]="{\"schema_version\":1,\"id\":\"world_paths_v1\",\"track_id\":\"world_paths_track_v1\",\"main_loop_id\":\"main\",\"entry_distance_m\":0,\"exit_distance_m\":60,\"lanes\":["+Lane("lane_1","mat")+"]}";
            return resources;
        }
        public static VenueAuthoringPlan Plan(Dictionary<string,string> resources=null)
        {resources=resources??Resources();return new VenueAuthoringPlan(VenueManifest.FromJson(resources["manifest"]),id=>resources[id]);}
        public static string Anchor(string loop,double distance)=>"{\"loop_id\":\""+loop+"\",\"distance_m\":"+N(distance)+"}";
        public static string Port(string id,string loop,double distance,double before,double after)=>"{\"id\":\""+id+"\",\"anchor\":"+Anchor(loop,distance)+",\"guard_before_m\":"+N(before)+",\"guard_after_m\":"+N(after)+"}";
        public static string Connection(string id,string from,string to)=>"{\"id\":\""+id+"\",\"junction_id\":\"local_single_lane_region\",\"from_port_id\":\""+from+"\",\"to_port_id\":\""+to+"\",\"kind\":\"coincident_seam\"}";
        public static string Lane(string id,string loop)=>"{\"id\":\""+id+"\",\"slices\":[{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":10},{\"loop_id\":\""+loop+"\",\"start_m\":0,\"end_m\":40},{\"loop_id\":\"exit_aisle\",\"start_m\":10,\"end_m\":20}],\"mat_anchor\":"+Anchor(loop,10)+"}";
        public static string Loop(string id,bool closed,double width,params double[] lengths)=>Loop(id,closed,width,lengths,false);
        private static string Loop(string id,bool closed,double width,double a,double b,double c,double d,bool penalty)=>Loop(id,closed,width,new[]{a,b,c,d},penalty);
        private static string Loop(string id,bool closed,double width,double[] lengths,bool penalty)
        {
            var segments=new string[lengths.Length];for(int i=0;i<segments.Length;i++)segments[i]="{\"id\":\"segment_"+i+"\",\"length_m\":"+N(lengths[i])+",\"start_grade\":0,\"end_grade\":0,\"curvature_per_m\":0,\"width_m\":"+N(width)+",\"snow_profile_id\":\"packed\",\"wind_exposure\":1,\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false,\"risk_level\":0,\"semantic_zone\":\""+(penalty?"penalty_loop":"range_approach")+"\"}";
            return "{\"id\":\""+id+"\",\"kind\":\""+(penalty?"penalty":"race")+"\",\"closed\":"+(closed?"true":"false")+",\"segments\":["+string.Join(",",segments)+"]}";
        }
        public static string Visual(string id,double[][] knots)
        {var points=new string[knots.Length];for(int i=0;i<points.Length;i++)points[i]="{\"distance_m\":"+N(knots[i][0])+",\"x\":"+N(knots[i][1])+",\"y\":"+N(knots[i][2])+",\"z\":"+N(knots[i][3])+"}";return "{\"loop_id\":\""+id+"\",\"knots\":["+string.Join(",",points)+"]}";}
        private static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);

        [Test] public void ExplicitPathBranchBindsCanonicalOccupancyAndIndividualMatAnchor()
        {
            var plan=Plan();Assert.That(plan.UsesPhysicalPaths,Is.True);Assert.That(plan.Lanes,Is.Null);Assert.That(plan.LaneCount,Is.EqualTo(1));Assert.That(plan.LaneId(0),Is.EqualTo("lane_1"));
            Assert.That(plan.MatPoint(0).X,Is.Zero);Assert.That(plan.MatPoint(0).Z,Is.Zero);Assert.That(plan.Paths.GetLane(0).FiringOffset.Meters,Is.EqualTo(20));
            Assert.That(plan.Track.Range.FiringLine.Distance.Meters,Is.EqualTo(30));Assert.That(plan.Occupancy.ToJson(),Is.EqualTo(OccupancyDefinition.FromJson(Resources()["occupancy"]).ToJson()));
            var roundtrip=new VenueAuthoringPlan(VenueManifest.FromJson(plan.Manifest.ToJson()),id=>Resources()[id]);Assert.That(roundtrip.Manifest.ToJson(),Is.EqualTo(plan.Manifest.ToJson()));Assert.That(roundtrip.Paths.ToJson(),Is.EqualTo(plan.Paths.ToJson()));
        }
        [Test] public void LegacyBranchRemainsLanesOnlyAndCanonicalOutputHasNoNewFields()
        {var plan=VenueValidatorTests.Plan();Assert.That(plan.UsesPhysicalPaths,Is.False);Assert.That(plan.Paths,Is.Null);Assert.That(plan.Occupancy,Is.Null);Assert.That(plan.LaneCount,Is.EqualTo(plan.Lanes.Count));Assert.That(plan.Manifest.ToJson(),Does.Not.Contain("paths_resource"));Assert.That(plan.Manifest.ToJson(),Does.Not.Contain("occupancy_resource"));}
        [TestCase("paths_resource")][TestCase("occupancy_resource")]
        public void MissingOrUnsafeNewResourcesFailBeforeReader(string field)
        {string json=Resources()["manifest"];Assert.Catch(()=>VenueManifest.FromJson(Regex.Replace(json,"\\\""+field+"\\\"\\s*:\\s*\\\"[^\\\"]*\\\"",'"'+field+"\":\"../outside\"")));Assert.Catch(()=>VenueManifest.FromJson(Regex.Replace(json,"\\\""+field+"\\\"\\s*:\\s*\\\"[^\\\"]*\\\",?","")));}
        [Test] public void VersionsCannotMixLegacyAndPathBranches()
        {string path=Resources()["manifest"];Assert.Catch(()=>VenueManifest.FromJson(path.Replace("\"paths_resource\": \"paths\"","\"paths_resource\": \"paths\",\"lanes_resource\":\"lanes\"")));Assert.Catch(()=>VenueManifest.FromJson(path.Replace("\"schema_version\": 2","\"schema_version\": 1")));Assert.Catch(()=>VenueManifest.FromJson(VenueValidatorTests.Read("manifest").Replace("\"schema_version\": 1","\"schema_version\": 2")));}
    }
}
