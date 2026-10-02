using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold, detached presentation corridor. Widths are full widths at explicit vertices.</summary>
    public sealed class TerrainBlendCorridor
    {
        private readonly Vector3[] points;
        private readonly float[] widths;
        public int Count=>points.Length;
        public Vector3 Point(int index)=>points[index];
        public float Width(int index)=>widths[index];
        public TerrainBlendCorridor(Vector3[] points,float[] widths)
        {
            if(points==null||widths==null||points.Length<2||points.Length!=widths.Length)throw new ArgumentException("Independent corridor points and widths required.");
            for(int i=0;i<points.Length;i++)
            {
                var p=points[i];if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)||!float.IsFinite(widths[i])||widths[i]<=0)throw new ArgumentException("Invalid terrain corridor.");
                if(i>0&&p.x==points[i-1].x&&p.z==points[i-1].z)throw new ArgumentException("A corridor segment must have a horizontal direction.");
            }
            this.points=(Vector3[])points.Clone();this.widths=(float[])widths.Clone();
        }
    }
}
