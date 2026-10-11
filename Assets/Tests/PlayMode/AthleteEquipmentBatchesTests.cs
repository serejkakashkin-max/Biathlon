using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteEquipmentBatchesTests
    {
        [UnityTest] public IEnumerator OriginalEquipmentRetainsVisibilityAndCanBeRestored()
        {
            if (!SystemInfo.supportsInstancing) Assert.Ignore("GPU instancing unavailable on this device.");
            var root = new GameObject("Equipment batches test");
            try
            {
                var rig = AthleteRigFactory.Create(root.transform);
                var batches = root.AddComponent<AthleteEquipmentBatches>();
                var originals = rig.GetComponentsInChildren<MeshRenderer>(true);
                batches.Register(rig); batches.Render();
                Assert.That(batches.SourceCount, Is.GreaterThan(0));
                Assert.That(batches.SubmittedInstances, Is.GreaterThan(0));
                int visible = batches.SubmittedInstances;
                foreach (var renderer in originals) renderer.enabled = false;
                batches.Render();
                Assert.That(batches.SubmittedInstances, Is.Zero, "The shooting camera must still be able to hide the player's body/equipment.");
                foreach (var renderer in originals) renderer.enabled = true;
                batches.Render(); Assert.That(batches.SubmittedInstances, Is.EqualTo(visible));
                batches.enabled = false;
                foreach (var renderer in originals) Assert.That(renderer.forceRenderingOff, Is.False);
                batches.enabled = true;
                batches.Clear(); Assert.That(batches.SourceCount, Is.Zero);
                foreach (var renderer in originals) Assert.That(renderer.forceRenderingOff, Is.False);
                batches.Register(rig); batches.Render();
                Assert.That(batches.SubmittedInstances, Is.EqualTo(visible), "Restarting a race must not retain dead sources.");
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
