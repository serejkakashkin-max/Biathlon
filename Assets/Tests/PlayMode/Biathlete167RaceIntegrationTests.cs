#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167RaceIntegrationTests
    {
        private static string Read(string name) => Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(name)).text;
        private static string Fixture(string name) => File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/",name));

        [Test] public void NewFieldKeepsTheExistingNearAndDistantShadowPolicy()
        {
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.MassStart,new BalanceEventAudit(5),new AiDecisionCounts());
            var geometry=TrackVisualDefinition.FromJson(Read("mass_stadium_visual_v1.json"),runner.Field.Track);
            var root=new GameObject("Upgraded field shadow compatibility");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<AthleteProxyPresentation>();view.Initialize(feed,geometry);
                var camera=new GameObject("Distance policy camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(10000,0,10000);view.SetViewCamera(camera);
                for(int tick=0;tick<200;tick++)runner.Step();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));view.Render(1);
                string id=runner.Field.GetAthlete(0).AthleteId.Value;
                var target=view.GetRig(id).GetComponent<Biathlete167RacePresentation>().Rig;
                foreach(var renderer in target.GetComponentsInChildren<Renderer>(true))Assert.That(renderer.shadowCastingMode,Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
                camera.transform.position=view.GetProxy(id).position+Vector3.up*2;view.Render(1);
                foreach(var renderer in target.GetComponentsInChildren<Renderer>(true))Assert.That(renderer.shadowCastingMode,Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [UnityTest] public IEnumerator AutomaticLodKeepsTheRigidSkiTipsVisible()
        {
            var root=new GameObject("Runtime automatic LOD equipment witness");
            var source=AthleteRigFactory.Create(root.transform);source.transform.position=new Vector3(10000,0,10000);
            var visual=Biathlete167RacePresentation.Attach(source);
            var camera=new GameObject("Equipment witness camera").AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.green;camera.orthographic=true;camera.orthographicSize=1.4f;camera.aspect=1;
            var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
            camera.transform.SetPositionAndRotation(source.transform.position+Vector3.up*3.2f,Quaternion.LookRotation(Vector3.down,Vector3.forward));
            foreach(var t in visual.Rig.GetComponentsInChildren<Transform>(true))t.gameObject.layer=26;
            camera.cullingMask=1<<26;
            var meshes=visual.Rig.Bone(HumanBodyBones.LeftFoot).GetComponentsInChildren<MeshFilter>(true);
            var mesh=meshes[0];foreach(var candidate in meshes)if(candidate.sharedMesh.bounds.size.sqrMagnitude>mesh.sharedMesh.bounds.size.sqrMagnitude)mesh=candidate;
            var bounds=mesh.sharedMesh.bounds;var size=bounds.size;
            var axis=size.x>size.y&&size.x>size.z?Vector3.right:size.y>size.z?Vector3.up:Vector3.forward;
            var extent=Mathf.Max(bounds.extents.x,Mathf.Max(bounds.extents.y,bounds.extents.z));
            string folder="artifacts/m16-7-race-integration/lod-witness";Directory.CreateDirectory(folder);
            try
            {
                foreach(int lod in new[]{0,-1})
                {
                    visual.Rig.Lods.ForceLOD(lod);
                    using(var target=new BenchmarkRenderTarget(512,512))
                    {
                        for(int frame=0;frame<3;frame++){target.Render(camera);yield return new WaitForEndOfFrame();yield return null;}
                        string path=folder+"/lod-"+lod+".png";target.SavePng(path);
                        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
                        try
                        {
                            var point=camera.WorldToViewportPoint(mesh.transform.TransformPoint(bounds.center+axis*(extent*.72f)));
                            var pixel=texture.GetPixel(Mathf.RoundToInt(point.x*512),Mathf.RoundToInt(point.y*512));
                            Assert.That(pixel.g-Mathf.Max(pixel.r,pixel.b),Is.LessThan(.3f),"Ski tip disappeared into the green background at LOD "+lod);
                        }
                        finally{Object.Destroy(texture);}
                    }
                }
            }
            finally{Object.Destroy(camera.gameObject);Object.Destroy(root);}
        }

        [Test] public void LiveAimingHidesTheWholeNewVisualAndRestoresItOnExit()
        {
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            var root=new GameObject("New body aiming visibility regression");
            try
            {
                var source=AthleteRigFactory.Create(root.transform);
                var visual=Biathlete167RacePresentation.Attach(source);
                var feed=root.AddComponent<RacePresentationFeed>();var range=root.AddComponent<RangePresentation>();range.enabled=false;
                range.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")));
                range.BindPlayerVisibility(source);
                bool reached=false;
                for(int tick=0;tick<10000;tick++)
                {
                    runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);range.Refresh();
                    if(!range.Visible)continue;
                    reached=true;
                    var state = snapshot.Get(0);
                    var frame = new AthleteAnimationFrame(state, snapshot.Tick, snapshot.TickFrequency, 1);
                    int poses = visual.RenderCount;
                    visual.Render(frame);
                    Assert.That(visual.Rig.gameObject.activeSelf,Is.False,"The upgraded body and instanced equipment must not obstruct the sight camera.");
                    Assert.That(visual.RenderCount, Is.EqualTo(poses), "A hidden sight body must not solve its shooting pose.");
                    feed.ResetRace();range.Refresh();
                    Assert.That(visual.Rig.gameObject.activeSelf,Is.True,"Return the new athlete to chase/broadcast view.");
                    visual.Render(frame, visibleShootingPose: false);
                    var next = new AthleteAnimationFrame(state, snapshot.Tick + 1, snapshot.TickFrequency, 1);
                    visual.Render(next, visibleShootingPose: false);
                    Assert.That(visual.RenderCount, Is.EqualTo(poses + 1), "Two adjacent off-camera frames reuse the fitted pose.");
                    visual.Render(next, visibleShootingPose: true);
                    Assert.That(visual.RenderCount, Is.EqualTo(poses + 2), "The first visible frame uses the latest accepted state.");
                    break;
                }
                Assert.That(reached,Is.True,"A real aiming phase must be reached.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void BothRaceSetupsCarryPolesAcrossTheArrivalSeam()
        {
            var runner = ChampionshipRaceFactory.Create(Read, Fixture, CompetitionFormat.Sprint,
                new BalanceEventAudit(5), new AiDecisionCounts(), count: 1);
            var geometry = TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"), runner.Field.Track);
            var root = new GameObject("Arrival transition witness");
            try
            {
                var feed = root.AddComponent<RacePresentationFeed>();
                var view = root.AddComponent<AthleteProxyPresentation>();
                view.Initialize(feed, geometry, shootingDirection: Vector3.right);
                string id = runner.Field.GetAthlete(0).AthleteId.Value;
                var checkedStances = new System.Collections.Generic.HashSet<ShootingStance>();
                Transform pole = null, leftPole = null;
                Biathlete167Rig athlete = null;
                Biathlete167RacePresentation visual = null;
                Vector3 gripLocal = default, leftGripLocal = default;
                ShootingPhase previousPhase = ShootingPhase.Outside;
                bool leftLift = false, rightLift = false;
                var gathered = new System.Collections.Generic.HashSet<ShootingStance>();
                var laidTogether = new System.Collections.Generic.HashSet<ShootingStance>();
                var evidence = new System.Collections.Generic.List<string> { "stance,held_gap_m,previous_instant_stow_gap_m" };
                for (int tick = 0; tick < 20000 && !runner.Field.IsComplete; tick++)
                {
                    runner.Step();
                    var snapshot = RacePresentationSnapshot.Capture(runner.Field);
                    var state = snapshot.Get(0);
                    feed.Publish(snapshot);
                    view.Render(1);
                    if (pole == null)
                    {
                        visual = view.GetRig(id).GetComponent<Biathlete167RacePresentation>();
                        athlete = visual.Rig;
                        pole = FindSkiPole(athlete.Bone(HumanBodyBones.RightHand));
                        leftPole = FindSkiPole(athlete.Bone(HumanBodyBones.LeftHand));
                        gripLocal = pole.InverseTransformPoint(athlete.GripContact(true).position);
                        leftGripLocal = leftPole.InverseTransformPoint(athlete.GripContact(false).position);
                    }
                    if (state.ShootingPhase == ShootingPhase.Setup && previousPhase != ShootingPhase.Setup)
                    {
                        float gap = Vector3.Distance(pole.TransformPoint(gripLocal), athlete.GripContact(true).position);
                        Assert.That(gap, Is.LessThan(.12f), $"{state.Stance}: poles must stay in the hands on arrival.");
                        // Exercise the earlier immediate-stow presentation on the same accepted setup frame.
                        visual.RenderShootingPreview(state.Stance, 0, state.Sight);
                        float oldGap = Vector3.Distance(pole.TransformPoint(gripLocal), athlete.GripContact(true).position);
                        Assert.That(oldGap, Is.GreaterThan(.35f));
                        evidence.Add(FormattableString.Invariant($"{state.Stance},{gap},{oldGap}"));
                        view.Render(1);
                        checkedStances.Add(state.Stance);
                    }
                    if (state.ShootingPhase == ShootingPhase.Setup)
                    {
                        float phase = (float)state.ShootingPhaseElapsedTicks / state.ShootingPhaseDurationTicks;
                        var rightGrip = pole.TransformPoint(gripLocal);
                        var leftGrip = leftPole.TransformPoint(leftGripLocal);
                        float separation = Vector3.Distance(leftGrip, rightGrip);
                        if (phase >= .20f && phase <= .30f)
                        {
                            var hand = athlete.GripContact(true).position;
                            if (separation > .02f && separation < .07f &&
                                Vector3.Distance(hand, rightGrip) < .045f && Vector3.Distance(hand, leftGrip) < .045f &&
                                Vector3.Distance(athlete.GripContact(false).position, leftGrip) > .09f)
                                gathered.Add(state.Stance);
                        }
                        if (phase >= .87f && separation > .02f && separation < .08f)
                        {
                            var localRight = athlete.transform.InverseTransformPoint(rightGrip);
                            var localLeft = athlete.transform.InverseTransformPoint(leftGrip);
                            if (localRight.x > .24f && localLeft.x > .24f &&
                                Mathf.Abs(localRight.y) < .10f && Mathf.Abs(localLeft.y) < .10f)
                                laidTogether.Add(state.Stance);
                        }
                        if (state.Stance == ShootingStance.Standing)
                        {
                            float difference = athlete.ToeHinge(false).position.y - athlete.ToeHinge(true).position.y;
                            leftLift |= difference > .02f;
                            rightLift |= difference < -.02f;
                        }
                    }
                    if (checkedStances.Count == 2 && state.ShootingPhase == ShootingPhase.Aiming) break;
                    previousPhase = state.ShootingPhase;
                }
                Assert.That(checkedStances.Count, Is.EqualTo(2));
                Assert.That(gathered.Count, Is.EqualTo(2), "Both grips must follow the right hand before being laid down.");
                Assert.That(laidTogether.Count, Is.EqualTo(2), "Both poles must rest together on the right of the mat.");
                Assert.That(leftLift && rightLift, Is.True, "Alternate ski lifts before the standing aim.");
                const string folder = "artifacts/m16-7-range-smooth";
                Directory.CreateDirectory(folder);
                File.WriteAllLines(folder + "/held-grip-seam.csv", evidence);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Transform FindSkiPole(Transform hand)
        {
            Transform pole = null;
            float largest = 0;
            foreach (var mesh in hand.GetComponentsInChildren<MeshFilter>(true))
            {
                float size = mesh.sharedMesh.bounds.size.sqrMagnitude;
                if (size > largest) { largest = size; pole = mesh.transform; }
            }
            return pole;
        }

        [TestCase(0f,false)]
        [TestCase(.08f,false)]
        [TestCase(0f,true)]
        public void RaceAdapterUsesTheApprovedQuietBodyThroughTheLiveSourceDriver(float grade,bool sprint)
        {
            var root=new GameObject("Runtime motion definition regression");
            var source=AthleteRigFactory.Create(root.transform);
            try
            {
                var visual=Biathlete167RacePresentation.Attach(source);
                var driver=source.GetComponent<AthleteHybridLocomotionDriver>();
                var input=new LocomotionInput(sprint?9:6,grade,sprint?1.2f:.9f,sprint:sprint);
                float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int frame=0;frame<360;frame++)
                {
                    driver.Sample(source,input,frame/60d,new ExertionInput(0,1,140));
                    visual.RenderPreview();
                    float x=visual.Rig.transform.InverseTransformPoint(visual.Rig.Bone(HumanBodyBones.Hips).position).x;
                    if(frame<120)continue;min=Mathf.Min(min,x);max=Mathf.Max(max,x);
                }
                Assert.That(max-min,Is.LessThanOrEqualTo(.001f),"The race must use the accepted no-sway motion definition, just like the studio.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(0f, false, 1.05f)]
        [TestCase(0f, true, 1.10f)]
        [TestCase(.08f, true, .60f)]
        public void SlowSkiingDoesNotUseRaceSpeedPoling(float grade, bool sprint, float maximumPushesPerSecond)
        {
            var root = new GameObject("Speed-matched live poling witness");
            try
            {
                var source = AthleteRigFactory.Create(root.transform);
                var visual = Biathlete167RacePresentation.Attach(source);
                var driver = source.GetComponent<AthleteHybridLocomotionDriver>();
                float peakHandSpeed = 0;
                Vector3 previous = default;
                for (int frame = 0; frame <= 360; frame++)
                {
                    driver.Sample(source, new LocomotionInput(1.8f, grade, sprint ? 1.3f : .9f, sprint: sprint), frame / 60d);
                    visual.RenderPreview();
                    Vector3 hand = visual.Rig.GripContact(true).position;
                    if (frame > 60) peakHandSpeed = Mathf.Max(peakHandSpeed, Vector3.Distance(previous, hand) * 60);
                    previous = hand;
                }
                Assert.That(driver.PoleCadenceHz, Is.InRange(.25f, maximumPushesPerSecond));
                Assert.That(peakHandSpeed, Is.LessThan(2.2f), "The final rendered hand must return at a controlled speed.");
                if (grade > 0) Assert.That(driver.PoleCadenceHz / driver.CadenceHz, Is.EqualTo(1).Within(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(ShootingStance.Prone)]
        [TestCase(ShootingStance.Standing)]
        public void UpgradedRifleFollowsTheAcceptedSightInsteadOfTheNominalLane(ShootingStance stance)
        {
            var root=new GameObject("Accepted sight visual fixture");
            var source=AthleteRigFactory.Create(root.transform);
            try
            {
                var visual=Biathlete167RacePresentation.Attach(source);
                var props=source.GetComponent<AthleteEquipmentBinding>();
                foreach(float sign in new[]{-1f,0f,1f})
                {
                    var sight=new WeaponAimState(sign*.05,sign*.03);
                    source.ResetPose();
                    source.GetComponent<AthleteShootingPose>().Sample(source,stance,1);
                    source.GetComponent<AthleteRiflePresentation>().Sample(source,1,sight,stance);
                    source.GetComponent<AthleteGripPresentation>().Render(source);
                    var position=source.ModelRoot.position;var rotation=source.ModelRoot.rotation;
                    visual.RenderShootingPreview(stance,1,sight);
                    Assert.That(Vector3.Angle(visual.Rig.Rifle.forward,props.Rifle.forward),Is.LessThan(.05f),"The upgraded rifle must display the same accepted aim.");
                    Assert.That(source.ModelRoot.position,Is.EqualTo(position));
                    Assert.That(source.ModelRoot.rotation,Is.EqualTo(rotation));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(CompetitionFormat.Sprint)]
        [TestCase(CompetitionFormat.MassStart)]
        public void DefaultRaceUsesTheNewVisualForTheEntireFieldWithoutAdvancingTheRace(CompetitionFormat format)
        {
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,format,new BalanceEventAudit(5),new AiDecisionCounts());
            var visual=TrackVisualDefinition.FromJson(Read(format==CompetitionFormat.MassStart?"mass_stadium_visual_v1.json":"stadium_visual_v1.json"),runner.Field.Track);
            var root=new GameObject("M16.7 race integration fixture");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();
                var view=root.AddComponent<AthleteProxyPresentation>();
                view.Initialize(feed,visual);
                for(int tick=0;tick<200;tick++)runner.Step();
                var snapshot=RacePresentationSnapshot.Capture(runner.Field);
                feed.Publish(snapshot);view.Render(1);
                Assert.That(view.Count,Is.EqualTo(30));
                for(int i=0;i<snapshot.Count;i++)
                {
                    var id=snapshot.Get(i).AthleteId;
                    var candidate=view.GetProxy(id).GetComponentInChildren<Biathlete167Rig>(true);
                    Assert.That(candidate,Is.Not.Null,"Missing new race athlete: "+id);
                    var rig=view.GetRig(id);
                    foreach(var renderer in rig.GetComponentsInChildren<Renderer>(true))
                        Assert.That(renderer.forceRenderingOff,Is.True,"The motion donor must not render a second athlete.");
                    Assert.That(candidate.transform.position,Is.EqualTo(rig.transform.position));
                    Assert.That(candidate.GetComponentInChildren<Collider>(),Is.Null);
                }
                view.SetVisualGroundSurface(_=>.2f);
                for(int frame=0;frame<16;frame++)view.Render(1);
                long before=System.GC.GetAllocatedBytesForCurrentThread();
                var watch=System.Diagnostics.Stopwatch.StartNew();
                // Warm the timer before measuring the per-frame presentation allocation.
                watch.Stop();before=System.GC.GetAllocatedBytesForCurrentThread();watch.Restart();
                for(int frame=0;frame<32;frame++)view.Render(1);
                watch.Stop();long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
                Directory.CreateDirectory("artifacts/m16-7-race-integration");
                File.WriteAllText("artifacts/m16-7-race-integration/pose-cost-"+format+".json",
                    FormattableString.Invariant($"{{\"schema_version\":1,\"id\":\"m167_pose_cost\",\"format\":\"{format}\",\"field_count\":30,\"frames\":32,\"cpu_ms_per_frame\":{watch.Elapsed.TotalMilliseconds/32},\"managed_bytes\":{allocated},\"scope\":\"warm presentation CPU only; no GPU/FPS claim\"}}"));
                TestContext.WriteLine($"{format}: 30-athlete presentation {watch.Elapsed.TotalMilliseconds/32:F3} ms/frame, {allocated} managed bytes.");
                Assert.That(allocated,Is.Zero,"Steady-state race visuals must not allocate.");
                Assert.That(runner.Field.Tick.Value,Is.EqualTo(snapshot.Tick),"Visual replacement cannot advance race authority.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
