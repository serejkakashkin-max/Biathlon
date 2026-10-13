using System.Collections;
using System.IO;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class FrostlineFieldTests
    {
        [UnityTest] public IEnumerator ReducedMotionPersistsAcrossNavigationWithoutWritingPreferences()
        {
            int preference=PlayerPrefs.GetInt("biathlon.ui.reduced_motion",-1);
            var root=new GameObject("Frostline reduced motion regression");var menu=root.AddComponent<FrontEndMenu>();
            menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            try
            {
                menu.Session.OpenSettings();menu.RefreshFrontEnd();
                menu.Root.Q<Toggle>("reduced_motion").value=true;
                Assert.That(menu.ReducedMotion,Is.True);Assert.That(menu.Root.ClassListContains("reduced-motion"),Is.True);
                menu.Session.Back();menu.RefreshFrontEnd();yield return null;
                Assert.That(menu.Root.Q("menu_body").resolvedStyle.opacity,Is.EqualTo(1));
                Assert.That(menu.Root.Q("menu_backdrop").style.left.value.value,Is.Zero);
                Assert.That(PlayerPrefs.GetInt("biathlon.ui.reduced_motion",-1),Is.EqualTo(preference));
            }
            finally{Object.DestroyImmediate(root);}
        }
        private sealed class Store:ICareerProfileStore
        {
            public bool HasProfile=>false;
            public string ReadLatest()=>throw new FileNotFoundException();
            public void WriteNew(string id,string json)=>Assert.Fail("Presentation probe must not create a career.");
            public void Replace(string id,string expected,string json)=>Assert.Fail("Presentation probe must not alter a career.");
        }

        [UnityTest] public IEnumerator EditableValuesRenderAsLightGlyphsOnDarkFieldsInBothLocales()
        {
            var root=new GameObject("Frostline field contrast regression");var menu=root.AddComponent<FrontEndMenu>();
            menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());menu.Session.OpenNewCareer();
            RenderTexture target=null;Texture2D pixels=null;var previous=RenderTexture.active;
            try
            {
                foreach(string locale in new[]{"ru","en"})
                {
                    target=new RenderTexture(1600,900,24);target.Create();root.GetComponent<UIDocument>().panelSettings.targetTexture=target;
                    menu.ApplyPreferences(locale,0);
                    var name=menu.Root.Q<TextField>("athlete_name");name.value=locale=="ru"?"Алекс Морозов":"Alex Morozov";
                    for(int frame=0;frame<5;frame++)yield return null;
                    yield return new WaitForSecondsRealtime(.3f);
                    pixels=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                    var diagnostic=new System.Text.StringBuilder();
                    menu.Root.Q("athlete_name").Query<VisualElement>().ForEach(v=>diagnostic.AppendLine(v.GetType().Name+" "+v.name+" "+string.Join(",",v.GetClasses())+" bg="+v.resolvedStyle.backgroundColor+" image="+v.resolvedStyle.backgroundImage+" tint="+v.resolvedStyle.unityBackgroundImageTintColor+" text="+(v is TextElement t?t.text:"")));
                    File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14-wow/field-hierarchy.txt")),diagnostic.ToString());
                    foreach(string id in new[]{"athlete_name","athlete_nation","career_start_profile"})
                    {
                        var field=menu.Root.Q(id);Assert.That(field,Is.Not.Null,id);
                        var rect=field.worldBound;var viewport=menu.Root.worldBound;
                        int bright=0,dark=0,total=0;
                        for(int y=4;y<(int)rect.height-4;y++)for(int x=12;x<(int)rect.width-12;x++)
                        {
                            int px=Mathf.Clamp(Mathf.RoundToInt((rect.x+x-viewport.x)/viewport.width*1600),0,1599);
                            int py=Mathf.Clamp(Mathf.RoundToInt(900-(rect.y+y-viewport.y)/viewport.height*900),0,899);
                            float value=pixels.GetPixel(px,py).grayscale;if(value>.65f)bright++;if(value<.3f)dark++;total++;
                        }
                        Assert.That(total,Is.GreaterThan(100),id+" must be visible.");
                        Assert.That(dark/(float)total,Is.GreaterThan(.6f),id+" default white inner input must not cover the dark field.");
                        Assert.That(bright/(float)total,Is.GreaterThan(.002f),id+" must actually paint its selected/editable value.");
                        Assert.That(bright/(float)total,Is.LessThan(.3f),id+" must show glyphs rather than an opaque white rectangle.");
                    }
                    Assert.That(menu.Root.Q<SliderInt>("career_start_age").value,Is.EqualTo(18));
                    string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14-wow/fields"));Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder,"new-career-fields-"+locale+".png"),pixels.EncodeToPNG());
                    Object.DestroyImmediate(pixels);pixels=null;root.GetComponent<UIDocument>().panelSettings.targetTexture=null;
                    Object.DestroyImmediate(target);target=null;
                }
            }
            finally
            {
                RenderTexture.active=previous;if(pixels!=null)Object.DestroyImmediate(pixels);if(target!=null)Object.DestroyImmediate(target);Object.DestroyImmediate(root);
            }
        }
    }
}
