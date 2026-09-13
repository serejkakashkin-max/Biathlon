#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class PlaytestEvidenceTests
    {
        [UnityTest] public IEnumerator FrozenEvidenceIsStableAndRejectsBackwardTicks()
        {
            var type=Type.GetType("Biathlon.Unity.PlaytestDiagnostics, Biathlon.Unity.Editor",true);
            type.GetMethod("Begin").Invoke(null,null);
            var first=(string)type.GetMethod("AdvanceTo").Invoke(null,new object[]{400});
            Assert.That(first,Does.Contain("\"tick\": 400").And.Contain("context_sha256").And.Contain("athlete_0"));
            Assert.That(type.GetMethod("CaptureState").Invoke(null,null),Is.EqualTo(first));
            var error=Assert.Throws<TargetInvocationException>(()=>type.GetMethod("AdvanceTo").Invoke(null,new object[]{399}));Assert.That(error.InnerException,Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(type.GetMethod("CaptureState").Invoke(null,null),Is.EqualTo(first));
            UnityEngine.Object.Destroy(UnityEngine.Object.FindFirstObjectByType<PlayableSprintHost>().gameObject);yield return null;
        }
    }
}
#endif
