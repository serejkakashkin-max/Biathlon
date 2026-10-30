using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class M15AudioTests
    {
        private GameObject root;
        [SetUp]public void Setup(){if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);root=new GameObject("M15 isolated test");}
        [TearDown]public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        private BiathlonAudio Audio()
        {var a=BiathlonAudio.Ensure(false);a.Preferences.mute_unfocused=false;a.SetMenu(false);return a;}
        [Test]public void BankHasEveryCuePreloadedAndValidFiniteHeadroom()
        {
            var audio=Audio();int clips=0;
            foreach(var cue in audio.Definition.cues)
            foreach(string id in cue.clips)
            {
                var clip=Resources.Load<AudioClip>("Biathlon/Audio/"+id);Assert.That(clip.loadState,Is.EqualTo(AudioDataLoadState.Loaded));
                Assert.That(clip.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
                var data=new float[clip.samples*clip.channels];Assert.That(clip.GetData(data,0),Is.True);
                double sum=0;float peak=0;bool finite=true;foreach(float x in data){finite&=float.IsFinite(x);sum+=x*x;peak=Math.Max(peak,Math.Abs(x));}Assert.That(finite,Is.True);
                Assert.That(peak,Is.InRange(.1f,.8f),id);Assert.That(sum/data.Length,Is.GreaterThan(.00001),id);
                if(cue.loop)Assert.That(Math.Abs(data[0]-data[data.Length-1]),Is.LessThan(.0001),"Seam: "+id);
                clips++;
            }
            Assert.That(clips,Is.EqualTo(28));Assert.That(audio.SourceCount,Is.EqualTo(22));Assert.That(audio.PcmBytes,Is.LessThan(20*1024*1024));
        }
        [Test]public void DefinitionsRejectInvalidBudgetsGainsAndPreferences()
        {
            var d=BiathlonAudioDefinition.Load();d.transient_voices=10000;Assert.Throws<ArgumentException>(d.Validate);
            d=BiathlonAudioDefinition.Load();d.cues[0].gain=float.NaN;Assert.Throws<ArgumentException>(d.Validate);
            d=BiathlonAudioDefinition.Load();d.cues[0].clips[0]="../outside";Assert.Throws<ArgumentException>(d.Validate);
            d=BiathlonAudioDefinition.Load();d.crowd_reaction_delay_seconds=float.NaN;Assert.Throws<ArgumentException>(d.Validate);
            var a=Audio();var p=new BiathlonAudioPreferences{master=float.PositiveInfinity};Assert.Throws<ArgumentException>(()=>a.SetPreferences(p,false));
            p.master=-1;Assert.That(p.Valid(),Is.False);
        }
        [UnityTest]public IEnumerator VoiceBudgetPrioritizesPlayerAndMuteStopsAllAudibleGain()
        {
            var a=Audio();int accepted=0;
            for(int i=0;i<1000;i++)if(a.Play(AudioCue.Rifle,new Vector3(i,0,0),true))accepted++;
            Assert.That(accepted,Is.EqualTo(16));Assert.That(a.Dropped,Is.EqualTo(984));
            Assert.That(a.Play(AudioCue.Rifle,local:true),Is.True,"Local rifle replaces distant reports.");
            Assert.That(a.SourceCount,Is.EqualTo(22));
            var p=a.Preferences;p.master=0;a.SetPreferences(p,false);yield return null;
            foreach(var s in a.GetComponentsInChildren<AudioSource>())Assert.That(s.volume,Is.Zero);
            Assert.That(a.ActiveVoices,Is.LessThanOrEqualTo(22));
            a.StopRaceVoices();foreach(var s in a.GetComponentsInChildren<AudioSource>())Assert.That(s.isPlaying,Is.False);
        }
        [UnityTest]public IEnumerator SpatialReportStaysAtWorldPositionWhenListenerMoves()
        {
            var a=Audio();var p=new Vector3(30,2,18);a.Play(AudioCue.Rifle,p,true);
            a.transform.position=new Vector3(20,0,0);yield return null;
            var source=a.GetComponentsInChildren<AudioSource>().Single(s=>s.clip!=null&&s.clip.name.StartsWith("rifle"));
            Assert.That(source.transform.position,Is.EqualTo(p));Assert.That(source.dopplerLevel,Is.Zero);
        }
        private sealed class Store:ICareerProfileStore
        {
            public bool HasProfile=>false;public string ReadLatest()=>throw new FileNotFoundException();
            public void WriteNew(string id,string json)=>throw new InvalidOperationException();
            public void Replace(string id,string expected,string json)=>throw new InvalidOperationException();
        }
        private sealed class MemoryStore:ICareerProfileStore
        {
            private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;
            public void WriteNew(string id,string value)=>json=value;
            public void Replace(string id,string expected,string value){Assert.That(json,Is.EqualTo(expected));json=value;}
        }
        [UnityTest]public IEnumerator SettingsAreAccessibleAndIsolatedPreferencesDoNotWriteOwnerProfile()
        {
            string before=PlayerPrefs.GetString("biathlon.audio.v1","__missing__");
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            menu.Session.OpenSettings();menu.RefreshFrontEnd();yield return null;
            var slider=menu.Root.Q<Slider>("audio_master");Assert.That(slider,Is.Not.Null);slider.value=37;
            Assert.That(BiathlonAudio.Instance.Preferences.master,Is.EqualTo(.37f).Within(.0001));
            menu.Root.Q<Toggle>("audio_quiet").value=true;menu.ApplyPreferences("en",2);yield return null;
            Assert.That(menu.Root.Q<Slider>("audio_master").value,Is.EqualTo(37).Within(.01));
            Assert.That(menu.Root.Q<Toggle>("audio_quiet").value,Is.True);
            Assert.That(PlayerPrefs.GetString("biathlon.audio.v1","__missing__"),Is.EqualTo(before));
        }
        [UnityTest]public IEnumerator ActualAcceptedShotsBoltAndReplaySuppressionDoNotAdvanceAuthority()
        {
            var audio=Audio();var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("audio_contract_route",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;host.Initialize(venue,profile,route,conditions,count:1);
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(host.ChaseCamera,0,false);
            var id=host.Runner.Field.GetAthlete(0).AthleteId;bool held=false;int shots=0,bolts=0;
            while(host.Runner.Field.GetAthlete(0).Lifecycle.CompletedBouts==0&&host.Runner.Field.Tick.Value<15000)
            {
                var actor=host.Runner.Field.GetAthlete(0);var shooting=actor.ActiveShooting;int oldShots=shooting?.ShotsFired??0;var oldPhase=shooting?.Phase??ShootingPhase.Outside;
                var tick=host.Runner.Field.Tick.Next();
                host.Step(new PlayerCommand(tick,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,1000,1000),shooting?.Phase==ShootingPhase.Aiming,false,shooting?.NeedsBoltCycle==true));
                if(actor.ActiveShooting!=null&&actor.ActiveShooting.ShotsFired>oldShots)shots++;
                if(actor.ActiveShooting?.Phase==ShootingPhase.BoltCycle&&oldPhase!=ShootingPhase.BoltCycle)bolts++;
                if(shooting?.Phase==ShootingPhase.Aiming&&!held)
                {
                    held=true;long authority=host.Runner.Field.Tick.Value;int emitted=audio.Played;
                    for(int repeat=0;repeat<10;repeat++)director.Observe(host.Feed.Current);
                    Assert.That(audio.Played,Is.EqualTo(emitted),"Duplicate observed tick cannot replay shots.");
                    Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(authority));
                }
                if(actor.ActiveShooting!=null||oldShots!=(actor.ActiveShooting?.ShotsFired??0)||oldPhase!=(actor.ActiveShooting?.Phase??ShootingPhase.Outside)||tick.Value%200==0)yield return null;
            }
            Assert.That(held,Is.True);Assert.That(shots,Is.EqualTo(5));Assert.That(director.AcceptedShots,Is.EqualTo(5));
            Assert.That(director.BoltCycles,Is.EqualTo(bolts));Assert.That(bolts,Is.EqualTo(5));
            Assert.That(director.TargetClosures,Is.Zero,"Deliberate off-board shots must not sound a closed target.");
            Assert.That(audio.PlayedCue(AudioCue.Rifle),Is.EqualTo(5));Assert.That(audio.PlayedCue(AudioCue.TargetMiss),Is.EqualTo(5));
            Assert.That(director.PlayerHitReactions,Is.Zero);Assert.That(director.PlayerMissReactions,Is.EqualTo(5));
            Assert.That(audio.PlayedCue(AudioCue.CrowdMiss),Is.GreaterThan(0));Assert.That(audio.PlayedCue(AudioCue.CrowdHit),Is.Zero);
            director.ReplayContext(host.ChaseCamera,0,true);director.RenderAudio();yield return null;
            foreach(var s in audio.GetComponentsInChildren<AudioSource>())Assert.That(s.isPlaying,Is.False,"Pause/seek must stop race audio.");
            Assert.That(audio.SourceCount,Is.EqualTo(22));
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m15"));Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir,"accepted-events.json"),"{\"shots\":"+director.AcceptedShots+",\"bolt_cycles\":"+director.BoltCycles+",\"false_target_closures\":"+director.TargetClosures+",\"voice_limit\":"+audio.SourceCount+",\"pause_seek_silent\":true}");
        }
        [UnityTest]public IEnumerator SkiOnlyAcceptedTicksNeverEmitRifleBoltOrTargets()
        {
            var audio=Audio();var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("audio_ski_only",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;host.Initialize(venue,profile,route,conditions,count:2);
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(host.ChaseCamera,0,false);
            int skiTicks=0;
            for(int tick=0;tick<400;tick++)
            {
                host.Runner.Step();var snapshot=RacePresentationSnapshot.Capture(host.Runner.Field);
                if(snapshot.Get(0).ShootingActive||snapshot.Get(1).ShootingActive)break;
                host.Feed.Publish(snapshot);skiTicks++;
                if(tick%20==0){director.RenderAudio();yield return new WaitForSecondsRealtime(.11f);}
            }
            Assert.That(skiTicks,Is.GreaterThan(10));
            Assert.That(audio.PlayedCue(AudioCue.Pole),Is.GreaterThan(0));
            foreach(var cue in new[]{AudioCue.Rifle,AudioCue.Bolt,AudioCue.TargetHit,AudioCue.TargetMiss})
                Assert.That(audio.PlayedCue(cue),Is.Zero,"Unexpected cue on ski-only course: "+cue);
        }
        [UnityTest]public IEnumerator WarmPoolUpdateBudgetIsBounded()
        {
            var a=Audio();a.Loop(0,AudioCue.SkiPacked,1);yield return null;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<10000;i++)a.Loop(0,AudioCue.SkiPacked,(i%100)*.01f,1);
            watch.Stop();Assert.That(a.SourceCount,Is.EqualTo(22));
            Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(1000),"10k cached updates must not perform loading/synthesis.");
            TestContext.WriteLine("10000 cached loop requests ms="+watch.Elapsed.TotalMilliseconds);
        }
        [UnityTest]public IEnumerator AudioSettingsFitNativePanelsInBothLocales()
        {
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());menu.Session.OpenSettings();
            var panel=root.GetComponent<UIDocument>().panelSettings;var old=RenderTexture.active;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m15"));Directory.CreateDirectory(folder);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1024,768)})foreach(string locale in new[]{"ru","en"})
            {
                var target=new RenderTexture(size.x,size.y,24);target.Create();panel.targetTexture=target;Texture2D pixels=null;
                try
                {
                    menu.ApplyPreferences(locale,size.x==1024?2:0);yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
                    var view=menu.Root.worldBound;var preview=menu.Root.Q<Button>("audio_preview");
                    Assert.That(menu.Root.Q("audio_settings").worldBound.xMax,Is.LessThanOrEqualTo(view.xMax));
                    foreach(var id in new[]{"audio_master","audio_effects","audio_body","audio_ambience","audio_ui","audio_music"})
                    {var s=menu.Root.Q<Slider>(id);Assert.That(s.worldBound.width,Is.GreaterThanOrEqualTo(80),id);Assert.That(s.worldBound.xMax,Is.LessThan(view.xMax),id);}
                    var scroll=preview.GetFirstAncestorOfType<ScrollView>();scroll?.ScrollTo(preview);yield return null;
                    Assert.That(preview.worldBound.yMax,Is.LessThanOrEqualTo(view.yMax));
                    RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"audio-settings-"+locale+"-"+size.x+".png"),pixels.EncodeToPNG());
                }
                finally{RenderTexture.active=old;panel.targetTexture=null;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);}
            }
        }
        [UnityTest]public IEnumerator AiHitsUseAcceptedMasksAndBreathHoldDoesNotSilenceHeartbeat()
        {
            var audio=Audio();var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("audio_ai_route",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;host.Initialize(venue,profile,route,conditions,count:1);
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(host.ChaseCamera,0,false);
            int closed=0;bool held=false;long beats=0;
            while(host.Runner.Field.GetAthlete(0).Lifecycle.CompletedBouts==0&&host.Runner.Field.Tick.Value<15000)
            {
                host.Runner.Step();host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));
                var s=host.Feed.Current.Get(0);closed|=s.ClosedTargetsMask;beats=Math.Max(beats,s.CompletedHeartbeats);
                if(s.BreathHolding)
                {
                    held=true;director.RenderAudio();
                    foreach(var source in audio.GetComponentsInChildren<AudioSource>())if(source.clip?.name=="breath")Assert.That(source.isPlaying,Is.False);
                }
                if(s.ShootingActive||host.Runner.Field.Tick.Value%200==0)yield return null;
            }
            int hits=0;for(int i=0;i<5;i++)if((closed&(1<<i))!=0)hits++;
            Assert.That(hits,Is.GreaterThan(0));Assert.That(director.TargetClosures,Is.EqualTo(hits));
            Assert.That(audio.PlayedCue(AudioCue.TargetHit),Is.EqualTo(hits));Assert.That(director.AcceptedShots,Is.EqualTo(5));
            Assert.That(director.PlayerHitReactions,Is.EqualTo(hits));Assert.That(director.PlayerMissReactions,Is.EqualTo(5-hits));
            Assert.That(audio.PlayedCue(AudioCue.Cheer),Is.Zero,"The fifth hit must not stack the old extra series cheer.");
            Assert.That(beats,Is.GreaterThan(0));Assert.That(audio.PlayedCue(AudioCue.Heart),Is.GreaterThan(0));
            // Explicit breath suppression leaves the separately mixed heart channel alive.
            audio.StopRaceVoices();audio.Play(AudioCue.Breath);audio.Play(AudioCue.Heart);audio.StopBreath();
            Assert.That(audio.GetComponentsInChildren<AudioSource>().Any(s=>s.clip?.name=="heart"&&s.isPlaying),Is.True);
            TestContext.WriteLine("AI accepted hits="+hits+", breath-hold observed="+held+", simulation beats="+beats);
        }
        [UnityTest]public IEnumerator SpectatedBotAndItsAcceptedShotsCannotTriggerCrowdReactions()
        {
            var audio=Audio();var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("audio_player_only",venue.Track.Id,"main",profile.ToRouteRequirements());
            var snow=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;host.Initialize(venue,profile,route,snow,count:2);
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(host.ChaseCamera,1,false);
            var id=host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId;
            RacePresentationSnapshot earlier=null;int bots=0;
            while(host.Runner.Field.GetAthlete(1).Lifecycle.CompletedBouts==0&&host.Runner.Field.Tick.Value<15000)
            {
                var prior=host.Feed.Current;var tick=host.Runner.Field.Tick.Next();
                host.Step(new PlayerCommand(tick,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false,false));
                var now=host.Feed.Current;if(earlier==null&&now.Get(host.PlayerIndex).ShootingActive)earlier=now;
                if(now.Get(1).ShotsFired>prior.Get(1).ShotsFired)bots++;
                if(now.Get(1).ShootingActive||tick.Value%200==0)yield return null;
            }
            Assert.That(bots,Is.EqualTo(5));Assert.That(host.Feed.Current.Get(host.PlayerIndex).ShotsFired,Is.Zero);
            Assert.That(director.PlayerHitReactions+director.PlayerMissReactions,Is.Zero);
            Assert.That(audio.PlayedCue(AudioCue.CrowdHit)+audio.PlayedCue(AudioCue.CrowdMiss)+audio.PlayedCue(AudioCue.Cheer),Is.Zero);
            if(earlier!=null){director.Observe(earlier);director.Observe(host.Feed.Current);Assert.That(director.PlayerHitReactions+director.PlayerMissReactions,Is.Zero);}
        }
        [UnityTest]public IEnumerator DenseFieldReactionCountsBelongToPlayerWhileCameraFollowsAnotherAthlete()
        {
            var audio=Audio();var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new MemoryStore());
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("crowd_audio_test","Алекс Северин","NOR",11);
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();var week=menu.Session.PreparedWeek;
            CareerCalendarEvent mass=null;for(int w=1;w<=52;w++){var e=menu.Session.Definitions.Calendar.AtWeek(w);if(e?.Format==CompetitionFormat.MassStart){mass=e;break;}}
            string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),mass,week.RaceSeed);
            var race=new GameObject("Actual30-athlete crowd audio field");race.transform.SetParent(root.transform);
            var host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);menu.enabled=false;menu.Root.style.display=DisplayStyle.None;
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(host.ChaseCamera,1,false);
            int hits=0,misses=0,botShots=0;RacePresentationSnapshot earlier=null;
            while(host.Runner.Field.Tick.Value<20000)
            {
                var prior=host.Feed.Current;host.Runner.Step();var now=RacePresentationSnapshot.Capture(host.Runner.Field);
                for(int i=0;i<now.Count;i++)
                {
                    var a=prior.Get(i);var b=now.Get(i);
                    if(a.ShootingActive&&b.ShootingActive&&b.CompletedBouts==a.CompletedBouts&&b.ShotsFired==a.ShotsFired+1)
                    {if(i!=host.PlayerIndex)botShots++;else if((b.ClosedTargetsMask&~a.ClosedTargetsMask)!=0)hits++;else misses++;}
                }
                host.Feed.Publish(now);
                int count=director.PlayerHitReactions+director.PlayerMissReactions;long tick=host.Runner.Field.Tick.Value;
                director.Observe(now);Assert.That(director.PlayerHitReactions+director.PlayerMissReactions,Is.EqualTo(count));Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));
                if(earlier==null&&now.Get(host.PlayerIndex).ShotsFired==1)earlier=now;
                bool done=true;for(int i=0;i<now.Count;i++)done&=now.Get(i).CompletedBouts>0;
                if(done)break;if(tick%50==0)yield return null;
            }
            Assert.That(hits+misses,Is.EqualTo(5));Assert.That(botShots,Is.EqualTo(145));
            Assert.That(director.PlayerHitReactions,Is.EqualTo(hits));Assert.That(director.PlayerMissReactions,Is.EqualTo(misses));
            int before=director.PlayerHitReactions+director.PlayerMissReactions;
            director.Observe(earlier);director.Observe(host.Feed.Current);Assert.That(director.PlayerHitReactions+director.PlayerMissReactions,Is.EqualTo(before),"Rewind/gap must not burst past reactions.");
            director.ReplayContext(host.ChaseCamera,1,true);director.RenderAudio();yield return null;
            foreach(var source in audio.GetComponentsInChildren<AudioSource>())Assert.That(source.isPlaying,Is.False);
            Assert.That(audio.SourceCount,Is.EqualTo(22));Assert.That(audio.PeakVoices,Is.LessThanOrEqualTo(22));
        }
    }
}
