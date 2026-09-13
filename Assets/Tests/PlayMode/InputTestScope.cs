using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.PlayModeTests
{
    /// <summary>Local synthetic-device tests must not depend on which desktop window is focused.
    /// Restore the exact settings object after each test; product focus policy is unchanged.</summary>
    internal sealed class InputTestScope : IDisposable
    {
        // The package's public Update() selects the Editor buffer when Game View lacks
        // focus. Select the player buffer explicitly for queued synthetic test events.
        private static readonly Action<UnityEngine.InputSystem.LowLevel.InputUpdateType> update =
            (Action<UnityEngine.InputSystem.LowLevel.InputUpdateType>)Delegate.CreateDelegate(
                typeof(Action<UnityEngine.InputSystem.LowLevel.InputUpdateType>),
                typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic, null,
                    new[] { typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType) }, null));
        public static void Update() => update(UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic);

        private readonly InputSettings original;
        private readonly InputSettings isolated;
        public InputTestScope()
        {
            original = InputSystem.settings;
            isolated = UnityEngine.Object.Instantiate(original);
            isolated.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            isolated.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
#if UNITY_EDITOR
            isolated.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = isolated;
        }
        public void Dispose()
        {
            InputSystem.settings = original;
            UnityEngine.Object.DestroyImmediate(isolated);
        }
    }
}
