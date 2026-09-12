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
    public class RaceHudPlayModeTests
    {
        [UnityTest] public IEnumerator HudShowsSplitContextAndResets()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var root=new GameObject("race_hud_test");var feed=root.AddComponent<RacePresentationFeed>();var hud=root.AddComponent<RaceHud>();hud.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value);
            for(int i=0;i<100;i++)runner.Step();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));hud.Refresh();
            Assert.That(hud.DisplayText,Does.Contain("Lap 1 / 3").And.Contain("Split").And.Contain("Shooting - | -"));
            feed.ResetRace();hud.Refresh();Assert.That(hud.DisplayText,Does.Contain("Waiting"));Object.Destroy(root);yield return null;
        }
    }
}
