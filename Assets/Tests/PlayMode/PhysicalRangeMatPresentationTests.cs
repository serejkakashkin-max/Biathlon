using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Adapter fixture only: intentionally not a production world-geometry or dense-field proof.</summary>
    public static class PathPresentationFixture
    {
        public static Dictionary<string,string> Resources()
        {
            string Read(string id)=>File.ReadAllText(Path.Combine(Application.dataPath,"..","tests","fixtures","venues","range_path_contract_v1",id+".json"));
            return new Dictionary<string,string>(StringComparer.Ordinal)
            {
                ["track"]=Read("track"),["junctions"]=Read("junctions"),["connections"]=Read("connections"),
                // Non-sorted stable IDs prove that logical ordinals retain the authored order.
                ["paths"]=Read("paths").Replace("\"lane_1\"","\"zulu_mat\"").Replace("\"lane_2\"","\"alpha_mat\""),
                ["occupancy"]=UnityEngine.Resources.Load<TextAsset>("Biathlon/occupancy_provisional_v1").text,
                ["visual"]=VisualJson(true),
                ["manifest"]=ManifestJson("venue_path_presentation_test_v2","{\"x\":0.6,\"y\":0,\"z\":0.8}")
            };
        }
        public static VenueAuthoringPlan Plan(Dictionary<string,string> resources=null)
        {
            resources=resources??Resources();
            return new VenueAuthoringPlan(VenueManifest.FromJson(resources["manifest"]),id=>resources[id]);
        }
        public static string ManifestJson(string id,string forward)
            =>"{\"schema_version\":2,\"id\":\""+id+"\",\"track_resource\":\"track\",\"visual_resource\":\"visual\",\"paths_resource\":\"paths\",\"occupancy_resource\":\"occupancy\",\"junctions_resource\":\"junctions\",\"connections_resource\":\"connections\",\"range_layout\":{\"approach\":\"side_entry\",\"entry\":{\"loop_id\":\"main\",\"distance_m\":0},\"exit\":{\"loop_id\":\"main\",\"distance_m\":100},\"firing_origin\":{\"x\":0,\"y\":0,\"z\":0},\"forward\":"+forward+",\"lane_pitch_m\":3,\"apron_depth_m\":10},\"cameras\":[{\"id\":\"range_overview\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":0},\"offset\":{\"x\":0,\"y\":10,\"z\":0},\"look_offset\":{\"x\":0,\"y\":0,\"z\":0},\"fov_deg\":50}],\"ambience\":[],\"assets\":[]}";
        public static string VisualJson(bool includeLastMat)
            =>"{\"schema_version\":1,\"id\":\"range_path_presentation_visual_v1\",\"track_id\":\"range_path_contract_track_v1\",\"sample_spacing_m\":1,\"loops\":["+
                "{\"loop_id\":\"main\",\"knots\":[{\"distance_m\":0,\"x\":0,\"y\":0,\"z\":0},{\"distance_m\":100,\"x\":100,\"y\":0,\"z\":0},{\"distance_m\":200,\"x\":100,\"y\":0,\"z\":100},{\"distance_m\":300,\"x\":0,\"y\":0,\"z\":0}]},"+
                "{\"loop_id\":\"mat_1\",\"knots\":[{\"distance_m\":0,\"x\":-20,\"y\":1,\"z\":5},{\"distance_m\":40,\"x\":20,\"y\":1,\"z\":5}]},"+
                (includeLastMat?"{\"loop_id\":\"mat_2\",\"knots\":[{\"distance_m\":0,\"x\":30,\"y\":3,\"z\":10},{\"distance_m\":40,\"x\":30,\"y\":3,\"z\":50}]},":"")+
                "{\"loop_id\":\"exit_aisle\",\"knots\":[{\"distance_m\":0,\"x\":-100,\"y\":0,\"z\":-10},{\"distance_m\":60,\"x\":-40,\"y\":0,\"z\":-10}]},"+
                "{\"loop_id\":\"penalty\",\"knots\":[{\"distance_m\":0,\"x\":-50,\"y\":0,\"z\":-50},{\"distance_m\":75,\"x\":-40,\"y\":0,\"z\":-50},{\"distance_m\":150,\"x\":-50,\"y\":0,\"z\":-50}]}]}";
        public static Vector3 Position(TrackVisualPoint point)=>new Vector3((float)point.X,(float)point.Y,(float)point.Z);
    }

    public class PhysicalRangeMatPresentationTests
    {
        [UnityTest] public IEnumerator ExplicitPhysicalMatKeepsAcceptedAnchorAndCorrectFootprint()
        {
            var resources=PathPresentationFixture.Resources();
            resources["manifest"]=resources["manifest"].Replace("\"apron_depth_m\":10", "\"apron_depth_m\":10,\"mat_definition\":{\"schema_version\":1,\"id\":\"test_mat_v1\",\"width_m\":1.5,\"length_m\":2,\"thickness_m\":0.015,\"athlete_stop_depth_m\":1}");
            var plan=PathPresentationFixture.Plan(resources);var root=new GameObject("Physical mat footprint test");
            try
            {
                var mats=root.AddComponent<RangeMatPresentation>();mats.Initialize(plan);
                for(int i=0;i<mats.Count;i++)
                {
                    Assert.That(mats.Anchor(i+1).localPosition,Is.EqualTo(PathPresentationFixture.Position(plan.MatPoint(i))));
                    var mat=mats.Anchor(i+1).GetChild(0);
                    Assert.That(mat.localScale,Is.EqualTo(new Vector3(1.5f,.015f,2)));
                    Assert.That(mat.localPosition,Is.EqualTo(new Vector3(0,-.0075f,0)));
                    Assert.That(mat.localPosition.y+mat.localScale.y*.5f,Is.Zero);
                    var material=mat.GetComponent<Renderer>().sharedMaterial;
                    Assert.That(material.shader.name,Is.EqualTo("Biathlon/PhysicalRangeMat"));
                    var expectedColour=Resources.Load<Material>("Biathlon/Stadium/rubber").GetColor("_BaseColor");
                    var actualColour=material.GetColor("_BaseColor");
                    // Unity colour properties round-trip through colour-space conversion (about 3e-8 here).
                    // Check every channel to float precision; this does not change any physical geometry check.
                    for(int channel=0;channel<4;channel++)
                        Assert.That(actualColour[channel],Is.EqualTo(expectedColour[channel]).Within(1e-7f));
                    var front=mat.localPosition.z+mat.localScale.z*.5f;
                    Assert.That(front,Is.EqualTo((float)plan.FiringDatumDepth));
                }
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))Assert.That(collider.enabled,Is.False,"Mat initialization must disable physics before deferred collider destruction.");
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator IndividualMatAnchorsAndExplicitShootingHeadingRemainReadOnly()
        {
            var plan=PathPresentationFixture.Plan();string track=plan.Track.ToJson(),paths=plan.Paths.ToJson(),visual=plan.Visual.ToJson();
            var root=new GameObject("Physical path mat adapter test");
            try
            {
                var mats=root.AddComponent<RangeMatPresentation>();mats.Initialize(plan);
                Assert.That(mats.Count,Is.EqualTo(2));Assert.That(mats.LaneId(1),Is.EqualTo("zulu_mat"));Assert.That(mats.LaneId(2),Is.EqualTo("alpha_mat"));
                Assert.That(mats.Anchor(1).localPosition,Is.EqualTo(new Vector3(-2,1,5)));Assert.That(mats.Anchor(2).localPosition,Is.EqualTo(new Vector3(30,3,28)));
                // Neither the incoming tangent nor the legacy scalar firing-line distance is the shooting heading/anchor.
                Assert.That(plan.Paths.GetLane(0).FiringOffset.Meters,Is.EqualTo(28));Assert.That(plan.Paths.GetLane(1).FiringOffset.Meters,Is.EqualTo(48));
                Assert.That(plan.Track.Range.FiringLine.Distance.Meters,Is.EqualTo(40));
                for(int i=0;i<mats.Count;i++)
                {
                    Assert.That(Vector3.Distance(mats.Anchor(i+1).forward,new Vector3(.6f,0,.8f)),Is.LessThan(1e-6f));
                    var tangent=plan.MatPoint(i);Assert.That(Vector3.Distance(mats.Anchor(i+1).forward,new Vector3((float)tangent.ForwardX,0,(float)tangent.ForwardZ)),Is.GreaterThan(.1f));
                    var mat=mats.Anchor(i+1).GetChild(0);var art=StadiumArtDefinition.Load();
                    Assert.That(mat.localPosition,Is.EqualTo(new Vector3(0,-.022f,0)));Assert.That(mat.localScale,Is.EqualTo(new Vector3(art.mat_width_m,.05f,art.mat_length_m)));
                }
                Assert.Throws<ArgumentOutOfRangeException>(()=>mats.Anchor(0));Assert.Throws<ArgumentOutOfRangeException>(()=>mats.LaneId(3));
                Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Paths.ToJson(),Is.EqualTo(paths));Assert.That(plan.Visual.ToJson(),Is.EqualTo(visual));
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))Assert.That(collider.enabled,Is.False);
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator LegacyPlanAndExistingThreeArgumentCallKeepAllThirtyAnchorsAndTangents()
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(UnityEngine.Resources.Load<TextAsset>("Biathlon/Venues/venue_pipeline_fixture_v1").text),id=>UnityEngine.Resources.Load<TextAsset>(id).text);
            var planned=new GameObject("Legacy plan mats test");var direct=new GameObject("Legacy direct mats test");
            try
            {
                var a=planned.AddComponent<RangeMatPresentation>();a.Initialize(plan);var b=direct.AddComponent<RangeMatPresentation>();b.Initialize(plan.Track,plan.Visual,plan.Lanes);
                Assert.That(a.Count,Is.EqualTo(30));Assert.That(b.Count,Is.EqualTo(a.Count));
                for(int i=0;i<a.Count;i++)
                {
                    var point=plan.MatPoint(i);Assert.That(a.Anchor(i+1).localPosition,Is.EqualTo(PathPresentationFixture.Position(point)));
                    Assert.That(a.Anchor(i+1).localPosition,Is.EqualTo(b.Anchor(i+1).localPosition));Assert.That(a.Anchor(i+1).localRotation,Is.EqualTo(b.Anchor(i+1).localRotation));
                    Assert.That(a.LaneId(i+1),Is.EqualTo(plan.Lanes.GetLane(i)));
                    Assert.That(Vector3.Distance(a.Anchor(i+1).forward,new Vector3((float)point.ForwardX,0,(float)point.ForwardZ)),Is.LessThan(1e-6f));
                }
                foreach(var collider in planned.GetComponentsInChildren<Collider>(true))Assert.That(collider.enabled,Is.False);
                foreach(var collider in direct.GetComponentsInChildren<Collider>(true))Assert.That(collider.enabled,Is.False);
                yield return null;Assert.That(planned.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(direct.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{Object.Destroy(planned);Object.Destroy(direct);}
        }
        [UnityTest] public IEnumerator MissingFinalMatLoopRejectsWithoutPartialChildrenAndAllowsFreshRetry()
        {
            var plan=PathPresentationFixture.Plan();var incomplete=TrackVisualDefinition.FromJson(PathPresentationFixture.VisualJson(false),plan.Track);var root=new GameObject("Rejected mat fixture test");
            try
            {
                var mats=root.AddComponent<RangeMatPresentation>();Assert.Throws<ArgumentException>(()=>mats.Initialize(plan.Track,incomplete,plan.Paths,plan.Manifest.Range.Forward));
                Assert.That(mats.Count,Is.Zero);Assert.That(root.transform.childCount,Is.Zero);
                mats.Initialize(plan.Track,plan.Visual,plan.Paths,plan.Manifest.Range.Forward);Assert.That(mats.Count,Is.EqualTo(2));
                var first=mats.Anchor(1);Assert.Throws<ArgumentException>(()=>mats.Initialize(plan));Assert.That(mats.Anchor(1),Is.SameAs(first));Assert.That(root.transform.childCount,Is.EqualTo(2));
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator MismatchedTrackAndInvalidExplicitDirectionsFailBeforeMutation()
        {
            var plan=PathPresentationFixture.Plan();var root=new GameObject("Mat binding validation test");
            try
            {
                var mats=root.AddComponent<RangeMatPresentation>();var foreign=TrackDefinition.FromJson(plan.Track.ToJson().Replace("\"width_m\":6","\"width_m\":7"));
                Assert.That(foreign.ToJson(),Is.Not.EqualTo(plan.Track.ToJson()));Assert.That(foreign.Id,Is.EqualTo(plan.Track.Id));
                Assert.Throws<ArgumentException>(()=>mats.Initialize(foreign,plan.Visual,plan.Paths,plan.Manifest.Range.Forward));
                foreach(var json in new[]{"{\"x\":0,\"y\":0,\"z\":0}","{\"x\":0,\"y\":0,\"z\":2}","{\"x\":0,\"y\":1,\"z\":1}"})
                {
                    var invalid=VenueManifest.FromJson(PathPresentationFixture.ManifestJson("invalid_heading_test",json)).Range.Forward;
                    Assert.Throws<ArgumentException>(()=>mats.Initialize(plan.Track,plan.Visual,plan.Paths,invalid));
                }
                Assert.Throws<ArgumentNullException>(()=>mats.Initialize((VenueAuthoringPlan)null));Assert.That(mats.Count,Is.Zero);Assert.That(root.transform.childCount,Is.Zero);
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}
