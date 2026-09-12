using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public static class TrackRibbonBuilder
    {
        public static Mesh Build(TrackDefinition track,TrackVisualDefinition visual,string loopId)
        {
            if(track==null||visual==null||visual.TrackId!=track.Id)throw new ArgumentException("Matching track required.");var path=visual.Get(loopId);var loop=track.GetLoop(loopId);var sampler=new TrackSampler(loop);
            var distances=new SortedSet<double>{0,loop.Length.Meters};for(int i=0;i<path.Count;i++)distances.Add(path.Get(i).Distance);for(int i=0;i<loop.SegmentCount;i++)distances.Add(loop.GetSegment(i).StartDistance.Meters);
            int steps=(int)Math.Ceiling(loop.Length.Meters/visual.SampleSpacing);if(steps>100000)throw new ArgumentException("Visual mesh sample budget exceeded.");for(int i=1;i<steps;i++)distances.Add(loop.Length.Meters*i/steps);
            var vertices=new Vector3[distances.Count*2];var uv=new Vector2[vertices.Length];var triangles=new int[(distances.Count-1)*6];int row=0;
            foreach(double distance in distances)
            {
                var point=path.Sample(distance);double half=sampler.Sample(TrackDistance.FromMeters(Math.Round(distance,3,MidpointRounding.ToEven))).WidthMeters/2;
                var center=new Vector3((float)point.X,(float)point.Y,(float)point.Z);var side=new Vector3((float)point.ForwardZ,0,(float)-point.ForwardX)*(float)half;
                vertices[row*2]=center-side;vertices[row*2+1]=center+side;uv[row*2]=new Vector2(0,(float)distance);uv[row*2+1]=new Vector2(1,(float)distance);
                if(row>0){int t=(row-1)*6,a=(row-1)*2,b=row*2;triangles[t]=a;triangles[t+1]=b;triangles[t+2]=a+1;triangles[t+3]=a+1;triangles[t+4]=b;triangles[t+5]=b+1;}row++;
            }
            var mesh=new Mesh{name=visual.Id+"_"+loopId,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
