using Biathlon.Unity.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteDetailTests
    {
        [Test] public void ARealDistantShooterRetainsMeshAndPoseDistanceDetail()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+System.IO.Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=Biathlon.AI.ChampionshipRaceFactory.Create(Read,Fixture,Biathlon.Domain.CompetitionFormat.Sprint,
                new Biathlon.AI.BalanceEventAudit(5),new Biathlon.AI.AiDecisionCounts(),count:1);
            while(runner.Field.GetAthlete(0).ActiveShooting==null&&runner.Field.Tick.Value<10000)runner.Step();
            var state=RacePresentationSnapshot.Capture(runner.Field).Get(0);Assert.That(state.ShootingActive,Is.True);
            long paidTick=runner.Field.Tick.Value;
            var pivot=new GameObject("distant shooting performance");var rig=AthleteRigFactory.Create(pivot.transform);
            try
            {
                var adapter=pivot.AddComponent<AthleteAnimationPresentation>();adapter.Initialize(rig);
                var skin=rig.GetComponent<AthleteSkinDetailBinding>();
                int nearVertices=rig.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount;
                for(int t=1;t<=60;t++)adapter.Render(new AthleteAnimationFrame(state,t,60,1),80);
                Assert.That(adapter.Detail.Distant,Is.True);Assert.That(skin.Level,Is.EqualTo(2));
                Assert.That(adapter.Detail.PoseUpdates,Is.InRange(20,21));
                Assert.That(rig.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount,Is.LessThan(nearVertices));
                adapter.Render(new AthleteAnimationFrame(state,61,60,1),1);
                Assert.That(adapter.Detail.Distant,Is.False);Assert.That(skin.Level,Is.Zero);
                Assert.That(runner.Field.Tick.Value,Is.EqualTo(paidTick));
            }
            finally{Object.DestroyImmediate(pivot);}
        }
        [Test]
        public void DistantMeshRetainsRigMaterialsAndReducesGeometryWithoutLosingNearDetail()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>(); var original = skin.sharedMesh;
                var far = Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant");
                Assert.That(far, Is.Not.Null); Assert.That(far.vertexCount, Is.LessThan(original.vertexCount * .6f));
                Assert.That(far.triangles.Length, Is.LessThan(original.triangles.Length * .65f));
                Assert.That(far.subMeshCount, Is.EqualTo(original.subMeshCount)); Assert.That(far.bindposes, Is.EqualTo(original.bindposes));
                foreach (var weight in far.boneWeights) { Assert.That(weight.weight0, Is.EqualTo(1)); Assert.That(weight.boneIndex0, Is.InRange(0, (int)AthleteBone.Count - 1)); }
                var detail = new AthleteDetailPresentation(rig);
                Assert.That(detail.Prepare(60, new AthleteAnimationFrame(default, 1, 60, 1)), Is.True);
                Assert.That(skin.sharedMesh, Is.SameAs(far)); Assert.That(skin.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                detail.Prepare(42, new AthleteAnimationFrame(default, 2, 60, 1)); Assert.That(detail.Distant, Is.True);
                detail.Prepare(39, new AthleteAnimationFrame(default, 3, 60, 1)); Assert.That(detail.Distant, Is.False);
                Assert.That(skin.sharedMesh, Is.SameAs(original)); Assert.That(skin.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                detail.Prepare(48, new AthleteAnimationFrame(default, 4, 60, 1)); Assert.That(detail.Distant, Is.False);
                detail.Prepare(51, new AthleteAnimationFrame(default, 5, 60, 1)); Assert.That(detail.Distant, Is.True);
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void DistantPoseCadenceIsBoundedRewindableAndAllocationFreeAfterWarmup()
        {
            var pivot = new GameObject("detail test"); var rig = OriginalAthleteRigBuilder.Create(pivot.transform);
            try
            {
                var adapter = pivot.AddComponent<AthleteAnimationPresentation>(); adapter.Initialize(rig);
                for (int i = 1; i <= 60; i++) adapter.Render(new AthleteAnimationFrame(default, i, 60, 1), 80);
                Assert.That(adapter.Detail.PoseUpdates, Is.InRange(20, 21));
                int before = adapter.Detail.PoseUpdates;
                adapter.Render(new AthleteAnimationFrame(default, 1, 60, 1), 80);
                Assert.That(adapter.Detail.PoseUpdates, Is.EqualTo(before + 1), "Rewind forces a pose refresh.");
                long allocation = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 2; i <= 180; i++) adapter.Render(new AthleteAnimationFrame(default, i, 60, 1), 80);
                allocation = System.GC.GetAllocatedBytesForCurrentThread() - allocation;
                Assert.That(allocation, Is.Zero, "Pose scheduling and warmed procedural layers must not allocate.");
                before = adapter.Detail.PoseUpdates;
                for (int i = 181; i <= 240; i++) adapter.Render(new AthleteAnimationFrame(default, i, 60, 1), 5);
                Assert.That(adapter.Detail.PoseUpdates, Is.EqualTo(before + 60));
                Assert.That(pivot.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(pivot); }
        }
    }
}
