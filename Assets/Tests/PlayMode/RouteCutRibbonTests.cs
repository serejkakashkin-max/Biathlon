using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RouteCutRibbonTests
    {
        [Test]public void ActualRouteCutRowsAreExactAndEveryOldSampleIsRetained()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            Mesh original=null,cut=null;
            try
            {
                original=TrackRibbonBuilder.Build(plan.Track,plan.Visual,"main",out var before);
                cut=TrackRibbonBuilder.Build(plan.Track,plan.Visual,"main",out var after,new[]{139.43,180.273,139.43});
                Assert.That(Array.IndexOf(after,139.43),Is.GreaterThanOrEqualTo(0));Assert.That(Array.IndexOf(after,180.273),Is.GreaterThanOrEqualTo(0));
                var p=original.vertices;var q=cut.vertices;
                for(int i=0;i<before.Length;i++){int at=Array.IndexOf(after,before[i]);Assert.That(at,Is.GreaterThanOrEqualTo(0));Assert.That(q[at*2],Is.EqualTo(p[i*2]));Assert.That(q[at*2+1],Is.EqualTo(p[i*2+1]));}
                for(int i=1;i<after.Length;i++)Assert.That(after[i],Is.GreaterThan(after[i-1]));
                Assert.Throws<ArgumentException>(()=>TrackRibbonBuilder.Build(plan.Track,plan.Visual,"main",out _,new[]{double.NaN}));
            }
            finally{if(original!=null)Object.DestroyImmediate(original);if(cut!=null)Object.DestroyImmediate(cut);}
        }
    }
}
