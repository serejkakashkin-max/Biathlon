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

        [UnityTest] public IEnumerator WoodlandMainInnerTubeAtEveryNativeRowFitsInsideTheRenderedRibbon()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var track=TrackDefinition.FromJson(Resources.Load<TextAsset>(root+"track").text);
            var visual=TrackVisualDefinition.FromJson(Resources.Load<TextAsset>(root+"visual").text,track);
            var body=OccupancyDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/occupancy_provisional_v1").text);
            var loop=visual.Get("main");var physical=track.GetLoop("main");
            double half=double.PositiveInfinity;
            for(int i=0;i<physical.SegmentCount;i++)half=System.Math.Min(half,physical.GetSegment(i).WidthMeters*.5);
            double maximumTurn=0;
            for(int i=1;i+1<loop.Count;i++)
            {
                var a=loop.Get(i-1);var b=loop.Get(i);var c=loop.Get(i+1);
                double ax=b.X-a.X,az=b.Z-a.Z,bx=c.X-b.X,bz=c.Z-b.Z;
                maximumTurn=System.Math.Max(maximumTurn,System.Math.Abs(System.Math.Atan2(ax*bz-az*bx,ax*bx+az*bz)));
            }
            double radius=half-.5*System.Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+
                body.LateralSeparation*body.LateralSeparation)-body.EdgeClearance-
                half*System.Math.Min(1,System.Math.Tan(maximumTurn*.5));
            Assert.That(radius,Is.GreaterThan(0));
            var mesh=TrackRibbonBuilder.Build(track,visual,"main",out var nativeDistances);
            try
            {
                var vertices=mesh.vertices;var triangles=mesh.triangles;int samples=0;
                for(int i=1;i+1<nativeDistances.Length;i++)
                {
                    var point=loop.Sample(nativeDistances[i]);
                    for(int spoke=0;spoke<8;spoke++)
                    {
                        double angle=2*System.Math.PI*spoke/8;
                        double x=point.X+radius*System.Math.Cos(angle);
                        double z=point.Z+radius*System.Math.Sin(angle);
                        Assert.That(InsideNativeRibbon(x,z,vertices,triangles),Is.True,
                            $"The authored inner tube escaped the actual native Main ribbon at row {i}, spoke {spoke}.");
                        samples++;
                    }
                }
                Assert.That(samples,Is.GreaterThan(2500));
                TestContext.Out.WriteLine("woodland_main_inner_tube_native_mesh_samples="+samples+
                    " conservative_radius_m="+radius.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
            finally{Object.Destroy(mesh);}
            yield return null;
        }

        private static bool InsideNativeRibbon(double x,double z,Vector3[] vertices,int[] triangles)
        {
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                double ab=(b.x-a.x)*(z-a.z)-(b.z-a.z)*(x-a.x);
                double bc=(c.x-b.x)*(z-b.z)-(c.z-b.z)*(x-b.x);
                double ca=(a.x-c.x)*(z-c.z)-(a.z-c.z)*(x-c.x);
                if(ab>=-1e-5&&bc>=-1e-5&&ca>=-1e-5||ab<=1e-5&&bc<=1e-5&&ca<=1e-5)return true;
            }
            return false;
        }
    }
}
