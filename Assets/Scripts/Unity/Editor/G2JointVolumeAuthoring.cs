#if UNITY_EDITOR
using Biathlon.Unity.Presentation;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class G2JointVolumeAuthoring
    {
        public static void Apply(Mesh mesh)
        {
            var d=JointVolumeSettings.Load();var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            var allowed=new bool[vertices.Length];
            foreach(int sub in new[]{0,1})foreach(int v in mesh.GetTriangles(sub))allowed[v]=true;
            mesh.ClearBlendShapes();
            foreach(int joint in new[]{6,9,12,15})
            {
                var centre=bind[joint].inverse.MultiplyPoint3x4(Vector3.zero);
                var upper=bind[joint-1].inverse.MultiplyPoint3x4(Vector3.zero);
                var axis=(centre-upper).normalized;bool arm=joint<11;
                float radius=arm?d.arm_radius_m:d.leg_radius_m,expansion=arm?d.arm_expansion_m:d.leg_expansion_m;
                var delta=new Vector3[vertices.Length];
                for(int i=0;i<delta.Length;i++)
                {
                    if(!allowed[i])continue;var w=weights[i];float belongs=0;
                    Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
                    if(belongs<.8f)continue;
                    var offset=vertices[i]-centre;var radial=Vector3.ProjectOnPlane(offset,axis);
                    float falloff=Mathf.Clamp01(1-Mathf.Abs(Vector3.Dot(offset,axis))/radius);
                    if(radial.magnitude>radius||radial.sqrMagnitude<.000001f)continue;
                    delta[i]=radial.normalized*(expansion*falloff*falloff);
                    void Add(int bone,float weight){if(bone==joint||bone==joint-1)belongs+=weight;}
                }
                mesh.AddBlendShapeFrame(((AthleteBone)joint)+"Volume",100,delta,null,null);
            }
        }
    }
}
#endif
