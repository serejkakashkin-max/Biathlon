using System.Collections;
using System.IO;
using Biathlon.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed partial class FrontEndMenuTests
    {
        [UnityTest] public IEnumerator DirectCareerNavigationPreservesPlanSaveAndRestoresFocus()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("ux_navigation","Алекс","NOR",11);menu.RefreshFrontEnd();yield return null;
            string before=menu.Session.Career.ToJson();Submit("career_training");yield return null;Submit("add_training_0");yield return null;
            foreach(var page in new[]{FrontEndScreen.Equipment,FrontEndScreen.Standings,FrontEndScreen.History,FrontEndScreen.Rivalries,FrontEndScreen.Replays,FrontEndScreen.Training})
            {
                Submit("career_"+page.ToString().ToLowerInvariant());yield return null;yield return null;
                Assert.That(menu.Session.Screen,Is.EqualTo(page));Assert.That(menu.Root.Q("career_current_page"),Is.Not.Null);
                Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Assert.That(menu.Session.TrainingCount,Is.EqualTo(1));
            }
            Assert.That((menu.Root.panel.focusController.focusedElement as VisualElement)?.name,Is.EqualTo("career_equipment"));
            Submit("career_overview");yield return null;
            Assert.That(menu.Root.Q<Label>("career_plan_summary").text,Does.Contain("1"));
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            Submit("rest_week");yield return null;
            Assert.That(menu.Session.Career.Week,Is.EqualTo(2));Assert.That(menu.Session.TrainingCount,Is.Zero);
        }

        [UnityTest] public IEnumerator HomePrioritizesExistingCareerAndNextStepUsesCurrentWeek()
        {
            yield return null;yield return null;
            Assert.That(menu.Root.Q<ScrollView>("menu_actions").Q<Button>().name,Is.EqualTo("new_career"));
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("ux_home","Алекс","NOR",11);
            menu.Session.Back();menu.RefreshFrontEnd();yield return null;
            Assert.That(menu.Root.Q<ScrollView>("menu_actions").Q<Button>().name,Is.EqualTo("continue"));
            Submit("continue");yield return null;
            for(int i=0;i<5;i++){Submit("rest_week");yield return null;}
            string before=menu.Session.Career.ToJson();Submit("prepare");yield return null;
            Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.PreRace));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            Submit("back");yield return null;Assert.That(menu.Session.PreparedWeek,Is.Null);
        }

        [UnityTest] public IEnumerator CareerUxRendersAtWideAndCompactLargeTextSizes()
        {
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("ux_layout","Александр Северин","NOR",11);
            yield return CaptureMenuUx(1600,900,"ru",0,false);
            yield return CaptureMenuUx(1024,768,"en",2,true);
        }

        private IEnumerator CaptureMenuUx(int width,int height,string locale,int size,bool contrast)
        {
            var settings=root.GetComponent<UIDocument>().panelSettings;var previous=RenderTexture.active;
            var target=new RenderTexture(width,height,0);target.Create();settings.targetTexture=target;Texture2D pixels=null;
            try
            {
                menu.ApplyPreferences(locale,size);menu.ApplyAccessibility(contrast);
                foreach(string page in new[]{"overview","training","equipment"})
                {
                    if(page!="overview")Submit("career_"+page);
                    yield return new WaitForSecondsRealtime(.4f);for(int i=0;i<10;i++)yield return null;
                    var content=menu.Root.Q<ScrollView>("page_content");content.scrollOffset=Vector2.zero;
                    menu.Root.Q<ScrollView>("menu_actions").scrollOffset=Vector2.zero;yield return null;
                    Assert.That(content.worldBound.xMax,Is.LessThanOrEqualTo(menu.Root.worldBound.xMax));
                    Assert.That(content.contentContainer.worldBound.width,Is.LessThanOrEqualTo(content.worldBound.width+1));
                    Assert.That(menu.Root.Q("career_current_page").resolvedStyle.borderLeftWidth,Is.GreaterThan(2));
                    Assert.That(menu.Root.Q("menu_control_hint").worldBound.yMax,Is.LessThanOrEqualTo(menu.Root.worldBound.yMax));
                    if(page=="overview")Assert.That(menu.Root.Q("rest_week").worldBound.yMax,Is.LessThan(content.worldBound.yMax));
                    if(page=="equipment")
                    {
                        foreach(string id in new[]{"equipment_money","equipment_skis","equipment_rifles"})
                        {
                            var bounds=menu.Root.Q(id).worldBound;
                            Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(content.worldBound.yMin));
                            Assert.That(bounds.yMax,Is.LessThan(content.worldBound.yMax));
                        }
                    }
                    RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
                    pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                    string folder=Path.GetFullPath("docs/quality/m16-6/menu-ux");Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder,page+"-"+locale+"-"+width+".png"),pixels.EncodeToPNG());
                    Object.Destroy(pixels);pixels=null;
                }
                Submit("career_overview");yield return null;
            }
            finally
            {
                if(pixels!=null)Object.Destroy(pixels);RenderTexture.active=previous;
                settings.targetTexture=null;Object.Destroy(target);
            }
        }
    }
}
