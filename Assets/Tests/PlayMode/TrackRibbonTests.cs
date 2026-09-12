using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class TrackRibbonTests
    {
        [UnityTest] public IEnumerator AuthoredLoopsGenerateFiniteRepeatableMeshAndUpwardSurface()
        {
            var track=TrackDefinition.FromJson(File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/synthetic_stadium_v1.json")));
            var visual=TrackVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/stadium_visual_v1").text,track);
            for(int i=0;i<visual.Count;i++)
            {
                var a=TrackRibbonBuilder.Build(track,visual,visual.Get(i).LoopId);var b=TrackRibbonBuilder.Build(track,visual,visual.Get(i).LoopId);
                Assert.That(a.vertices,Is.EqualTo(b.vertices));Assert.That(a.triangles,Is.EqualTo(b.triangles));Assert.That(a.vertexCount,Is.GreaterThan(3));
                foreach(var v in a.vertices)Assert.That(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.z),Is.False);
                foreach(var normal in a.normals)Assert.That(normal.y,Is.GreaterThanOrEqualTo(0));Object.Destroy(a);Object.Destroy(b);
            }
            yield return null;
        }
    }
}
