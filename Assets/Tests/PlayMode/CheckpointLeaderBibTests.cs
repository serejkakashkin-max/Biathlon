using System;
using System.Collections;
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
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class CheckpointLeaderBibTests
    {
        [TestCase(CareerLeaderBib.None)][TestCase(CareerLeaderBib.Total)][TestCase(CareerLeaderBib.Discipline)]
        [TestCase(CareerLeaderBib.Total|CareerLeaderBib.Discipline)]
        public void RetainedRowsShowExactlyTheBoundRoleAndClearItOnReuse(CareerLeaderBib role)
        {
            var row=new TimingAthleteRow(1);row.Bind("a",17,"Anton Lind","NOR","01:25.2","+5.6",false,role,"Leader");
            var icon=row.Q<LeaderBibGraphic>();Assert.That(icon.Role,Is.EqualTo(role));
            Assert.That(icon.style.display.value,Is.EqualTo(role==CareerLeaderBib.None?DisplayStyle.None:DisplayStyle.Flex));
            Assert.That(row.Q<NationFlagGraphic>().Nation,Is.EqualTo("NOR"));int nodes=row.childCount;
            row.Bind("b",1,"Leo Salo","FIN","01:24.0","LEADER",false);
            Assert.That(row.Q<LeaderBibGraphic>(),Is.SameAs(icon));Assert.That(row.childCount,Is.EqualTo(nodes));
            Assert.That(icon.Role,Is.EqualTo(CareerLeaderBib.None));Assert.That(icon.style.display.value,Is.EqualTo(DisplayStyle.None),"Leading this split does not confer a season leader's bib.");
            Assert.That(row.Q<NationFlagGraphic>().Nation,Is.EqualTo("FIN"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>icon.SetRole((CareerLeaderBib)4,"Invalid"));
        }
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private static string Definitions(string file)
        {string json=Read(file);return file=="woodland_world_cup_calendar_v1.json"?json.Replace("\"minimum_tier\": 2","\"minimum_tier\": 0"):json;}
        private static void Register(CareerSave save,CareerDefinitions definitions,int index,params int[] order)
        {
            var e=definitions.Calendar.Get(index);var finishes=new CareerFinish[order.Length];
            for(int i=0;i<order.Length;i++)finishes[i]=new CareerFinish(save.Athlete(order[i]).Id,100000+i*1000,100000+i*1000);
            CareerStandings.Register(save,new CareerRaceRecord("tab_leader_"+index,save.SeasonId,e.Id,e.Format,1,new string('a',64),finishes));
        }
        [UnityTest,Timeout(240000)] public IEnumerator ActualCareerTabUsesTheSamePreStartBibsAsAthletesAcrossHistoryAndLocale()
        {
            var d=new CareerDefinitions(Definitions,"woodland_world_cup_calendar_v1.json");
            var save=CareerFactory.Create(d.Roster,d.Calendar,"tab_bibs","Alex","NOR","appearance_default",new SeededRandomSource(71000));
            Register(save,d,0,0,1,2);Register(save,d,1,1,2,0);var e=d.Calendar.Get(3);
            var leaders=CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,e);string career=save.ToJson();
            var prepared=new CareerRaceProvider(Read,d,ProductionVenueHost.ReadResource).Prepare(save,e,71000);
            var root=new GameObject("Real career split leader bib acceptance");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);
                host.Proxies.ConfigureCareerAppearance(save,d.Calendar,d.Scoring,e);var hud=host.GameplayHud;hud.SetBroadcastRoster(save);
                bool all=false;
                for(int i=0;i<26000;i++)
                {
                    host.Runner.Step();if(i%200!=0)continue;all=true;
                    for(int a=0;a<host.Runner.Field.Count;a++)if(host.Runner.Field.GetAthlete(a).Checkpoints.Count==0){all=false;break;}
                    if(all)break;if(i%1000==0)yield return null;
                }
                Assert.That(all,Is.True,"All actual scheduled starters must pass the first split.");
                host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));hud.Refresh();hud.SetCheckpointTableVisible(true);
                while(hud.SelectedCheckpointOrdinal>0)hud.MoveCheckpointHistory(-1);
                long tick=host.Runner.Field.Tick.Value;
                foreach(string locale in new[]{"ru","en"})
                {
                    hud.SetPreferences(locale,locale=="en"?2:0);hud.SetCheckpointTableVisible(true);while(hud.SelectedCheckpointOrdinal>0)hud.MoveCheckpointHistory(-1);
                    var table=CheckpointStandings.Capture(host.Runner.Field,0);int total=0,red=0;
                    for(int i=0;i<table.Count;i++)
                    {
                        var row=table.Get(i);var expected=leaders.ForAthlete(row.AthleteId);var card=hud.Root.Q<TimingAthleteRow>("checkpoint_row_"+i);var icon=card.Q<LeaderBibGraphic>();
                        Assert.That(card.AthleteId,Is.EqualTo(row.AthleteId));Assert.That(icon.Role,Is.EqualTo(expected));Assert.That(host.Proxies.LeaderRole(row.AthleteId),Is.EqualTo(expected));
                        Assert.That(host.Proxies.GetRig(row.AthleteId).GetComponent<AthleteBibPresentation>().LeaderRole,Is.EqualTo(expected));
                        if((expected&CareerLeaderBib.Total)!=0)total++;if((expected&CareerLeaderBib.Discipline)!=0)red++;
                    }
                    Assert.That(total,Is.EqualTo(1));Assert.That(red,Is.EqualTo(1));
                    yield return Capture(hud,locale=="ru"?1600:1024,locale=="ru"?900:768,"tab-"+locale);
                    var first=hud.Root.Q<TimingAthleteRow>("checkpoint_row_0").Q<LeaderBibGraphic>();var retained=first;
                    hud.MoveCheckpointHistory(1);hud.FollowLatestCheckpoint();hud.SetCheckpointTableVisible(false);hud.SetCheckpointTableVisible(true);
                    Assert.That(hud.Root.Q<TimingAthleteRow>("checkpoint_row_0").Q<LeaderBibGraphic>(),Is.SameAs(retained));
                }
                Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));Assert.That(save.ToJson(),Is.EqualTo(career));
            }
            finally{Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
            yield return null;
        }
        private static IEnumerator Capture(GameplayHud hud,int width,int height,string name)
        {
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var target=new RenderTexture(width,height,0);target.Create();var previous=panel.targetTexture;var active=RenderTexture.active;Texture2D image=null;
            try
            {
                panel.targetTexture=target;for(int i=0;i<4;i++)yield return null;
                var table=hud.Root.Q("checkpoint_table").worldBound;Assert.That(table.yMax,Is.LessThan(hud.Root.worldBound.yMax));
                foreach(var row in hud.Root.Query<TimingAthleteRow>().ToList())
                {
                    if(row.style.display.value==DisplayStyle.None)continue;var icon=row.Q<LeaderBibGraphic>();
                    if(icon.Role!=CareerLeaderBib.None){Assert.That(icon.worldBound.yMin,Is.GreaterThanOrEqualTo(row.worldBound.yMin));Assert.That(icon.worldBound.yMax,Is.LessThanOrEqualTo(row.worldBound.yMax));}
                }
                RenderTexture.active=target;image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                string folder=Path.GetFullPath("artifacts/m16-4/leader-hud");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            }
            finally{panel.targetTexture=previous;RenderTexture.active=active;if(image!=null)Object.DestroyImmediate(image);Object.DestroyImmediate(target);}
        }
    }
}
