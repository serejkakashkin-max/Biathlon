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
        private static Mesh Quad(float first,float last,float z,bool reverse)
        {
            var mesh=new Mesh{vertices=new[]{new Vector3(first,0,z),new Vector3(first,-.03f,z),new Vector3(last,0,z),new Vector3(last,-.03f,z)},
                triangles=reverse?new[]{0,2,1,2,3,1}:new[]{0,1,2,2,1,3},normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward},uv=new Vector2[4],colors=new Color[4]};
            mesh.RecalculateBounds();return mesh;
        }
    }
}
