using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional venue-only visibility. Legacy benchmark adapters keep their original radius/density contracts.</summary>
    [Serializable] public sealed class ProductionVisibilityDefinition
    {
        public int schema_version;
        public string id,mode;
        public static ProductionVisibilityDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<ProductionVisibilityDefinition>(json);
            if(d==null||d.schema_version!=1||d.mode!="complete_fog_frustum_v1")throw new ArgumentException("Versioned complete scenery visibility required.");
            ProductionEnvironmentDefinition.Identity(d.id);return d;
        }
        public ProductionVisibility Bind(float fogEndMeters)=>new ProductionVisibility(id,fogEndMeters);
    }
    public sealed class ProductionVisibility
    {
        public string Id{get;}
        public float FogEndMeters{get;}
        internal ProductionVisibility(string id,float fogEndMeters)
        {if(!float.IsFinite(fogEndMeters)||fogEndMeters<=0)throw new ArgumentException("Finite positive visibility horizon required.");Id=id;FogEndMeters=fogEndMeters;}
        public ProductionVisibilityFrame Frame(Camera view)=>new ProductionVisibilityFrame(view,FogEndMeters);
    }
    /// <summary>Camera-depth support for native URP linear fog. Uncertified projections retain the full frustum.</summary>
    public readonly struct ProductionVisibilityFrame
    {
        private readonly bool supported;
        private readonly Vector3 eye,forward,absoluteForward;
        private readonly float depth;
        public ProductionVisibilityFrame(Camera view,float fogEnd)
        {
            eye=view.transform.position;forward=view.transform.forward;
            absoluteForward=new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z));depth=fogEnd+view.nearClipPlane;
            supported=!view.orthographic&&!view.usePhysicalProperties&&SameProjection(view.projectionMatrix,Matrix4x4.Perspective(view.fieldOfView,view.aspect,view.nearClipPlane,view.farClipPlane));
        }
        public bool Hidden(Bounds bounds)=>supported&&Vector3.Dot(bounds.center-eye,forward)-Vector3.Dot(bounds.extents,absoluteForward)>=depth;
        private static bool SameProjection(Matrix4x4 a,Matrix4x4 b){for(int i=0;i<16;i++)if(a[i]!=b[i])return false;return true;}
    }
}
