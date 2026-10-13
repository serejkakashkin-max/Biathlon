using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class BalanceReviewUiTests
    {
        private sealed class Store:ICareerProfileStore
        {
            public string Json;
            public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json??throw new FileNotFoundException();
            public void WriteNew(string id,string value)=>Json=value;
            public void Replace(string id,string expected,string value){if(Json!=expected)throw new IOException("Stale test profile.");Json=value;}
        }
        private GameObject root;
        private FrontEndMenu menu;
        private Store store;
        [SetUp] public void Setup()
        {
            store=new Store();root=new GameObject("M14 balance review interface");menu=root.AddComponent<FrontEndMenu>();
            menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store);
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);}
        private void Submit(string id)
        {
            var button=menu.Root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);Assert.That(button.enabledInHierarchy,Is.True,id);
            button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);
        }
        private static void FitsViewport(VisualElement element)
        {
            var scroll=element.GetFirstAncestorOfType<ScrollView>();Assert.That(scroll,Is.Not.Null);
            var viewport=scroll.contentViewport.worldBound;var bounds=element.worldBound;
            Assert.That(bounds.width,Is.GreaterThan(40));Assert.That(bounds.height,Is.GreaterThan(20));
            Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(viewport.yMin-1),element.name+" top must not be clipped.");
            Assert.That(bounds.yMax,Is.LessThanOrEqualTo(viewport.yMax+1),element.name+" must fit without scrolling.");
            Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(viewport.xMin-1));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(viewport.xMax+1));
        }
        private IEnumerator Capture(string filename,string locale,int textSize,int width,int height,bool creation,Action<Texture2D> inspect=null)
        {
            var document=root.GetComponent<UIDocument>();var target=new RenderTexture(width,height,0);target.Create();
            var priorTarget=document.panelSettings.targetTexture;var priorActive=RenderTexture.active;Texture2D pixels=null;
            try
            {
                document.panelSettings.targetTexture=target;menu.ApplyPreferences(locale,textSize);
                yield return null;yield return null;yield return null;yield return null;
                // Retained UI entrance transitions use elapsed time, not frame count.
                // Uncapped editor frames can finish before the scheduled first paint.
                float deadline=Time.realtimeSinceStartup+2;
                while(menu.Root.Q(className:"fl-screen").resolvedStyle.opacity<.999f&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(menu.Root.Q(className:"fl-screen").resolvedStyle.opacity,Is.GreaterThanOrEqualTo(.999f));
                yield return null;
                if(creation){FitsViewport(menu.Root.Q<Button>("create"));FitsViewport(menu.Root.Q<SliderInt>("career_start_age"));}
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                inspect?.Invoke(pixels);
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/balance-review-20261012"));
                Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,filename+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=priorActive;document.panelSettings.targetTexture=priorTarget;
                if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);
            }
        }
        [UnityTest] public IEnumerator NewCareerProfileAndAgeControlsCreateAndRestoreTheChosenLeague()
        {
            yield return null;Submit("new_career");yield return null;
            var profile=menu.Root.Q<DropdownField>("career_start_profile");var age=menu.Root.Q<SliderInt>("career_start_age");
            Assert.That(profile.index,Is.Zero);Assert.That(age.value,Is.EqualTo(18));Assert.That(age.lowValue,Is.EqualTo(18));Assert.That(age.highValue,Is.EqualTo(20));
            yield return Capture("new-career-ru","ru",0,1440,900,true);

            profile=menu.Root.Q<DropdownField>("career_start_profile");profile.value=profile.choices[1];yield return null;
            age=menu.Root.Q<SliderInt>("career_start_age");Assert.That(age.value,Is.EqualTo(22),"Senior debut uses its own age default, not a clamped junior default.");
            Assert.That(age.lowValue,Is.EqualTo(21));Assert.That(age.highValue,Is.EqualTo(30));
            age.value=26;menu.Root.Q<TextField>("athlete_name").value="Алекс";
            yield return Capture("new-career-en","en",2,1024,768,true);
            Assert.That(menu.Root.Q<SliderInt>("career_start_age").value,Is.EqualTo(26),"Locale and accessible text size preserve form state.");
            Submit("create");yield return null;
            Assert.That(menu.LastMessage,Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));
            Assert.That(menu.Session.Player.Age,Is.EqualTo(26));Assert.That(menu.Session.Definitions.Calendar.Circuit,Is.EqualTo(CareerCircuit.Development));
            Assert.That(store.HasProfile,Is.True);var saved=menu.Session.Career.ToJson();
            menu.Back();Submit("continue");yield return null;
            Assert.That(menu.Session.Player.Age,Is.EqualTo(26));Assert.That(menu.Session.Definitions.Calendar.Circuit,Is.EqualTo(CareerCircuit.Development));
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(saved),"The selected age/league is durable, not a cosmetic creation preview.");
        }

        [UnityTest] public IEnumerator EquipmentCardsActuallyPaintEachCatalogSkiColor()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("ski_colors_ui","Алекс","NOR",11);menu.Session.OpenCareerPage(FrontEndScreen.Equipment);menu.RefreshFrontEnd();
            yield return Capture("equipment-colors-ru","ru",0,1440,900,false,pixels=>
            {
                var graphics=new List<VisualElement>();menu.Root.Q("equipment_tiles").Query<VisualElement>().ForEach(element=>{if(element.GetType().Name=="SkiPairGraphic")graphics.Add(element);});
                var catalog=menu.Session.EquipmentCatalog;Assert.That(graphics.Count,Is.EqualTo(catalog.Count));
                var rootBounds=menu.Root.worldBound;var image=pixels.GetPixels32();
                for(int index=0;index<graphics.Count;index++)
                {
                    var rect=graphics[index].worldBound;Assert.That(rect.width,Is.GreaterThan(80));Assert.That(rect.height,Is.GreaterThan(40));
                    var definition=CareerSkiAppearance.Catalog.Get(catalog.Get(index).Id);
                    Assert.That(ColorUtility.TryParseHtmlString(definition.BaseColorHex,out var expected),Is.True);Color.RGBToHSV(expected,out float expectedHue,out _,out _);
                    int minX=Mathf.Clamp(Mathf.FloorToInt((rect.xMin-rootBounds.xMin)/rootBounds.width*pixels.width),0,pixels.width-1);
                    int maxX=Mathf.Clamp(Mathf.CeilToInt((rect.xMax-rootBounds.xMin)/rootBounds.width*pixels.width),0,pixels.width);
                    int minY=Mathf.Clamp(Mathf.FloorToInt(pixels.height-(rect.yMax-rootBounds.yMin)/rootBounds.height*pixels.height),0,pixels.height-1);
                    int maxY=Mathf.Clamp(Mathf.CeilToInt(pixels.height-(rect.yMin-rootBounds.yMin)/rootBounds.height*pixels.height),0,pixels.height);
                    int matching=0;
                    for(int y=minY;y<maxY;y++)for(int x=minX;x<maxX;x++)
                    {
                        Color.RGBToHSV(image[y*pixels.width+x],out float hue,out float saturation,out float value);
                        float distance=Mathf.Abs(hue-expectedHue);distance=Mathf.Min(distance,1-distance);
                        if(distance<.04f&&saturation>.25f&&value>.25f)matching++;
                    }
                    Assert.That(matching,Is.GreaterThan(20),definition.Id+": the actual retained graphic must visibly render its catalog deck color.");
                }
            });
        }
    }
}
