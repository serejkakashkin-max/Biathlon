using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Biathlon.Domain;

/// <summary>The existing one-lane VenuePathAuthoringTests geometry, kept as a
/// harness-local tiny fixture. It is never evidence of production field capacity.</summary>
internal static class TinyVenueFixture
{
    internal static string Write(string project,string output)
    {
        string root=Path.Combine(output,"tiny-resources");Directory.CreateDirectory(root);
        var resources=new Dictionary<string,string>(StringComparer.Ordinal);
        resources["manifest"]=File.ReadAllText(Path.Combine(project,"tests/fixtures/venues/validator_valid/manifest.json"))
            .Replace("\"schema_version\": 1","\"schema_version\": 2")
            .Replace("\"lanes_resource\": \"lanes\"","\"paths_resource\": \"paths\",\"occupancy_resource\":\"occupancy\"")
            .Replace("100.499","60").Replace("\"apron_depth_m\": 10","\"apron_depth_m\": 30");
        resources["occupancy"]=File.ReadAllText(Path.Combine(project,"Assets/Resources/Biathlon/occupancy_provisional_v1.json"));
        resources["track"]="{\"schema_version\":1,\"id\":\"world_paths_track_v1\",\"authoring_limits\":{\"max_abs_grade\":0.25,\"max_abs_curvature_per_m\":1,\"min_width_m\":1,\"max_width_m\":20},\"loops\":["+
            Loop("main",true,6,new double[]{10,10,20,10,10,30,30,40})+","+Loop("mat",false,2.5,new double[]{10,10,20})+","+Loop("exit_aisle",false,6,new double[]{20})+","+
            Loop("penalty",true,4,new[]{37.5,37.5,37.5,37.5},true)+"],\"checkpoints\":[],\"range\":{\"id\":\"range\",\"entry\":"+Anchor("main",1)+
            ",\"firing_line\":"+Anchor("main",30)+",\"exit\":"+Anchor("main",55)+"},\"penalty\":{\"loop_id\":\"penalty\",\"entry\":"+Anchor("main",60)+",\"rejoin\":"+Anchor("main",60)+"}}";
        resources["visual"]="{\"schema_version\":1,\"id\":\"world_paths_visual_v1\",\"track_id\":\"world_paths_track_v1\",\"sample_spacing_m\":1,\"loops\":["+
            Visual("main",new[]{new[]{0d,10,0,-10},new[]{10d,0,0,-10},new[]{20d,-10,0,-10},new[]{40d,-30,0,-10},new[]{50d,-30,0,-20},new[]{60d,-20,0,-20},new[]{90d,-20,0,-50},new[]{120d,10,0,-50},new[]{160d,10,0,-10}})+","+
            Visual("mat",new[]{new[]{0d,0,0,-10},new[]{10d,0,0,0},new[]{20d,-10,0,0},new[]{40d,-10,0,-20}})+","+
            Visual("exit_aisle",new[]{new[]{0d,0,0,-20},new[]{20d,-20,0,-20}})+","+
            Visual("penalty",new[]{new[]{0d,-20,0,-20},new[]{37.5,-57.5,0,-20},new[]{75d,-57.5,0,17.5},new[]{112.5,-20,0,17.5},new[]{150d,-20,0,-20}})+"]}";
        var ports=new[]{Port("fork","main",10,40,40),Port("mat_in","mat",0,0,39.999),Port("mat_out","mat",40,39.999,0),Port("exit_in","exit_aisle",10,10,9.999),
            Port("exit_out","exit_aisle",20,19.999,0),Port("exit_zero","exit_aisle",0,0,19.999),Port("rejoin","main",60,40,40),Port("penalty","penalty",0,74,74),Port("penalty_cross","penalty",140,74,74)};
        resources["junctions"]="{\"schema_version\":1,\"id\":\"world_paths_junctions_v1\",\"track_id\":\"world_paths_track_v1\",\"junctions\":[{\"id\":\"local_single_lane_region\",\"ports\":["+string.Join(",",ports)+"]}]}";
        resources["connections"]="{\"schema_version\":1,\"id\":\"world_paths_connections_v1\",\"junction_definition_id\":\"world_paths_junctions_v1\",\"connections\":["+
            Connection("enter","fork","mat_in")+","+Connection("leave","mat_out","exit_in")+","+Connection("rejoin","exit_out","rejoin")+","+Connection("penalty_enter","exit_out","penalty")+","+Connection("penalty_leave","penalty","rejoin")+"]}";
        resources["paths"]="{\"schema_version\":1,\"id\":\"world_paths_v1\",\"track_id\":\"world_paths_track_v1\",\"main_loop_id\":\"main\",\"entry_distance_m\":0,\"exit_distance_m\":60,\"lanes\":[{\"id\":\"lane_1\",\"slices\":[{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":10},{\"loop_id\":\"mat\",\"start_m\":0,\"end_m\":40},{\"loop_id\":\"exit_aisle\",\"start_m\":10,\"end_m\":20}],\"mat_anchor\":"+Anchor("mat",10)+"}]}";
        resources["profile"]=Regex.Replace(File.ReadAllText(Path.Combine(project,"Assets/Resources/Biathlon/sprint_test_v1.json")),"(\"distance_m\":)3000","${1}480");
        var profile=CompetitionProfile.FromJson(resources["profile"]);
        resources["route"]=RaceRoutePlan.TimeTrial("tiny_stream_replay_route_v1","world_paths_track_v1","main",profile.ToRouteRequirements()).ToJson();
        resources["pursuit_profile"]=Regex.Replace(File.ReadAllText(Path.Combine(project,"Assets/Resources/Biathlon/pursuit_test_v1.json")),"(\"distance_m\":)[0-9.]+","${1}800");
        var pursuit=CompetitionProfile.FromJson(resources["pursuit_profile"]);
        resources["pursuit_route"]=RaceRoutePlan.TimeTrial("tiny_stream_pursuit_route_v1","world_paths_track_v1","main",pursuit.ToRouteRequirements()).ToJson();
        resources["conditions"]=File.ReadAllText(Path.Combine(project,"Assets/Resources/Biathlon/race_snow_neutral_v1.json")).Replace("snow_test_packed","packed");
        foreach(var pair in resources)File.WriteAllText(Path.Combine(root,pair.Key+".json"),pair.Value);
        return root;
    }
    private static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
    private static string Anchor(string loop,double distance)=>"{\"loop_id\":\""+loop+"\",\"distance_m\":"+N(distance)+"}";
    private static string Port(string id,string loop,double distance,double before,double after)=>"{\"id\":\""+id+"\",\"anchor\":"+Anchor(loop,distance)+",\"guard_before_m\":"+N(before)+",\"guard_after_m\":"+N(after)+"}";
    private static string Connection(string id,string from,string to)=>"{\"id\":\""+id+"\",\"junction_id\":\"local_single_lane_region\",\"from_port_id\":\""+from+"\",\"to_port_id\":\""+to+"\",\"kind\":\"coincident_seam\"}";
    private static string Loop(string id,bool closed,double width,double[] lengths,bool penalty=false)
    {
        var segments=new string[lengths.Length];for(int i=0;i<segments.Length;i++)segments[i]="{\"id\":\"segment_"+i+"\",\"length_m\":"+N(lengths[i])+",\"start_grade\":0,\"end_grade\":0,\"curvature_per_m\":0,\"width_m\":"+N(width)+",\"snow_profile_id\":\"packed\",\"wind_exposure\":1,\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false,\"risk_level\":0,\"semantic_zone\":\""+(penalty?"penalty_loop":"range_approach")+"\"}";
        return "{\"id\":\""+id+"\",\"kind\":\""+(penalty?"penalty":"race")+"\",\"closed\":"+(closed?"true":"false")+",\"segments\":["+string.Join(",",segments)+"]}";
    }
    private static string Visual(string id,double[][] knots)
    {var points=new string[knots.Length];for(int i=0;i<points.Length;i++)points[i]="{\"distance_m\":"+N(knots[i][0])+",\"x\":"+N(knots[i][1])+",\"y\":"+N(knots[i][2])+",\"z\":"+N(knots[i][3])+"}";return "{\"loop_id\":\""+id+"\",\"knots\":["+string.Join(",",points)+"]}";}
}
