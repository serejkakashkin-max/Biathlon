using Biathlon.PlayModeTests;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.VenueEditorTests
{
    public sealed class ProductionEnvironmentEditorLifecycleTests
    {
        [Test]
        public void EditModeConstructionRemovesOnlyOwnedCloneCollidersAndRestoresGlobals()
        {
            Assert.That(Application.isPlaying, Is.False);
            var fixture = new ProductionEnvironmentFixture();
            var source = Resources.Load<GameObject>(fixture.Recipe.assets[0].resource);
            int sourceColliders = source.GetComponentsInChildren<Collider>(true).Length;
            var root = new GameObject("Temporary editor environment lifecycle test");
            var oldSky = RenderSettings.skybox;
            bool oldFog = RenderSettings.fog;
            Mesh mesh = null;
            try
            {
                var cameraRoot = new GameObject("Temporary editor camera");
                cameraRoot.transform.SetParent(root.transform, false);
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var view = root.AddComponent<ProductionVenueEnvironment>();
                view.Initialize(fixture.Plan, fixture.Recipe, fixture.Read,
                    root.AddComponent<RacePresentationFeed>(), camera);
                Assert.That(view.Initialized, Is.True);
                Assert.That(view.BankCount, Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty,
                    "Editor construction must immediately remove cloned presentation colliders, including the generated backstop.");
                Assert.That(source.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(sourceColliders),
                    "The source prefab must remain untouched.");
                mesh = view.TileMesh(0);
                view.Render(camera, "high", 0);
                var ownedSky = RenderSettings.skybox;
                view.Shutdown();
                Assert.That(mesh == null, Is.True, "Editor shutdown immediately releases owned terrain.");
                Assert.That(ownedSky == null, Is.True, "Editor shutdown immediately releases its weather material.");
                Assert.That(view.Initialized, Is.False);
                Assert.That(RenderSettings.skybox, Is.SameAs(oldSky));
                Assert.That(RenderSettings.fog, Is.EqualTo(oldFog));
                view.Initialize(fixture.Plan, fixture.Recipe, fixture.Read,
                    root.GetComponent<RacePresentationFeed>(), camera);
                view.Render(camera, "high", 1);
                view.Shutdown();
                Assert.That(RenderSettings.skybox, Is.SameAs(oldSky),
                    "A second construction must acquire and release a fresh weather lease.");
            }
            finally
            {
                root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();
                Object.DestroyImmediate(root);
            }
        }
    }
}
