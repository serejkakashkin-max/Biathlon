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
        [UnityTest] public IEnumerator ProductionBypassFeedsAcceptedLateralSpeedToSkisWithoutMovingRace()
        {
            string Read(string id)=>Resources.Load<TextAsset>(id).text;
            string Core(string name)=>Read("Biathlon/"+Path.GetFileNameWithoutExtension(name));
            const string v="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(v+"manifest")),Read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Read(v+"mass_start_profile")),
                RaceRoutePlan.FromJson(Read(v+"mass_start_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                Core,new BalanceEventAudit(5),new AiDecisionCounts(),count:6);
            var root=new GameObject("accepted_lateral_proxy_test");var feed=root.AddComponent<RacePresentationFeed>();
            var view=root.AddComponent<AthleteProxyPresentation>();view.Initialize(feed,venue);
            var field=runner.Field;var player=field.GetAthlete(0).AthleteId;double playerLine=field.GetLateral(0);bool observed=false;
            feed.Publish(RacePresentationSnapshot.Capture(field));
            for(int t=0;t<600&&!observed;t++)
            {
                var tick=field.Tick.Next();
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,player,0),1,1,0),new WeaponAimCommand(tick,player,0,0)),playerLine);
                feed.Publish(RacePresentationSnapshot.Capture(field));
                for(int i=1;i<field.Count;i++)
                {
                    var current=feed.Current.Get(i);var prior=feed.Previous.Get(i);
                    if(current.LoopId!=prior.LoopId||System.Math.Abs(current.LateralMeters-prior.LateralMeters)<.001)continue;
                    long before=field.GetAthlete(i).Motion.DistanceNanometers;view.Render(1);
                    var shown=view.GetProxy(current.AthleteId).GetComponent<AthleteAnimationPresentation>().LastFrame;
                    Assert.That(shown.LateralMetersPerSecond,Is.EqualTo((current.LateralMeters-prior.LateralMeters)*field.TickFrequency).Within(1e-9));
                    Assert.That(view.GetRig(current.AthleteId).GetComponent<AthleteLocomotionDriver>().State,Is.Not.EqualTo(SkiLocomotion.Idle));
                    Assert.That(field.GetAthlete(i).Motion.DistanceNanometers,Is.EqualTo(before));observed=true;break;
                }
            }
            Assert.That(observed,Is.True,"Actual stationary-player bypass must feed the observed sideways movement.");
            Object.Destroy(root);yield return null;
        }
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
