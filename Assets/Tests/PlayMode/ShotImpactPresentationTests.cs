using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class ShotImpactPresentationTests
    {
        private static string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
        [Test]
        public void EveryActualShotIncludingBothFifthsSurvivesWithoutRenderingAndResetsWithTheRace()
        {
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=Biathlon.AI.ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new Biathlon.AI.BalanceEventAudit(5),new Biathlon.AI.AiDecisionCounts(),count:2);
            var initial=RacePresentationSnapshot.Capture(runner.Field);string actor=initial.Get(0).AthleteId;
            var root=new GameObject("Accepted impact integration");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();
                var geometry=TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json"));
                var bank=Object.Instantiate(Resources.Load<TargetBankArt>("Biathlon/Stadium/TargetBank"),root.transform);
                var view=root.AddComponent<ShotImpactPresentation>();view.Initialize(feed,geometry,new[]{bank},actor);
                int accepted=0,fifths=0,stances=0;
                for(int i=0;i<20000;i++)
                {
                    runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);var a=snapshot.Get(0);
                    feed.Publish(snapshot);
                    if(a.ShootingActive&&a.ShotOutcome!=ShotOutcome.NoShot)
                    {
                        accepted++;if(a.ShotsFired==5)fifths++;stances|=1<<(int)a.Stance;
                        var evidence=view.Ledger.Last(actor);
                        Assert.That(evidence.Tick,Is.EqualTo(snapshot.Tick));Assert.That(evidence.Impact.X,Is.EqualTo(a.ShotImpact.X));Assert.That(evidence.Impact.Y,Is.EqualTo(a.ShotImpact.Y));
                        Assert.That(evidence.Outcome,Is.EqualTo(a.ShotOutcome));Assert.That(evidence.Ordinal,Is.EqualTo(a.ShotsFired));
                    }
                    if(a.CompletedBouts>=2&&!a.ShootingActive)break;
                }
                Assert.That(accepted,Is.EqualTo(10));Assert.That(fifths,Is.EqualTo(2));Assert.That(stances,Is.EqualTo(3));
                Assert.That(view.Ledger.AcceptedCount,Is.EqualTo(10),"Catch-up ticks are observed without camera/HUD refresh; foreign athlete shots are excluded.");
                feed.ResetRace();Assert.That(view.Ledger.AcceptedCount,Is.Zero);Assert.That(bank.ImpactMarks.Count,Is.Zero);feed.Publish(initial);
                view.Shutdown();Assert.DoesNotThrow(()=>feed.Publish(RacePresentationSnapshot.Capture(runner.Field)));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void OnlyTheFrontPlateGetsBoundedPersistentDamageAndHitsDoNotDrillClosureFlaps()
        {
            var root=new GameObject("Bounded target impact geometry");
            try
            {
                var geometry=TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json"));var resolver=new TargetGeometrySystem(geometry);
                var settings=ShotImpactVisualDefinition.Load();var ledger=new ShotImpactLedger(geometry,settings.bullet_diameter_m,1);
                var bank=Object.Instantiate(Resources.Load<TargetBankArt>("Biathlon/Stadium/TargetBank"),root.transform);bank.Apply(geometry,ShootingStance.Prone,0);bank.ConfigureImpacts(geometry,settings);
                var mesh=bank.ImpactMarks.MarkMesh;var material=bank.ImpactMarks.MarkRenderer.sharedMaterial;int objects=root.GetComponentsInChildren<Transform>(true).Length;
                Assert.That(Add(0,0,ShotOutcome.TargetClosed),Is.False,"A central bullet passed through the aperture, not through the visible white flap.");
                Assert.That(Add(geometry.Radius(ShootingStance.Prone)-.0002,0,ShotOutcome.TargetClosed),Is.True,"An edge hit can scrape the front-plate rim.");
                Assert.That(Add(geometry.Radius(ShootingStance.Prone)+.0002,0,ShotOutcome.Miss),Is.True);
                Assert.That(Add(50,50,ShotOutcome.Miss),Is.False,"Do not hang a hole in empty space outside the actual bank.");
                for(int i=0;i<settings.marks_per_bank+10;i++)Assert.That(Add(.08,.05,ShotOutcome.Miss),Is.True);
                Assert.That(bank.ImpactMarks.Count,Is.EqualTo(settings.marks_per_bank));Assert.That(bank.ImpactMarks.MarkMesh,Is.SameAs(mesh));Assert.That(bank.ImpactMarks.MarkRenderer.sharedMaterial,Is.SameAs(material));
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(objects));
                foreach(var v in mesh.vertices)Assert.That(float.IsFinite(v.x)&&float.IsFinite(v.y)&&v.z==-settings.surface_offset_m,Is.True);
                bank.Apply(geometry,ShootingStance.Standing,31);Assert.That(bank.ImpactMarks.Count,Is.EqualTo(settings.marks_per_bank),"Historical plate damage persists across occupants; shader hides apertures and closed flaps.");
                bank.ClearImpacts();Assert.That(bank.ImpactMarks.Count,Is.Zero);Assert.That(bank.ImpactMarks.MarkRenderer.enabled,Is.False);
                bool Add(double x,double y,ShotOutcome outcome)
                {
                    var impact=resolver.IntersectPlanePoint(x,y,ShootingStance.Prone);int tick=ledger.AcceptedCount+1;
                    Assert.That(ledger.TryObserve("player",tick,tick,1,1,ShootingStance.Prone,outcome,impact,out var evidence),Is.True);
                    return bank.RecordImpact(evidence);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
