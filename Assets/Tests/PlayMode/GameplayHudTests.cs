using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Biathlon.PlayModeTests
{
    public sealed class GameplayHudTests
    {
        [UnityTest] public IEnumerator AcceptedBoutUpdatesRetainedHudWithoutBlockingAimOrAdvancingRace()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            var root=new GameObject("HUD acceptance");var feed=root.AddComponent<RacePresentationFeed>();
            var hud=root.AddComponent<GameplayHud>();hud.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value);
            bool aiming=false,exit=false;
            try
            {
                feed.Publish(RacePresentationSnapshot.Capture(runner.Field));hud.Refresh();
                yield return Capture(hud,1600,900,"ru",0,"skiing-ru");
                hud.SetCheckpointTableVisible(true);
                Assert.That(hud.CheckpointTableVisible,Is.True);
                for(int i=0;i<20000;i++)
                {
                    runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);hud.Refresh();
                    var state=snapshot.Get(0);var tick=runner.Field.Tick;int refreshes=hud.RefreshCount;
                    hud.Refresh();Assert.That(hud.RefreshCount,Is.EqualTo(refreshes),"An unchanged snapshot should do no UI work.");
                    Assert.That(hud.Model.ClosedMask,Is.EqualTo(state.ClosedTargetsMask));
                    Assert.That(runner.Field.Tick,Is.EqualTo(tick));
                    if(!aiming&&state.ShootingPhase==ShootingPhase.Aiming)
                    {
                        Assert.That(hud.CheckpointTableVisible,Is.False,"Range overview must replace the split table at shooting.");
                        aiming=true;yield return Capture(hud,1600,900,"ru",0,"shooting-ru");
                        yield return Capture(hud,1024,768,"en",2,"shooting-en-large-4x3");
                    }
                    if(state.ShootingPhase==ShootingPhase.Exiting)
                    {exit=true;Assert.That(hud.Model.Weapon,Is.EqualTo("Shooting complete"));break;}
                }
                Assert.That(aiming&&exit,Is.True);
                Assert.That(hud.HelpVisible,Is.False);
                Assert.That(hud.Root.Q("help_panel").style.display.value,Is.EqualTo(DisplayStyle.None));
                hud.SetHelpVisible(true);Assert.That(hud.Root.Q("help_panel").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                hud.SetHelpVisible(false);
                hud.Root.Query<VisualElement>().ForEach(v=>Assert.That(v.pickingMode,Is.EqualTo(PickingMode.Ignore)));
                root.SetActive(false);yield return null;Assert.That(hud.Root?.panel,Is.Null,"Independent document must hide with the race.");
                root.SetActive(true);yield return null;Assert.That(hud.Root.panel,Is.Not.Null);
                Assert.That(hud.Root.Q<Label>("weapon").text,Is.EqualTo(hud.Model.Weapon));
                feed.ResetRace();hud.Refresh();Assert.That(hud.Root.style.display.value,Is.EqualTo(DisplayStyle.None));
            }
            finally{Object.DestroyImmediate(root);}
            yield return null;
        }

        private static IEnumerator Capture(GameplayHud hud,int width,int height,string locale,int textSize,string filename)
        {
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var previous=RenderTexture.active;var target=new RenderTexture(width,height,0);target.Create();panel.targetTexture=target;
            Texture2D pixels=null;
            try
            {
                hud.SetPreferences(locale,textSize,"Alex");yield return null;yield return null;yield return null;
                var top=hud.Root.Q("timing_panel").worldBound;
                var bottom=hud.Root.Q(hud.Model.Shooting?"shooting_panel":"motion_panel").worldBound;
                Assert.That(bottom.yMin,Is.GreaterThan(top.yMax+12),"HUD panels must not overlap, even with large type.");
                Assert.That(bottom.xMax,Is.LessThan(hud.Root.worldBound.center.x),"The optical center must stay clear.");
                Assert.That(bottom.yMax,Is.LessThan(hud.Root.worldBound.yMax));
                var condition=hud.Root.Q("condition_panel").worldBound;var hint=hud.Root.Q("help_hint").worldBound;
                float area=top.width*top.height+bottom.width*bottom.height+condition.width*condition.height+hint.width*hint.height;
                float coverage=area/(hud.Root.worldBound.width*hud.Root.worldBound.height);
                Assert.That(coverage,Is.LessThan(textSize==0?.10f:.17f),"Normal HUD must leave at least 90% of the screen unobstructed; large 4:3 at least 83%.");
                Assert.That(bottom.xMax,Is.LessThan(condition.xMin));
                TestContext.WriteLine(filename+" coverage="+coverage.ToString("P2"));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/compact-hud-20261012"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,filename+".png"),pixels.EncodeToPNG());
                File.WriteAllText(Path.Combine(folder,filename+"-coverage.json"),"{\"panel_area_percent\":"+(coverage*100).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"}");
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;panel.targetTexture=null;Object.Destroy(target);}
        }
    }
}
