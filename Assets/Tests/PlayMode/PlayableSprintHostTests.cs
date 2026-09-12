using System.Collections;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class PlayableSprintHostTests
    {
        [UnityTest] public IEnumerator ComposedHostCompletesSprintWithPlayerCommands()
        {
            var root=new GameObject("playable_host_test");var host=root.AddComponent<PlayableSprintHost>();host.AutoStart=false;host.enabled=false;host.Initialize(2);
            var id=host.Runner.Field.GetAthlete(0).AthleteId;
            while(!host.Runner.Field.IsComplete&&host.Runner.Field.Tick.Value<50000)
            {
                var t=host.Runner.Field.Tick.Next();host.Step(new PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(t,id,1000,1000),true,false));
            }
            Assert.That(host.Runner.Field.IsComplete,Is.True);Assert.That(host.Feed.Current.Complete,Is.True);Assert.That(host.Runner.Field.GetAthlete(0).TotalPenaltyLoops,Is.EqualTo(10));
            Object.Destroy(root);yield return null;
        }
    }
}
