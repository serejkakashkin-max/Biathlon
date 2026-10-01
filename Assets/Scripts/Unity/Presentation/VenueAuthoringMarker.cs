using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Generated editor/runtime presentation hook; never a gameplay identifier lookup by object name.</summary>
    public sealed class VenueAuthoringMarker : MonoBehaviour
    {
        [SerializeField] private string stableId,kind,reference;
        [SerializeField] private float extent;
        public string StableId=>stableId;public string Kind=>kind;public string Reference=>reference;public float Extent=>extent;
        public void Configure(string id,string markerKind,string binding,float size=0)
        {stableId=id;kind=markerKind;reference=binding;extent=size;}
    }
}
