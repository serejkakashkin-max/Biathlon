#if UNITY_EDITOR
using System;
using Biathlon.Unity;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.VenueEditorTests
{
    public sealed class RoadsideEventAuthoringTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void LegacySeatedGenerationStillMatchesExistingAssetVertexForVertex(int lod)
        {
            var actual=Resources.Load<Mesh>("Biathlon/Crowd/SpectatorLOD"+lod);Assert.That(actual,Is.Not.Null);
            var generated=SpectatorCrowdAuthoring.Build(lod);
            try
            {
                Assert.That(generated.vertices,Is.EqualTo(actual.vertices));Assert.That(generated.normals,Is.EqualTo(actual.normals));
                Assert.That(generated.colors,Is.EqualTo(actual.colors));Assert.That(generated.triangles,Is.EqualTo(actual.triangles));Assert.That(generated.bounds,Is.EqualTo(actual.bounds));
            }
            finally{Object.DestroyImmediate(generated);}
        }

        [Test]
        public void CrowdAuthoringRejectsUnknownPoseAndLod()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>SpectatorCrowdAuthoring.Build(2,SpectatorPose.Standing));
            Assert.Throws<ArgumentOutOfRangeException>(()=>SpectatorCrowdAuthoring.Build(0,(SpectatorPose)52));
        }
    }
}
#endif
