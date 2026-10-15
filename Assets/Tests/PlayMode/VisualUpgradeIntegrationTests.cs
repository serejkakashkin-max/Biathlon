using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class VisualUpgradeIntegrationTests
    {
        [Test] public void CosmeticIdentityKeepsJointShapesNationalKitsAndEquipmentBindings()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var appearance=rig.GetComponent<AthleteAppearancePresentation>();appearance.ApplyNation("NOR");
                var equipment=rig.GetComponent<AthleteEquipmentBinding>();var before=equipment.Rifle.localPosition;
                appearance.ApplyIdentity("stable-athlete");int variant=appearance.CosmeticVariant;appearance.ApplyIdentity("stable-athlete");
                Assert.That(appearance.CosmeticVariant,Is.EqualTo(variant));Assert.That(appearance.Nation,Is.EqualTo("NOR"));Assert.That(equipment.Rifle.localPosition,Is.EqualTo(before));
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach(float distance in new[]{0,20,70})
                {
                    rig.GetComponent<AthleteSkinDetailBinding>().Prepare(distance,false,true);appearance.ApplyIdentity("stable-athlete");
                    Assert.That(skin.sharedMesh.blendShapeCount,Is.EqualTo(7+AthleteFaceCatalog.Load().profiles.Length));
                    for(int i=0;i<4;i++)Assert.That(skin.GetBlendShapeWeight(i),Is.Zero,"Cosmetic assignment must not activate a joint correction.");
                    int index=skin.sharedMesh.GetBlendShapeIndex("OriginalBuild"+variant);Assert.That(skin.GetBlendShapeWeight(index),Is.EqualTo(100));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void WinterClustersAreGroundedClearAllCoursesAndRenderWithoutWarmAllocations()
        {
            var root=new GameObject("winter dressing integration");
            try
            {
                var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
                var d=WinterDressingDefinition.Load("Biathlon/winter_dressing_v1");var dressing=root.AddComponent<ProductionForestDressing>();
                float Height(Vector3 p)=>.03f*p.x+.02f*p.z;
                dressing.Initialize(d,recipe.route_corridors,Height);Assert.That(dressing.Count,Is.InRange(24,96));
                for(int i=0;i<dressing.Count;i++)
                {var p=dressing.Position(i);Assert.That(p.y,Is.EqualTo(Height(p)-d.burial_m).Within(.0001f));Assert.That(ProductionForestDressing.ClearOfCourses(p,.8f,recipe.route_corridors,2),Is.True);}
                Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.Zero);
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=dressing.Position(0)+new Vector3(0,3,-10);camera.transform.LookAt(dressing.Position(0));
                for(int i=0;i<20;i++)dressing.Render(camera,"high");Assert.That(dressing.Visible,Is.GreaterThan(0));
                long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)dressing.Render(camera,"high");Assert.That(GC.GetAllocatedBytesForCurrentThread()-start,Is.Zero);
                camera.transform.position=Vector3.one*10000;dressing.Render(camera,"low");Assert.That(dressing.Visible,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void FiveAtmospheresShareTheWeatherLeaseAndReleaseIt()
        {
            var root=new GameObject("five atmosphere fixture");var sky=RenderSettings.skybox;
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=Vector3.up*2;
                var light=new GameObject("key",typeof(Light));light.transform.SetParent(root.transform);var key=light.GetComponent<Light>();key.type=LightType.Directional;
                var weather=root.AddComponent<WeatherPresentation>();weather.Initialize(camera,key,Resources.Load<Material>("Biathlon/Weather/G2Lighting/BenchmarkSky"));
                foreach(var id in new[]{"clear","overcast","light_snow","snowfall","winter_haze"})
                {
                    weather.Render(id,"high",12,Vector2.right,0);Assert.That(weather.PresetId,Is.EqualTo(id));
                    if(id=="snowfall"||id=="light_snow")Assert.That(weather.SubmittedFlakes,Is.GreaterThan(0));else Assert.That(weather.SubmittedFlakes,Is.Zero);
                }
                weather.Shutdown();Assert.That(RenderSettings.skybox,Is.SameAs(sky));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void SnowfallVariationIsSmoothPeriodicAndDoesNotAllocate()
        {
            var d=WeatherVisualDefinition.Load();Assert.That(d.LightSnowBlend(12),Is.Zero);Assert.That(d.LightSnowBlend(60),Is.EqualTo(1));
            for(int i=0;i<1200;i++)Assert.That(Mathf.Abs(d.LightSnowBlend(i*.1)-d.LightSnowBlend(i*.1+.1)),Is.LessThan(.025f));
            Assert.That(d.LightSnowBlend(60+d.snowfall_cycle_s),Is.EqualTo(d.LightSnowBlend(60)));
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++)d.LightSnowBlend(i);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void PushAndSnowSurfaceChangeSprayStrengthWithoutChangingItsBudgetOrRewind()
        {
            var s=SnowSpraySettings.Load();var quiet=new SnowSprayBuffer(s);var attack=new SnowSprayBuffer(s);
            quiet.Sample("same",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,0,intensity:.5f);
            attack.Sample("same",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,0,intensity:1.5f);
            Assert.That(quiet.TotalEmitted,Is.EqualTo(attack.TotalEmitted));Assert.That(attack.Get(0).Velocity.y,Is.GreaterThan(quiet.Get(0).Velocity.y));
            Assert.That(s.performance_draw_budget,Is.LessThan(s.high_draw_budget));Assert.That(s.high_draw_budget,Is.LessThan(s.ultra_draw_budget));
            Assert.Throws<ArgumentException>(()=>attack.Sample("same",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,0,intensity:float.NaN));
            Assert.That(RenderQualityDefinition.DisplayName("high"),Is.EqualTo("Ultra"));Assert.That(RenderQualityDefinition.Load().Get("balanced").render_scale,Is.EqualTo(1));
        }
    }
}
