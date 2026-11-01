using System.Collections;
using System;
using System.IO;
using System.Reflection;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class FrontEndMenuTests
    {
        private sealed class Store : ICareerProfileStore
        {
            public string Json;
            public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json??throw new FileNotFoundException();
            public void WriteNew(string id,string json)=>Json=json;
            public void Replace(string id,string expectedJson,string json){if(Json!=expectedJson)throw new IOException("stale test");Json=json;}
        }
        private GameObject root;
        private FrontEndMenu menu;
        private InputTestScope inputs;
        private Keyboard keyboard;
        private Gamepad gamepad;
        [SetUp] public void Setup()
        {
            inputs=new InputTestScope();keyboard=InputSystem.AddDevice<Keyboard>();gamepad=InputSystem.AddDevice<Gamepad>();
            root=new GameObject("Front end test");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
        }
        [TearDown] public void Cleanup()
        {Object.DestroyImmediate(root);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(gamepad);inputs.Dispose();}
        private void Submit(string id)
        {var button=menu.Root.Q<Button>(id);button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);}
        [UnityTest] public IEnumerator GamepadNavigatesAndAddsARealTrainingSessionWithTheWeekStillUnapplied()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_m166_pad","Алекс","NOR",11);string before=menu.Session.Career.ToJson();menu.Session.OpenCareerPage(FrontEndScreen.Training);menu.RefreshFrontEnd();yield return null;yield return null;
            menu.Root.Q<Button>("back").Focus();InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(0,-1)});InputTestScope.Update();PumpUiInput();yield return null;yield return null;
            Assert.That((menu.Root.panel.focusController.focusedElement as Button)?.name,Is.EqualTo("add_training_0"));
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));InputTestScope.Update();PumpUiInput();yield return null;yield return null;
            Assert.That(menu.Session.TrainingCount,Is.EqualTo(1));Assert.That(menu.Session.Training(0),Is.EqualTo(CareerTrainingKind.Endurance));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator IllustratedTrainingUsesAllSixExistingTasksAndExactPreviewWithoutApplyingTheWeek()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_m166_training","Алекс","NOR",11);string before=menu.Session.Career.ToJson();menu.RefreshFrontEnd();yield return null;Submit("career_training");yield return null;
            Assert.That(menu.Root.Q("training_tiles").childCount,Is.EqualTo(6));
            for(int kind=0;kind<6;kind++)
            {
                Assert.That(menu.Root.Q("training_art_"+kind),Is.Not.Null);Submit("add_training_"+kind);yield return null;
                Assert.That(menu.Session.TrainingCount,Is.EqualTo(1));Assert.That((int)menu.Session.Training(0),Is.EqualTo(kind));Assert.That(menu.Root.Q<Label>("training_selected_count_"+kind).text,Does.Contain("1"));
                Assert.That(menu.Root.Q<Label>("training_health_forecast"),Is.Not.Null);Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Submit("training_clear");yield return null;
            }
            Submit("add_training_0");yield return null;Submit("add_training_3");yield return null;Submit("add_training_5");yield return null;
            Assert.That(menu.Session.TrainingPlan().UsedSlots,Is.EqualTo(3));Assert.That(menu.Root.Q<Button>("add_training_0").enabledSelf,Is.False);
            yield return CaptureTraining(1600,900,"ru",0,false);yield return CaptureTraining(1024,768,"en",2,true);
            Submit("training_remove_1");yield return null;Assert.That(menu.Session.TrainingCount,Is.EqualTo(2));Assert.That(menu.Root.Q<Button>("add_training_0").enabledSelf,Is.True);Submit("back");yield return null;Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Assert.That(menu.Session.TrainingCount,Is.EqualTo(2));
        }
        private IEnumerator CaptureTraining(int width,int height,string locale,int size,bool contrast)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;var target=new RenderTexture(width,height,0);target.Create();settings.targetTexture=target;Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);menu.ApplyAccessibility(contrast);yield return new WaitForSecondsRealtime(.4f);for(int f=0;f<10;f++)yield return null;
                string folder=Path.GetFullPath("docs/quality/m16-6/training");Directory.CreateDirectory(folder);
                foreach(string section in new[]{"training_forecast","training_card_0","training_card_2","training_card_4"})
                {
                    menu.Root.Q<ScrollView>("page_content").ScrollTo(menu.Root.Q(section));yield return null;yield return null;
                    var selected=menu.Root.Q(section);Assert.That(selected.worldBound.width,Is.GreaterThan(100));Assert.That(selected.worldBound.xMax,Is.LessThanOrEqualTo(menu.Root.worldBound.xMax+1));
                    RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,section+"-"+locale+".png"),pixels.EncodeToPNG());Object.Destroy(pixels);pixels=null;
                }
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;settings.targetTexture=null;Object.Destroy(target);}
        }
        [UnityTest] public IEnumerator PersonalWeatherForecastAndOwnedSkiSelectionUsePreparedRaceWithoutChangingSource()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_m166_weather","Алекс","NOR",11);
            for(int i=0;i<5;i++)menu.Session.RestWeek();string before=menu.Session.Career.ToJson();menu.Session.OpenPreRace();menu.RefreshFrontEnd();yield return null;
            var week=menu.Session.PreparedWeek;Assert.That(menu.Root.Q("pre_race_weather_plan"),Is.Not.Null);Assert.That(menu.Root.Q<Label>("personal_start_weather").text,Does.Contain("°C"));
            Assert.That(menu.Root.Q<Button>("pre_race_ski_ski_neutral"),Is.Not.Null);Assert.That(menu.Root.Q<Button>("pre_race_ski_ski_cold_race"),Is.Null);
            Assert.Throws<ArgumentException>(()=>menu.Session.SelectPreRaceSkis("rifle_neutral_v1"));menu.Session.SelectPreRaceSkis("ski_neutral");Assert.That(menu.Session.PreparedWeek.RaceSnapshotJson,Is.EqualTo(week.RaceSnapshotJson));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            foreach(var dimensions in new[]{new Vector2Int(1600,900),new Vector2Int(1024,768)})
            {
                var target=new RenderTexture(dimensions.x,dimensions.y,0);target.Create();panel.targetTexture=target;Texture2D pixels=null;
                try
                {
                    menu.ApplyPreferences(dimensions.x==1600?"ru":"en",dimensions.x==1600?0:2);yield return new WaitForSecondsRealtime(.4f);for(int f=0;f<10;f++)yield return null;
                    menu.Root.Q<ScrollView>("page_content").ScrollTo(menu.Root.Q("pre_race_weather_plan"));yield return null;yield return null;
                    RenderTexture.active=target;pixels=new Texture2D(dimensions.x,dimensions.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,dimensions.x,dimensions.y),0,0);pixels.Apply();string folder=Path.GetFullPath("docs/quality/m16-6/weather");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,dimensions.x==1600?"forecast-ru.png":"forecast-en-large.png"),pixels.EncodeToPNG());
                }
                finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;panel.targetTexture=null;Object.Destroy(target);}
            }
        }
        [UnityTest] public IEnumerator ExpandedShopHasBothSlotsPaidPurchasesAndReadableLocalizedCards()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_m166_shop","Алекс","NOR",11);menu.RefreshFrontEnd();yield return null;
            Submit("career_equipment");yield return null;Assert.That(menu.Root.Q("equipment_tiles").childCount,Is.EqualTo(8));
            Assert.That(menu.Root.Q<Button>("equip_ski_glide").enabledSelf,Is.False,"New starter cannot buy every upgrade immediately.");
            yield return CaptureEquipment(1600,900,"ru",0,"skis");Submit("equipment_rifles");yield return null;
            Assert.That(menu.Root.Q("equipment_tiles").childCount,Is.EqualTo(4));Assert.That(menu.Root.Q("rifle_art_rifle_precision"),Is.Not.Null);
            Assert.That(menu.Session.Player.SelectedRifle,Is.EqualTo("rifle_neutral_v1"));
            yield return CaptureEquipment(1600,900,"ru",0,"rifles");yield return CaptureEquipment(1024,768,"en",2,"rifles-large");
        }
        private IEnumerator CaptureEquipment(int width,int height,string locale,int size,string name)
        {
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;var target=new RenderTexture(width,height,0);target.Create();panel.targetTexture=target;
            Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);yield return new WaitForSecondsRealtime(.4f);for(int frame=0;frame<10;frame++)yield return null;
                var button=menu.Root.Q<Button>(rifleCategoryId());Assert.That(button.worldBound.width,Is.GreaterThan(60));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath("docs/quality/m16-6/equipment");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+"-"+locale+".png"),pixels.EncodeToPNG());
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;panel.targetTexture=null;Object.Destroy(target);}
            string rifleCategoryId()=>name=="skis"?"equip_ski_neutral":"equip_rifle_neutral_v1";
        }
        [UnityTest] public IEnumerator CareerPagesUseActualPlanAndReturnWithKeyboardFocus()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_pages_ui","Алекс","NOR",11);menu.RefreshFrontEnd();yield return null;
            string before=menu.Session.Career.ToJson();Submit("career_training");yield return null;
            Submit("add_training_0");yield return null;Assert.That(menu.Session.TrainingCount,Is.EqualTo(1));
            Assert.That(menu.Root.Q<Label>("training_budget").text,Does.Contain("1"));Assert.That(menu.Root.Q<Label>("training_budget").text,Does.Contain("3"));Submit("back");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            foreach(string page in new[]{"career_equipment","career_standings","career_history","career_rivalries"})
            {Submit(page);yield return null;Assert.That(menu.Root.Q<Button>("back"),Is.Not.Null);Assert.That(menu.Root.panel.focusController.focusedElement,Is.Not.Null);Submit("back");yield return null;}
            Submit("rest_week");yield return null;Assert.That(menu.Session.Career.Week,Is.EqualTo(2));Assert.That(menu.Session.TrainingCount,Is.Zero);
        }
        [UnityTest] public IEnumerator ResultsShowSavedFieldShootingSplitsPointsAndReturnToCareer()
        {
            // Explicit boundary fixture; full completed production race collection is exercised in core tests.
            var session=menu.Session;session.OpenNewCareer();session.CreateCareer("career_results_ui","Алекс","NOR",81000);
            for(int i=0;i<5;i++)session.RestWeek();session.OpenPreRace();var prepared=session.PreparedWeek;
            var saved=CareerSave.FromJson(prepared.RaceSnapshotJson);var rows=new CareerFinish[saved.AthleteCount];
            for(int i=0;i<rows.Length;i++)
            {
                var athlete=saved.Athlete((i+23)%rows.Length);
                var details=new CareerResultDetails(athlete.Id,new[]{31,27},new[]{ShootingStance.Prone,ShootingStance.Standing},new[]{
                    new CareerResultSplit("split_1","lap_1",75000,75000),new CareerResultSplit("split_2","lap_2",210000,210000)});
                rows[i]=new CareerFinish(athlete.Id,300000+i*1523,300000+i*1523,1,details:details);
            }
            var record=new CareerRaceRecord(saved.SeasonId+"_"+prepared.Event.Id,saved.SeasonId,prepared.Event.Id,prepared.Event.Format,prepared.RaceSeed,new string('a',64),rows);
            session.BeginRace();session.FinishRace(record);menu.RefreshFrontEnd();yield return null;yield return null;
            Assert.That(session.ResultsSummary.Player.Rank,Is.EqualTo(8));Assert.That(menu.Root.Q("result_protocol").childCount,Is.EqualTo(30));
            Assert.That(menu.Root.Q<Label>("result_penalties").text,Does.Contain("1"));Assert.That(menu.Root.Q("result_shooting").childCount,Is.EqualTo(2));
            yield return CaptureResults(1600,900,"ru",0);
            yield return CaptureResults(1024,768,"en",2);
            Submit("result_protocol_next");yield return null;yield return null;
            Assert.That(menu.Root.Q("result_row_0").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));Assert.That(menu.Root.Q("result_row_10").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Submit("result_protocol_prev");yield return null;yield return null;Assert.That(menu.Root.Q("result_row_7").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Submit("results_splits");yield return null;yield return null;
            Assert.That(menu.Root.Q("result_splits").childCount,Is.EqualTo(2));
            Assert.That(menu.Root.Q("result_split_1").Q<Label>().text,Is.EqualTo("Split 2"));
            Assert.That(menu.Root.Q("result_split_1").Query<Label>().ToList()[1].text,Does.Contain("2:15.000"));
            string before=session.Career.ToJson();Submit("career_return");yield return null;
            Assert.That(session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(session.Career.ToJson(),Is.EqualTo(before));
            Assert.That(session.NextEvent().Format,Is.EqualTo(CompetitionFormat.Pursuit));
            Assert.That(menu.Root.Q("career_calendar").childCount,Is.EqualTo(16));
            Assert.That(menu.Root.Q<Label>("career_standing").text,Does.Contain("23"));
            Assert.That(menu.Root.Q("career_event_junior_open_sprint").Query<Label>().ToList()[1].text,Does.Contain("8"));
            yield return CaptureDashboard(1600,900,"ru",0);yield return CaptureDashboard(1024,768,"en",2);
            Assert.That(session.Career.ToJson(),Is.EqualTo(before));
        }
        private IEnumerator CaptureDashboard(int width,int height,string locale,int size)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            var target=new RenderTexture(width,height,0);target.Create();settings.targetTexture=target;Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);yield return new WaitForSecondsRealtime(.35f);for(int f=0;f<10;f++)yield return null;
                var bounds=menu.Root.worldBound;var button=menu.Root.Q<Button>("prepare");
                Assert.That(button.worldBound.yMax,Is.LessThan(bounds.yMax));Assert.That(button.worldBound.xMax,Is.LessThan(bounds.xMax));
                Assert.That(menu.Root.Q("career_calendar").childCount,Is.EqualTo(16));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/dashboard-20261012"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,"dashboard-"+locale+".png"),pixels.EncodeToPNG());
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;settings.targetTexture=null;Object.Destroy(target);}
        }
        private IEnumerator CaptureResults(int width,int height,string locale,int size)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            var target=new RenderTexture(width,height,24);target.Create();settings.targetTexture=target;Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);yield return new WaitForSecondsRealtime(.35f);for(int f=0;f<10;f++)yield return null;
                var bounds=menu.Root.worldBound;var button=menu.Root.Q<Button>("career_return");
                Assert.That(button.worldBound.yMax,Is.LessThan(bounds.yMax));Assert.That(button.worldBound.xMax,Is.LessThan(bounds.xMax));
                var header=menu.Root.Q("result_table_header");Assert.That(header.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));
                var pager=menu.Root.Q<Button>("result_protocol_next");
                Assert.That(pager.worldBound.yMax,Is.LessThan(pager.GetFirstAncestorOfType<ScrollView>().worldBound.yMax-8));
                var playerRow=menu.Root.Q("result_row_7");Assert.That(playerRow.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
                int visibleRows=0;foreach(var row in menu.Root.Q("result_protocol").Children())if(row.resolvedStyle.display==DisplayStyle.Flex)visibleRows++;
                Assert.That(visibleRows,Is.GreaterThanOrEqualTo(10),"The results protocol must show at least10 athletes per page.");
                foreach(var row in menu.Root.Q("result_protocol").Children())if(row.resolvedStyle.display==DisplayStyle.Flex)Assert.That(row.worldBound.yMax,Is.LessThan(pager.worldBound.yMin));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/results-20261012"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,"results-"+locale+".png"),pixels.EncodeToPNG());
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;settings.targetTexture=null;Object.Destroy(target);}
        }
        [UnityTest] public IEnumerator CareerLaunchUsesSavedAthleteAndPauseAbandonPreserveTheWeek()
        {
            yield return null;Submit("new_career");yield return null;Submit("creator_mode_own");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Алекс";Submit("create");yield return null;Submit("confirm_create");yield return null;
            for(int i=0;i<5;i++){Submit("rest_week");yield return null;}
            string before=menu.Session.Career.ToJson();Submit("prepare");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.PreRace));Assert.That(menu.Root.Q<Button>("start_race"),Is.Not.Null);
            yield return CapturePreparation(1600,900,"ru",0);
            yield return CapturePreparation(1024,768,"en",2);
            Submit("start_race");
            for(int i=0;i<10&&menu.Race.Loading;i++)yield return null;
            Assert.That(menu.LastMessage,Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Race));
            var host=menu.Race.Host;Assert.That(host,Is.Not.Null);Assert.That(host.ManualStepper,Is.Not.Null);
            Assert.That(host.Environment,Is.Not.Null);
            Assert.That(host.Runner.Field.WeatherTimeline,Is.Not.Null);host.GameplayHud.Refresh();
            Assert.That(host.GameplayHud.Root.Q<Label>("race_weather_compact").text,Does.Contain("°C"));
            Assert.That(host.Runner.Field.GetAthlete(host.PlayerIndex).AthleteId.Value,Is.EqualTo(menu.Session.Career.PlayerId));
            Assert.That(menu.Root.style.display.value,Is.EqualTo(DisplayStyle.None));
            host.GameplayHud.SetBroadcastVisible(true);yield return null;
            Assert.That(host.GameplayHud.Root.Q<Label>("live_tv_row_0").text,Does.Contain(menu.Session.Player.Name));
            host.GameplayHud.SetBroadcastVisible(false);host.GameplayHud.Refresh();yield return null;
            Assert.That(host.GameplayHud.Root.panel,Is.Not.Null);
            Assert.That(host.GameplayHud.Locale,Is.EqualTo("en"));Assert.That(host.GameplayHud.TextSizeIndex,Is.EqualTo(2));
            Assert.That(host.GameplayHud.Root.Q<Label>("athlete_name").text,Is.EqualTo(menu.Session.Player.Name));
            Assert.That(host.GetComponent<RaceHud>().enabled,Is.False);Assert.That(host.GetComponent<RangePresentation>().DrawLegacyHud,Is.False);
            string hudFolder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/compact-hud-20261012"));Directory.CreateDirectory(hudFolder);
            host.GameplayHud.SetPreferences("ru",0,menu.Session.Player.Name);
            yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(hudFolder,"career-game-view.png"));
            yield return null;yield return null;yield return null;
            menu.Race.Pause();long tick=host.Runner.Field.Tick.Value;yield return null;yield return null;
            Assert.That(host.GameplayHud.Root?.panel,Is.Null);
            Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));Assert.That(menu.Root.Q<Button>("resume"),Is.Not.Null);
            Submit("resume");yield return null;Assert.That(menu.Race.Paused,Is.False);
            menu.Race.Pause();yield return null;Submit("abandon");yield return null;
            Assert.That(menu.Race.Host,Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.PreRace));
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Assert.That(root.GetComponentsInChildren<ProductionVenueHost>().Length,Is.Zero);
        }
        private IEnumerator CapturePreparation(int width,int height,string locale,int size)
        {
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            var target=new RenderTexture(width,height,0);target.Create();panel.targetTexture=target;Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);yield return new WaitForSecondsRealtime(.35f);for(int f=0;f<10;f++)yield return null;
                var bounds=menu.Root.worldBound;var start=menu.Root.Q<Button>("start_race");
                Assert.That(start.worldBound.yMax,Is.LessThan(bounds.yMax));Assert.That(start.worldBound.width,Is.GreaterThan(200));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/pre-race-20261011"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,"pre-race-"+locale+".png"),pixels.EncodeToPNG());
            }
            finally{if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;panel.targetTexture=null;Object.Destroy(target);}
        }
        [UnityTest] public IEnumerator NewProfileContinueAndSettingsUseTheRealMenuWithoutAdvancingAWeek()
        {
            yield return null;yield return null;
            Assert.That(menu.Root.Q<Button>("continue").enabledSelf,Is.False);
            Submit("new_career");yield return null;Submit("creator_mode_own");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Алекс";Submit("create");yield return null;Submit("confirm_create");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(menu.Session.Player.Name,Is.EqualTo("Алекс"));
            string saved=menu.Session.Career.ToJson();Assert.That(menu.Session.Career.Week,Is.EqualTo(1));
            Submit("settings");yield return null;menu.ApplyPreferences("en",2);yield return null;
            Assert.That(menu.Root.Q<Button>("back").text,Is.EqualTo("Back"));Submit("back");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Submit("back");yield return null;
            Submit("continue");yield return null;Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(saved));
            Assert.That(menu.LastMessage,Is.Null);
        }
        [UnityTest] public IEnumerator AccessibilitySettingsExplainAssistsAndPreserveCareerAndPreferences()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("accessibility_ui","Алекс","NOR",11);string before=menu.Session.Career.ToJson();
            menu.Session.OpenSettings();menu.ApplyPreferences("en",2);menu.ApplyAccessibility(true);yield return null;yield return null;
            Assert.That(menu.Root.Q<Toggle>("high_contrast"),Is.Not.Null);
            Assert.That(menu.Root.Q<Label>("accessibility_assists").text,Does.Contain("manual steering").And.Contain("automatic mat approach"));
            Assert.That(menu.HighContrast,Is.True);Assert.That(menu.TextSizeIndex,Is.EqualTo(2));
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));menu.Back();yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(menu.HighContrast,Is.True);
        }
        [UnityTest] public IEnumerator NavigationHasVisibleFocusAndSkipsUnavailableContinue()
        {
            yield return null;yield return null;
            var first=menu.Root.Q<Button>("new_career");first.Focus();
            using(var move=NavigationMoveEvent.GetPooled(Vector2.down))first.SendEvent(move);
            yield return null;
            var selected=menu.Root.panel.focusController.focusedElement as Button;
            Assert.That(selected.name,Is.EqualTo("settings"));Assert.That(selected.style.borderLeftWidth.value,Is.EqualTo(5));
            Assert.That(selected.resolvedStyle.borderLeftWidth,Is.GreaterThan(3),"Pixel snapping may quantize the authored five-point focus edge.");
            using(var submit=NavigationSubmitEvent.GetPooled())selected.SendEvent(submit);yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Settings));menu.Back();yield return null;yield return null;
            Assert.That((menu.Root.panel.focusController.focusedElement as Button).name,Is.EqualTo("settings"));
        }
        [UnityTest] public IEnumerator KeyboardAndGamepadCancelReturnToTheCallingPage()
        {
            yield return null;Submit("settings");yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputTestScope.Update();PumpUiInput();yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Home));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();yield return null;
            Submit("settings");yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.East));InputTestScope.Update();PumpUiInput();yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Home));
        }
        [UnityTest] public IEnumerator InvalidNameStaysEditableWithReadableErrorAndDoesNotCreateProfile()
        {
            yield return null;Submit("new_career");yield return null;Submit("creator_mode_own");yield return null;Submit("create");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.NewCareer));Assert.That(menu.Session.CanContinue,Is.False);
            Assert.That(menu.LastMessage,Does.Contain("имя"));Assert.That(menu.Root.Q<TextField>("athlete_name"),Is.Not.Null);
            Assert.That(menu.Root.Q<Label>("message").text,Is.EqualTo(menu.LastMessage));
        }
        [UnityTest] public IEnumerator RealGamepadMovesFocusAndSubmitsTheMenu()
        {
            yield return null;yield return null;menu.Root.Q<Button>("new_career").Focus();
            InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(0,-1)});InputTestScope.Update();
            PumpUiInput();
            yield return null;yield return null;
            Assert.That((menu.Root.panel.focusController.focusedElement as Button)?.name,Is.EqualTo("settings"));
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));InputTestScope.Update();
            PumpUiInput();
            yield return null;yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Settings));
        }
        private static void PumpUiInput()
        {
            // A hidden Editor switches input buffers and normally skips unfocused
            // panel events. Pump Unity's real provider and event dispatcher while
            // the synthetic player buffer is selected. Product focus policy stays
            // unchanged; zero is DefaultEventSystem.UpdateMode.Always.
            var type=Type.GetType("UnityEngine.InputForUI.EventProvider, UnityEngine.InputForUIModule",true);
            type.GetMethod("NotifyUpdate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            var runtime=typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility",true);
            var events=runtime.GetProperty("defaultEventSystem",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).GetValue(null);
            var method=events.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            method.Invoke(events,new[]{Enum.ToObject(method.GetParameters()[0].ParameterType,0)});
        }
        [UnityTest] public IEnumerator MenuRendersReadableRussianAndEnglishAtDifferentAspectRatios()
        {
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/pre-race-20261011"));Directory.CreateDirectory(folder);
            foreach(var dimensions in new[]{new Vector2Int(1600,900),new Vector2Int(1024,768)})
            {
                var target=new RenderTexture(dimensions.x,dimensions.y,0);target.Create();panel.targetTexture=target;
                Texture2D pixels=null;
                try
                {
                    menu.ApplyPreferences(dimensions.x==1600?"ru":"en",dimensions.x==1600?0:2);
                    yield return null;yield return null;yield return null;
                    var bounds=menu.Root.worldBound;var button=menu.Root.Q<Button>("new_career");
                    Assert.That(button.worldBound.width,Is.GreaterThan(200));Assert.That(button.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));
                    Assert.That(button.worldBound.yMax,Is.LessThanOrEqualTo(bounds.yMax));
                    RenderTexture.active=target;pixels=new Texture2D(dimensions.x,dimensions.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,dimensions.x,dimensions.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,dimensions.x==1600?"frontend-ru.png":"frontend-en-large-4x3.png"),pixels.EncodeToPNG());
                }
                finally
                {if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;panel.targetTexture=null;Object.Destroy(target);}
            }
        }
    }
}
