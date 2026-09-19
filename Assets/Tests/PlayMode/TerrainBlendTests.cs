using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class TerrainBlendTests
    {
        [Test] public void LayerWeightsStayNormalizedContinuousAndRespectExplicitIceMask()
        {
            var d=TerrainBlendDefinition.Load();var previous=d.Weights(0,3.5f,0);
            for(int i=0;i<=1000;i++){float distance=i*.01f;foreach(float ice in new[]{0f,.5f,1f}){var w=d.Weights(distance,3.5f,ice);Assert.That(w.r+w.g+w.b+w.a,Is.EqualTo(1).Within(1e-6));for(int c=0;c<4;c++)Assert.That(w[c],Is.InRange(0,1));if(ice==0){Assert.That(w.b,Is.Zero);for(int c=0;c<4;c++)Assert.That(Mathf.Abs(w[c]-previous[c]),Is.LessThan(.03f));previous=w;}}}
            Assert.That(d.Weights(0,3.5f,0),Is.EqualTo(new Color(0,1,0,0)));Assert.That(d.Weights(0,3.5f,1),Is.EqualTo(new Color(0,0,1,0)));Assert.That(d.Weights(10,3.5f,1),Is.EqualTo(new Color(1,0,0,0)));
            Assert.Throws<ArgumentException>(()=>d.Weights(0,3.5f,float.NaN));Assert.Throws<ArgumentException>(()=>d.Weights(0,.1f,0));
            string json=Resources.Load<TextAsset>("Biathlon/terrain_blend_v1").text;Assert.Throws<ArgumentException>(()=>TerrainBlendDefinition.FromJson(json.Replace("0.8,","0,")));
        }
        [Test] public void SingleSurfaceIsRepeatableFacesUpAndRejectsNonfiniteOrExcessiveAuthoring()
        {
            var d=TerrainBlendDefinition.Load();var route=new[]{new Vector3(-1,0,-10),new Vector3(1,0,10)};var saved=(Vector3[])route.Clone();
            var a=TerrainBlendMesh.Build(route,3,new Vector2(12,20),.5f,d,p=>p.z*.02f,p=>Mathf.Clamp01(p.z/10));
            var b=TerrainBlendMesh.Build(route,3,new Vector2(12,20),.5f,d,p=>p.z*.02f,p=>Mathf.Clamp01(p.z/10));
            try{CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.colors,b.colors);CollectionAssert.AreEqual(saved,route);foreach(var n in a.normals)Assert.That(n.y,Is.GreaterThan(.99f));Assert.That(a.vertexCount,Is.EqualTo(25*41));}
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
            Assert.Throws<ArgumentException>(()=>TerrainBlendMesh.Build(route,3,new Vector2(1000,1000),.1f,d));Assert.Throws<ArgumentException>(()=>TerrainBlendMesh.Build(route,3,new Vector2(12,20),.5f,d,p=>float.NaN));
        }
        [Test] public void BlendMaterialUsesConfiguredLayersAndPlainSnowRemainsOptOut()
        {
            var d=TerrainBlendDefinition.Load();var asset=Resources.Load<Material>("Biathlon/Terrain/WinterBlend");Assert.That(asset,Is.Not.Null);Assert.That(asset.shader.isSupported,Is.True);Assert.That(asset.GetFloat("_TerrainBlend"),Is.EqualTo(1));
            var normals=asset.GetVector("_LayerNormals");for(int i=0;i<3;i++)Assert.That(normals[i],Is.EqualTo(d.layer_normals[i]));
            foreach(string id in new[]{"cold","wet"})Assert.That(Resources.Load<Material>("Biathlon/Snow/"+id).GetFloat("_TerrainBlend"),Is.Zero);
            var material=new Material(asset);try{SnowMaterialDefinition.Load().Configure(material,"cold");Assert.That(material.GetFloat("_TerrainBlend"),Is.Zero);d.Configure(material);Assert.That(material.GetFloat("_TerrainBlend"),Is.EqualTo(1));}finally{Object.DestroyImmediate(material);}
            var mesh=Resources.Load<Mesh>("Biathlon/Terrain/BlendSurface");Assert.That(mesh,Is.Not.Null);float maxIce=0,maxEdge=0;foreach(var c in mesh.colors){maxIce=Mathf.Max(maxIce,c.b);maxEdge=Mathf.Max(maxEdge,c.a);}Assert.That(maxIce,Is.GreaterThan(.9f));Assert.That(maxEdge,Is.GreaterThan(.6f));
        }
    }
}
