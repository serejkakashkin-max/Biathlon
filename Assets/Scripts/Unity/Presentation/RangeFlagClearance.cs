using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold exclusion from real canopy bounds, including every LOD and wind excursion.</summary>
    public sealed class RangeFlagClearance
    {
        private readonly float firRadius,pineRadius,windRadius,flagRadius;
        private readonly Vector3[] anchors;
        public RangeFlagClearance(RangeFlagDefinition flags,Vector3[] positions)
        {
            if(flags==null||positions==null||positions.Length!=flags.nations.Length)throw new ArgumentException("Matching flag row required.");
            anchors=(Vector3[])positions.Clone();firRadius=CanopyRadius("G2Fir/Fir");pineRadius=CanopyRadius("G2Pine/Pine");
            windRadius=VegetationDefinition.Load().wind_amplitude_m;
            float aspect=0;foreach(var nation in flags.nations)aspect=Mathf.Max(aspect,nation.aspect);
            // Complete cloth sweep around the pinned mast, for every wind direction.
            float reach=flags.flag_height_m*aspect,amplitude=flags.maximum_wave_m*1.23f;
            flagRadius=Mathf.Max(flags.tree_clearance_m,Mathf.Sqrt(reach*reach+amplitude*amplitude));
        }
        private static float CanopyRadius(string resource)
        {
            float result=0;
            for(int lod=0;lod<3;lod++)
            {
                var mesh=Resources.Load<Mesh>("Biathlon/Vegetation/"+resource+"LOD"+lod);
                if(mesh==null)throw new ArgumentException("Actual canopy mesh required for flag clearance.");
                var bounds=mesh.bounds;
                float x=Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)),z=Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z));
                result=Mathf.Max(result,Mathf.Sqrt(x*x+z*z));
            }
            return result;
        }
        public bool Clears(ProductionTree tree)
        {
            if(tree==null||tree.species!="fir"&&tree.species!="pine")throw new ArgumentException("Known tree species required.");
            float radius=flagRadius+windRadius+(tree.species=="pine"?pineRadius:firRadius)*tree.scale;
            foreach(var anchor in anchors)
            {float dx=tree.position.x-anchor.x,dz=tree.position.z-anchor.z;if(dx*dx+dz*dz<=radius*radius)return false;}
            return true;
        }
    }
}
