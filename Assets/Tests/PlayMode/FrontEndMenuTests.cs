using System.Collections;
using System;
using System.IO;
using System.Reflection;
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
        [UnityTest] public IEnumerator NewProfileContinueAndSettingsUseTheRealMenuWithoutAdvancingAWeek()
        {
            yield return null;yield return null;
            Assert.That(menu.Root.Q<Button>("continue").enabledSelf,Is.False);
            Submit("new_career");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Алекс";Submit("create");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(menu.Session.Player.Name,Is.EqualTo("Алекс"));
            string saved=menu.Session.Career.ToJson();Assert.That(menu.Session.Career.Week,Is.EqualTo(1));
            Submit("settings");yield return null;menu.ApplyPreferences("en",2);yield return null;
            Assert.That(menu.Root.Q<Button>("back").text,Is.EqualTo("Back"));Submit("back");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Career));Submit("back");yield return null;
            Submit("continue");yield return null;Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(saved));
            Assert.That(menu.LastMessage,Is.Null);
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
            yield return null;Submit("new_career");yield return null;Submit("create");yield return null;
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
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/entry-20261011"));Directory.CreateDirectory(folder);
            foreach(var dimensions in new[]{new Vector2Int(1600,900),new Vector2Int(1024,768)})
            {
                var target=new RenderTexture(dimensions.x,dimensions.y,0);target.Create();panel.targetTexture=target;
                Texture2D pixels=null;
                try
                {
                    menu.ApplyPreferences(dimensions.x==1600?"ru":"en",dimensions.x==1600?0:2);
                    yield return null;yield return null;yield return null;
                    var bounds=menu.Root.worldBound;var button=menu.Root.Q<Button>("new_career");
                    Assert.That(button.worldBound.width,Is.GreaterThan(250));Assert.That(button.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));
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
