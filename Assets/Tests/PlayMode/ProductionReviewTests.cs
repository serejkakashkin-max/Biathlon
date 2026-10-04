using System;
using System.Collections;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class ProductionReviewTests
    {
        private static ProductionReviewDefinition Definition(string venue,int count=1)=>ProductionReviewDefinition.FromJson(new ProductionReviewDefinition{schema_version=1,id="review_fixture_v1",venue_id=venue,initial_entry_id="sprint40",initial_quality_tier="high",entries=new[]{
            new ProductionReviewEntry{id="mass30",label="Mass30",scene_path="Assets/Scenes/Production/FixtureMass.unity",format_id="mass_start",field_count=count},
            new ProductionReviewEntry{id="sprint40",label="Sprint40",scene_path="Assets/Scenes/Production/FixtureSprint.unity",format_id="sprint",field_count=count}}}.ToJson());
        private static ProductionVenueHost Host(GameObject root)
        {
            var plan=PathPresentationFixture.Plan();var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("review_parity_fixture_route",plan.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.Initialize(plan,profile,route,conditions,count:1);return host;
        }
        [Test] public void DefinitionRejectsUnknownDuplicateNullAndTraversalBeforeAnyGlobalChange()
        {
            var baseline=QualitySettings.renderPipeline;string json=Definition("venue").ToJson();
            foreach(var bad in new[]{"null","{}",json.Replace("\"schema_version\":1","\"schema_version\":2"),json.Replace("\"sprint40\"","\"mass30\""),json.Replace("FixtureSprint.unity","../FixtureSprint.unity"),json.Replace("\"high\"","\"ultra\""),json.Replace("\"field_count\":1","\"field_count\":0")})
                Assert.Throws<ArgumentException>(()=>ProductionReviewDefinition.FromJson(bad));
            Assert.That(ProductionVenueHost.ReadOptionalReview(null),Is.Null);Assert.That(ProductionVenueHost.ReadOptionalReview(""),Is.Null);
            Assert.Throws<ArgumentException>(()=>Definition("venue").Get("unknown"));Assert.That(QualitySettings.renderPipeline,Is.SameAs(baseline));
        }
        [UnityTest] public IEnumerator ResourceHighAppliesBeforeFirstFrameAndPresentationControlsKeepPrefixExact()
        {
            var baseline=QualitySettings.renderPipeline;var a=new GameObject("Reviewed parity fixture");var b=new GameObject("Legacy parity fixture");
            try
            {
                var reviewed=Host(a);var quality=reviewed.GetComponent<RenderQualityPresentation>();
                Assert.That(quality.TierId,Is.EqualTo("balanced"));
                var source=Definition(reviewed.Venue.Manifest.Id);reviewed.InitializeReview(source,"sprint40");
                Assert.That(reviewed.Review.TierId,Is.EqualTo("high"));Assert.That(reviewed.Feed.Current.Tick,Is.Zero);
                bool rendered=false,correctPipeline=false;string firstTier=null;
                void OnRender(ScriptableRenderContext context,Camera camera){if(camera==reviewed.ChaseCamera&&!rendered){rendered=true;firstTier=reviewed.Review.TierId;correctPipeline=QualitySettings.renderPipeline==quality.Pipeline;}}
                RenderPipelineManager.endCameraRendering+=OnRender;
                try{for(int i=0;i<12&&!rendered;i++)yield return null;}
                finally{RenderPipelineManager.endCameraRendering-=OnRender;}
                Assert.That(rendered,Is.True);Assert.That(firstTier,Is.EqualTo("high"));Assert.That(correctPipeline,Is.True);Assert.That(reviewed.ChaseCamera.targetTexture,Is.Null);
                var legacy=Host(b);Assert.That(reviewed.Runner.Field.ContextJson,Is.EqualTo(legacy.Runner.Field.ContextJson));
                foreach(string tier in new[]{"low","balanced","high"})reviewed.Review.ApplyQuality(tier);
                source.initial_quality_tier="low";source.entries[1].label="Changed caller DTO";Assert.That(reviewed.Review.TierId,Is.EqualTo("high"));
                Assert.Throws<ArgumentException>(()=>reviewed.Review.ApplyQuality("unknown"));Assert.Throws<ArgumentException>(()=>reviewed.Review.RequestEntry("unknown"));
                ProductionPlayerBuildSmoke.AdvanceAndValidate(reviewed,400);ProductionPlayerBuildSmoke.AdvanceAndValidate(legacy,400);
                Assert.That(reviewed.Runner.Field.ContextJson,Is.EqualTo(legacy.Runner.Field.ContextJson));
                Assert.That(reviewed.Feed.Current.Tick,Is.EqualTo(legacy.Feed.Current.Tick));
                for(int i=0;i<reviewed.Feed.Current.Count;i++)
                {
                    var x=reviewed.Feed.Current.Get(i);var y=legacy.Feed.Current.Get(i);
                    Assert.That(x,Is.EqualTo(y),"The complete detached athlete presentation state stays exact after quality/menu transitions.");
                    // Physical points deliberately retain different map owners in two independent fields.
                    // Compare every public physical value exactly, alongside the complete state/context above.
                    var reviewedPoint=reviewed.Runner.Field.GetPhysicalPoint(i);
                    var legacyPoint=legacy.Runner.Field.GetPhysicalPoint(i);
                    Assert.That(reviewedPoint.IsValid&&legacyPoint.IsValid,Is.True);
                    Assert.That(reviewedPoint.LoopId,Is.EqualTo(legacyPoint.LoopId));
                    Assert.That(reviewedPoint.LocalNanometers,Is.EqualTo(legacyPoint.LocalNanometers));
                    Assert.That(reviewedPoint.LoopLengthNanometers,Is.EqualTo(legacyPoint.LoopLengthNanometers));
                    Assert.That(reviewedPoint.Closed,Is.EqualTo(legacyPoint.Closed));
                }
                Assert.Throws<InvalidOperationException>(()=>reviewed.InitializeReview(Definition(reviewed.Venue.Manifest.Id),"sprint40"));
            }
            finally{Object.Destroy(a);Object.Destroy(b);}
            yield return null;Assert.That(QualitySettings.renderPipeline,Is.SameAs(baseline));
        }
        [UnityTest] public IEnumerator InvalidReviewIsAtomicAndLeaseRestoresOnDisableReenableAndDestroy()
        {
            var baseline=QualitySettings.renderPipeline;var root=new GameObject("Review lifecycle");
            try
            {
                var host=Host(root);var quality=host.GetComponent<RenderQualityPresentation>();int components=root.GetComponents<Component>().Length;
                Assert.Throws<ArgumentNullException>(()=>host.InitializeReview(null,"sprint40"));
                Assert.Throws<ArgumentException>(()=>host.InitializeReview(Definition("foreign"),"sprint40"));
                Assert.Throws<ArgumentException>(()=>host.InitializeReview(Definition(host.Venue.Manifest.Id,2),"sprint40"));
                Assert.Throws<ArgumentException>(()=>host.InitializeReview(Definition(host.Venue.Manifest.Id),"mass30"));
                Assert.That(root.GetComponents<Component>().Length,Is.EqualTo(components));Assert.That(host.Review,Is.Null);Assert.That(quality.TierId,Is.EqualTo("balanced"));
                host.InitializeReview(Definition(host.Venue.Manifest.Id),"sprint40");host.Review.ApplyQuality("low");
                root.SetActive(false);Assert.That(QualitySettings.renderPipeline,Is.SameAs(baseline));
                root.SetActive(true);Assert.That(quality.TierId,Is.EqualTo("low"));Assert.That(QualitySettings.renderPipeline,Is.SameAs(quality.Pipeline));
                yield return null;
            }
            finally{Object.Destroy(root);}
            yield return null;Assert.That(QualitySettings.renderPipeline,Is.SameAs(baseline));
        }
    }
}
