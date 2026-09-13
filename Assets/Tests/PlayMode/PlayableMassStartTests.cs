using System.Collections;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class PlayableMassStartTests
    {
        [UnityTest] public IEnumerator ThirtyAthletesCompleteWithPlayerAndSharedGrid()
        {
            var root=new GameObject("mass_host_test");var host=root.AddComponent<PlayableSprintHost>();host.AutoStart=false;host.enabled=false;host.InitializeMassStart();
            var field=host.Runner.Field;Assert.That(field.Count,Is.EqualTo(30));Assert.That(host.Feed.Current.Count,Is.EqualTo(30));
            var id=field.GetAthlete(host.PlayerIndex).AthleteId;
            for(int i=0;i<30;i++)Assert.That(field.GetAthlete(i).ScheduledStart,Is.EqualTo(field.GetAthlete(0).ScheduledStart));
            while(!field.IsComplete&&field.Tick.Value<100000){var t=field.Tick.Next();host.Step(new PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(t,id,1000,1000),true,false));}
            Assert.That(field.IsComplete,Is.True);Assert.That(field.MassStartResult.Count,Is.EqualTo(30));
            var state=host.Feed.Current.Get(host.PlayerIndex);Assert.That(state.Hud.Final,Is.True);Assert.That(state.CompletedBouts,Is.EqualTo(4));
            Object.Destroy(root);yield return null;
        }
    }
}
