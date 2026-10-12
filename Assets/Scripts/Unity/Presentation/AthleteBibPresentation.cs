using System;
using Biathlon.Career;
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
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int SecondaryColorProperty = Shader.PropertyToID("_SecondaryColor");
        private static readonly int SplitProperty = Shader.PropertyToID("_ColorSplit");
        [Serializable] private sealed class LeaderColors { public int schema_version; public string id, regular, total_leader, discipline_leader; }
        private static LeaderColors colors;
        public int Number { get; private set; }
        public CareerLeaderBib LeaderRole { get; private set; }
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
            var appearance=GetComponent<AthleteAppearancePresentation>();if(appearance!=null)appearance.Apply(number);
        }
        public void SetLeader(CareerLeaderBib role)
        {
            if (role < CareerLeaderBib.None || role > (CareerLeaderBib.Total | CareerLeaderBib.Discipline)) throw new ArgumentOutOfRangeException(nameof(role));
            if (body == null) throw new InvalidOperationException("Missing bib renderer.");
            if (colors == null)
            {
                colors = JsonUtility.FromJson<LeaderColors>(Resources.Load<TextAsset>("Biathlon/UI/leader_bib_appearance_v1").text);
                if (colors == null || colors.schema_version != 1 || string.IsNullOrWhiteSpace(colors.id)) throw new ArgumentException("Invalid leader bib palette.");
            }
            string primary = (role & CareerLeaderBib.Total) != 0 ? colors.total_leader : (role & CareerLeaderBib.Discipline) != 0 ? colors.discipline_leader : colors.regular;
            if (!ColorUtility.TryParseHtmlString(primary, out var a) || !ColorUtility.TryParseHtmlString(colors.discipline_leader, out var b)) throw new ArgumentException("Invalid leader bib color.");
            if (properties == null) properties = new MaterialPropertyBlock();
            body.GetPropertyBlock(properties, materialIndex);
            properties.SetColor(BaseColorProperty, a); properties.SetColor(SecondaryColorProperty, b);
            properties.SetFloat(SplitProperty, role == (CareerLeaderBib.Total | CareerLeaderBib.Discipline) ? 1 : 0);
            body.SetPropertyBlock(properties, materialIndex); LeaderRole = role;
        }
    }
}
