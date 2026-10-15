using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Biathlon.Career;
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
    public sealed class CareerAthleteCardUiTests
    {
        private sealed class Store:ICareerProfileStore
        {
            public string Json;public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json;
            public void WriteNew(string id,string json){Assert.That(Json,Is.Null);Json=json;}
            public void Replace(string id,string expected,string json){Assert.That(Json,Is.EqualTo(expected));Json=json;}
        }
        private GameObject root;private FrontEndMenu menu;private Store store;
        [SetUp] public void Setup()
        {
            root=new GameObject("M15.6 career card UI");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;store=new Store();menu.Initialize(store);
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_card_ui","Алекс","NOR",71000);menu.RefreshFrontEnd();
        }
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private void Submit(string id)
        {var button=menu.Root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);}
        [UnityTest,Timeout(240000)] public IEnumerator LiveCardBindsSavedAppearanceThirteenAbilitiesAndReadinessWithoutMutation()
        {
            yield return null;string before=menu.Session.Career.ToJson();ulong random=menu.Session.Career.RandomState;
            var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;bool fog=RenderSettings.fog;float lod=QualitySettings.lodBias;
            Assert.That(menu.Root.Q<Label>("career_player_ovr").text,Is.EqualTo(BiathlonRatingEngine.Calculate(menu.Session.Player,menu.Session.Definitions.Ratings).Overall.ToString()));
            Submit("career_athlete_card");yield return null;yield return null;
            Assert.That(menu.ViewedCareerAthleteId,Is.EqualTo(menu.Session.Career.PlayerId));var studio=menu.CreatorStudio;var rig=studio.Rig;var texture=studio.Texture;
            Assert.That(menu.Root.Q<Image>("creator_model").image,Is.SameAs(texture));Assert.That(menu.Root.Q<Label>("athlete_card_name").text,Is.EqualTo(menu.Session.Player.Name));
            Assert.That(menu.Root.Q("athlete_group_4"),Is.Not.Null);Assert.That(menu.Root.Q<Label>("athlete_card_fatigue").text,Does.Contain((menu.Session.Player.Fatigue/100).ToString()));
            yield return Capture("athlete-profile-ru",1600,900,"ru",0,"athlete_card_readiness");
            Submit("athlete_card_tab_1");yield return null;for(int i=0;i<7;i++)Assert.That(menu.Root.Q("athlete_ability_"+i),Is.Not.Null);
            Submit("athlete_abilities_next");yield return null;for(int i=7;i<13;i++)Assert.That(menu.Root.Q("athlete_ability_"+i),Is.Not.Null);
            yield return Capture("athlete-abilities-en-large",1024,768,"en",2,"athlete_abilities_next");
            Submit("athlete_card_tab_3");yield return null;Assert.That(menu.Root.Q("athlete_achievements").childCount,Is.EqualTo(6));
            yield return Capture("athlete-milestones-ru",1600,900,"ru",0,"athlete_achievements_next");
            Assert.That(menu.CreatorStudio,Is.SameAs(studio));Assert.That(menu.CreatorStudio.Rig,Is.SameAs(rig));Assert.That(menu.CreatorStudio.Texture,Is.SameAs(texture));
            Assert.That(RenderSettings.ambientLight,Is.EqualTo(ambient));Assert.That(RenderSettings.ambientMode,Is.EqualTo(mode));Assert.That(RenderSettings.fog,Is.EqualTo(fog));Assert.That(QualitySettings.lodBias,Is.EqualTo(lod));
            Submit("back");yield return null;Assert.That(menu.ViewedCareerAthleteId,Is.Null);Assert.That(menu.CreatorStudio,Is.Null);Assert.That(menu.Root.Q<Button>("career_athlete_card"),Is.Not.Null);
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Assert.That(menu.Session.Career.RandomState,Is.EqualTo(random));Assert.That(store.Json,Is.EqualTo(before));
        }
        [UnityTest,Timeout(240000)] public IEnumerator SavedSeasonGraphAndKnownRivalCardsSurviveNavigationLocaleAndFilters()
        {
            // Accelerated observation fixture: real adaptation and annual boundaries, no invented races or history rows.
            int initialSeason=menu.Session.Career.Season,initialAge=menu.Session.Player.Age;
            var sessions=new[]{CareerTrainingKind.Endurance,CareerTrainingKind.Technique,CareerTrainingKind.Standing};
            for(int season=0;season<sessions.Length;season++)
            {
                // Continue reloads the actual circuit definitions after any promotion or age-out.
                var save=menu.Session.Career;var d=menu.Session.Definitions;
                for(int i=0;i<400;i++)CareerAdaptation.Apply(save.FindWorldAthlete(save.PlayerId),d.Training.Get(sessions[season]),d.Adaptation,10000,d.Growth);
                string json=save.ToJson();int at=json.LastIndexOf("\"week\":",StringComparison.Ordinal);
                save=CareerSave.FromJson(json.Substring(0,at)+Regex.Replace(json.Substring(at),"^(\"week\":)[0-9]+","${1}"+d.Calendar.WeeksPerSeason));
                var prepared=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));Assert.That(prepared.RequiresRace,Is.False);
                var next=CareerSeasonEngine.CompleteWeek(prepared,d,null,s=>new SeededRandomSource(s));
                Assert.That(next.Season,Is.EqualTo(initialSeason+season+1));
                store.Json=next.ToJson();menu.Session.Back();menu.Session.ContinueCareer();menu.RefreshFrontEnd();
                Assert.That(menu.Session.Player.DevelopmentCount,Is.EqualTo(season+2));
                var observation=menu.Session.Player.Development(season+1);
                Assert.That(observation.Kind,Is.EqualTo(CareerDevelopmentObservation.SeasonEnd));
                Assert.That(observation.Season,Is.EqualTo(initialSeason+season));
                Assert.That(observation.Age,Is.EqualTo(initialAge+season));
                Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(store.Json));
            }
            yield return null;
            string before=menu.Session.Career.ToJson();Submit("career_history");yield return null;Submit("career_development");yield return null;yield return null;
            Assert.That(menu.Root.Q("athlete_development_graph"),Is.Not.Null);Assert.That(menu.Session.Player.DevelopmentCount,Is.EqualTo(4));Assert.That(menu.Root.Q("athlete_career_peak"),Is.Not.Null);
            Assert.That(menu.Session.Player.Development(0).Kind,Is.EqualTo(CareerDevelopmentObservation.Baseline));
            for(int season=1;season<4;season++)Assert.That(menu.Session.Player.Development(season).Season,Is.EqualTo(initialSeason+season-1));
            yield return Capture("athlete-development-ru",1600,900,"ru",0,"athlete_lifetime_results");Submit("back");yield return null;Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.History));Submit("back");yield return null;
            Submit("career_rivalries");yield return null;Assert.That(menu.Root.Q("rivalry_rows").childCount,Is.EqualTo(30));
            menu.Root.Q<DropdownField>("career_roster_scope").index=1;yield return null;Assert.That(menu.Root.Q("rivalry_rows").childCount,Is.EqualTo(menu.Session.Career.WorldAthleteCount));
            yield return Capture("career-rivals-ru",1600,900,"ru",0,"rivalry_rows_next");
            menu.Root.Q<DropdownField>("career_roster_circuit").index=3;yield return null;
            foreach(var row in menu.Root.Q("rivalry_rows").Children())
            {var model=new AthleteCareerSummary(menu.Session.Career,menu.Session.Definitions,row.name.Substring("rival_card_".Length));Assert.That(model.Circuit,Is.EqualTo(CareerCircuit.WorldCup));}
            menu.Root.Q<DropdownField>("career_roster_circuit").index=0;yield return null;
            Assert.That(menu.Session.Career.RetiredCount,Is.GreaterThan(0));Submit("career_roster_archive");yield return null;Assert.That(menu.Root.Q("retired_athletes"),Is.Not.Null);Assert.That(menu.Root.Q("rivalry_rows"),Is.Null);
            yield return Capture("career-athlete-archive-ru",1600,900,"ru",0,"retired_athletes_next");
            Submit("back");yield return null;Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Rivalries));Assert.That(menu.Root.Q("retired_athletes"),Is.Null);
            var rival=menu.Session.Career.Pool(0).Athlete;menu.OpenAthleteCard(rival.Id);yield return null;Assert.That(menu.Root.Q<Label>("athlete_card_name").text,Is.EqualTo(rival.Name));Assert.That(menu.Root.Q<Label>("athlete_card_ovr").text,Is.EqualTo(BiathlonRatingEngine.Calculate(rival,menu.Session.Definitions.Ratings).Overall.ToString()));
            yield return Capture("rival-profile-en",1600,900,"en",0,"athlete_card_readiness");Submit("back");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Rivalries));Assert.That(menu.Root.Q<DropdownField>("career_roster_scope").index,Is.EqualTo(1));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            menu.Root.Q<DropdownField>("career_roster_nation").index=1;yield return null;
            string nation=menu.Session.Definitions.Roster.Nation(0);foreach(var element in menu.Root.Q("rivalry_rows").Children())Assert.That(menu.Session.Career.FindWorldAthlete(element.name.Substring("rival_card_".Length)).Nation,Is.EqualTo(nation));
        }
        private IEnumerator Capture(string name,int width,int height,string locale,int size,string visibleId)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=settings.targetTexture;var target=new RenderTexture(width,height,24);target.Create();
            try
            {
                settings.targetTexture=target;menu.ApplyPreferences(locale,size);for(int i=0;i<6;i++)yield return null;yield return new WaitForSecondsRealtime(.25f);
                var bounds=menu.Root.Q("page_content").worldBound;var visible=menu.Root.Q(visibleId);Assert.That(visible,Is.Not.Null,visibleId);
                var old=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
                try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m15-6"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
                finally{RenderTexture.active=old;Object.Destroy(image);}
                Assert.That(visible.worldBound.yMax,Is.LessThanOrEqualTo(bounds.yMax),visibleId+" should fit without scrolling; panel="+bounds+" element="+visible.worldBound);Assert.That(visible.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));
            }
            finally{settings.targetTexture=previous;target.Release();Object.Destroy(target);}
        }
    }
}
