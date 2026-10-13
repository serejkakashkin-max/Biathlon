using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Native retained-UI evidence. No user profiles, PlayerPrefs or historic evidence are written.</summary>
    public sealed class FrostlineVisualTests
    {
        private sealed class Store : ICareerProfileStore
        {
            private string json;
            public bool HasProfile => json != null;
            public string ReadLatest() => json ?? throw new FileNotFoundException();
            public void WriteNew(string id, string value) => json = value;
            public void Replace(string id, string expected, string value)
            { Assert.That(json, Is.EqualTo(expected)); json = value; }
        }
        [Serializable] private sealed class Audit
        {
            public int schema_version = 1;
            public string id, page, locale, phase;
            public int width, height, text_percent, visible_controls, visible_labels;
            public bool high_contrast;
            public float image_luminance_range;
            public string[] layout_findings;
        }
        [Serializable] private sealed class Performance
        {
            public int schema_version = 1;
            public string id = "frostline_ui_refresh_measurements";
            public string phase;
            public bool allocation_counter_supported;
            public int warmup_calls = 32, unchanged_samples = 2048, changed_samples = 128, menu_samples = 8;
            public long unchanged_allocated_bytes, changed_allocated_bytes, menu_allocated_bytes;
            public double unchanged_ms, changed_ms, menu_ms;
        }
        private GameObject root, race;
        private FrontEndMenu menu;
        private static readonly int[,] Resolutions = { {1024,768}, {1600,900}, {1920,1080}, {2560,1440} };
        private static string BaseFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/quality/m14-wow"));
        private static string Phase
        {
            get
            {
                string file = Path.Combine(BaseFolder, "capture-phase.txt");
                string phase = File.Exists(file) ? File.ReadAllText(file).Trim() : "before";
                if (phase.Length == 0 || phase.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || phase.Contains(".."))
                    throw new InvalidDataException("capture-phase.txt must contain one safe directory name.");
                return phase;
            }
        }
        private static string Folder => Path.Combine(BaseFolder, Phase);
        private static string Read(string file) => ProductionVenueHost.ReadResource("Biathlon/" + Path.GetFileNameWithoutExtension(file));
        [SetUp] public void Setup()
        {
            root = new GameObject("Frostline native UI audit");
            menu = root.AddComponent<FrontEndMenu>(); menu.AutoStart = false; menu.PersistPreferences = false;
            menu.Initialize(new Store());
        }
        [TearDown] public void Cleanup()
        { if (race != null) Object.DestroyImmediate(race); if (root != null) Object.DestroyImmediate(root); }

        [UnityTest, Timeout(180000)] public IEnumerator AuditScreens()
        { yield return FrontEndPages(false, true); }

        [UnityTest, Timeout(600000)] public IEnumerator FrontEndAccessibilityMatrix()
        { yield return FrontEndPages(true, false); }

        [UnityTest, Timeout(300000)] public IEnumerator SecondaryAccessibilityMatrix()
        { yield return FrontEndPages(true, true, true); }

        [UnityTest] public IEnumerator SettingsAccessibilityMatrix()
        {menu.Session.OpenSettings();yield return Page("settings","back",true);}

        private IEnumerator FrontEndPages(bool matrix,bool secondary,bool secondaryOnly=false)
        {
            if(secondaryOnly)
            {
                menu.Session.OpenNewCareer();menu.Session.CreateCareer("frostline_visual_fixture","Александр Северин","NOR",81000);
                for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();FinishFixture();
            }
            else
            {
            yield return Page("home", "new_career", matrix);
            menu.Session.OpenNewCareer(); yield return Page("new-career", "create", matrix);
            menu.Session.CreateCareer("frostline_visual_fixture", "Александр Северин", "NOR", 81000);
            yield return Page("dashboard", "career_training", matrix);
            menu.Session.OpenCareerPage(FrontEndScreen.Training); yield return Page("training", "back", matrix);
            menu.Session.Back(); menu.Session.OpenCareerPage(FrontEndScreen.Equipment);
            yield return Page("equipment", "back", matrix);
            menu.Session.Back(); yield return Page("calendar", "career_calendar_next", matrix);
            for (int i = 0; i < 5; i++) menu.Session.RestWeek();
            menu.Session.OpenPreRace(); yield return Page("pre-race", "start_race", matrix);
            FinishFixture(); yield return Page("results", "career_return", matrix);
            }
            if(!secondary)yield break;
            menu.Session.ReturnFromResults();
            foreach(var page in new[]{FrontEndScreen.Standings,FrontEndScreen.History,FrontEndScreen.Rivalries,FrontEndScreen.Replays})
            {
                menu.Session.OpenCareerPage(page);yield return Page(page.ToString().ToLowerInvariant(),"back",matrix);menu.Session.Back();
            }
            menu.Session.OpenSettings();yield return Page("settings","back",matrix);
        }
        private IEnumerator Page(string name, string primary, bool matrix)
        {
            string career = menu.Session.Career?.ToJson();
            var panel = root.GetComponent<UIDocument>().panelSettings;
            if (!matrix)
            {
                yield return Capture(panel, menu.Root, name, 1600, 900, "ru", 0, false,
                    () => { menu.ApplyPreferences("ru",0); menu.ApplyAccessibility(false); }, primary);
            }
            else
            {
                for (int r=0;r<4;r++) foreach(string locale in new[]{"ru","en"})
                    for(int size=0;size<3;size++) for(int contrast=0;contrast<2;contrast++)
                    {
                        string l=locale; int s=size; bool c=contrast==1;
                        yield return Capture(panel,menu.Root,name,Resolutions[r,0],Resolutions[r,1],l,s,c,
                            ()=>{menu.ApplyPreferences(l,s);menu.ApplyAccessibility(c);},primary);
                    }
            }
            Assert.That(menu.Session.Career?.ToJson(),Is.EqualTo(career),"UI preference/layout changes must preserve the career.");
        }
        [UnityTest, Timeout(180000)] public IEnumerator AuditHudScreens()
        { yield return HudPages(false); }
        [UnityTest, Timeout(600000)] public IEnumerator HudAccessibilityMatrix()
        { yield return HudPages(true); }

        private IEnumerator HudPages(bool matrix)
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("frostline_hud_fixture","Александр Северин","NOR",81000);
            CareerCalendarEvent mass=null;
            for(int week=1;week<=52;week++)
            {var entry=menu.Session.Definitions.Calendar.AtWeek(week);if(entry?.Format==CompetitionFormat.MassStart){mass=entry;break;}}
            Assert.That(mass,Is.Not.Null);
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource)
                .Prepare(CareerSave.FromJson(menu.Session.Career.ToJson()),mass,81000);
            race=new GameObject("Frostline real field HUD");
            var host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;
            host.InitializeCareer(prepared);menu.SetRaceVisible(true);
            var hud=host.GameplayHud;hud.SetBroadcastRoster(menu.Session.Career);
            CheckpointStandings table=null;
            // One bounded, real deterministic field run per test; all matrix cells reuse the same accepted observation.
            for(int step=0;step<10000;step++)
            {
                host.Runner.Step();
                if(step%100!=0)continue;
                var s=RacePresentationSnapshot.Capture(host.Runner.Field);
                table=CheckpointStandings.Capture(host.Runner.Field,s.Get(host.PlayerIndex).Hud.CheckpointOrdinal);
                if(table.Count>=15){host.Feed.Publish(s);break;}
                yield return null;
            }
            Assert.That(table.Count,Is.GreaterThanOrEqualTo(15),"Actual field must populate the timing table before capture.");
            yield return HudPage(host,"hud-ski",matrix,false,false);
            yield return HudPage(host,"hud-tab",matrix,true,false);
            yield return HudPage(host,"hud-f2",matrix,false,true);
            hud.SetCheckpointTableVisible(false);hud.SetBroadcastVisible(false);
            bool aiming=false;
            for(int step=0;step<16000;step++)
            {
                host.Runner.Step();
                if(step%20!=0)continue;
                var s=RacePresentationSnapshot.Capture(host.Runner.Field);
                if(s.Get(host.PlayerIndex).ShootingPhase==ShootingPhase.Aiming)
                {host.Feed.Publish(s);aiming=true;break;}
                if(step%200==0)yield return null;
            }
            Assert.That(aiming,Is.True,"The same deterministic field must reach an actual aiming observation.");
            yield return HudPage(host,"hud-shooting",matrix,false,false);
        }
        private IEnumerator HudPage(ProductionVenueHost host,string name,bool matrix,bool table,bool broadcast)
        {
            var hud=host.GameplayHud;
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var tick=host.Runner.Field.Tick;
            for(int r=matrix?0:1;r<(matrix?4:2);r++)foreach(string locale in matrix?new[]{"ru","en"}:new[]{"ru"})
                for(int size=0;size<(matrix?3:1);size++)for(int contrast=0;contrast<(matrix?2:1);contrast++)
                {
                    string l=locale;int s=size;bool c=contrast==1;
                    yield return Capture(panel,hud.Root,name,Resolutions[r,0],Resolutions[r,1],l,s,c,()=>
                    {hud.SetPreferences(l,s,"Александр Северин");hud.SetAccessibility(c);hud.SetCheckpointTableVisible(table);hud.SetBroadcastVisible(broadcast);});
                    Assert.That(hud.CheckpointTableVisible,Is.EqualTo(table));Assert.That(hud.BroadcastVisible,Is.EqualTo(broadcast));
                    if(!table&&!broadcast)AssertHudClearance(hud);
                }
            Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick),"Rendering all UI variants cannot advance race authority.");
        }
        private static void AssertHudClearance(GameplayHud hud)
        {
            var bounds=hud.Root.worldBound;
            var top=hud.Root.Q("timing_panel").worldBound;
            var motion=hud.Root.Q(hud.Model.Shooting?"shooting_panel":"motion_panel").worldBound;
            var condition=hud.Root.Q("condition_panel").worldBound;var hint=hud.Root.Q("help_hint").worldBound;
            Assert.That(motion.yMin,Is.GreaterThan(top.yMax+12),"Top and bottom HUD must remain separated.");
            Assert.That(motion.xMax,Is.LessThan(bounds.center.x),"Optical centre must remain clear.");
            Assert.That(motion.xMax,Is.LessThan(condition.xMin));
            Assert.That(motion.yMax,Is.LessThan(bounds.yMax));
            float area=top.width*top.height+motion.width*motion.height+condition.width*condition.height+hint.width*hint.height;
            Assert.That(area/(bounds.width*bounds.height),Is.LessThan(hud.TextSizeIndex==0?.10f:.17f),"Existing M14 HUD area budget remains mandatory.");
        }
        [UnityTest, Timeout(120000)] public IEnumerator RefreshCostAfterWarmup()
        {
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            race=new GameObject("Frostline measured HUD");var feed=race.AddComponent<RacePresentationFeed>();
            var hud=race.AddComponent<GameplayHud>();hud.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value);
            // Snapshot construction/physics are outside the measured UI loop.
            var observations=new RacePresentationSnapshot[160];
            for(int i=0;i<observations.Length;i++){runner.Step();observations[i]=RacePresentationSnapshot.Capture(runner.Field);}
            feed.Publish(observations[0]);hud.Refresh();yield return null;
            var result=new Performance{phase=Phase};
            long probeBefore=GC.GetAllocatedBytesForCurrentThread();var allocationProbe=new byte[8192];GC.KeepAlive(allocationProbe);
            result.allocation_counter_supported=GC.GetAllocatedBytesForCurrentThread()-probeBefore>=8192;
            for(int i=0;i<32;i++)hud.Refresh();
            int refreshes=hud.RefreshCount;var timer=new Stopwatch();
            long bytes=GC.GetAllocatedBytesForCurrentThread();timer.Start();
            for(int i=0;i<2048;i++)hud.Refresh();
            timer.Stop();result.unchanged_allocated_bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;result.unchanged_ms=timer.Elapsed.TotalMilliseconds;
            Assert.That(hud.RefreshCount,Is.EqualTo(refreshes),"An unchanged accepted snapshot must not rebuild the HUD.");
            for(int i=1;i<32;i++){feed.Publish(observations[i]);hud.Refresh();}
            timer.Reset();bytes=GC.GetAllocatedBytesForCurrentThread();timer.Start();
            for(int i=32;i<160;i++){feed.Publish(observations[i]);hud.Refresh();}
            timer.Stop();result.changed_allocated_bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;result.changed_ms=timer.Elapsed.TotalMilliseconds;
            for(int i=0;i<8;i++)menu.RefreshFrontEnd();
            timer.Reset();bytes=GC.GetAllocatedBytesForCurrentThread();timer.Start();
            for(int i=0;i<8;i++)menu.RefreshFrontEnd();
            timer.Stop();result.menu_allocated_bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;result.menu_ms=timer.Elapsed.TotalMilliseconds;
            Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"refresh-performance.json"),JsonUtility.ToJson(result,true));
            if(result.allocation_counter_supported)Assert.That(result.unchanged_allocated_bytes,Is.Zero,"Warm unchanged retained HUD refresh must allocate nothing.");
            // CPU times are evidence for same-machine before/after review, not flaky machine-speed pass thresholds.
        }
        private void FinishFixture()
        {
            // A recorded-result boundary fixture, not a synthetic running race or modified player save.
            var prepared=menu.Session.PreparedWeek;
            var saved=CareerSave.FromJson(prepared.RaceSnapshotJson);
            var rows=new CareerFinish[saved.AthleteCount];
            for(int i=0;i<rows.Length;i++)
            {
                var a=saved.Athlete((i+23)%rows.Length);
                var details=new CareerResultDetails(a.Id,new[]{31,27},new[]{ShootingStance.Prone,ShootingStance.Standing},
                    new[]{new CareerResultSplit("split_1","lap_1",75000,75000),new CareerResultSplit("split_2","lap_2",210000,210000)});
                rows[i]=new CareerFinish(a.Id,300000+i*1523,300000+i*1523,1,details:details);
            }
            var record=new CareerRaceRecord(saved.SeasonId+"_"+prepared.Event.Id,saved.SeasonId,prepared.Event.Id,
                prepared.Event.Format,prepared.RaceSeed,new string('a',64),rows);
            menu.Session.BeginRace();menu.Session.FinishRace(record);
        }
        private static bool Visible(VisualElement element)
        {
            for(var e=element;e!=null;e=e.parent)
                if(e.resolvedStyle.display==DisplayStyle.None||e.resolvedStyle.visibility==Visibility.Hidden||e.resolvedStyle.opacity<=0) return false;
            return element.worldBound.width>0 && element.worldBound.height>0;
        }
        private static string Identify(VisualElement v) => string.IsNullOrEmpty(v.name)?v.GetType().Name:v.name;
        private static Audit Inspect(VisualElement ui,string page,int width,int height,string locale,int size,bool contrast,string primary)
        {
            var audit=new Audit{id="frostline_"+page,page=page,locale=locale,phase=Phase,width=width,height=height,
                text_percent=size==0?100:size==1?115:130,high_contrast=contrast};
            var findings=new List<string>(); var bounds=ui.worldBound;
            Assert.That(bounds.width,Is.GreaterThan(100),page+" root must have resolved geometry.");
            ui.Query<Button>().ForEach(button=>
            {
                if(!Visible(button))return;audit.visible_controls++;
                var r=button.worldBound;
                if(r.xMin<bounds.xMin-2||r.xMax>bounds.xMax+2) findings.Add(Identify(button)+": horizontal bounds "+r);
                if(button.GetFirstAncestorOfType<ScrollView>()==null&&(r.yMin<bounds.yMin-2||r.yMax>bounds.yMax+2))
                    findings.Add(Identify(button)+": unreachable vertical bounds "+r);
            });
            ui.Query<Label>().ForEach(label=>
            {
                if(!Visible(label)||string.IsNullOrWhiteSpace(label.text))return;audit.visible_labels++;
                var r=label.worldBound;
                if(r.xMax>bounds.xMax+2||r.xMin<bounds.xMin-2) findings.Add(Identify(label)+": text outside root "+r);
            });
            if(primary!=null)
            {
                var c=ui.Q(primary);
                if(c==null||!Visible(c))findings.Add(primary+": missing/hidden primary action");
                else if(c.worldBound.yMax>bounds.yMax+2||c.worldBound.xMax>bounds.xMax+2)
                    findings.Add(primary+": primary action requires scrolling "+c.worldBound);
            }
            audit.layout_findings=findings.ToArray();return audit;
        }
        private static IEnumerator Capture(PanelSettings panel,VisualElement ui,string page,int width,int height,
            string locale,int size,bool contrast,Action prepare,string primary=null)
        {
            // UI Toolkit input/scroll clipping uses stencil; a depthless target paints clip masks white.
            var rt=new RenderTexture(width,height,24);rt.Create();var previous=panel.targetTexture;
            var active=RenderTexture.active;Texture2D pixels=null;
            try
            {
                panel.targetTexture=rt;prepare();for(int i=0;i<5;i++)yield return null;
                yield return new WaitForSecondsRealtime(.3f);
                yield return null;
                var audit=Inspect(ui,page,width,height,locale,size,contrast,primary);
                RenderTexture.active=rt;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                float minimum=1,maximum=0;
                for(int y=0;y<height;y+=Math.Max(1,height/60))for(int x=0;x<width;x+=Math.Max(1,width/90))
                {float v=pixels.GetPixel(x,y).grayscale;minimum=Math.Min(minimum,v);maximum=Math.Max(maximum,v);}
                audit.image_luminance_range=maximum-minimum;
                string name=page+"-"+locale+"-"+width+"x"+height+"-"+audit.text_percent+(contrast?"-contrast":"");
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(Path.Combine(Folder,name+".png"),pixels.EncodeToPNG());
                File.WriteAllText(Path.Combine(Folder,name+".json"),JsonUtility.ToJson(audit,true));
                Assert.That(audit.image_luminance_range,Is.GreaterThan(.15f),name+": native panel must paint varied, nonblank pixels.");
                // Findings remain visible in both phases; do not suppress baseline defects before all screens are collected.
                foreach(var finding in audit.layout_findings)TestContext.WriteLine(name+": "+finding);
            }
            finally{RenderTexture.active=active;panel.targetTexture=previous;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(rt);}
        }
    }
}
