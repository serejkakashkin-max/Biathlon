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
    public class PenaltyPresentationTests
    {
        [UnityTest] public IEnumerator GeometricMissesTraversePenaltyGuideAndRejoin()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var field=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1).Field;
            var root=new GameObject("penalty_test");var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<PenaltyLoopPresentation>();
            view.Initialize(feed,field.GetAthlete(0).AthleteId.Value,field.Track,TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"),field.Track));
            var inputs=new SprintAthleteInput[1];var lines=new double[1];bool seen=false,rejoined=false;int priorLoops=0;
            for(int i=0;i<30000;i++)
            {
                var tick=field.Tick.Next();var id=field.GetAthlete(0).AthleteId;
                inputs[0]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,800),1,1,0),new WeaponAimCommand(tick,id,1000,1000),true);
                field.Step(inputs,lines);feed.Publish(RacePresentationSnapshot.Capture(field));view.Refresh();
                if(view.Active)
                {
                    seen=true;Assert.That(view.RaceSeconds,Is.EqualTo(field.Tick.Value/(double)field.TickFrequency));Assert.That(view.LoopProgress,Is.InRange(0,1));
                    Assert.That(view.RemainingLoops,Is.EqualTo(field.GetAthlete(0).Lifecycle.PendingPenaltyLoops));
                    if(priorLoops>0)Assert.That(view.RemainingLoops,Is.LessThanOrEqualTo(priorLoops));priorLoops=view.RemainingLoops;
                }
                else if(seen){rejoined=true;break;}
            }
            Assert.That(seen&&rejoined,Is.True);Assert.That(view.DisplayText,Is.Empty);
            feed.ResetRace();view.Refresh();Assert.That(view.RaceSeconds,Is.Zero);Object.Destroy(root);yield return null;
        }
    }
}
