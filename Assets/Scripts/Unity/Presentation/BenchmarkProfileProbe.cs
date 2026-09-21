using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in development-player ablation. Never a qualification run.</summary>
    public sealed class BenchmarkProfileProbe
    {
        public string CaseId {get;private set;}
        public string Name {get;private set;}="baseline";
        public bool IsDiagnostic=>CaseId!=null;
        public bool Includes(string id)=>CaseId==null||CaseId==id;
        public static BenchmarkProfileProbe Parse(string[] args,BenchmarkProfileDefinition settings)
        {
            var result=new BenchmarkProfileProbe();bool probeProvided=false,caseProvided=false;
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]!="-biathlonProfileCase"&&args[i]!="-biathlonProfileProbe")continue;
                string flag=args[i];if(i+1==args.Length||string.IsNullOrWhiteSpace(args[i+1])||args[i+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Missing profiling diagnostic argument.");
                if(flag=="-biathlonProfileCase"){if(caseProvided)throw new ArgumentException("Duplicate case selector.");caseProvided=true;result.CaseId=args[++i];}
                else{if(probeProvided)throw new ArgumentException("Duplicate diagnostic probe.");probeProvided=true;result.Name=args[++i];}
            }
            if(probeProvided&&!caseProvided)throw new ArgumentException("Diagnostic probes require exactly one explicit case.");
            if(caseProvided&&!Array.Exists(settings.cases,c=>c.id==result.CaseId))throw new ArgumentException("Unknown profiling case.");
            if(result.Name!="baseline"&&result.Name!="forest_shadows"&&result.Name!="surface_shadows"&&result.Name!="athlete_shadows"&&result.Name!="crowd_shadows"&&result.Name!="forest")throw new ArgumentException("Unknown diagnostic probe.");
            return result;
        }
        public void Apply(VisualBenchmarkPresentation p)
        {
            if(!IsDiagnostic||Name=="baseline")return;
            if(Application.isEditor)throw new InvalidOperationException("Ablation mutates runtime materials; use an isolated development player.");
            if(Name=="forest")p.Forests=Array.Empty<VegetationPresentation>();
            else if(Name=="forest_shadows")
            {
                DisableShadowPass("Biathlon/Vegetation/G2Fir/Fir");DisableShadowPass("Biathlon/Vegetation/G2Pine/Pine");
            }
            else if(Name=="crowd_shadows")DisableShadowPass("Biathlon/Crowd/Spectator");
            else if(Name=="athlete_shadows")foreach(var renderer in p.Athletes.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.Off;
            else if(Name=="surface_shadows")
            {
                var surface=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurface");var proxy=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurfaceShadow");bool found=false;
                foreach(var root in p.gameObject.scene.GetRootGameObjects())foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if((filter.sharedMesh==surface||(proxy!=null&&filter.sharedMesh==proxy))&&filter.TryGetComponent(out MeshRenderer renderer)){renderer.shadowCastingMode=ShadowCastingMode.Off;found=true;}
                if(!found)throw new InvalidOperationException("Diagnostic surface binding missing.");
            }
        }
        private static void DisableShadowPass(string resource)
        {
            var material=Resources.Load<Material>(resource);if(material==null||material.FindPass("ShadowCaster")<0)throw new InvalidOperationException("Diagnostic shadow material missing.");
            material.SetShaderPassEnabled("ShadowCaster",false);
        }
    }
}
