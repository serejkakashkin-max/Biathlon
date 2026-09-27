using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Rigid head surface follows the typed Head bone. Local licensed assets remain separate
    /// from the distributable CC0 body meshes; all LOD decisions come from the body detail binding.</summary>
    public sealed class AthleteFaceDetailBinding : MonoBehaviour
    {
        [SerializeField] private MeshFilter face;
        [SerializeField] private Mesh[] levels;
        [SerializeField] private MeshFilter cap;
        [SerializeField] private Mesh[] capLevels;
        public int Level {get;private set;}
        public MeshFilter Face=>face;
        public MeshFilter Cap=>cap;
        public void Configure(MeshFilter filter,Mesh[] meshes,MeshFilter fittedCap,Mesh[] capMeshes)
        {
            face=filter;levels=(Mesh[])meshes.Clone();cap=fittedCap;capLevels=(Mesh[])capMeshes.Clone();Validate();SetLevel(0);
        }
        private void Awake(){Validate();SetLevel(0);}
        public void Validate()
        {
            if(face==null||!face.transform.IsChildOf(GetComponent<AthleteRigBinding>().Bone(AthleteBone.Head))||levels==null||levels.Length!=3)
                throw new InvalidOperationException("Generate licensed hero face with scripts/Build-G2Hero.ps1.");
            foreach(var m in levels)if(m==null)throw new InvalidOperationException("Missing locally generated hero face LOD.");
            if(cap==null||cap.transform.parent!=face.transform.parent||capLevels==null||capLevels.Length!=3)
                throw new InvalidOperationException("Missing fitted hero cap.");
            foreach(var m in capLevels)if(m==null)throw new InvalidOperationException("Missing fitted cap LOD.");
        }
        public void SetLevel(int level)
        {
            if(level<0||level>2)throw new ArgumentOutOfRangeException(nameof(level));
            Level=level;face.sharedMesh=levels[level];cap.sharedMesh=capLevels[level];
        }
    }
}
