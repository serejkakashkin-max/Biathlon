using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class PlayablePursuitTests
    {
        [UnityTest] public IEnumerator SprintLoserRetainsIdentityAndQualificationGapInPursuit()
        {
            var root=new GameObject("sprint_to_pursuit");var sprint=root.AddComponent<PlayableSprintHost>();sprint.AutoStart=false;sprint.enabled=false;sprint.Initialize(2);
            var id=sprint.Runner.Field.GetAthlete(sprint.PlayerIndex).AthleteId;
            while(!sprint.Runner.Field.IsComplete&&sprint.Runner.Field.Tick.Value<50000){var t=sprint.Runner.Field.Tick.Next();sprint.Step(new PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(t,id,1000,1000),true,false));}
            Assert.That(sprint.Runner.Field.IsComplete,Is.True);var source=SprintResultSnapshot.FromJson(sprint.Runner.Field.Result.ToJson());
            Assert.That(source.Get(1).AthleteId,Is.EqualTo(id.Value));
            var pursuit=sprint.ContinueToPursuit();pursuit.enabled=false;
            Assert.That(pursuit.PlayerIndex,Is.EqualTo(1));var field=pursuit.Runner.Field;Assert.That(field.GetAthlete(pursuit.PlayerIndex).AthleteId,Is.EqualTo(id));
            Assert.That(field.GetAthlete(1).ScheduledStart.Value,Is.GreaterThan(field.GetAthlete(0).ScheduledStart.Value));
            while(!field.IsComplete&&field.Tick.Value<70000){var t=field.Tick.Next();pursuit.Step(new PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(t,id,1000,1000),true,false));}
            Assert.That(field.IsComplete,Is.True);Assert.That(field.PursuitResult.Count,Is.EqualTo(2));Assert.That(pursuit.Feed.Current.Get(pursuit.PlayerIndex).Hud.Final,Is.True);
            Object.Destroy(pursuit.gameObject);yield return null;
        }
    }
}
