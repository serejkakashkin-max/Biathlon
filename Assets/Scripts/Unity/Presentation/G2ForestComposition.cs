using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class G2ForestComposition
    {
        public int schema_version,species_seed;
        public string id;
        public string dressing_resource;
        public float pine_fraction;
        public static G2ForestComposition Load()
        {
            var d=JsonUtility.FromJson<G2ForestComposition>(Resources.Load<TextAsset>("Biathlon/g2_forest_composition_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!float.IsFinite(d.pine_fraction)||d.pine_fraction<0||d.pine_fraction>1)throw new ArgumentException("Invalid forest composition.");
            return d;
        }
        public int Species(Vector3 worldPosition)
        {
            if(!float.IsFinite(worldPosition.x)||!float.IsFinite(worldPosition.z))throw new ArgumentException("Finite decoration position required.");
            uint key=unchecked((uint)(Mathf.RoundToInt(worldPosition.x*100)*73856093^Mathf.RoundToInt(worldPosition.z*100)*19349663^species_seed));
            return (VegetationDefinition.Mix(key)&0xffffff)/16777216f<pine_fraction?1:0;
        }
    }
}
