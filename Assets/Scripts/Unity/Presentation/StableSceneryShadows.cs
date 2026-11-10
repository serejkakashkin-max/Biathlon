using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Shadow receivers can be visible when their caster is outside the view.
    /// Keep a bounded, fixed-mesh caster set independent of visible LOD/frustum changes.</summary>
    internal sealed class StableSceneryShadows
    {
        private static RenderQualityDefinition quality;
        private readonly Mesh[] meshes;
        private readonly Material[] materials;
        private readonly Matrix4x4[][] matrices;
        private readonly int[] counts;
        private readonly uint[] triangles;
        private readonly Bounds[] bounds;
        private Vector3 eye,ray;
        private float distanceSquared;
        public int Count{get;private set;}
        public long Triangles{get;private set;}
        public StableSceneryShadows(Mesh[] meshes,Material[] materials,int capacity)
        {
            this.meshes=meshes;this.materials=materials;matrices=new Matrix4x4[meshes.Length][];counts=new int[meshes.Length];triangles=new uint[meshes.Length];bounds=new Bounds[meshes.Length];
            for(int i=0;i<meshes.Length;i++){matrices[i]=new Matrix4x4[capacity];triangles[i]=meshes[i].GetIndexCount(0)/3;}
            if(quality==null)quality=RenderQualityDefinition.Load();
        }
        public void Begin(Camera camera,string tier,Light light)
        {
            Array.Clear(counts,0,counts.Length);Count=0;Triangles=0;eye=camera.transform.position;
            var sun=light!=null?light:RenderSettings.sun;ray=sun!=null?sun.transform.forward:Vector3.down;
            float distance=quality.Get(tier).shadow_distance_m;distanceSquared=distance*distance;
        }
        public void Add(int group,Matrix4x4 matrix,Bounds caster)
        {
            // Conservatively project the complete canopy/body below its base as well as
            // towards the receiver. URP performs the final light-frustum/distance cull.
            var influence=caster;
            if(ray.y<-.001f)
            {
                float drop=caster.size.y+caster.extents.y+Mathf.Abs(eye.y-caster.min.y);
                influence.Encapsulate(new Bounds(caster.center+ray*(drop/-ray.y),caster.size));
            }
            if(ray.y<-.001f&&influence.SqrDistance(eye)>distanceSquared)return;
            int count=counts[group];matrices[group][count]=matrix;
            if(count==0)bounds[group]=caster;else bounds[group].Encapsulate(caster);
            counts[group]++;Count++;Triangles+=triangles[group];
        }
        public bool MayInfluence(Bounds grove)
        {
            if(ray.y>=-.001f)return true;
            // Bound every possible per-caster projection inside this grove.
            float maxDrop=grove.size.y+grove.extents.y+Mathf.Max(Mathf.Abs(eye.y-grove.min.y),Mathf.Abs(eye.y-grove.max.y));
            var influence=grove;
            influence.Encapsulate(new Bounds(grove.center+ray*(maxDrop/-ray.y),grove.size));
            return influence.SqrDistance(eye)<=distanceSquared;
        }
        public void Render(Camera camera,MaterialPropertyBlock properties,int layer)
        {
            for(int group=0;group<counts.Length;group++)if(counts[group]>0)
            {
                var parameters=new RenderParams(materials[group]){camera=camera,matProps=properties,worldBounds=bounds[group],shadowCastingMode=ShadowCastingMode.ShadowsOnly,receiveShadows=false,layer=layer};
                for(int offset=0;offset<counts[group];offset+=500)
                    Graphics.RenderMeshInstanced(parameters,meshes[group],0,matrices[group],Math.Min(500,counts[group]-offset),offset);
            }
        }
    }
}
