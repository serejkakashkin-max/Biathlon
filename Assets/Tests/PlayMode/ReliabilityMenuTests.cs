using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
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
    public sealed class ReliabilityMenuTests
    {
        private string directory;private GameObject root;private FrontEndMenu menu;
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"biathlon-native-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
        [TearDown]public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);if(Directory.Exists(directory)){foreach(string file in Directory.GetFiles(directory))File.Delete(file);foreach(string folder in Directory.GetDirectories(directory)){foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}Directory.Delete(directory);}}
        private void Open(FileCareerProfileStore store)
        {root=new GameObject("M16 native reliability");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store);menu.SetDiagnosticDirectoryForReview(Path.Combine(directory,"Reports"));}
        private void Submit(string id){var b=menu.Root.Q<Button>(id);Assert.That(b,Is.Not.Null,id);b.Focus();using(var e=NavigationSubmitEvent.GetPooled())b.SendEvent(e);}
        private string CreateProfile()
        {
            var store=new FileCareerProfileStore(directory);var session=new FrontEndSession(Read,store,s=>new SeededRandomSource(s));session.OpenNewCareer();session.CreateCareer("career_recovery","PRIVATE_NATIVE_NAME","NOR",1616);session.RestWeek();return Path.Combine(directory,"career_recovery.json");
        }
        [UnityTest]public IEnumerator CorruptPrimaryRecoversSameCareerWithVisibleNoticeAndAnonymousReport()
        {
            string path=CreateProfile();File.WriteAllText(path,"{truncated");var store=new FileCareerProfileStore(directory);Open(store);yield return null;Submit("continue");yield return null;
            Assert.That(menu.Session.Career.Id,Is.EqualTo("career_recovery"));Assert.That(menu.Session.Career.Week,Is.EqualTo(1));Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.Backup));Assert.That(menu.LastMessage,Does.Contain("СЂРµР·РµСЂРІРЅРѕР№"));Assert.That(File.ReadAllText(path),Is.EqualTo("{truncated"));
            yield return Capture("recovered-career-ru",1600,900,"ru",0);
            menu.Session.OpenSettings();menu.RefreshFrontEnd();Submit("settings_page_4");yield return null;Assert.That(menu.Root.Q<Label>("save_health_status").text,Does.Contain("СЂРµР·РµСЂРІРЅРѕР№"));
            yield return Capture("save-support-en-large",1024,768,"en",2);Submit("export_diagnostic");yield return null;
            Assert.That(File.Exists(menu.LastDiagnosticExport),Is.True);Assert.That(menu.LastMessage,Does.Contain("Diagnostics"));
            using(var zip=ZipFile.OpenRead(menu.LastDiagnosticExport))foreach(var entry in zip.Entries)using(var reader=new StreamReader(entry.Open()))
            {string json=reader.ReadToEnd();Assert.That(json,Does.Not.Contain("PRIVATE_NATIVE_NAME"));Assert.That(json,Does.Not.Contain(directory));Assert.That(json,Does.Not.Contain("career_recovery"));}
            menu.ApplyPreferences("ru",0);menu.Back();menu.Session.RestWeek();Assert.That(CareerSave.FromJson(File.ReadAllText(path)).Week,Is.EqualTo(2));
            menu.Session.OpenSettings();menu.RefreshFrontEnd();Submit("settings_page_4");Assert.That(menu.Root.Q<Label>("save_health_status").text,Does.Not.Contain("СЂРµР·РµСЂРІРЅРѕР№"));
        }
        [UnityTest]public IEnumerator UnsupportedSaveShowsFriendlyMessageAndNeverDowngradesItsFiles()
        {
            string path=CreateProfile();string future=File.ReadAllText(path).Replace("\"save_version\":4","\"save_version\":99");File.WriteAllText(path,future);Open(new FileCareerProfileStore(directory));yield return null;Submit("continue");yield return null;
            Assert.That(menu.Session.Career,Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Home));Assert.That(menu.LastMessage,Does.Contain("РґСЂСѓРіРѕР№ РІРµСЂСЃРёРµР№"));Assert.That(File.ReadAllText(path),Is.EqualTo(future));
            yield return Capture("unsupported-save-ru",1600,900,"ru",0);
        }
        [UnityTest]public IEnumerator NewlyCreatedCareerReportsPublishedPrimaryWithoutNeedingContinue()
        {
            Open(new FileCareerProfileStore(directory));menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_created","Review","NOR",1616);
            menu.Session.OpenSettings();menu.RefreshFrontEnd();Submit("settings_page_4");yield return null;
            Assert.That(menu.Root.Q<Label>("save_health_status").text,Is.EqualTo("РћСЃРЅРѕРІРЅРѕРµ СЃРѕС…СЂР°РЅРµРЅРёРµ РїСЂРѕРІРµСЂРµРЅРѕ."));
        }
        [UnityTest]public IEnumerator UnavailableSavedCalendarKeepsValidFilesAndOffersReadableHomeRecovery()
        {
            string path=CreateProfile();string before=File.ReadAllText(path);var save=CareerSave.FromJson(before);
            string unavailable=before.Replace("\"calendar_id\":\""+save.CalendarId+"\"","\"calendar_id\":\"missing_calendar_v1\"");
            Assert.That(CareerSave.FromJson(unavailable).CalendarId,Is.EqualTo("missing_calendar_v1"));
            File.WriteAllText(path,unavailable);string backup=File.ReadAllText(path+".bak");Open(new FileCareerProfileStore(directory));yield return null;Submit("continue");yield return null;
            Assert.That(menu.Session.Career,Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Home));Assert.That(menu.LastMessage,Does.Contain("РґР°РЅРЅС‹Рµ"));
            Assert.That(menu.Root.Q<Label>("message").text,Is.EqualTo(menu.LastMessage));Assert.That(File.ReadAllText(path),Is.EqualTo(unavailable));Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(backup));
            Submit("settings");yield return null;Submit("settings_page_4");Submit("export_diagnostic");Assert.That(File.Exists(menu.LastDiagnosticExport),Is.True);
        }
        private IEnumerator Capture(string file,int width,int height,string locale,int size)
        {
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=panel.targetTexture;var target=new RenderTexture(width,height,24);target.Create();
            try
            {panel.targetTexture=target;if(menu.Locale!=locale||menu.TextSizeIndex!=size)menu.ApplyPreferences(locale,size);for(int i=0;i<6;i++)yield return null;yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
                Assert.That(menu.Root.Q("menu_body").resolvedStyle.opacity,Is.GreaterThan(.95f),"Capture waits for the approved page fade to complete.");
                if(!string.IsNullOrEmpty(menu.LastMessage)){var notice=menu.Root.Q<Label>("message");Assert.That(notice.text,Is.EqualTo(menu.LastMessage));Assert.That(notice.worldBound.yMax,Is.LessThan(menu.Root.worldBound.yMax),"Recovery notice stays visible above navigation.");}
                var before=RenderTexture.active;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
                try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();Directory.CreateDirectory("docs/quality/m16");File.WriteAllBytes("docs/quality/m16/"+file+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=before;Object.Destroy(pixels);}}
            finally{panel.targetTexture=previous;target.Release();Object.Destroy(target);}
        }
    }
}
