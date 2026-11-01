using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Presentation copy: smooth cross-row directions while retaining centers,
    /// row distances, heights, full widths, indices and the original source mesh.</summary>
    public static class SnowRibbonRim
    {
        public static Mesh Refine(Mesh source,double[] distances,bool closed,float window,float maximumDisplacement,System.Collections.Generic.IReadOnlyList<double> fixedCuts=null)
        {
            if(source==null||distances==null||source.vertexCount!=distances.Length*2||distances.Length<2||
                !float.IsFinite(window)||window<=0||!float.IsFinite(maximumDisplacement)||maximumDisplacement<=0)
                throw new ArgumentException("Bounded native ribbon rows required.");
            var p=source.vertices;var centers=new Vector3[distances.Length];int last=distances.Length-1;
            if(fixedCuts!=null)foreach(double cut in fixedCuts)
                if(!double.IsFinite(cut)||cut<distances[0]||cut>distances[last])throw new ArgumentException("Fixed cut must belong to the ribbon.");
            for(int row=0;row<=last;row++)
            {
                if(!double.IsFinite(distances[row])||row>0&&distances[row]<=distances[row-1])throw new ArgumentException("Ordered native distances required.");
                centers[row]=(p[row*2]+p[row*2+1])*.5f;
            }
            for(int row=0;row<=last;row++)
            {
                if(!closed&&(row==0||row==last))continue;
                var tangent=At(distances[row]+window)-At(distances[row]-window);tangent.y=0;
                if(tangent.sqrMagnitude<1e-10f)continue;
                var original=(p[row*2+1]-p[row*2])*.5f;float half=original.magnitude;
                var across=new Vector3(tangent.z,0,-tangent.x).normalized*half;
                if(Vector3.Dot(across,original)<=0)continue;
                if((across-original).sqrMagnitude<1e-10f)continue;
                // An open branch's endpoint tangent is fixed by its connection.
                // Fade the permitted rotation into that tangent over the same
                // authored smoothing window; a full rotation on the penultimate
                // short row can fold the rim back and bury it in the terrain.
                float displacement=maximumDisplacement;
                if(!closed&&fixedCuts!=null)displacement*=Mathf.SmoothStep(0,1,(float)(Math.Min(distances[row]-distances[0],distances[last]-distances[row])/window));
                if(fixedCuts!=null)foreach(double cut in fixedCuts)
                {
                    double gap=Math.Abs(distances[row]-cut);if(closed)gap=Math.Min(gap,distances[last]-distances[0]-gap);
                    displacement=Mathf.Min(displacement,maximumDisplacement*Mathf.SmoothStep(0,1,(float)(gap/window)));
                }
                if(displacement==0)continue;
                float maxAngle=2*Mathf.Asin(Mathf.Clamp01(displacement/(2*half)));
                across=Vector3.RotateTowards(original,across,maxAngle,0);
                p[row*2]=centers[row]-across;p[row*2+1]=centers[row]+across;
            }
            if(closed){p[last*2]=p[0];p[last*2+1]=p[1];}
            var mesh=new Mesh{name=source.name+" natural rim",indexFormat=source.indexFormat};
            try{mesh.vertices=p;mesh.uv=source.uv;mesh.colors=source.colors;mesh.triangles=source.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
            catch{if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);throw;}
            Vector3 At(double d)
            {
                double start=distances[0],length=distances[last]-start;
                if(closed)d=start+((d-start)%length+length)%length;else d=Math.Max(start,Math.Min(distances[last],d));
                int lo=0,hi=last;
                while(lo+1<hi){int mid=(lo+hi)/2;if(distances[mid]<=d)lo=mid;else hi=mid;}
                return Vector3.Lerp(centers[lo],centers[hi],(float)((d-distances[lo])/(distances[hi]-distances[lo])));
            }
        }
    }
}
