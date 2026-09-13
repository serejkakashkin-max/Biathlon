using System;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteRifleTests
    {
        [Test]
        public void RealBoutObservationsDriveTransferWithoutChangingTheShootingClock()
        {
            string Read(string n) => Resources.Load<TextAsset>("Biathlon/" + System.IO.Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n) => System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../tests/fixtures/tracks/" + n));
            var runner = Biathlon.AI.ChampionshipRaceFactory.Create(Read, Fixture, Biathlon.Domain.CompetitionFormat.Sprint,
                new Biathlon.AI.BalanceEventAudit(5), new Biathlon.AI.AiDecisionCounts(), count: 1);
            var root = new GameObject("rifle integration");
            var feed = root.AddComponent<RacePresentationFeed>(); var proxies = root.AddComponent<AthleteProxyPresentation>();
            try
            {
                proxies.Initialize(feed, Biathlon.Domain.TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"), runner.Field.Track));
                int seen = 0, stances = 0;
                for (int tick = 0; tick < 20000; tick++)
                {
                    runner.Step(); var snapshot = RacePresentationSnapshot.Capture(runner.Field); var a = snapshot.Get(0);
                    feed.Publish(snapshot); proxies.Render(1);
                    var rifle = proxies.GetRig(a.AthleteId).GetComponent<AthleteRiflePresentation>();
                    if (a.ShootingPhase == ShootingPhase.Aiming && a.Stance == Biathlon.Domain.ShootingStance.Prone)
                    {
                        var rig = proxies.GetRig(a.AthleteId);
                        Assert.That(rig.transform.InverseTransformPoint(rig.Bone(AthleteBone.Head).position).y, Is.LessThan(.5f), "Prone body must be ready at the actual aiming deadline.");
                    }
                    if (a.ShootingPhase == ShootingPhase.Aiming)
                    {
                        var presentedRig = proxies.GetRig(a.AthleteId);
                        var ray = a.Sight.Ray;
                        var expected = presentedRig.transform.TransformDirection(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ));
                        Assert.That(Vector3.Angle(presentedRig.GetComponent<AthleteEquipmentBinding>().Rifle.forward,expected),Is.LessThan(.03f),"Use composed Sight exactly once, including sway and handling.");
                        stances |= 1 << (int)a.Stance;
                        if (a.Stance == Biathlon.Domain.ShootingStance.Standing)
                        {
                            var rig = proxies.GetRig(a.AthleteId);
                            Assert.That(rig.transform.InverseTransformPoint(rig.Bone(AthleteBone.Head).position).y, Is.GreaterThan(1.2f));
                            Assert.That(Vector3.Dot(rig.Bone(AthleteBone.Hips).forward,rig.transform.right),Is.GreaterThan(.8f));
                        }
                    }
                    Assert.That(rifle.TransferProgress, Is.EqualTo(AthleteRiflePresentation.Progress(a.ShootingPhase, a.ShootingPhaseElapsedTicks, a.ShootingPhaseDurationTicks)));
                    Assert.That(runner.Field.Tick.Value, Is.EqualTo(snapshot.Tick));
                    Assert.That(runner.Field.GetAthlete(0).ActiveShooting?.PhaseRemainingTicks ?? 0, Is.EqualTo(a.ShootingPhaseRemainingTicks));
                    seen |= 1 << (int)a.ShootingPhase;
                    if (a.CompletedBouts >= 2 && !a.ShootingActive) break;
                }
                foreach (var phase in new[] { ShootingPhase.Setup, ShootingPhase.Aiming, ShootingPhase.BoltCycle, ShootingPhase.Exiting })
                    Assert.That(seen & (1 << (int)phase), Is.Not.Zero, phase.ToString());
                Assert.That(stances,Is.EqualTo(3),"Both actual Sprint stance deadlines are presented.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void TransferUsesTheActualPhaseDeadlineAtDifferentTickRates()
        {
            foreach (int frequency in new[] {20,30,60})
            {
                int duration = frequency * 3;
                Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Setup, 0, duration), Is.Zero);
                Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Setup, duration / 2, duration), Is.EqualTo(.5f));
                Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Exiting, duration / 2, duration), Is.EqualTo(.5f));
                Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Exiting, duration, duration), Is.Zero);
            }
            Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Aiming,0,0), Is.EqualTo(1));
            Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.BoltCycle,2,4), Is.EqualTo(1));
            Assert.That(AthleteRiflePresentation.Progress(ShootingPhase.Complete,0,0), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => AthleteRiflePresentation.Progress(ShootingPhase.Setup, 3, 2));
        }

        [Test]
        public void RifleTransferReturnsExactlyAndHandsReachReadyWithoutRootOrPropDrift()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                rig.transform.SetPositionAndRotation(new Vector3(2,1,-3),Quaternion.Euler(0,47,0));
                var rifle = rig.GetComponent<AthleteRiflePresentation>(); var props=rig.GetComponent<AthleteEquipmentBinding>();
                props.Validate(); var settings=RiflePresentationSettings.Load();
                var parent=props.Rifle.parent; var root=rig.transform.position;
                Sample(0); var carried=props.Rifle.position; var rotation=props.Rifle.rotation; var prior=carried;
                for(int i=1;i<=100;i++)
                {
                    Sample(i/100f);
                    Assert.That(Vector3.Distance(prior,props.Rifle.position),Is.LessThan(.045f));
                    Assert.That(props.Rifle.parent,Is.SameAs(parent)); prior=props.Rifle.position;
                }
                Assert.That(Vector3.Distance(props.Rifle.position,rig.Socket(AthleteSocket.RifleAim).position),Is.LessThan(.0001f));
                Assert.That(Vector3.Angle(props.Rifle.forward,rig.Socket(AthleteSocket.RifleAim).forward),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(rig.Bone(AthleteBone.RightHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(settings.trigger_grip)),Is.LessThan(.015f));
                Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(settings.support_grip)),Is.LessThan(.015f));
                Assert.That(Vector3.Distance(props.LeftPole.position,rig.transform.TransformPoint(settings.pole_stow_left)),Is.LessThan(.0001f));
                for(int i=99;i>=0;i--) Sample(i/100f);
                Assert.That(Vector3.Distance(carried,props.Rifle.position),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(rotation,props.Rifle.rotation),Is.LessThan(.001f));
                Assert.That(rig.transform.position,Is.EqualTo(root));
                void Sample(float p){rig.ResetPose();rifle.Sample(rig,p);}
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}
