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
            yield return Capture("creator-mode-ru",1600,900,"ru",0,false);Submit("creator_mode_own");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Алекс Северин";
            var tier=menu.Root.Q<DropdownField>("career_start_profile");tier.value=tier.choices[2];yield return null;
            Assert.That(menu.CreationDraft.Age,Is.EqualTo(25));Assert.That(menu.CreationDraft.StartProfileId,Is.EqualTo("world_cup_debut"));
            var rig=menu.CreatorStudio.Rig;yield return Capture("creator-identity-ru",1600,900,"ru",0);
            Submit("creator_step_1");yield return null;
            var face=menu.Root.Q<DropdownField>("creator_face");face.value=face.choices[4];yield return null;
            var skin=menu.Root.Q<DropdownField>("creator_skin");skin.value=skin.choices[7];yield return null;
            var build=menu.Root.Q<DropdownField>("creator_body");build.value=build.choices[2];yield return null;
            var eyes=menu.Root.Q<DropdownField>("creator_eyes");eyes.value=eyes.choices[2];yield return null;
            Submit("creator_look_1");yield return null;
            menu.Root.Q<SliderInt>("creator_height").value=198;menu.Root.Q<SliderInt>("creator_shoulders").value=100;yield return null;
            Assert.That(menu.CreatorStudio.Rig.transform.localScale.x,Is.EqualTo(1980/1850f).Within(.00001));
            Submit("creator_look_2");yield return null;
            var headwear=menu.Root.Q<DropdownField>("creator_headwear");headwear.value=headwear.choices[1];yield return null;
            var hair=menu.Root.Q<DropdownField>("creator_hair");hair.value=hair.choices[1];yield return null;
            var hairColor=menu.Root.Q<DropdownField>("creator_hair_color");hairColor.value=hairColor.choices[3];yield return null;
            Assert.That(menu.CreationDraft.Appearance.GogglesId,Is.EqualTo("raised"));
            yield return Capture("creator-expanded-hair-ru",1600,900,"ru",0);
            Submit("creator_look_3");yield return null;
            var gloves=menu.Root.Q<DropdownField>("creator_gloves");gloves.value=gloves.choices[1];yield return null;
            var boots=menu.Root.Q<DropdownField>("creator_boots");boots.value=boots.choices[2];yield return null;
            var lens=menu.Root.Q<DropdownField>("creator_lens");lens.value=lens.choices[1];yield return null;
            var training=menu.Root.Q<DropdownField>("creator_training_accent");training.value=training.choices[2];yield return null;
            yield return Capture("creator-expanded-gear-en-large",1024,768,"en",2);
            Submit("creator_look_0");yield return null;
            Assert.That(menu.CreatorStudio.Rig,Is.SameAs(rig));Submit("creator_view_face");yield return null;
            yield return Capture("creator-face-ru",1600,900,"ru",0);
            yield return Capture("creator-face-en-large",1024,768,"en",2);
            var appearance=menu.CreationDraft.Appearance;Assert.That(appearance.FaceId,Is.EqualTo("broad"));Assert.That(appearance.SkinId,Is.EqualTo("dark_deep"));Assert.That(appearance.BodyId,Is.EqualTo("athletic"));
            Assert.That(appearance.HeightMillimeters,Is.EqualTo(1980));Assert.That(appearance.ShoulderWidthBasis,Is.EqualTo(10000));Assert.That(appearance.HairStyleId,Is.EqualTo("short_swept"));Assert.That(appearance.HairColorId,Is.EqualTo("blond"));Assert.That(appearance.EyeColorId,Is.EqualTo("green"));Assert.That(appearance.GlovesId,Is.EqualTo("ivory"));Assert.That(appearance.BootsId,Is.EqualTo("red"));Assert.That(appearance.TrainingAccentId,Is.EqualTo("sunrise"));
            Submit("creator_step_0");yield return null;Assert.That(menu.Root.Q<TextField>("athlete_name").value,Is.EqualTo("Алекс Северин"));
            Submit("creator_step_2");yield return null;var preset=menu.Root.Q<DropdownField>("creator_specialization");preset.value=preset.choices[2];yield return null;
            Submit("creator_minus_0");yield return null;Submit("creator_plus_0");yield return null;Assert.That(menu.CreationDraft.BudgetRemaining,Is.GreaterThanOrEqualTo(0));
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
            Assert.That(applied.AppliedProfile.ToJson(),Is.EqualTo(appearance.ToJson()));Assert.That(raceRig.transform.localScale.x,Is.EqualTo(1980/1850f).Within(.00001));
            var actualKit=raceRig.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials[1].GetColor("_KitColor");var expectedKit=AthleteAppearancePresentation.NationColor(menu.Session.Player.Nation);
            Assert.That(Vector4.Distance(actualKit,expectedKit),Is.LessThan(.00001f),"Training customization must not override the official national race kit; tolerate only native material float precision.");
            var cameraObject=new GameObject("Created athlete race evidence",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.GetComponent<Camera>();
            camera.transform.position=raceRig.transform.position+raceRig.transform.forward*3.6f+Vector3.up*1.15f;camera.transform.LookAt(raceRig.transform.position+Vector3.up*.95f);camera.fieldOfView=34;
            host.Environment.SetViewCamera(camera);host.Environment.Render(camera,"high",0);
            var target=new RenderTexture(1280,900,24);target.Create();camera.targetTexture=target;
            try{for(int i=0;i<3;i++)yield return null;Save(target,"created-athlete-in-race");}
            finally{camera.targetTexture=null;target.Release();Object.Destroy(target);}
            menu.Race.Abandon();yield return null;
        }
        [UnityTest,Timeout(240000)] public IEnumerator ExistingAthleteCatalogFiltersWithoutRerollsAndConfirmsTheOriginalIdentity()
        {
            yield return null;Submit("new_career");yield return null;
            Assert.That(menu.Root.Q<Button>("creator_mode_own"),Is.Not.Null);Assert.That(menu.Root.Q<Button>("creator_mode_existing"),Is.Not.Null);
            Submit("creator_mode_existing");yield return null;var catalog=menu.AthleteCatalog;Assert.That(catalog.Count,Is.EqualTo(90));
            var league=menu.Root.Q<DropdownField>("catalog_circuit");league.value=league.choices[3];yield return null;
            var selected=catalog.Get(menu.SelectedCatalogAthlete);var rig=menu.CreatorStudio.Rig;
            Assert.That(selected.Circuit,Is.EqualTo(CareerCircuit.WorldCup));
            yield return Capture("creator-catalog-ru",1600,900,"ru",0);
            Submit("catalog_details");yield return null;yield return Capture("creator-catalog-en-large",1024,768,"en",2);
            for(int stat=0;stat<7;stat++)Assert.That(menu.Root.Q("catalog_stat_"+stat),Is.Not.Null);
            Submit("catalog_details_next");yield return null;for(int stat=7;stat<13;stat++)Assert.That(menu.Root.Q("catalog_stat_"+stat),Is.Not.Null);
            yield return Capture("creator-catalog-abilities-2-en",1024,768,"en",2);
            menu.Root.Q<TextField>("catalog_search").value=selected.Name;yield return null;
            Submit("catalog_advanced");yield return null;
            menu.Root.Q<IntegerField>("catalog_age_min").value=selected.Age;yield return null;menu.Root.Q<IntegerField>("catalog_age_max").value=selected.Age;yield return null;
            menu.Root.Q<IntegerField>("catalog_rating_min").value=selected.Ratings.Overall;yield return null;menu.Root.Q<IntegerField>("catalog_rating_max").value=selected.Ratings.Overall;yield return null;
            Assert.That(menu.SelectedCatalogAthlete,Is.EqualTo(selected.Id));Assert.That(menu.CreatorStudio.Rig,Is.SameAs(rig));
            menu.Root.Q<TextField>("catalog_search").value="no matching athlete";yield return null;Assert.That(rig.gameObject.activeSelf,Is.False);Assert.That(menu.Root.Q<Button>("catalog_review").enabledSelf,Is.False);
            menu.Root.Q<TextField>("catalog_search").value=selected.Name;yield return null;Assert.That(rig.gameObject.activeSelf,Is.True);Assert.That(menu.CreatorStudio.Rig,Is.SameAs(rig));
            Submit("catalog_review");yield return null;Assert.That(store.HasProfile,Is.False);Assert.That(menu.Session.Career,Is.Null);
            Assert.That(menu.Root.Q<SliderInt>("career_start_age"),Is.Null,"Authentic athlete abilities and age are not editable.");
            yield return Capture("creator-catalog-confirm-ru",1600,900,"ru",0);
            Submit("catalog_confirm");yield return null;Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));
            Assert.That(menu.Session.Player.Id,Is.EqualTo(selected.Id));Assert.That(menu.Session.Player.Name,Is.EqualTo(selected.Name));Assert.That(menu.Session.Player.Nation,Is.EqualTo(selected.Nation));Assert.That(menu.Session.Player.Age,Is.EqualTo(selected.Age));
            Assert.That(menu.Session.Career.Creation.Mode,Is.EqualTo(CareerCreationMode.ExistingAthlete));int copies=0;
            for(int i=0;i<menu.Session.Career.AthleteCount;i++)if(menu.Session.Career.Athlete(i).Id==selected.Id)copies++;
            Assert.That(copies,Is.EqualTo(1));Assert.That(menu.Session.Career.AthleteCount,Is.EqualTo(30));
            for(int stat=0;stat<13;stat++)Assert.That(menu.Session.Player.Stat((CareerStat)stat),Is.EqualTo(selected.Stat((CareerStat)stat)));
            Assert.That(menu.CreatorStudio,Is.Null);Assert.That(menu.Session.Player.Appearance.ToJson(),Is.EqualTo(selected.Appearance.ToJson()));
        }
        private IEnumerator Capture(string name,int width,int height,string locale,int size,bool modelRequired=true)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=settings.targetTexture;var target=new RenderTexture(width,height,24);target.Create();
            try
            {
                settings.targetTexture=target;menu.ApplyPreferences(locale,size);for(int i=0;i<5;i++)yield return null;yield return new WaitForSecondsRealtime(.25f);
                var bounds=menu.Root.worldBound;var model=menu.Root.Q<Image>("creator_model");var score=menu.Root.Q<Label>("creator_ovr");
                if(modelRequired)
                {
                    Assert.That(model.image,Is.SameAs(menu.CreatorStudio.Texture));Assert.That(model.worldBound.width,Is.GreaterThan(bounds.width*.2f));
                    Assert.That(model.worldBound.height,Is.GreaterThan(bounds.height*.4f));
                    if(score!=null){Assert.That(score.worldBound.xMin,Is.GreaterThanOrEqualTo(model.worldBound.xMax));Assert.That(score.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));}
                }
                foreach(string id in new[]{"create","confirm_create","back"})
                {
                    var action=menu.Root.Q<Button>(id);if(action==null)continue;
                    Assert.That(action.worldBound.height,Is.GreaterThan(25),id+" must remain a complete actionable button.");
                    for(VisualElement ancestor=action.parent;ancestor!=null;ancestor=ancestor.parent)
                    {
                        if(ancestor!=menu.Root&&ancestor.style.overflow.value!=Overflow.Hidden)continue;
                        var visible=ancestor.worldBound;
                        Assert.That(action.worldBound.yMin,Is.GreaterThanOrEqualTo(visible.yMin-.5f),name+" "+id+" top inside "+ancestor.name);
                        Assert.That(action.worldBound.yMax,Is.LessThanOrEqualTo(visible.yMax+.5f),name+" "+id+" bottom inside "+ancestor.name);
                        Assert.That(action.worldBound.xMin,Is.GreaterThanOrEqualTo(visible.xMin-.5f),name+" "+id+" left inside "+ancestor.name);
                        Assert.That(action.worldBound.xMax,Is.LessThanOrEqualTo(visible.xMax+.5f),name+" "+id+" right inside "+ancestor.name);
                    }
                }
                Save(target,name);
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
