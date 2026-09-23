using System;
using System.IO;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Offscreen camera request after Unity updates skinning; no swapchain/HUD workload.</summary>
    public sealed class BenchmarkRenderTarget : IDisposable
    {
        private RenderTexture target;
        private Camera queuedCamera;
        public int Width => target!=null?target.width:throw new ObjectDisposedException(nameof(BenchmarkRenderTarget));
        public int Height => target!=null?target.height:throw new ObjectDisposedException(nameof(BenchmarkRenderTarget));
        private readonly UniversalRenderPipeline.SingleCameraRequest request=new UniversalRenderPipeline.SingleCameraRequest();
        public BenchmarkRenderTarget(int width,int height)
        {
            if(width<64||height<64||width>3840||height>2160)throw new ArgumentOutOfRangeException();
            target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Benchmark offscreen output"};
            target.Create();request.destination=target;ChangeLoop(true);
        }
        public void Render(Camera camera)
        {
            if(target==null)throw new ObjectDisposedException(nameof(BenchmarkRenderTarget));
            if(camera==null)throw new ArgumentNullException(nameof(camera));
            queuedCamera=camera;
        }
        private void Execute()
        {
            if(queuedCamera==null||target==null)return;
            var camera=queuedCamera;queuedCamera=null;
            var previous=RenderTexture.active;
            try{RenderPipeline.SubmitRenderRequest(camera,request);}
            finally{RenderTexture.active=previous;}
        }
        private void ChangeLoop(bool add)
        {
            var loop=PlayerLoop.GetCurrentPlayerLoop();
            for(int i=0;i<loop.subSystemList.Length;i++)
            {
                if(loop.subSystemList[i].type!=typeof(UnityEngine.PlayerLoop.PostLateUpdate))continue;
                var list=new System.Collections.Generic.List<PlayerLoopSystem>(loop.subSystemList[i].subSystemList);
                if(add)
                {
                    int index=list.FindIndex(s=>s.type==typeof(UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering));
                    if(index<0)throw new InvalidOperationException("No safe render-request phase in player loop.");
                    list.Insert(index,new PlayerLoopSystem{type=typeof(BenchmarkRenderTarget),updateDelegate=Execute});
                }
                else list.RemoveAll(s=>s.type==typeof(BenchmarkRenderTarget)&&s.updateDelegate==(PlayerLoopSystem.UpdateFunction)Execute);
                loop.subSystemList[i].subSystemList=list.ToArray();PlayerLoop.SetPlayerLoop(loop);return;
            }
            throw new InvalidOperationException("PostLateUpdate unavailable.");
        }
        public void SavePng(string path)
        {
            if(target==null)throw new ObjectDisposedException(nameof(BenchmarkRenderTarget));
            var previous=RenderTexture.active;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.Destroy(texture);}
        }
        public void Dispose()
        {
            if(target==null)return;ChangeLoop(false);queuedCamera=null;
            if(RenderTexture.active==target)RenderTexture.active=null;
            target.Release();UnityEngine.Object.Destroy(target);target=null;request.destination=null;
        }
    }
}
