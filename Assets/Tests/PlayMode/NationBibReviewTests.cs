using System;
using System.Collections;
using System.IO;
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
    public sealed class NationBibReviewTests
    {
        private GameObject root;
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/nation-bibs-20261012"));
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private static string FixtureRead(string file)
        {
            // Fixture allows the factory's local entrants into the actual WC event geometry.
            var json=Read(file);return file=="woodland_world_cup_calendar_v1.json"?json.Replace("\"minimum_tier\": 2","\"minimum_tier\": 0"):json;
        }
        private static void Register(CareerSave save,CareerDefinitions definitions,int eventIndex,params int[] order)
        {
            var e=definitions.Calendar.Get(eventIndex);var finishes=new CareerFinish[order.Length];
            for(int i=0;i<order.Length;i++)finishes[i]=new CareerFinish(save.Athlete(order[i]).Id,100000+i*1000,100000+i*1000);
            CareerStandings.Register(save,new CareerRaceRecord("bib_fixture_"+eventIndex,save.SeasonId,e.Id,e.Format,1,new string('a',64),finishes));
        }
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        [Test] public void WorldCupFieldBindsActualNationalityAndPreStartClassificationsWithoutAuthorityChanges()
        {
            var definitions=new CareerDefinitions(FixtureRead,"woodland_world_cup_calendar_v1.json");
            var save=CareerFactory.Create(definitions.Roster,definitions.Calendar,"bib_start","Alex","NOR","appearance_default",new SeededRandomSource(71000));
            Register(save,definitions,0,0,1,2);Register(save,definitions,1,1,2,0);
            var e=definitions.Calendar.Get(3);var race=new CareerRaceProvider(Read,definitions,ProductionVenueHost.ReadResource).Prepare(save,e,71000);
            root=new GameObject("World Cup kit integration");var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(race);
            string before=save.ToJson();var tick=host.Runner.Field.Tick;
            host.Proxies.ConfigureCareerAppearance(save,definitions.Calendar,definitions.Scoring,e);
            var materialByNation=new System.Collections.Generic.Dictionary<string,Material>();
            for(int i=0;i<host.Runner.Field.Count;i++)
            {
                var state=host.Runner.Field.GetAthlete(i);CareerAthlete athlete=null;
                for(int j=0;j<save.AthleteCount;j++)if(save.Athlete(j).Id==state.AthleteId.Value)athlete=save.Athlete(j);
                var rig=host.Proxies.GetRig(athlete.Id);Assert.That(rig.GetComponent<AthleteAppearancePresentation>().Nation,Is.EqualTo(athlete.Nation));
                var material=rig.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials[1];
                if(materialByNation.TryGetValue(athlete.Nation,out var shared))Assert.That(material,Is.SameAs(shared));else materialByNation.Add(athlete.Nation,material);
            }
            Assert.That(materialByNation.Count,Is.EqualTo(6));
            Assert.That(host.Proxies.GetRig(save.Athlete(0).Id).GetComponent<AthleteBibPresentation>().LeaderRole,Is.EqualTo(CareerLeaderBib.Discipline));
            Assert.That(host.Proxies.GetRig(save.Athlete(1).Id).GetComponent<AthleteBibPresentation>().LeaderRole,Is.EqualTo(CareerLeaderBib.Total));
            Assert.That(host.Proxies.GetRig(save.Athlete(2).Id).GetComponent<AthleteBibPresentation>().LeaderRole,Is.EqualTo(CareerLeaderBib.None));
            for(int i=0;i<10;i++)host.Proxies.Render(1);
            Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));Assert.That(save.ToJson(),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator SixNationalKitsAndAllFourBibRolesRenderWithOriginalTextureQuality()
        {
            root=new GameObject("National kit visual review");
            var nations=new[]{"NOR","SWE","FIN","GER","FRA","ITA"};
            var roles=new[]{CareerLeaderBib.None,CareerLeaderBib.Total,CareerLeaderBib.Discipline,CareerLeaderBib.Total|CareerLeaderBib.Discipline};
            for(int i=0;i<6;i++)
            {
                var rig=AthleteRigFactory.Create(root.transform);rig.transform.localPosition=new Vector3((2.5f-i)*1.3f,0,0);
                rig.GetComponent<AthleteBibPresentation>().SetNumber(i+1);rig.GetComponent<AthleteAppearancePresentation>().ApplyNation(nations[i]);
                rig.GetComponent<AthleteBibPresentation>().SetLeader(roles[i%4]);
                var label=new GameObject("Country label",typeof(TextMesh));label.transform.SetParent(root.transform,false);
                label.transform.localPosition=new Vector3((2.5f-i)*1.3f,-.35f,0);label.transform.localRotation=Quaternion.Euler(0,180,0);
                var text=label.GetComponent<TextMesh>();text.text=nations[i];text.anchor=TextAnchor.MiddleCenter;text.characterSize=.09f;text.fontSize=48;text.color=Color.white;
            }
            var light=new GameObject("Review light",typeof(Light));light.transform.SetParent(root.transform,false);light.transform.rotation=Quaternion.Euler(40,-25,0);
            light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=2.5f;
            var oldSun=RenderSettings.sun;var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
            RenderSettings.sun=light.GetComponent<Light>();RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.56f,.63f);
            var go=new GameObject("Review camera",typeof(Camera));go.transform.SetParent(root.transform,false);
            var camera=go.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.17f,.23f);
            camera.transform.position=new Vector3(0,1.25f,9);camera.transform.LookAt(new Vector3(0,.85f,0));camera.fieldOfView=36;
            var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;
            Texture2D pixels=null;var previous=RenderTexture.active;
            try
            {
                for(int i=0;i<4;i++)yield return null;
                RenderTexture.active=target;pixels=new Texture2D(1600,900,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                Directory.CreateDirectory(Folder);File.WriteAllBytes(Path.Combine(Folder,"national-kits-and-leader-bibs.png"),pixels.EncodeToPNG());
                int detail=0;for(int y=300;y<650;y+=5)for(int x=120;x<1480;x+=5)if(Vector4.Distance(pixels.GetPixel(x,y),camera.backgroundColor)>.15f)detail++;
                Assert.That(detail,Is.GreaterThan(100),"Actual meshes and fabric must paint in the preview.");
            }
            finally{RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;RenderTexture.active=previous;camera.targetTexture=null;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);}
        }
        private sealed class Store:ICareerProfileStore
        {private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;public void WriteNew(string id,string value)=>json=value;public void Replace(string id,string expected,string value){Assert.That(json,Is.EqualTo(expected));json=value;}}
        [UnityTest] public IEnumerator WorldCupStandingsShowTheCorrectTwoBibRolesBesideNationalFlags()
        {
            var definitions=new CareerDefinitions(Read,"woodland_world_cup_calendar_v1.json");
            var save=CareerFactory.Create(definitions.Roster,definitions.Calendar,"kit_table","Alex","NOR","appearance_default",new SeededRandomSource(71000));
            Register(save,definitions,0,0,1,2);Register(save,definitions,1,1,2,0);
            typeof(CareerSave).GetProperty("Week").SetValue(save,definitions.Calendar.Get(3).Week);
            var store=new Store();store.WriteNew(save.Id,save.ToJson());
            root=new GameObject("World Cup standings acceptance");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store);
            menu.Session.ContinueCareer();menu.Session.OpenCareerPage(FrontEndScreen.Standings);
            var panel=root.GetComponent<UIDocument>().panelSettings;var target=new RenderTexture(1600,900,0);target.Create();var old=panel.targetTexture;panel.targetTexture=target;
            Texture2D pixels=null;var previous=RenderTexture.active;
            try
            {
                menu.ApplyPreferences("ru",0);for(int i=0;i<6;i++)yield return null;
                Assert.That(menu.Root.Query<LeaderBibGraphic>().ToList().Count,Is.EqualTo(2));
                // B leads the total; A still leads the next Sprint classification.
                Assert.That(menu.Root.Q("standing_0").Q<LeaderBibGraphic>().Role,Is.EqualTo(CareerLeaderBib.Total));
                Assert.That(menu.Root.Q("standing_1").Q<LeaderBibGraphic>().Role,Is.EqualTo(CareerLeaderBib.Discipline));
                var next=menu.Root.Q("standings_rows_next");var viewport=next.GetFirstAncestorOfType<ScrollView>().contentViewport;
                Assert.That(next.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax+1),"Pager must remain visible under the kit legend.");
                RenderTexture.active=target;pixels=new Texture2D(1600,900,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                Directory.CreateDirectory(Folder);File.WriteAllBytes(Path.Combine(Folder,"world-cup-leader-table-ru.png"),pixels.EncodeToPNG());
                Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(save.ToJson()));
            }
            finally{panel.targetTexture=old;RenderTexture.active=previous;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);}
        }
        [UnityTest] public IEnumerator CountryLegendAndPreRaceIdentityFitTheCompactInterface()
        {
            root=new GameObject("Country UI acceptance");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("kit_ui","Alex","FRA",11);
            var panel=root.GetComponent<UIDocument>().panelSettings;var target=new RenderTexture(1024,768,0);target.Create();var old=panel.targetTexture;panel.targetTexture=target;
            try
            {
                menu.Session.OpenCareerPage(FrontEndScreen.Standings);menu.ApplyPreferences("en",2);for(int i=0;i<6;i++)yield return null;
                Assert.That(menu.Root.Q("national_kit_legend").childCount,Is.EqualTo(6));Assert.That(menu.Root.Q("kit_swatch"),Is.Null);Capture("kit-legend-en-large");
                menu.Session.Back();for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();menu.ApplyPreferences("en",2);for(int i=0;i<6;i++)yield return null;
                Assert.That(menu.Root.Q("pre_race_kit").Q("national_kit_FRA"),Is.Not.Null);
                Assert.That(menu.Root.Q("kit_swatch"),Is.Null,"Country identity uses the flag/code, without a suit-colour marker.");
                Assert.That(menu.Root.Q<LeaderBibGraphic>(),Is.Null,"Junior standings must not invent World Cup leaders.");
                var weather=menu.Root.Q("pre_race_weather");var viewport=weather.GetFirstAncestorOfType<ScrollView>().contentViewport;
                Assert.That(weather.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax+1));Capture("pre-race-kit-en-large");
                menu.ApplyPreferences("ru",0);for(int i=0;i<6;i++)yield return null;Capture("pre-race-kit-ru");
            }
            finally{panel.targetTexture=old;Object.DestroyImmediate(target);}
            void Capture(string name)
            {
                var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1024,768,TextureFormat.RGBA32,false);
                try{pixels.ReadPixels(new Rect(0,0,1024,768),0,0);pixels.Apply();Directory.CreateDirectory(Folder);File.WriteAllBytes(Path.Combine(Folder,name+".png"),pixels.EncodeToPNG());}
                finally{RenderTexture.active=previous;Object.DestroyImmediate(pixels);}
            }
        }
    }
}
