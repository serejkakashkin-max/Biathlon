using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Presentation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class ShotImpactVisualDefinition
    {
        public int schema_version,marks_per_bank;
        public string id,material_resource;
        public float bullet_diameter_m,scuff_diameter_m,surface_offset_m;
        public static ShotImpactVisualDefinition Load()
        {
            var d=JsonUtility.FromJson<ShotImpactVisualDefinition>(ProductionVenueHost.ReadResource("Biathlon/shooting_impact_visual_v1"));
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||string.IsNullOrWhiteSpace(d.material_resource)||
                !(d.bullet_diameter_m>0&&d.bullet_diameter_m<=.02f)||!(d.scuff_diameter_m>=d.bullet_diameter_m&&d.scuff_diameter_m<=.03f)||
                !(d.surface_offset_m>0&&d.surface_offset_m<=.002f)||d.marks_per_bank<5||d.marks_per_bank>64)
                throw new ArgumentException("Invalid bounded impact presentation definition.");
            return d;
        }
    }

    /// <summary>Consumes every published accepted tick, even when several ticks share one render.
    /// Marks are never inferred from a moving sight or a changed target mask.</summary>
    public sealed class ShotImpactPresentation : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private TargetBankArt[] banks;
        private string onlyAthlete;
        private long observed=-1;
        public ShotImpactLedger Ledger {get;private set;}
        public ShotImpactVisualDefinition Definition {get;private set;}
        public TargetGeometryDefinition Geometry {get;private set;}
        public void Initialize(RacePresentationFeed source,TargetGeometryDefinition geometry,TargetBankArt[] targets,string selectedAthlete=null)
        {
            if(feed!=null||source==null||geometry==null||targets==null||targets.Length<1)throw new ArgumentException("Fresh impact view and bound target banks required.");
            Geometry=geometry;Definition=ShotImpactVisualDefinition.Load();Ledger=new ShotImpactLedger(geometry,Definition.bullet_diameter_m,128);
            banks=(TargetBankArt[])targets.Clone();onlyAthlete=selectedAthlete;
            foreach(var bank in banks){if(bank==null)throw new ArgumentException("Missing impact bank.");bank.ConfigureImpacts(geometry,Definition);}
            feed=source;feed.Published+=Observe;feed.RaceReset+=ResetHistory;
            if(feed.Current!=null)Observe(feed.Current);
        }
        private void Observe(RacePresentationSnapshot snapshot)
        {
            if(snapshot.Tick==observed)return;
            if(snapshot.Tick<observed){Ledger.Clear();foreach(var bank in banks)bank.ClearImpacts();}
            observed=snapshot.Tick;
            for(int i=0;i<snapshot.Count;i++)
            {
                var a=snapshot.Get(i);
                if(onlyAthlete!=null&&a.AthleteId!=onlyAthlete||!a.ShootingActive||a.ShotOutcome==ShotOutcome.NoShot)continue;
                if(Ledger.TryObserve(a.AthleteId,snapshot.Tick,a.CompletedBouts,a.RangeLane,a.ShotsFired,a.Stance,a.ShotOutcome,a.ShotImpact,out var impact))
                {
                    int index=onlyAthlete!=null?0:impact.Lane-1;
                    if(index>=0&&index<banks.Length)banks[index].RecordImpact(impact);
                }
            }
        }
        public void Shutdown()
        {if(feed!=null){feed.Published-=Observe;feed.RaceReset-=ResetHistory;}feed=null;banks=null;Ledger=null;Definition=null;Geometry=null;onlyAthlete=null;observed=-1;}
        private void ResetHistory(){Ledger.Clear();foreach(var bank in banks)bank.ClearImpacts();observed=-1;}
        private void OnDestroy()=>Shutdown();
    }
}
