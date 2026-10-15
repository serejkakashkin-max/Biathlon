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
    /// <summary>Real native UI/RenderTexture footage for the owner's visual acceptance.</summary>
    public sealed class CareerCreatorDemoCaptureTests
    {
        private sealed class Store:ICareerProfileStore
        {
            public bool HasProfile=>false;
            public string ReadLatest()=>throw new System.InvalidOperationException();
            public void WriteNew(string id,string json)=>throw new System.InvalidOperationException("Capture must not publish a career.");
            public void Replace(string id,string expected,string json)=>throw new System.InvalidOperationException();
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator NativeWizardAndRotatingAthleteProduceVisibleDemoFrames()
        {
            var root=new GameObject("M15.6 demo capture");var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            target.Create();var panel=root.GetComponent<UIDocument>().panelSettings;var previous=panel.targetTexture;panel.targetTexture=target;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/m15-6/demo-frames"));Directory.CreateDirectory(folder);
            try
            {
                for(int i=0;i<8;i++)yield return null;
                Submit("new_career");yield return null;
                for(int i=0;i<8;i++){yield return null;Save(i);}
                Submit("creator_mode_own");yield return null;
                for(int i=8;i<32;i++){menu.CreatorStudio.SetView((i-8)*7,0);yield return null;Save(i);}
                Submit("creator_step_1");yield return null;
                Submit("creator_look_0");yield return null;
                var eyes=menu.Root.Q<DropdownField>("creator_eyes");Assert.That(eyes,Is.Not.Null);eyes.value=eyes.choices[1];yield return null;
                for(int i=32;i<64;i++){menu.CreatorStudio.SetView(15+Mathf.Sin((i-32)*Mathf.PI/16)*35,Mathf.Clamp01((i-32)/14f));yield return null;Save(i);}
                Submit("creator_step_2");yield return null;
                menu.CreatorStudio.SetView(25,0);
                for(int i=64;i<88;i++){yield return null;Save(i);}
                Assert.That(menu.Session.Career,Is.Null,"The demonstration must not change saved careers.");
                Assert.That(Directory.GetFiles(folder,"frame-*.png").Length,Is.GreaterThanOrEqualTo(88));
            }
            finally{panel.targetTexture=previous;target.Release();Object.Destroy(pixels);Object.Destroy(target);Object.DestroyImmediate(root);}
            void Submit(string id)
            {var button=menu.Root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);}
            void Save(int index)
            {
                var before=RenderTexture.active;RenderTexture.active=target;
                try{pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"frame-"+index.ToString("D3")+".png"),pixels.EncodeToPNG());}
                finally{RenderTexture.active=before;}
            }
        }
    }
}
