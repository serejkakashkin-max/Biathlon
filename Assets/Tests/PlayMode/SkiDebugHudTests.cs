using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class SkiDebugHudTests
    {
        [UnityTest] public IEnumerator HudUsesStableIdentityAndHandlesRaceReset()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var root=new GameObject("hud_test");var feed=root.AddComponent<RacePresentationFeed>();var hud=root.AddComponent<SkiDebugHud>();
            hud.Initialize(feed,runner.Field.GetAthlete(1).AthleteId.Value);
            Assert.That(hud.DisplayText,Does.Contain("Waiting"));
            for(int i=0;i<20;i++)runner.Step();
            feed.Publish(RacePresentationSnapshot.Capture(runner.Field));hud.Refresh();
            Assert.That(hud.DisplayText,Does.Contain(runner.Field.GetAthlete(1).AthleteId.Value));
            Assert.That(hud.DisplayText,Does.Contain("tick 20").And.Contain("km/h").And.Contain("bpm").And.Contain("drag reduction"));
            var text=hud.DisplayText;hud.Refresh();Assert.That(hud.DisplayText,Is.SameAs(text));
            hud.Initialize(feed,"missing");Assert.That(hud.DisplayText,Does.Contain("unavailable"));
            feed.ResetRace();hud.Refresh();Assert.That(hud.DisplayText,Does.Contain("Waiting"));
            yield return null;Object.Destroy(root);yield return null;
        }
    }
}
