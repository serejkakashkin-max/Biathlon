using Biathlon.Presentation;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Resolve only when the selected devices or preferences change. Sampling creates no actions, controls or collections.</summary>
    public sealed class BoundDeviceControls
    {
        private readonly InputBindingCatalog catalog;
        private readonly InputControl<float>[] keyboardControls,padControls;
        private PlayerSettings preferences;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad pad;
        public BoundDeviceControls(PlayerSettings preferences,InputBindingCatalog catalog)
        {this.catalog=catalog;this.preferences=preferences;keyboardControls=new InputControl<float>[catalog.Count];padControls=new InputControl<float>[catalog.Count];}
        public void Resolve(Keyboard keys,Mouse pointer,Gamepad gamepad)
        {
            if(keyboard==keys&&mouse==pointer&&pad==gamepad)return;
            keyboard=keys;mouse=pointer;pad=gamepad;
            for(int i=0;i<catalog.Count;i++)
            {keyboardControls[i]=ResolvePath(preferences.Binding(catalog,catalog.Get(i).Id,"keyboard"),keyboard,mouse,pad);padControls[i]=ResolvePath(preferences.Binding(catalog,catalog.Get(i).Id,"gamepad"),keyboard,mouse,pad);}
        }
        private static InputControl<float> ResolvePath(string path,Keyboard keys,Mouse pointer,Gamepad gamepad)
        {
            var device=path.StartsWith("<Keyboard>/",System.StringComparison.Ordinal)?(InputDevice)keys:path.StartsWith("<Mouse>/",System.StringComparison.Ordinal)?pointer:gamepad;
            return device?.TryGetChildControl<InputControl<float>>(path.Substring(path.IndexOf('/')+1));
        }
        public float Value(string id,bool gamepad)
        {int i=Index(id);var control=(gamepad?padControls:keyboardControls)[i];return control?.ReadUnprocessedValue()??0;}
        public bool Pressed(string id,bool gamepad)=>Value(id,gamepad)>.5f;
        public bool Down(string id,bool gamepad)
        {int i=Index(id);return ((gamepad?padControls:keyboardControls)[i] as ButtonControl)?.wasPressedThisFrame==true;}
        private int Index(string id)
        {for(int i=0;i<catalog.Count;i++)if(catalog.Get(i).Id==id)return i;throw new System.ArgumentException("Unknown bound action.");}
    }
}
