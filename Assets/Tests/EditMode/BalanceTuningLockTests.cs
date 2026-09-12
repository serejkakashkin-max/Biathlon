using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BalanceTuningLockTests
    {
        private static BalanceTuningLock Definition()=>BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/tuning-lock-v1.json"));
        [Test] public void FirstPlayableTuningMatchesEveryLockedDefinitionAndExperiment()
        {
            var d=Definition();Assert.That(d.Count,Is.GreaterThan(100));
            Assert.That(TuningLockVerification.Verify(d,PhysiologyContractTests.ProjectFile),Is.Empty);
            Assert.That(BalanceTuningLock.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
        }
        [Test] public void LockDetectsChangesMissingInputsAndUnsafePaths()
        {
            var d=Definition();string first=d.Get(0).Path;
            Assert.That(TuningLockVerification.Verify(d,path=>path==first?"changed":PhysiologyContractTests.ProjectFile(path)),Is.EqualTo(new[]{"Changed: "+first}));
            Assert.That(TuningLockVerification.Verify(d,path=>path==first?throw new System.IO.FileNotFoundException():PhysiologyContractTests.ProjectFile(path)),Is.EqualTo(new[]{"Missing/unreadable: "+first}));
            Assert.Catch(()=>BalanceTuningLock.FromJson(d.ToJson().Replace("\\/","/").Replace(first,"Assets/Resources/Biathlon/../../secret.json")));
            Assert.Catch(()=>BalanceTuningLock.FromJson(d.ToJson().Replace("locked_provisional","released")));
            Assert.That(TuningLockVerification.HashText("\uFEFFa\r\nb\r\n"),Is.EqualTo(TuningLockVerification.HashText("a\nb\n")));
        }
    }
}
