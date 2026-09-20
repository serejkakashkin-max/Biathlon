using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>One detached real-race skiing pose for material/lighting comparison, not a playable race.</summary>
    public sealed class LightingAthletePreview : MonoBehaviour
    {
        public Camera View;
        private void Start()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            var runner=ChampionshipRaceFactory.Create(Read,n=>Resources.Load<TextAsset>("Biathlon/Tracks/"+Path.GetFileNameWithoutExtension(n)).text,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            for(int i=0;i<400;i++)runner.Step();var fixture=gameObject.AddComponent<AthleteCrowdValidation>();fixture.Initialize(RacePresentationSnapshot.Capture(runner.Field),View);fixture.Frozen=true;fixture.SampleTime=20;
        }
    }
}
