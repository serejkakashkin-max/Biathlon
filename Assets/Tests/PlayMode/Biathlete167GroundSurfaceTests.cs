using System;
using Biathlon.Career;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167GroundSurfaceTests
    {
        [Test] public void RenderedBankOverridesTerrainAndQueriesAllocateNothing()
        {
            var root=new GameObject("Actual snow triangle witness");var mesh=new Mesh();
            try
            {
                mesh.vertices=new[]{new Vector3(-2,.2f,-2),new Vector3(2,.2f,-2),new Vector3(0,.6f,2)};mesh.triangles=new[]{0,2,1};
                var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=mesh;root.AddComponent<MeshRenderer>();
                var surface=new RenderedSnowSurface(new[]{filter},2,_=>-.03f);
                Assert.That(surface.Height(Vector3.zero),Is.EqualTo(.4f).Within(1e-6));
                Assert.That(surface.Height(new Vector3(8,0,8)),Is.EqualTo(-.03f));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)surface.Height(Vector3.zero);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(mesh);}
        }
        [Test] public void SnowAdaptationMovesOnlyTheVisualAndRetainsCareerEquipment()
        {
            var root=new GameObject("Banked snow visual compatibility");
            try
            {
                var source=AthleteRigFactory.Create(root.transform);var visual=Biathlete167RacePresentation.Attach(source);
                var p=source.transform.position;var q=source.transform.rotation;
                visual.SetGroundSurface(_=>.2f);visual.RenderPreview();
                Assert.That(visual.GroundOffsetMeters,Is.EqualTo(.2f).Within(1e-5));
                Assert.That(visual.Rig.transform.position.y,Is.EqualTo(p.y+.2f).Within(1e-5));
                Assert.That(source.transform.position,Is.EqualTo(p));Assert.That(source.transform.rotation,Is.EqualTo(q));
                Assert.That(visual.SelectedSkiId,Is.EqualTo("ski_neutral"));
                visual.SetGroundSurface(null);visual.RenderPreview();Assert.That(visual.GroundOffsetMeters,Is.Zero);
                Assert.That(visual.Rig.transform.position,Is.EqualTo(p));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
