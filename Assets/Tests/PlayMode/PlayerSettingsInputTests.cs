using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MouseButton=UnityEngine.InputSystem.LowLevel.MouseButton;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Biathlon.PlayModeTests
{
    public sealed class PlayerSettingsInputTests
    {
        private GameObject root;
        private InputTestScope scope;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;
        private InputBindingCatalog catalog;
        private string settingsDirectory;
        private sealed class Profiles:ICareerProfileStore
        {public bool HasProfile=>false;public string ReadLatest()=>throw new FileNotFoundException();public void WriteNew(string id,string json){}public void Replace(string id,string expectedJson,string json){}}
        [SetUp] public void Setup()
        {scope=new InputTestScope();keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();gamepad=InputSystem.AddDevice<Gamepad>();root=new GameObject("M16 settings test");catalog=PlayerSettingsPreferences.Catalog;}
        [TearDown] public void Cleanup()
        {UnityEngine.Object.DestroyImmediate(root);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(gamepad);scope.Dispose();if(settingsDirectory!=null&&Directory.Exists(settingsDirectory))Directory.Delete(settingsDirectory,true);}
        [Test] public void ReboundKeyboardAndGamepadActuallyDriveTheApprovedCommands()
        {
            var p=new PlayerSettings();p.Rebind(catalog,"attack","keyboard","<Keyboard>/r");p.Rebind(catalog,"steer_right","keyboard","<Keyboard>/t");p.Rebind(catalog,"attack","gamepad","<Gamepad>/rightStickPress");
            var adapter=root.AddComponent<PlayerDeviceInput>();adapter.enabled=false;adapter.Initialize(new AthleteId("player"),20,p);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift,Key.D));InputTestScope.Update();adapter.Capture(keyboard,null);
            var old=adapter.BuildManual(new SimulationTick(1));Assert.That(old.Preset,Is.EqualTo(EffortPreset.Analog));Assert.That(old.SteeringInput,Is.Zero);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R,Key.T));InputTestScope.Update();adapter.Capture(keyboard,null);
            var remapped=adapter.BuildManual(new SimulationTick(2));Assert.That(remapped.Preset,Is.EqualTo(EffortPreset.Attack));Assert.That(remapped.SteeringInput,Is.GreaterThan(0));
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.RightStick));InputTestScope.Update();adapter.Capture(null,gamepad);
            Assert.That(adapter.BuildManual(new SimulationTick(3)).Preset,Is.EqualTo(EffortPreset.Attack));
            adapter.SetInputFocus(false);Assert.That(adapter.BuildManual(new SimulationTick(4)).Preset,Is.EqualTo(EffortPreset.Analog));
        }
        [Test] public void EveryAuthoredBindingPathResolvesToTheExpectedActualInputControl()
        {
            for(int i=0;i<catalog.PathCount;i++)
            {
                var p=catalog.PathAt(i);InputDevice device=p.Path.StartsWith("<Keyboard>/",StringComparison.Ordinal)?(InputDevice)keyboard:p.Path.StartsWith("<Mouse>/",StringComparison.Ordinal)?mouse:gamepad;
                var control=device.TryGetChildControl<InputControl<float>>(p.Path.Substring(p.Path.IndexOf('/')+1));Assert.That(control,Is.Not.Null,p.Path);
                if(p.Kind=="button")Assert.That(control,Is.InstanceOf<UnityEngine.InputSystem.Controls.ButtonControl>(),p.Path);
            }
        }
        [Test] public void ReboundShotBoltBreathAndSensitivityUseFixedTickInputWithoutAutoShooting()
        {
            var p=new PlayerSettings{AimSensitivity=1500,InvertAim=true};
            p.Rebind(catalog,"shoot","keyboard","<Keyboard>/g");p.Rebind(catalog,"bolt","keyboard","<Keyboard>/r");p.Rebind(catalog,"breath","keyboard","<Keyboard>/b");
            var adapter=root.AddComponent<ShootingDeviceInput>();adapter.enabled=false;adapter.Initialize(new AthleteId("player"),20,preferences:p);
            InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(10,4)}.WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputTestScope.Update();adapter.Capture(mouse,keyboard,null);
            var first=adapter.Build(new SimulationTick(1));Assert.That(first.Trigger||first.CycleBolt||first.HoldBreath,Is.False);Assert.That(first.Aim.Horizontal,Is.EqualTo(75));Assert.That(first.Aim.Vertical,Is.EqualTo(-30));
            InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G,Key.R,Key.B));InputTestScope.Update();adapter.Capture(mouse,keyboard,null);
            var second=adapter.Build(new SimulationTick(2));Assert.That(second.Trigger&&second.CycleBolt&&second.HoldBreath,Is.True);
            adapter.Capture(mouse,keyboard,null);var held=adapter.Build(new SimulationTick(3));Assert.That(held.Trigger||held.CycleBolt,Is.False);Assert.That(held.HoldBreath,Is.True);
            adapter.SetInputFocus(false);Assert.That(adapter.Build(new SimulationTick(4)).HoldBreath,Is.False);
        }
        [UnityTest] public IEnumerator SettingsPagesRebindConflictCancelResetAndAccessibilityAreUsable()
        {
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Profiles());menu.Session.OpenSettings();menu.RefreshFrontEnd();yield return null;
            Submit(menu,"settings_page_2");yield return null;
            Assert.That(menu.Root.Q<VisualElement>("settings_panel_2").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            int expectedRows=menu.Root.worldBound.width<1250?3:5;
            Assert.That(menu.Root.Query<VisualElement>().Where(e=>e.name!=null&&e.name.StartsWith("binding_row_",StringComparison.Ordinal)).ToList().Count,Is.EqualTo(expectedRows));
            string before=menu.Settings.ToJson(catalog);menu.BeginRebind("attack","keyboard");Assert.That(menu.CompleteRebind("<Keyboard>/w"),Is.False);Assert.That(menu.Rebinding,Is.True);
            Assert.That(menu.Root.Q<Label>("binding_status").text,Does.Contain("Темп"));Assert.That(menu.Settings.ToJson(catalog),Is.EqualTo(before));menu.CancelRebind();Assert.That(menu.Rebinding,Is.False);
            menu.BeginRebind("attack","keyboard");Assert.That(menu.CompleteRebind("<Keyboard>/r"),Is.True);yield return null;
            Assert.That(menu.Settings.Binding(catalog,"attack","keyboard"),Is.EqualTo("<Keyboard>/r"));
            Submit(menu,"bindings_reset");yield return null;Assert.That(menu.Settings.BindingOverrideCount,Is.Zero);
            menu.Root.Q<DropdownField>("input_device").index=1;Assert.That(menu.Settings.Device,Is.EqualTo("gamepad"));
            menu.Root.Q<Slider>("aim_sensitivity").value=125;Assert.That(menu.Settings.AimSensitivity,Is.EqualTo(1250));
            Submit(menu,"settings_page_3");yield return null;menu.Root.Q<Toggle>("high_contrast").value=true;yield return null;Assert.That(menu.Settings.HighContrast,Is.True);
            menu.ApplyPreferences("en",2);yield return null;Assert.That(menu.Settings.Locale,Is.EqualTo("en"));Assert.That(menu.Settings.TextSize,Is.EqualTo(2));
            Assert.That(menu.Root.Q<VisualElement>("settings_panel_3").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
        }
        private static void Submit(FrontEndMenu menu,string id)
        {var button=menu.Root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);}
        [UnityTest] public IEnumerator ActualDeviceCaptureRebindsKeyboardGamepadAndHonorsReservedCancel()
        {
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Profiles());menu.Session.OpenSettings();menu.RefreshFrontEnd();yield return null;
            Submit(menu,"settings_page_2");yield return null;menu.BeginRebind("attack","keyboard");yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));InputTestScope.Update();Poll(menu);Assert.That(menu.Rebinding,Is.False);Assert.That(menu.Settings.Binding(catalog,"attack","keyboard"),Is.EqualTo("<Keyboard>/r"));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTestScope.Update();menu.BeginRebind("attack","gamepad");yield return null;yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.RightStick));InputTestScope.Update();Poll(menu);Assert.That(menu.Rebinding,Is.False);Assert.That(menu.Settings.Binding(catalog,"attack","gamepad"),Is.EqualTo("<Gamepad>/rightStickPress"));
            InputSystem.QueueStateEvent(gamepad,new GamepadState());InputTestScope.Update();menu.BeginRebind("shoot","gamepad");yield return null;yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.East));InputTestScope.Update();
            using(var cancel=NavigationCancelEvent.GetPooled())menu.Root.SendEvent(cancel);
            Assert.That(menu.Rebinding,Is.True,"B remains assignable while standard UI Back is suppressed during capture.");Poll(menu);
            Assert.That(menu.Settings.Binding(catalog,"shoot","gamepad"),Is.EqualTo("<Gamepad>/buttonEast"));
            InputSystem.QueueStateEvent(gamepad,new GamepadState());InputTestScope.Update();string before=menu.Settings.ToJson(catalog);menu.BeginRebind("tuck","keyboard");yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputTestScope.Update();Poll(menu);Assert.That(menu.Rebinding,Is.False);Assert.That(menu.Settings.ToJson(catalog),Is.EqualTo(before));Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Settings));
        }
        private static void Poll(FrontEndMenu menu)=>typeof(FrontEndMenu).GetMethod("PollRebind",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
        [Test] public void FailedSettingsPublicationRestoresEveryControlAndRuntimeUntilAnUnlockedRetrySucceeds()
        {
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Profiles());menu.Session.OpenSettings();menu.RefreshFrontEnd();
            settingsDirectory=Path.Combine(Path.GetTempPath(),"biathlon-settings-native-"+Guid.NewGuid().ToString("N"));menu.SetSettingsDirectoryForReview(settingsDirectory);
            menu.BeginRebind("attack","keyboard");Assert.That(menu.CompleteRebind("<Keyboard>/r"),Is.True);
            string before=menu.Settings.ToJson(catalog),path=Path.Combine(settingsDirectory,"settings_v1.json"),disk=File.ReadAllText(path);
            string audio=JsonUtility.ToJson(BiathlonAudio.Instance.Preferences);
            using(var held=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                menu.Root.Q<DropdownField>("input_device").index=1;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<DropdownField>("graphics_quality").index=0;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<Slider>("aim_sensitivity").value=150;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<Toggle>("invert_aim").value=true;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<DropdownField>("language").index=1;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<DropdownField>("text_size").index=2;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<Toggle>("high_contrast").value=true;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<Toggle>("reduced_motion").value=true;AssertPreserved(menu,before,path,disk,audio);
                foreach(string bus in new[]{"master","effects","body","ambience","ui","music"})
                {menu.Root.Q<Slider>("audio_"+bus).value=13;AssertPreserved(menu,before,path,disk,audio);}
                menu.Root.Q<Toggle>("audio_quiet").value=true;AssertPreserved(menu,before,path,disk,audio);
                menu.Root.Q<Toggle>("audio_background").value=false;AssertPreserved(menu,before,path,disk,audio);
                menu.BeginRebind("shoot","keyboard");Assert.That(menu.CompleteRebind("<Keyboard>/g"),Is.False);AssertPreserved(menu,before,path,disk,audio);
                Submit(menu,"bindings_reset");AssertPreserved(menu,before,path,disk,audio);
                Assert.That(new FilePlayerSettingsStore(settingsDirectory,catalog).Load(new PlayerSettings()).Settings.ToJson(catalog),Is.EqualTo(before));
            }
            menu.Root.Q<DropdownField>("input_device").index=1;Assert.That(menu.Settings.Device,Is.EqualTo("gamepad"));Assert.That(menu.LastMessage,Is.Null);
            Assert.That(new FilePlayerSettingsStore(settingsDirectory,catalog).Load(new PlayerSettings()).Settings.Device,Is.EqualTo("gamepad"));
            menu.Root.Q<Slider>("audio_master").value=37;Assert.That(menu.Settings.Master,Is.EqualTo(370));Assert.That(BiathlonAudio.Instance.Preferences.master,Is.EqualTo(.37f).Within(.0001));
        }
        private void AssertPreserved(FrontEndMenu menu,string before,string path,string disk,string audio)
        {
            Assert.That(menu.Settings.ToJson(catalog),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));Assert.That(JsonUtility.ToJson(BiathlonAudio.Instance.Preferences),Is.EqualTo(audio));
            Assert.That(menu.Locale,Is.EqualTo("ru"));Assert.That(menu.TextSizeIndex,Is.Zero);Assert.That(menu.HighContrast||menu.ReducedMotion,Is.False);
            Assert.That(menu.Root.Q<DropdownField>("input_device").index,Is.Zero);Assert.That(menu.Root.Q<DropdownField>("language").index,Is.Zero);Assert.That(menu.Root.Q<DropdownField>("text_size").index,Is.Zero);
            Assert.That(menu.Root.Q<DropdownField>("graphics_quality").index,Is.EqualTo(1));Assert.That(menu.Root.Q<Slider>("aim_sensitivity").value,Is.EqualTo(100));Assert.That(menu.Root.Q<Toggle>("invert_aim").value,Is.False);
            Assert.That(menu.Root.Q<Toggle>("high_contrast").value||menu.Root.Q<Toggle>("reduced_motion").value,Is.False);
            string[] buses={"master","effects","body","ambience","ui","music"};float[] levels={80,85,65,70,65,45};
            for(int i=0;i<buses.Length;i++)Assert.That(menu.Root.Q<Slider>("audio_"+buses[i]).value,Is.EqualTo(levels[i]),buses[i]+" slider must match the last published setting");
            Assert.That(menu.Root.Q<Toggle>("audio_quiet").value,Is.False);Assert.That(menu.Root.Q<Toggle>("audio_background").value,Is.True);
            Assert.That(menu.Settings.Binding(catalog,"attack","keyboard"),Is.EqualTo("<Keyboard>/r"));Assert.That(menu.LastMessage,Does.Contain("Не удалось сохранить настройки"));
        }
        [UnityTest] public IEnumerator SettingsPagesRenderSevenReadableNativeViewsAtNormalAndLargeText()
        {
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Profiles());menu.Session.OpenSettings();menu.RefreshFrontEnd();yield return null;
            yield return Capture(menu,"settings-display-ru",0,1600,900,"ru",0,"graphics_quality");
            yield return Capture(menu,"settings-audio-ru",1,1600,900,"ru",0,"audio_master");
            yield return Capture(menu,"settings-controls-ski-ru",2,1600,900,"ru",0,"input_device");
            Submit(menu,"binding_context_shooting");yield return null;
            yield return Capture(menu,"settings-controls-shooting-ru",2,1600,900,"ru",0,"bind_shoot_keyboard");
            yield return Capture(menu,"settings-accessibility-en-large",3,1024,768,"en",2,"high_contrast");
            yield return Capture(menu,"settings-saves-en-large",4,1024,768,"en",2,"export_diagnostic");
            yield return Capture(menu,"settings-controls-en-large",2,1024,768,"en",2,"input_device");
        }
        private IEnumerator Capture(FrontEndMenu menu,string name,int page,int width,int height,string locale,int size,string primaryControl)
        {
            var panel=root.GetComponent<UIDocument>().panelSettings;var previous=panel.targetTexture;var target=new RenderTexture(width,height,24);target.Create();
            var before=RenderTexture.active;Texture2D pixels=null;
            try
            {
                panel.targetTexture=target;menu.ApplyPreferences(locale,size);Submit(menu,"settings_page_"+page);
                // This is an elapsed-time transition. An arbitrary six fast Editor frames can still have opacity=0.
                yield return new WaitForSecondsRealtime(.35f);yield return null;yield return null;yield return new WaitForEndOfFrame();
                var body=menu.Root.Q<VisualElement>("menu_body");Assert.That(body.resolvedStyle.opacity,Is.GreaterThan(.95f),name+" body must be painted");
                var selected=menu.Root.Q<VisualElement>("settings_panel_"+page);Assert.That(selected.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
                Assert.That(selected.worldBound.width,Is.GreaterThan(300));Assert.That(selected.worldBound.height,Is.GreaterThan(100));
                var control=menu.Root.Q<VisualElement>(primaryControl);Assert.That(control,Is.Not.Null,name);Assert.That(control.worldBound.width,Is.GreaterThan(40));Assert.That(control.worldBound.height,Is.GreaterThan(20));
                Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(selected.worldBound.xMin-1));Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(selected.worldBound.xMax+1));
                if(page==2)
                {
                    var reset=menu.Root.Q<Button>("bindings_reset");var cancel=menu.Root.Q<Button>("rebind_cancel");var viewport=menu.Root.Q<ScrollView>("page_content").worldBound;
                    Assert.That(reset.worldBound.yMax,Is.LessThanOrEqualTo(viewport.yMax+1),name+" page controls must fit without scrolling");
                    Assert.That(cancel.worldBound.yMax,Is.LessThanOrEqualTo(viewport.yMax+1),name+" cancellation must stay visible");
                }
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m16/settings"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally{if(pixels!=null)UnityEngine.Object.Destroy(pixels);RenderTexture.active=before;panel.targetTexture=previous;target.Release();UnityEngine.Object.Destroy(target);}
        }
    }
}
