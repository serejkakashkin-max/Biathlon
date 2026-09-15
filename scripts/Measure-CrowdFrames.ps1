param([ValidateSet('near','far')][string]$Mode='near',[string]$OutputDirectory='docs/quality/m11/wp150')
$ErrorActionPreference='Stop'
$code=@'
using System;
using UnityEngine;
using UnityEditor;
public static class CrowdFrameProbe {
  private static float[] times;
  private static int lastFrame, skip, count, maxDrawCalls, maxTriangles;
  [Serializable] private class Result {
    public int schema_version=1;
    public string id="wp150-__MODE__-editor-frames", context="Editor; detached30-athlete pose fixture; not release1080p qualification", gpu, cpu, unity;
    public int samples, width, height, max_draw_calls, max_triangles, vsync;
    public float mean_ms,p95_ms,max_ms;
  }
  public static void Begin() {
    var crowd=UnityEngine.Object.FindFirstObjectByType<Biathlon.Unity.Presentation.AthleteCrowdValidation>();
    if(crowd==null)throw new InvalidOperationException("Create crowd fixture first.");
    var camera=Camera.main;camera.transform.position=new Vector3(9,7,__Z__);camera.transform.LookAt(new Vector3(0,.8f,-5));
    crowd.Frozen=false; times=new float[180]; count=0; skip=10; lastFrame=-1;
    EditorApplication.update+=Sample;
  }
  private static void Sample() {
    if(!EditorApplication.isPlaying){EditorApplication.update-=Sample;return;}
    if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
    if(skip-->0)return;
    times[count++]=Time.unscaledDeltaTime*1000;
    maxDrawCalls=Math.Max(maxDrawCalls,UnityStats.drawCalls);maxTriangles=Math.Max(maxTriangles,UnityStats.triangles);
    if(count<times.Length)return;
    EditorApplication.update-=Sample;
    float sum=0;foreach(var t in times)sum+=t;Array.Sort(times);
    var result=new Result {gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,unity=Application.unityVersion,samples=count,width=Screen.width,height=Screen.height,
      max_draw_calls=maxDrawCalls,max_triangles=maxTriangles,vsync=QualitySettings.vSyncCount,mean_ms=sum/count,p95_ms=times[(int)(count*.95f)-1],max_ms=times[count-1]};
    System.IO.File.WriteAllText("__OUTPUT__/frames-__MODE__.json",JsonUtility.ToJson(result,true));
  }
}
'@
$z=if($Mode -eq 'near'){14}else{82}
$code=$code.Replace('__MODE__',$Mode).Replace('__Z__',[string]$z).Replace('__OUTPUT__',((Resolve-Path $OutputDirectory).Path.Replace([char]92,[char]47)))
./scripts/Invoke-UnityMcp.ps1 -Tool script-execute -Arguments @{csharpCode=$code;isMethodBody=$false;className='CrowdFrameProbe';methodName='Begin'} | Out-Null
