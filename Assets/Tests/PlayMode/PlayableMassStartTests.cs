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
            int maximumConcurrentShooting=0;long playerWaitTicks=0,firstWaitTick=-1;string firstWaitLoop="";double firstWaitMeters=0;
            for(int i=0;i<30;i++)Assert.That(field.GetAthlete(i).ScheduledStart,Is.EqualTo(field.GetAthlete(0).ScheduledStart));
            while(!field.IsComplete&&field.Tick.Value<100000){var t=field.Tick.Next();host.Step(new PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(t,id,1000,1000),true,false));
                int shooting=0;for(int i=0;i<30;i++)if(field.GetAthlete(i).ActiveShooting!=null)shooting++;maximumConcurrentShooting=System.Math.Max(maximumConcurrentShooting,shooting);
                if(field.GetJunctionWaitSince(host.PlayerIndex)>0){playerWaitTicks++;if(firstWaitTick<0){firstWaitTick=field.Tick.Value;var at=field.GetPhysicalPoint(host.PlayerIndex);firstWaitLoop=at.LoopId;firstWaitMeters=at.LocalNanometers/1e9;}}
            }
            Assert.That(field.IsComplete,Is.True);Assert.That(field.MassStartResult.Count,Is.EqualTo(30));
            var state=host.Feed.Current.Get(host.PlayerIndex);Assert.That(state.Hud.Final,Is.True);Assert.That(state.CompletedBouts,Is.EqualTo(4));
            System.IO.Directory.CreateDirectory("artifacts/g2-owner-feedback");
            System.IO.File.WriteAllText("artifacts/g2-owner-feedback/mass-range.json",UnityEngine.JsonUtility.ToJson(new RangeDiagnostic{maximum_concurrent_shooting=maximumConcurrentShooting,player_wait_ticks=playerWaitTicks,first_wait_tick=firstWaitTick,first_wait_loop=firstWaitLoop,first_wait_meters=firstWaitMeters},true));
            Object.Destroy(root);yield return null;
        }
        [System.Serializable] private sealed class RangeDiagnostic
        {
            public int schema_version=1;public string id="mass_range_admission_diagnostic_v1";
            public int maximum_concurrent_shooting;public long player_wait_ticks,first_wait_tick;public string first_wait_loop;public double first_wait_meters;
            public string note="30 entrants, constant player effort800/line0, forced misses; junction tickets include all junctions, not only range; fixture diagnostic, not owner input replay";
        }
    }
}
