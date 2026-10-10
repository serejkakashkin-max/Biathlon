using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold spatial batching for explicitly supplied, immobile decoration only.
    /// Moving targets, fabric, characters and property-block renderers stay independent.</summary>
    public sealed class StaticSceneryBatches : MonoBehaviour
    {
        [Serializable] private sealed class Settings {public int schema_version;public string id;public float cell_m;}
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<MeshRenderer> sources=new List<MeshRenderer>();
        private readonly List<GameObject> objects=new List<GameObject>();
        public int SourceRendererCount=>sources.Count;
        public int BatchRendererCount=>objects.Count;
        public void Initialize(IEnumerable<MeshRenderer> candidates)
        {
            var settings=JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("Biathlon/static_scenery_batching_v1").text);
            if(settings.schema_version!=1||string.IsNullOrWhiteSpace(settings.id)||!float.IsFinite(settings.cell_m)||settings.cell_m<8||settings.cell_m>128)
                throw new ArgumentException("Invalid scenery batching definition.");
            Initialize(candidates,settings.cell_m);
        }
        public void Initialize(IEnumerable<MeshRenderer> candidates,float cell)
        {
            if(candidates==null||!float.IsFinite(cell)||cell<8||cell>128||objects.Count!=0)throw new ArgumentException("Fresh static batch and bounded cells required.");
            var groups=new Dictionary<(int,int,int,Material,ShadowCastingMode,bool,int,uint),List<(MeshRenderer,Mesh,int)>>();
            var seen=new HashSet<MeshRenderer>();
            foreach(var renderer in candidates)
            {
                if(renderer==null||!seen.Add(renderer)||!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.HasPropertyBlock()||renderer.GetComponent<TextMesh>()!=null)continue;
                var filter=renderer.GetComponent<MeshFilter>();var mesh=filter==null?null:filter.sharedMesh;
                if(mesh==null||!mesh.isReadable)continue;
                var materials=renderer.sharedMaterials;if(materials.Length!=mesh.subMeshCount)continue;
                var p=renderer.bounds.center;
                for(int sub=0;sub<materials.Length;sub++)
                {
                    if(materials[sub]==null)throw new ArgumentException("Static renderer needs complete materials.");
                    var key=((int)Math.Floor(p.x/cell),(int)Math.Floor(p.y/cell),(int)Math.Floor(p.z/cell),materials[sub],renderer.shadowCastingMode,renderer.receiveShadows,renderer.gameObject.layer,renderer.renderingLayerMask);
                    if(!groups.TryGetValue(key,out var group)){group=new List<(MeshRenderer,Mesh,int)>();groups.Add(key,group);}group.Add((renderer,mesh,sub));
                }
                sources.Add(renderer);
            }
            try
            {
                foreach(var pair in groups)
                {
                    var key=pair.Key;var group=pair.Value;var parts=new CombineInstance[group.Count];
                    var origin=new Vector3((key.Item1+.5f)*cell,(key.Item2+.5f)*cell,(key.Item3+.5f)*cell);
                    var inverse=Matrix4x4.Translate(-origin);
                    for(int i=0;i<parts.Length;i++)parts[i]=new CombineInstance{mesh=group[i].Item2,subMeshIndex=group[i].Item3,transform=inverse*group[i].Item1.localToWorldMatrix};
                    var mesh=new Mesh{name="Spatial static scenery",indexFormat=IndexFormat.UInt32};meshes.Add(mesh);mesh.CombineMeshes(parts,true,true,false);
                    var go=new GameObject("Static scenery cell");objects.Add(go);go.transform.SetParent(transform,false);go.transform.SetPositionAndRotation(origin,Quaternion.identity);go.transform.localScale=Vector3.one;go.layer=key.Item7;
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=key.Item4;renderer.shadowCastingMode=key.Item5;renderer.receiveShadows=key.Item6;renderer.renderingLayerMask=key.Item8;
                }
                foreach(var source in sources)source.enabled=false;
            }
            catch{Clear();throw;}
        }
        private void Clear()
        {
            foreach(var source in sources)if(source!=null)source.enabled=true;sources.Clear();
            foreach(var go in objects)if(go!=null)Release(go);objects.Clear();
            foreach(var mesh in meshes)if(mesh!=null)Release(mesh);meshes.Clear();
        }
        private static void Release(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Clear();
    }
}
