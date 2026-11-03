using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed partial class FrontEndMenuTests
    {
        [UnityTest] public IEnumerator RussiaCanBeChosenCreatedAndContinuedThroughTheRealMenu()
        {
            yield return null;Submit("new_career");yield return null;Submit("creator_mode_own");yield return null;
            var nations=menu.Root.Q<DropdownField>("athlete_nation");Assert.That(nations.choices,Does.Contain("Россия"));
            nations.index=nations.choices.IndexOf("Россия");yield return null;
            menu.Root.Q<TextField>("athlete_name").value="Александр Северин";Submit("create");yield return null;Submit("confirm_create");yield return null;
            Assert.That(menu.Session.Player.Nation,Is.EqualTo("RUS"));Assert.That(menu.Session.Career.RulesId,Is.EqualTo("career_roster_v2"));
            string before=menu.Session.Career.ToJson();Submit("back");yield return null;Submit("continue");yield return null;
            Assert.That(menu.Session.Player.Nation,Is.EqualTo("RUS"));Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
        }
    }
    public sealed class RussiaKitTests
    {
        [UnityTest] public IEnumerator OriginalAthleteRendersRussianBlueWhiteAndRedPanels()
        {
            var root=new GameObject("Russian kit visual");bool fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;
            try
            {
                var camera=new GameObject("Russian kit camera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform);
                camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.1f,.14f);camera.fieldOfView=35;
                root.AddComponent<RenderQualityPresentation>().Initialize(camera);root.GetComponent<RenderQualityPresentation>().Apply("high");RenderSettings.fog=false;RenderSettings.ambientLight=Color.white*.65f;
                var light=new GameObject("Kit daylight",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(25,-30,0);light.cullingMask=1<<31;
                var rig=AthleteRigFactory.Create(null);rig.transform.SetParent(root.transform);rig.ResetPose();
                foreach(var t in rig.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var appearance=rig.GetComponent<AthleteAppearancePresentation>();appearance.ApplyNation("RUS");
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();Assert.That(skin.sharedMaterials[0].GetFloat("_NationalPanels"),Is.EqualTo(1));
                Assert.That(AthleteAppearancePresentation.NationGarmentColor("RUS",new Vector3(.3f,1.5f,0)),Is.EqualTo(RangeFlagDefinition.ColorOf("#F4F5F7")));
                Assert.That(AthleteAppearancePresentation.NationGarmentColor("RUS",new Vector3(.3f,1,0)),Is.EqualTo(RangeFlagDefinition.ColorOf("#D52B3E")));
                string folder=Path.GetFullPath("docs/quality/m16-6/russia-shadows/kits");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1024,1024))foreach(int side in new[]{1,-1})
                {
                    camera.transform.position=new Vector3(.7f,1.2f,side*3.6f);camera.transform.LookAt(Vector3.up*.95f);
                    for(int i=0;i<4;i++){target.Render(camera);yield return null;}
                    string file=Path.Combine(folder,side==1?"russia-front.png":"russia-back.png");target.SavePng(file);
                    var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(file));int blue=0,red=0,white=0;
                    foreach(var p in image.GetPixels32()){if(p.b>p.r*1.3f&&p.b>90)blue++;if(p.r>p.b*1.3f&&p.r>100)red++;if(p.r>160&&p.g>160&&p.b>160)white++;}
                    Object.Destroy(image);Assert.That(blue,Is.GreaterThan(200));Assert.That(red,Is.GreaterThan(50));Assert.That(white,Is.GreaterThan(50));
                }
            }
            finally{RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;Object.DestroyImmediate(root);}
        }
    }
}
