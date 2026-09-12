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
    public class AthleteProxyTests
    {
        [UnityTest] public IEnumerator ThirtyStableIdentitiesFollowSnapshotsWithoutMovingAuthority()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts());
            var visual=TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"),runner.Field.Track);var root=new GameObject("proxy_test");var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<AthleteProxyPresentation>();view.Initialize(feed,visual);
            feed.Publish(RacePresentationSnapshot.Capture(runner.Field));view.Render(1);Assert.That(view.Count,Is.EqualTo(30));
            for(int tick=0;tick<20;tick++){runner.Step();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));view.Render(1);}
            long before=runner.Field.GetAthlete(0).Motion.DistanceNanometers;var state=feed.Current.Get(0);var position=visual.Get(state.LoopId).Sample(state.LocalNanometers/1e9);var proxy=view.GetProxy(state.AthleteId);
            Assert.That(proxy.localPosition.y,Is.EqualTo((float)position.Y+1).Within(.0001));var old=proxy;view.Render(.5);Assert.That(view.GetProxy(state.AthleteId),Is.SameAs(old));Assert.That(runner.Field.GetAthlete(0).Motion.DistanceNanometers,Is.EqualTo(before));
            yield return null;Assert.That(proxy.GetComponent<Collider>(),Is.Null);Object.Destroy(root);yield return null;
        }
    }
}
