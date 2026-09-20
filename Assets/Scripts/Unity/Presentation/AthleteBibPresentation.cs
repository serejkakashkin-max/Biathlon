using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Print only: the one-based registered roster slot. Never a gameplay identifier.</summary>
    public sealed class AthleteBibPresentation : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer body;
        [SerializeField] private int materialIndex = 2;
        private MaterialPropertyBlock properties;
        private static readonly int NumberProperty = Shader.PropertyToID("_BibNumber");
        public int Number { get; private set; }
        public void Configure(SkinnedMeshRenderer renderer, int slot)
        {
            if (renderer == null || slot < 0 || slot >= renderer.sharedMaterials.Length) throw new ArgumentException("Invalid bib material binding.");
            body=renderer; materialIndex=slot;
        }
        public void SetNumber(int number)
        {
            if (number < 1 || number > 60) throw new ArgumentOutOfRangeException(nameof(number));
            if (body == null) throw new InvalidOperationException("Missing bib renderer.");
            if (Number == number) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            body.GetPropertyBlock(properties,materialIndex); properties.SetFloat(NumberProperty,number);
            body.SetPropertyBlock(properties,materialIndex); Number=number;
        }
    }
}
