using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class SnowMaterialTests
    {
        [Test] public void PresetsRejectInvalidRangesAndKeepWetSnowDarkerAndSmoother()
        {
            string json=Resources.Load<TextAsset>("Biathlon/snow_material_v1").text;
            var d=SnowMaterialDefinition.FromJson(json);
            Assert.That(d.presets[0].roughness,Is.GreaterThan(d.presets[1].roughness));
            Assert.That(d.presets[0].albedo.grayscale,Is.GreaterThan(d.presets[1].albedo.grayscale));
            Assert.Throws<ArgumentException>(()=>SnowMaterialDefinition.FromJson(json.Replace("0.045","0")));
            Assert.Throws<ArgumentException>(()=>SnowMaterialDefinition.FromJson(json.Replace("\"wet\"","\"cold\"")));
            Assert.Throws<ArgumentException>(()=>SnowMaterialDefinition.FromJson(json.Replace("0.8,","2,")));
        }
        [Test] public void GeneratedSnowAssetsUseSupportedPbrShaderAndExactVersionedValues()
        {
            var d=SnowMaterialDefinition.Load();
            foreach(var p in d.presets)
            {
                var material=Resources.Load<Material>("Biathlon/Snow/"+p.id);
                Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);
                Assert.That(material.shader.name,Is.EqualTo("Biathlon/Snow"));
                // Material color storage may round-trip through the project's color space.
                var color=material.GetColor("_BaseColor");
                for(int channel=0;channel<4;channel++)Assert.That(color[channel],Is.EqualTo(p.albedo[channel]).Within(0.000001f));
                Assert.That(material.GetFloat("_Roughness"),Is.EqualTo(p.roughness));
                Assert.That(material.GetFloat("_GrainScale"),Is.EqualTo(d.grain_scale_m));
                Assert.That(material.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
                var instance=new Material(material);
                try {d.Configure(instance,p.id); Assert.That(instance.GetFloat("_NormalStrength"),Is.EqualTo(p.normal_strength));Assert.Throws<ArgumentException>(()=>d.Configure(instance,"missing"));}
                finally {Object.DestroyImmediate(instance);}
            }
            var mesh=Resources.Load<Mesh>("Biathlon/Snow/ValidationSurface");
            Assert.That(mesh,Is.Not.Null);Assert.That(mesh.bounds.size.y,Is.GreaterThan(.5f));
            foreach(var n in mesh.normals)Assert.That(n.y,Is.GreaterThan(0),"Fixture winding faces the sky.");
        }
    }
}
