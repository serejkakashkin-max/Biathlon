using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.PlayModeTests
{
    public sealed class SnowJoinCoverageTests
    {
        [TestCase(0,false,2)][TestCase(.00001f,false,0)][TestCase(0,true,0)]
        public void OnlyWholeSamePlaneSameFacingCoverageCanRemoveAConnectedCut(float offset,bool opposite,int expected)
        {
            var coverage=new SnowJoinCoverage();var main=Quad(0,3,0,false);var start=Quad(1,3,offset,opposite);Mesh result=null;
            try
            {
                Assert.That(coverage.RetainUncovered(main,out int first),Is.SameAs(main));Assert.That(first,Is.Zero);
                result=coverage.RetainUncovered(start,out int removed);Assert.That(removed,Is.EqualTo(expected));
                Assert.That(result.triangles.Length,Is.EqualTo(expected==2?0:6));
                CollectionAssert.AreEqual(start.vertices,result.vertices);
            }
            finally{if(result!=null&&result!=start)Object.DestroyImmediate(result);Object.DestroyImmediate(main);Object.DestroyImmediate(start);}
        }
        [Test] public void PartialCoverageRetainsTheUncoveredOriginalCurtain()
        {
            var coverage=new SnowJoinCoverage();var main=Quad(0,2,0,false);var start=Quad(1,3,0,false);
            try{coverage.RetainUncovered(main,out _);var result=coverage.RetainUncovered(start,out int removed);Assert.That(removed,Is.Zero);Assert.That(result,Is.SameAs(start));}
            finally{Object.DestroyImmediate(main);Object.DestroyImmediate(start);}
        }
        [TestCase("both",2)][TestCase("one",0)][TestCase("lower",0)]
        [TestCase("partial",0)][TestCase("offset",0)][TestCase("roundoff",2)][TestCase("two_ulps",0)]
        [TestCase("scene_roundoff",2)][TestCase("scene_gap",0)]
        public void InternalCurtainsRequireCompleteExactRoofsAtSufficientHeightOnBothSides(string variant,int expected)
        {
            var wall=Quad(0,2,0,false);var points=wall.vertices;for(int i=0;i<points.Length;i++)points[i].y+=1;wall.vertices=points;wall.RecalculateBounds();
            float height=variant=="lower"?.99f:variant=="roundoff"?System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(1f)-1):
                variant=="two_ulps"?System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(1f)-2):1;
            float offset=variant=="offset"?.00001f:0,last=variant=="partial"?1:2;
            var roof=new Mesh{vertices=new[]{new Vector3(0,1,0),new Vector3(2,1,0),new Vector3(1,1,1),
                new Vector3(0,height,offset),new Vector3(last,height,offset),new Vector3(1,height,-1)},
                triangles=variant=="one"?new[]{0,2,1}:new[]{0,2,1,3,4,5}};
            if(variant.StartsWith("scene_"))
            {
                var translation=new Vector3(200,0,200);points=wall.vertices;for(int i=0;i<points.Length;i++)points[i]+=translation;wall.vertices=points;wall.RecalculateBounds();
                points=roof.vertices;for(int i=0;i<points.Length;i++)points[i]+=translation;
                float displaced=variant=="scene_roundoff"?System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(200f)+1):200.001f;
                points[3].z=points[4].z=displaced;roof.vertices=points;
            }
            Mesh result=null;
            try
            {
                var coverage=new SnowJoinCoverage();coverage.CaptureRoofs(new[]{roof});
                result=coverage.RetainExteriorCurtains(wall,out int removed);Assert.That(removed,Is.EqualTo(expected));
                CollectionAssert.AreEqual(wall.vertices,result.vertices);
                if(expected==0)Assert.That(result,Is.SameAs(wall),"Visible, partial and almost coincident boundaries retain their complete wall.");
                else Assert.That(result.triangles,Is.Empty,"Only the fully interior curtain is removed; both original snow roofs stay exact.");
            }
            finally{if(result!=null&&result!=wall)Object.DestroyImmediate(result);Object.DestroyImmediate(wall);Object.DestroyImmediate(roof);}
        }
        private static Mesh Quad(float first,float last,float z,bool reverse)
        {
            var mesh=new Mesh{vertices=new[]{new Vector3(first,0,z),new Vector3(first,-.03f,z),new Vector3(last,0,z),new Vector3(last,-.03f,z)},
                triangles=reverse?new[]{0,2,1,2,3,1}:new[]{0,1,2,2,1,3},normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward},uv=new Vector2[4],colors=new Color[4]};
            mesh.RecalculateBounds();return mesh;
        }
    }
}
