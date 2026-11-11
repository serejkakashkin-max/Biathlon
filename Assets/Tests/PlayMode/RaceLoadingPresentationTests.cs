using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    public sealed class RaceLoadingPresentationTests
    {
        private sealed class Store:ICareerProfileStore
        {public string Json;public bool HasProfile=>Json!=null;public string ReadLatest()=>Json;public void WriteNew(string id,string value)=>Json=value;public void Replace(string id,string expected,string value){Assert.That(Json,Is.EqualTo(expected));Json=value;}}
        private static FrontEndMenu Create(Store store)
        {
            var menu=new GameObject("Race loading acceptance").AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store);
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("loading_review","Алекс","NOR",11);for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();menu.RefreshFrontEnd();return menu;
        }
        [UnityTest]public IEnumerator RealLaunchPaintsAllStagesBeforeWorkAndThenArmsTheOriginalCountdown()
        {
            var store=new Store();var menu=Create(store);menu.Race.LoadEnvironment=false;
            try
            {
                yield return null;string before=store.Json;var start=menu.Root.Q<Button>("start_race");Assert.That(start,Is.Not.Null);
                using(var submit=NavigationSubmitEvent.GetPooled())start.SendEvent(submit);
                Assert.That(menu.Race.Loading,Is.True);Assert.That(menu.Race.Host,Is.Null);Assert.That(menu.Root.Q("race_loading_screen"),Is.Not.Null);Assert.That(menu.Root.Q<Button>("start_race"),Is.Null);
                Assert.Throws<InvalidOperationException>(()=>menu.Race.Launch(menu));menu.Back();Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.PreRace));
                var stages=new HashSet<CareerRaceLoadingStage>();var frames=new List<int>();var seenHost=false;Label preparedClock=null;
                for(int i=0;i<40&&menu.Race.Loading;i++)
                {
                    stages.Add(menu.Race.LoadingStage);frames.Add(Time.frameCount);
                    Assert.That(menu.Root.Q<Label>("race_loading_status").text,Is.Not.Empty);Assert.That(store.Json,Is.EqualTo(before));
                    if(menu.Race.Host!=null){seenHost=true;Assert.That(menu.Race.Host.enabled,Is.False);Assert.That(menu.Race.Host.Runner.Field.Tick.Value,Is.Zero);Assert.That(menu.Race.Host.WaitingForPlayerStart,Is.False);
                        if(menu.Race.LoadingStage==CareerRaceLoadingStage.Ready)
                        {
                            var hud=menu.Race.Host.GameplayHud;
                            Assert.That(hud.enabled,Is.True);
                            var hudDocument=Array.Find(Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None),doc=>doc.rootVisualElement==hud.Root);
                            Assert.That(hudDocument.panelSettings.sortingOrder,Is.LessThan(menu.GetComponent<UIDocument>().panelSettings.sortingOrder),"Warm the retained HUD behind the opaque loader before starting the countdown.");
                            Assert.That(hud.Root.panel,Is.Not.Null);
                            preparedClock=hud.Root.Q<Label>("race_clock");
                        }
                        else
                        {Assert.That(menu.Race.Host.GameplayHud.enabled,Is.False);Assert.That(menu.Race.Host.GameplayHud.Root?.panel,Is.Null);}}
                    yield return null;
                }
                Assert.That(stages.Count,Is.EqualTo(4));Assert.That(seenHost,Is.True);Assert.That(menu.Race.Loading,Is.False);
                Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Race));Assert.That(menu.Race.Host.enabled,Is.True);
                Assert.That(menu.Race.Host.GameplayHud.enabled,Is.True,"The HUD appears at the handoff to the race.");
                Assert.That(preparedClock,Is.Not.Null);
                Assert.That(menu.Race.Host.GameplayHud.Root.Q<Label>("race_clock"),Is.SameAs(preparedClock),"The countdown uses the already rendered HUD tree.");
                Assert.That(menu.Race.Host.WaitingForPlayerStart,Is.True);Assert.That(menu.Race.Host.Runner.Field.Tick.Value,Is.Zero);
                Assert.That(menu.Root.style.display.value,Is.EqualTo(DisplayStyle.None));Assert.That(store.Json,Is.EqualTo(before));
                menu.Race.Pause();yield return null;menu.Race.Abandon();Assert.That(menu.Root.Q("race_loading_screen"),Is.Null);Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.PreRace));Assert.That(store.Json,Is.EqualTo(before));
            }
            finally{Object.DestroyImmediate(menu.gameObject);}
        }
        [UnityTest]public IEnumerator APreparationFailureRemovesLoadingAndReturnsAnActionableMenuWithoutSaving()
        {
            var store=new Store();var menu=Create(store);
            try
            {
                string before=store.Json;menu.Race.Launch(menu);
                // An invalidated preparation is an explicit failure fixture. Ordinary
                // loading navigation is blocked by FrontEndMenu.Back, checked above.
                menu.Session.Back();
                for(int i=0;i<12&&menu.Race.Loading;i++)yield return null;
                Assert.That(menu.Race.Loading,Is.False);Assert.That(menu.Race.Host,Is.Null);Assert.That(menu.Root.Q("race_loading_screen"),Is.Null);
                Assert.That(menu.LastMessage,Is.Not.Empty);Assert.That(menu.Root.style.display.value,Is.EqualTo(DisplayStyle.Flex));Assert.That(store.Json,Is.EqualTo(before));
            }
            finally{Object.DestroyImmediate(menu.gameObject);}
        }
        [UnityTest]public IEnumerator FrozenRealLoadingScreenFitsLocalesSizesAndReducedMotion()
        {
            var store=new Store();var menu=Create(store);var panel=menu.GetComponent<UIDocument>().panelSettings;var original=RenderTexture.active;
            try
            {
                string before=store.Json;menu.Race.Launch(menu);menu.Race.StopAllCoroutines();
                // Freeze only the view before composition: this is a loading layout
                // fixture, not a claim that it ran a complete loading pipeline.
                foreach(var item in new[]{(1600,900,"ru",0,false),(1024,768,"en",2,true),(1280,720,"ru",0,false)})
                {
                    var target=new RenderTexture(item.Item1,item.Item2,0);target.Create();panel.targetTexture=target;Texture2D texture=null;
                    try
                    {
                        menu.Settings.ReducedMotion=item.Item5;
                        menu.ApplyPreferences(item.Item3,item.Item4);Assert.That(menu.ReducedMotion,Is.EqualTo(item.Item5));yield return null;yield return null;yield return null;
                        var screen=menu.Root.Q("race_loading_screen");var card=menu.Root.Q("race_loading_card");Assert.That(screen,Is.Not.Null);
                        var bounds=menu.Root.worldBound;Assert.That(card.worldBound.yMin,Is.GreaterThan(50));Assert.That(card.worldBound.yMax,Is.LessThanOrEqualTo(bounds.yMax+1));
                        foreach(string name in new[]{"race_loading_event","race_loading_title","race_loading_venue","race_loading_status","race_loading_note"})
                        {var label=menu.Root.Q<Label>(name);Assert.That(label.worldBound.xMin,Is.GreaterThanOrEqualTo(card.worldBound.xMin));Assert.That(label.worldBound.xMax,Is.LessThanOrEqualTo(card.worldBound.xMax+1));Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(card.worldBound.yMax));}
                        Assert.That(menu.Root.Query<Button>().ToList().Count,Is.Zero);Assert.That(menu.Root.Q<Image>("race_loading_backdrop").image,Is.Not.Null);
                        RenderTexture.active=target;texture=new Texture2D(item.Item1,item.Item2,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,item.Item1,item.Item2),0,0);texture.Apply();
                        string output=Path.GetFullPath("artifacts/m16-4/loading/views");Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"loading-"+item.Item1+"-"+item.Item3+".png"),texture.EncodeToPNG());
                        Assert.That(menu.Race.Host,Is.Null);Assert.That(store.Json,Is.EqualTo(before));
                    }
                    finally{panel.targetTexture=null;RenderTexture.active=original;if(texture!=null)Object.Destroy(texture);Object.Destroy(target);}
                }
            }
            finally{Object.DestroyImmediate(menu.gameObject);}
        }
    }
}
