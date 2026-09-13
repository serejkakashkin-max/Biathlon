using System;
using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class PlayerBuildSmokeTests
    {
        [UnityTest] public IEnumerator SmokeChecksInitializedHostAndAdvancesSnapshot()
        {
            var go = new GameObject("Smoke test");
            var host = go.AddComponent<PlayableSprintHost>(); host.AutoStart = false; host.enabled = false;
            try
            {
                Assert.Throws<InvalidOperationException>(() => PlayerBuildSmoke.AdvanceAndValidate(host, 400));
                host.Initialize(2);
                Assert.Throws<ArgumentOutOfRangeException>(() => PlayerBuildSmoke.AdvanceAndValidate(host, 0));
                PlayerBuildSmoke.AdvanceAndValidate(host, 400);
                Assert.That(host.Feed.Current.Tick, Is.EqualTo(400));
                Assert.That(host.Feed.Current.Count, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.Destroy(go); }
            yield return null;
        }
    }
}
