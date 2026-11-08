using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Shared garment/body surface morphs. Skeleton, equipment and head anatomy stay fixed.</summary>
    internal static class Biathlete167BodyShape
    {
        private static readonly Dictionary<Mesh,Mesh> meshes=new Dictionary<Mesh,Mesh>();
        private static readonly string[] names={"CareerBodyNarrow","CareerBodyWide","CareerBodyThin","CareerBodyDeep"};

        public static void Prepare(Biathlete167Rig rig,Biathlete167RaceDefinition definition)
        {
            foreach(var lod in rig.Lods.GetLODs())foreach(var item in lod.renderers)
            {
                if(!(item is SkinnedMeshRenderer skin))continue;
                var source=skin.sharedMesh;
                if(!meshes.TryGetValue(source,out var mesh))
                {
                    mesh=Object.Instantiate(source);mesh.name=source.name+" career body";mesh.hideFlags=HideFlags.DontSave;
                    var vertices=source.vertices;var rootPoints=new Vector3[vertices.Length];
                    float centre=0;int count=0;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var p=rig.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));rootPoints[i]=p;
                        if(Mathf.Abs(p.x)<definition.bodySideBand.x&&p.y>definition.bodyHeightBand.y&&p.y<definition.bodyHeightBand.z){centre+=p.z;count++;}
                    }
                    if(count>0)centre/=count;
                    for(int shape=0;shape<4;shape++)
                    {
                        var delta=new Vector3[vertices.Length];
                        for(int i=0;i<delta.Length;i++)
                        {
                            var p=rootPoints[i];var y=definition.bodyHeightBand;var x=definition.bodySideBand;
                            float mask=Smooth(y.x,y.y,p.y)*(1-Smooth(y.z,y.w,p.y))*(1-Smooth(x.x,x.y,Mathf.Abs(p.x)));
                            float sign=shape%2==0?-1:1;
                            var offset=shape<2?new Vector3(p.x*definition.maximumWidthMorph*sign*mask,0,0):
                                new Vector3(0,0,(p.z-centre)*definition.maximumDepthMorph*sign*mask);
                            delta[i]=skin.transform.InverseTransformVector(rig.transform.TransformVector(offset));
                        }
                        mesh.AddBlendShapeFrame(names[shape],100,delta,null,null);
                    }
                    meshes.Add(source,mesh);meshes.Add(mesh,mesh);
                }
                skin.sharedMesh=mesh;
            }
        }
        private static float Smooth(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
        public static void Apply(SkinnedMeshRenderer[] skins,float width,float depth,Biathlete167RaceDefinition definition)
        {
            foreach(var skin in skins)
            {
                skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(names[0]),Mathf.Max(0,1-width)/definition.maximumWidthMorph*100);
                skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(names[1]),Mathf.Max(0,width-1)/definition.maximumWidthMorph*100);
                skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(names[2]),Mathf.Max(0,1-depth)/definition.maximumDepthMorph*100);
                skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(names[3]),Mathf.Max(0,depth-1)/definition.maximumDepthMorph*100);
            }
        }
    }
}
