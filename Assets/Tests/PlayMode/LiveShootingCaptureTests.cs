using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class LiveShootingCaptureTests
    {
        [UnityTest] public IEnumerator RemovedOverlayContentClearsToTransparentOnTheNextRenderedFrame()
        {
            var settings=Object.Instantiate(Resources.Load<PanelSettings>("Biathlon/UI/front_end_panel_v1"));
            var target=new RenderTexture(200,100,24,RenderTextureFormat.ARGB32);target.Create();
            var root=new GameObject("Isolated native capture clearing regression");
            var pixels=new Texture2D(200,100,TextureFormat.RGBA32,false);
            var prior=RenderTexture.active;
            try
            {
                LiveShootingPlayerReview.ConfigureCapturePanel(settings,target);
                var document=root.AddComponent<UIDocument>();document.panelSettings=settings;
                var marker=new VisualElement();marker.style.position=Position.Absolute;
                marker.style.left=0;marker.style.top=0;marker.style.width=Length.Percent(100);marker.style.height=Length.Percent(100);
                marker.style.backgroundColor=Color.magenta;document.rootVisualElement.Add(marker);
                yield return null;yield return null;yield return null;
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,200,100),0,0);pixels.Apply();RenderTexture.active=prior;
                Assert.That(pixels.GetPixel(100,50).a,Is.GreaterThan(.99f),"Control: live marker reached the real native target.");
                marker.RemoveFromHierarchy();
                yield return null;yield return null;yield return null;
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,200,100),0,0);pixels.Apply();RenderTexture.active=prior;
                Assert.That(pixels.GetPixel(100,50).a,Is.LessThan(.01f),"Removed rows must leave transparent pixels, not stale text in subsequent video frames.");
            }
            finally
            {
                settings.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(root);
                Object.DestroyImmediate(settings);Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
