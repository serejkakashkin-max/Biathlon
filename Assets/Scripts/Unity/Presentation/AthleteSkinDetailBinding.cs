using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Asset-owned three-level skin detail. Uses the caller's projection-adjusted distance.</summary>
    public sealed class AthleteSkinDetailBinding : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer body;
        [SerializeField] private Mesh[] levels;
        [SerializeField] private float mediumDistance = 12, farDistance = 45, hysteresis = 2;
        public int Level { get; private set; }
        public void Configure(SkinnedMeshRenderer renderer, Mesh[] meshes, float medium, float far, float margin)
        {
            body = renderer; levels = meshes == null ? null : (Mesh[])meshes.Clone();
            mediumDistance = medium; farDistance = far; hysteresis = margin; Validate();
            Level = 0; body.sharedMesh = levels[0];
        }
        private void Awake() { if (body != null) { Validate(); Level = 0; body.sharedMesh = levels[0]; } }
        public void Validate()
        {
            if (body == null || !body.transform.IsChildOf(transform) || levels == null || levels.Length != 3 ||
                !float.IsFinite(mediumDistance) || !float.IsFinite(farDistance) || !float.IsFinite(hysteresis) ||
                mediumDistance <= 0 || farDistance <= mediumDistance || hysteresis <= 0 || hysteresis * 2 >= mediumDistance || hysteresis * 2 >= farDistance-mediumDistance)
                throw new ArgumentException("Invalid athlete skin LOD binding.");
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] == null || levels[i].bindposes.Length != (int)AthleteBone.Count || levels[i].subMeshCount != body.sharedMaterials.Length)
                    throw new ArgumentException("Skin LOD does not match M11 bones/materials.");
        }
        public void Prepare(float distance, bool forceNear, bool reset)
        {
            if (!float.IsFinite(distance) || distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            int next = Level;
            if (forceNear) next = 0;
            else if (reset) next = distance >= farDistance ? 2 : distance >= mediumDistance ? 1 : 0;
            else
            {
                if (next == 0 && distance >= mediumDistance+hysteresis) next = 1;
                if (next == 1 && distance >= farDistance+hysteresis) next = 2;
                if (next == 2 && distance <= farDistance-hysteresis) next = 1;
                if (next == 1 && distance <= mediumDistance-hysteresis) next = 0;
            }
            if (next == Level) return;
            Level = next; body.sharedMesh = levels[Level];
        }
    }
}
