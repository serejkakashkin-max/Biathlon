using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit development-player native portrait review. Absent from ordinary gameplay.</summary>
    public sealed class AthleteFaceReview : MonoBehaviour
    {
        [Serializable] private sealed class Portrait {public string file,athlete_id,face,skin;}
        [Serializable] private sealed class Receipt
        {public int schema_version=1;public string id="athlete_face_native_review";public bool success;public Portrait[] portraits;public string[] errors;}
        private readonly List<string> errors=new List<string>();
        private readonly List<Portrait> portraits=new List<Portrait>();
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonFaceReview")<0)return;
            new GameObject("Explicit face review").AddComponent<AthleteFaceReview>();
        }
        private void Log(string text,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception)errors.Add(text);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Application.logMessageReceived+=Log;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-biathlonFaceReviewOutput");
            if(index<0||index+1>=args.Length){Application.Quit(1);yield break;}
            output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object current=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            var recipe=AthleteFaceCatalog.Load();bool success=errors.Count==0&&portraits.Count==recipe.skin_colors.Length+recipe.profiles.Length;
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(new Receipt{success=success,portraits=portraits.ToArray(),errors=errors.ToArray()},true));
            Application.logMessageReceived-=Log;Application.Quit(success?0:1);
        }
        private IEnumerator Run()
        {
            yield return null;
            foreach(var menu in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))menu.gameObject.SetActive(false);
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))light.enabled=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.31f,.35f,.40f);RenderSettings.fog=false;
            var root=new GameObject("Face review studio");var rig=AthleteRigFactory.Create(root.transform);
            var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;
            rig.GetComponent<AthleteHybridLocomotionDriver>().Sample(rig,new LocomotionInput(0,0,0,travelling:false),0);
            rig.GetComponent<AthleteSkinDetailBinding>().Prepare(0,true,true);
            var appearance=rig.GetComponent<AthleteAppearancePresentation>();appearance.ApplyNation("NOR");
            var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
            camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.21f);camera.fieldOfView=28;camera.nearClipPlane=.05f;
            LightAt("Portrait key",new Vector3(35,-145,0),1.6f);LightAt("Portrait fill",new Vector3(15,35,0),.65f);
            foreach(var transform in root.GetComponentsInChildren<Transform>())transform.gameObject.layer=30;
            var recipe=AthleteFaceCatalog.Load();
            using(var target=new BenchmarkRenderTarget(640,800))
            {
                for(int i=0;i<recipe.skin_colors.Length;i++)yield return Capture("skin-"+i,i%recipe.profiles.Length,i,new Vector3(.22f,.035f,.76f));
                for(int i=0;i<recipe.profiles.Length;i++)yield return Capture("face-"+recipe.profiles[i].id,i,1,new Vector3(0,.025f,.78f));
                IEnumerator Capture(string file,int face,int tone,Vector3 offset)
                {
                    string id=Identity(face,tone);appearance.ApplyIdentity(id);
                    var focus=new Vector3(0,1.65f,.02f);camera.transform.position=focus+offset;camera.transform.LookAt(focus);
                    for(int frame=0;frame<5;frame++){target.Render(camera);yield return null;}
                    target.SavePng(Path.Combine(output,file+".png"));
                    portraits.Add(new Portrait{file=file+".png",athlete_id=id,face=recipe.profiles[face].id,skin=recipe.skin_colors[tone]});
                }
            }
            Destroy(root);
            void LightAt(string name,Vector3 rotation,float intensity)
            {var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(rotation);light.intensity=intensity;light.cullingMask=1<<30;}
        }
        private static string Identity(int face,int tone)
        {
            var recipe=AthleteFaceCatalog.Load();
            // Match the neutral existing build in every portrait, so the face
            // comparison changes only the new face channel and/or skin tone.
            for(int i=0;i<10000;i++){string id="face-review-"+i;recipe.Resolve(id,out int a,out int b);if(a==face&&b==tone&&AthleteFaceCatalog.Hash(id)%3==1)return id;}
            throw new InvalidOperationException("Face review identity not found.");
        }
    }
}
