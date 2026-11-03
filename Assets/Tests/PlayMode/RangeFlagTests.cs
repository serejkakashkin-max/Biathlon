using System;
using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class RangeFlagTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private static RangeFlagDefinition Flags()=>RangeFlagDefinition.FromJson(Resources.Load<TextAsset>(Prefix+"range_flags_v1").text);
        private static ProductionEnvironmentDefinition Venue()=>ProductionEnvironmentDefinition.FromJson(Resources.Load<TextAsset>(Prefix+"environment").text);

        [TestCase("RUS",.5f,.85f,"#FFFFFF")]
        [TestCase("RUS",.5f,.5f,"#0039A6")]
        [TestCase("RUS",.5f,.15f,"#D52B1E")]
        [TestCase("NOR",.1f,.1f,"#BA0C2F")]
        [TestCase("NOR",.36f,.1f,"#00205B")]
        [TestCase("NOR",.29f,.1f,"#FFFFFF")]
        [TestCase("SWE",.1f,.1f,"#006AA7")]
        [TestCase("SWE",.36f,.1f,"#FECC00")]
        [TestCase("FIN",.1f,.1f,"#FFFFFF")]
        [TestCase("FIN",.36f,.1f,"#002F6C")]
        [TestCase("GER",.5f,.85f,"#000000")]
        [TestCase("GER",.5f,.5f,"#DD0000")]
        [TestCase("GER",.5f,.15f,"#FFCE00")]
        [TestCase("FRA",.15f,.5f,"#000091")]
        [TestCase("FRA",.5f,.5f,"#FFFFFF")]
        [TestCase("FRA",.85f,.5f,"#E1000F")]
        [TestCase("ITA",.15f,.5f,"#009246")]
        [TestCase("ITA",.5f,.5f,"#FFFFFF")]
        [TestCase("ITA",.85f,.5f,"#CE2B37")]
        public void RecognizableNationalPatterns(string nation,float u,float v,string expected)
        {
            var flag=Array.Find(Flags().nations,n=>n.id==nation);
            Assert.That(flag,Is.Not.Null);
            Assert.That((Color32)RangeFlagDefinition.PaintAt(flag,u,v),Is.EqualTo((Color32)RangeFlagDefinition.ColorOf(expected)));
        }

        [Test] public void PlacementFollowsTranslatedAndRotatedActualBackstop()
        {
            var flags=Flags();var recipe=Venue();var a=flags.Positions(recipe.range,_=>0);
            var rotation=Quaternion.Euler(0,63,0);var offset=new Vector3(145,0,-73);
            recipe.range.forward=rotation*recipe.range.forward;
            recipe.range.origin=rotation*recipe.range.origin+offset;
            recipe.range.backstop.position=rotation*recipe.range.backstop.position+offset;
            foreach(var bank in recipe.range.banks)bank.position=rotation*bank.position+offset;
            var b=flags.Positions(recipe.range,_=>0);
            for(int i=0;i<a.Length;i++)Assert.That(Vector3.Distance(b[i],rotation*a[i]+offset),Is.LessThan(.0001f));
            Assert.Throws<ArgumentException>(()=>flags.Positions(recipe.range,_=>float.NaN));
        }

        [Test] public void RejectsUnboundedMeshesAndAmbiguousDefinitions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>RangeFlagPresentation.BuildCloth(500,12));
            var flags=Flags();flags.nations[1].id=flags.nations[0].id;
            Assert.Throws<ArgumentException>(()=>RangeFlagDefinition.FromJson(JsonUtility.ToJson(flags)));
            flags=Flags();flags.nations[0].marks[0].width=2;
            Assert.Throws<ArgumentException>(()=>RangeFlagDefinition.FromJson(JsonUtility.ToJson(flags)));
        }

        [Test] public void ExcludesCanopiesAcrossAllLodsRatherThanOnlyTreeTrunks()
        {
            var flags=Flags();var anchors=flags.Positions(Venue().range,_=>0);var clear=new RangeFlagClearance(flags,anchors);
            foreach(string species in new[]{"fir","pine"})
            {
                var tree=new ProductionTree{species=species,scale=1.5f,position=anchors[0]+Vector3.right*4.5f};
                Assert.That(clear.Clears(tree),Is.False,"Canopy near the cloth sweep must be removed even when its trunk clears the pole.");
                tree.position=anchors[0]+Vector3.forward*200;
                Assert.That(clear.Clears(tree),Is.True,"Keep the distant forest intact.");
            }
        }

        [UnityTest] public IEnumerator WindKeepsAllTargetsClearAndReusesOwnedResources()
        {
            var recipe=Venue();string unchanged=JsonUtility.ToJson(recipe);
            var flags=Flags();var root=new GameObject("Isolated national flag integration test");
            var view=root.AddComponent<RangeFlagPresentation>();
            var materials=new Material[flags.nations.Length];var textures=new Texture[materials.Length];Mesh shared=null;
            try
            {
                view.Initialize(flags,recipe,_=>0);yield return null;
                Assert.That(view.Count,Is.EqualTo(7));
                Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.Zero,"Decoration cannot obstruct skiers or shooting.");
                for(int i=0;i<view.Count;i++)
                {
                    var fabric=view.Fabric(i);materials[i]=fabric.sharedMaterial;textures[i]=materials[i].mainTexture;
                    var mesh=fabric.GetComponent<MeshFilter>().sharedMesh;
                    if(i==0)shared=mesh;else Assert.That(mesh,Is.SameAs(shared));
                    Assert.That(mesh.vertexCount,Is.LessThanOrEqualTo(49*25));
                    foreach(var p in mesh.vertices)Assert.That(mesh.bounds.Contains(p),Is.True);
                }
                var block=new MaterialPropertyBlock();
                foreach(var wind in new[]{Vector2.zero,new Vector2(.3f,.5f),new Vector2(12,-8),new Vector2(-10,7)})
                {
                    for(int frame=0;frame<64;frame++)view.Render(frame*.1f,wind);
                    Assert.That(view.ObservedWind,Is.EqualTo(wind));
                    for(int i=0;i<view.Count;i++)
                    {
                        var fabric=view.Fabric(i);fabric.GetPropertyBlock(block);
                        Assert.That(block.GetVector("_FlagWind").x,Is.InRange(0f,1f));
                        Assert.That(fabric.sharedMaterial,Is.SameAs(materials[i]));
                        Assert.That(fabric.sharedMaterial.mainTexture,Is.SameAs(textures[i]));
                        // Every conservative deformed mesh bound lies past all target planes,
                        // therefore neither prone nor standing mat-to-target segments can hit it.
                        var bounds=fabric.bounds;
                        foreach(var bank in recipe.range.banks)
                        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                        {
                            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                            Assert.That(Vector3.Dot(corner-bank.position,recipe.range.forward),Is.GreaterThan(0));
                        }
                    }
                }
                Assert.That(JsonUtility.ToJson(recipe),Is.EqualTo(unchanged),"Flags only observe the environment.");
                for(int i=0;i<20;i++)view.Render(i,new Vector2(3,2));
                long allocated=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<300;i++)view.Render(i*.05f,new Vector2(3,2));
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocated,Is.Zero,"Steady cloth observation must not allocate managed memory.");
                view.enabled=false;Assert.That(view.Fabric(0).gameObject.activeInHierarchy,Is.False);
                view.enabled=true;Assert.That(view.Fabric(0).gameObject.activeInHierarchy,Is.True);
                view.Shutdown();yield return null;yield return null;
                Assert.That(view.Count,Is.Zero);Assert.That(shared==null,Is.True);
                foreach(var material in materials)Assert.That(material==null,Is.True);
                foreach(var texture in textures)Assert.That(texture==null,Is.True);
            }
            finally{Object.Destroy(root);}
            yield return null;
        }
    }
}
