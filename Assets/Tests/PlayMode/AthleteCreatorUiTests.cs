using System.Collections;
using System.IO;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteCreatorUiTests
    {
        private sealed class Store:ICareerProfileStore
        {
            public string Json;public int Writes;public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json;
            public void WriteNew(string id,string json){Assert.That(Json,Is.Null);Json=json;Writes++;}
            public void Replace(string id,string expected,string json){Assert.That(Json,Is.EqualTo(expected));Json=json;}
        }
        private GameObject root;private FrontEndMenu menu;private Store store;
        [SetUp] public void Setup()
        {
            root=new GameObject("M15.6 native creator UI");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;
            store=new Store();menu.Initialize(store);
        }
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private void Submit(string id)
        {var button=menu.Root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);}
        [UnityTest,Timeout(240000)] public IEnumerator OwnAthleteCanBeEditedReviewedSavedRestoredAndRenderedInARealRace()
        {
            yield return null;Submit("new_career");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Алекс Северин";
            var tier=menu.Root.Q<DropdownField>("career_start_profile");tier.value=tier.choices[2];yield return null;
            Assert.That(menu.CreationDraft.Age,Is.EqualTo(25));Assert.That(menu.CreationDraft.StartProfileId,Is.EqualTo("world_cup_debut"));
            var rig=menu.CreatorStudio.Rig;yield return Capture("creator-identity-ru",1600,900,"ru",0);
            Submit("creator_step_1");yield return null;
            var face=menu.Root.Q<DropdownField>("creator_face");face.value=face.choices[4];yield return null;
            var skin=menu.Root.Q<DropdownField>("creator_skin");skin.value=skin.choices[7];yield return null;
            var build=menu.Root.Q<DropdownField>("creator_body");build.value=build.choices[2];yield return null;
            Assert.That(menu.CreatorStudio.Rig,Is.SameAs(rig));Submit("creator_view_face");yield return null;
            yield return Capture("creator-face-ru",1600,900,"ru",0);
            yield return Capture("creator-face-en-large",1024,768,"en",2);
            var appearance=menu.CreationDraft.Appearance;Assert.That(appearance.FaceId,Is.EqualTo("broad"));Assert.That(appearance.SkinId,Is.EqualTo("dark_deep"));Assert.That(appearance.BodyId,Is.EqualTo("athletic"));
            Submit("creator_step_0");yield return null;Assert.That(menu.Root.Q<TextField>("athlete_name").value,Is.EqualTo("Алекс Северин"));
            Submit("creator_step_2");yield return null;var preset=menu.Root.Q<DropdownField>("creator_specialization");preset.value=preset.choices[2];yield return null;
            Submit("creator_plus_0");yield return null;Assert.That(menu.CreationDraft.BudgetRemaining,Is.GreaterThanOrEqualTo(0));
            Submit("creator_view_front");yield return Capture("creator-skills-ru",1600,900,"ru",0);
            Submit("create");yield return null;Assert.That(store.HasProfile,Is.False);Assert.That(menu.Session.Career,Is.Null);
            yield return Capture("creator-confirm-ru",1600,900,"ru",0);
            Submit("confirm_create");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(menu.CreatorStudio,Is.Null);
            Assert.That(menu.Session.Player.Appearance.ToJson(),Is.EqualTo(appearance.ToJson()));
            string saved=store.Json;menu.Back();menu.Session.ContinueCareer();menu.RefreshFrontEnd();Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(saved));
            while(menu.Session.Definitions.Calendar.AtWeek(menu.Session.Career.Week)==null)menu.Session.RestWeek();
            menu.Session.OpenPreRace();menu.RefreshFrontEnd();menu.Race.Launch(menu);
            for(int i=0;i<120&&menu.Race.Loading;i++)yield return null;
            var host=menu.Race.Host;Assert.That(host,Is.Not.Null,menu.LastMessage);host.enabled=false;
            var raceRig=host.Proxies.GetRig(menu.Session.Player.Id);var applied=raceRig.GetComponent<AthleteAppearancePresentation>();var recipe=AthleteFaceCatalog.Load();
            Assert.That(applied.FaceVariant,Is.EqualTo(4));Assert.That(applied.SkinVariant,Is.EqualTo(recipe.SkinIndex(appearance.SkinId)));Assert.That(applied.CosmeticVariant,Is.EqualTo(recipe.BodyIndex(appearance.BodyId)));
            Assert.That(applied.Nation,Is.EqualTo(menu.Session.Player.Nation));Assert.That(host.Runner.Field.Tick.Value,Is.Zero,"Preview and loading must not bypass the countdown.");
            var cameraObject=new GameObject("Created athlete race evidence",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.GetComponent<Camera>();
            camera.transform.position=raceRig.transform.position+raceRig.transform.forward*3.6f+Vector3.up*1.15f;camera.transform.LookAt(raceRig.transform.position+Vector3.up*.95f);camera.fieldOfView=34;
            host.Environment.SetViewCamera(camera);host.Environment.Render(camera,"high",0);
            var target=new RenderTexture(1280,900,24);target.Create();camera.targetTexture=target;
            try{for(int i=0;i<3;i++)yield return null;Save(target,"created-athlete-in-race");}
            finally{camera.targetTexture=null;target.Release();Object.Destroy(target);}
            menu.Race.Abandon();yield return null;
        }
        private IEnumerator Capture(string name,int width,int height,string locale,int size)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=settings.targetTexture;var target=new RenderTexture(width,height,24);target.Create();
            try
            {
                settings.targetTexture=target;menu.ApplyPreferences(locale,size);for(int i=0;i<5;i++)yield return null;yield return new WaitForSecondsRealtime(.25f);
                var bounds=menu.Root.worldBound;var model=menu.Root.Q<Image>("creator_model");var score=menu.Root.Q<Label>("creator_ovr");
                Assert.That(model.image,Is.SameAs(menu.CreatorStudio.Texture));Assert.That(model.worldBound.width,Is.GreaterThan(bounds.width*.2f));
                Assert.That(model.worldBound.height,Is.GreaterThan(bounds.height*.4f));Assert.That(score.worldBound.xMin,Is.GreaterThanOrEqualTo(model.worldBound.xMax));
                Assert.That(score.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));Save(target,name);
            }
            finally{settings.targetTexture=previous;target.Release();Object.Destroy(target);}
        }
        private static void Save(RenderTexture target,string name)
        {
            var previous=RenderTexture.active;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m15-6"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;Object.Destroy(texture);}
        }
    }
}
