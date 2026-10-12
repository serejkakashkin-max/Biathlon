using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Tests
{
    public sealed class FrontEndPanelTemplateTests
    {
        [Test]public void PlayerTemplateContainsUiShadersAndInternationalTextAndEachViewGetsItsOwnInstance()
        {
            var template=Resources.Load<PanelSettings>(FrontEndPanelSettings.Resource);Assert.That(template,Is.Not.Null);
            var data=new SerializedObject(template);
            foreach(string key in new[]{"m_ICUDataAsset","m_DefaultShader","m_AtlasBlitShader"})Assert.That(data.FindProperty(key).objectReferenceValue,Is.Not.Null,key);
            var a=FrontEndPanelSettings.Create();var b=FrontEndPanelSettings.Create();
            try{Assert.That(a,Is.Not.SameAs(b));a.scale=2;Assert.That(b.scale,Is.EqualTo(template.scale));Assert.That(template.targetTexture,Is.Null);}
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
    }
}
