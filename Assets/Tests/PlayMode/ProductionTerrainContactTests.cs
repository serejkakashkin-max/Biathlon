using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Cold geometric acceptance of actual rendered meshes, including their lateral edges.</summary>
    public class ProductionTerrainContactTests
    {
        internal readonly struct CellKey : IEquatable<CellKey>
        {
            public readonly int X, Z;
            public CellKey(int x, int z) { X = x; Z = z; }
            public bool Equals(CellKey other) => X == other.X && Z == other.Z;
            public override bool Equals(object value) => value is CellKey other && Equals(other);
            public override int GetHashCode() => unchecked(X * 397) ^ Z;
        }

        internal readonly struct Point
        {
            public readonly double X, Z;
            public Point(double x, double z) { X = x; Z = z; }
            public Point(Vector3 value) { X = value.x; Z = value.z; }
        }

        internal readonly struct Triangle
        {
            public readonly Vector3 A, B, C;
            public Triangle(Vector3 a, Vector3 b, Vector3 c) { A = a; B = b; C = c; }
            public double Area => Cross(new Point(A), new Point(B), new Point(C));
            public double Height(Point p)
            {
                double bx = (double)B.x - A.x, bz = (double)B.z - A.z;
                double cx = (double)C.x - A.x, cz = (double)C.z - A.z;
                double u = ((p.X - A.x) * cz - (p.Z - A.z) * cx) / Area;
                double v = (bx * (p.Z - A.z) - bz * (p.X - A.x)) / Area;
                return A.y + u * ((double)B.y - A.y) + v * ((double)C.y - A.y);
            }
        }

        internal struct TerrainCell
        {
            public Triangle First, Second;
            public int Count;
        }

        private static double Cross(Point a, Point b, Point c) =>
            (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);

        private static List<Point> Overlay(Triangle ribbon, Triangle terrain)
        {
            var polygon = new List<Point>(6) { new Point(ribbon.A), new Point(ribbon.B), new Point(ribbon.C) };
            var edges = new[] { new Point(terrain.A), new Point(terrain.B), new Point(terrain.C) };
            double sign = terrain.Area >= 0 ? 1 : -1;
            for (int edge = 0; edge < 3 && polygon.Count > 0; edge++)
            {
                var a = edges[edge]; var b = edges[(edge + 1) % 3];
                var result = new List<Point>(6); var prior = polygon[polygon.Count - 1];
                double previous = sign * Cross(a, b, prior);
                foreach (var current in polygon)
                {
                    double side = sign * Cross(a, b, current);
                    if ((previous >= 0) != (side >= 0))
                    {
                        double fraction = previous / (previous - side);
                        result.Add(new Point(prior.X + (current.X - prior.X) * fraction,
                            prior.Z + (current.Z - prior.Z) * fraction));
                    }
                    if (side >= 0) result.Add(current);
                    prior = current; previous = side;
                }
                polygon = result;
            }
            return polygon;
        }

        private static int Cell(double coordinate, double spacing) => (int)Math.Floor(coordinate / spacing);

        [Test] public void ExactOverlayNeverExtrapolatesAnAlmostOutsideClippedEdge()
        {
            // Exact float witness from the rotated-parent terrain fixture. Treating a
            // negative side as inside while intersecting at zero produces t>1 and false area.
            var shoulder=new Triangle(new Vector3(85.33332061767578f,0,90.66667938232422f),new Vector3(84.87867736816406f,0,89.12132263183594f),new Vector3(85.31298828125f,0,90.68701171875f));
            var terrain=new Triangle(new Vector3(79.99999237060547f,0,88.00001525878906f),new Vector3(79.99998474121094f,0,96),new Vector3(87.99999237060547f,0,88.00001525878906f));
            var polygon=Overlay(shoulder,terrain);double covered=0;
            for(int i=1;i<polygon.Count-1;i++)covered+=Math.Abs(Cross(polygon[0],polygon[i],polygon[i+1]));
            Assert.That(covered,Is.EqualTo(.04066467166852563).Within(1e-12));Assert.That(covered,Is.LessThanOrEqualTo(Math.Abs(shoulder.Area)));
            double minX=Math.Min(shoulder.A.x,Math.Min(shoulder.B.x,shoulder.C.x)),maxX=Math.Max(shoulder.A.x,Math.Max(shoulder.B.x,shoulder.C.x));
            double minZ=Math.Min(shoulder.A.z,Math.Min(shoulder.B.z,shoulder.C.z)),maxZ=Math.Max(shoulder.A.z,Math.Max(shoulder.B.z,shoulder.C.z));
            foreach(var point in polygon){Assert.That(point.X,Is.InRange(minX,maxX));Assert.That(point.Z,Is.InRange(minZ,maxZ));}
        }

        internal sealed class JoinedWorld
        {
            internal readonly double Spacing;
            internal readonly Vector2 Origin;
            internal readonly Dictionary<CellKey,TerrainCell> Cells;
            internal readonly Dictionary<CellKey,List<int>> RibbonCells;
            internal readonly List<Triangle> RibbonTriangles;
            internal int X(double p)=>(int)Math.Floor((p-Origin.x)/Spacing);
            internal int Z(double p)=>(int)Math.Floor((p-Origin.y)/Spacing);
            internal JoinedWorld(ProductionVenueEnvironment environment,ProductionCoursePresentation course)
            {
                double spacing=Spacing=environment.TerrainGridSpacing;var origin=Origin=environment.TerrainGridOrigin;
            var cells=new Dictionary<CellKey,TerrainCell>();var filters=environment.GetComponentsInChildren<MeshFilter>(true);
            for(int tile=0;tile<environment.TerrainTileCount;tile++)
            {
                var mesh=environment.TileMesh(tile);Transform owner=null;
                foreach(var filter in filters)if(filter.sharedMesh==mesh){owner=filter.transform;break;}
                Assert.That(owner,Is.Not.Null);var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<vertices.Length;i++)vertices[i]=owner.TransformPoint(vertices[i]);
                for(int i=0;i<indices.Length;i+=3)
                {
                    var triangle=new Triangle(vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]);
                    var key=new CellKey((int)Math.Round((Math.Min(triangle.A.x,Math.Min(triangle.B.x,triangle.C.x))-origin.x)/spacing),
                        (int)Math.Round((Math.Min(triangle.A.z,Math.Min(triangle.B.z,triangle.C.z))-origin.y)/spacing));
                    cells.TryGetValue(key,out var cell);Assert.That(cell.Count,Is.LessThan(2));
                    if(cell.Count==0)cell.First=triangle;else cell.Second=triangle;cell.Count++;cells[key]=cell;
                }
            }
            var ribbonCells=new Dictionary<CellKey,List<int>>();var ribbonTriangles=new List<Triangle>();
            for(int loop=0;loop<course.LoopCount;loop++)
            {
                var actual=course.LoopMesh(loop);var visible=actual.vertices;var ribbonFaces=actual.triangles;
                for(int i=0;i<ribbonFaces.Length;i+=3)
                {
                    var triangle=new Triangle(visible[ribbonFaces[i]],visible[ribbonFaces[i+1]],visible[ribbonFaces[i+2]]);
                    if(Math.Abs(triangle.Area)<1e-12)continue;int index=ribbonTriangles.Count;ribbonTriangles.Add(triangle);
                    for(int z=Z(Math.Min(triangle.A.z,Math.Min(triangle.B.z,triangle.C.z)));z<=Z(Math.Max(triangle.A.z,Math.Max(triangle.B.z,triangle.C.z)));z++)
                        for(int x=X(Math.Min(triangle.A.x,Math.Min(triangle.B.x,triangle.C.x)));x<=X(Math.Max(triangle.A.x,Math.Max(triangle.B.x,triangle.C.x)));x++)
                        {var key=new CellKey(x,z);if(!ribbonCells.TryGetValue(key,out var list)){list=new List<int>();ribbonCells.Add(key,list);}list.Add(index);}
                }
            }
                Cells=cells;RibbonCells=ribbonCells;RibbonTriangles=ribbonTriangles;
            }
            internal IEnumerable<Triangle> Over(Vector3 a,Vector3 b,Vector3 c)
            {
                for(int z=Z(Math.Min(a.z,Math.Min(b.z,c.z)))-1;z<=Z(Math.Max(a.z,Math.Max(b.z,c.z)))+1;z++)
                    for(int x=X(Math.Min(a.x,Math.Min(b.x,c.x)))-1;x<=X(Math.Max(a.x,Math.Max(b.x,c.x)))+1;x++)
                        if(Cells.TryGetValue(new CellKey(x,z),out var cell)){yield return cell.First;if(cell.Count==2)yield return cell.Second;}
            }
        }

        private static bool HiddenOriginalCurtain(Triangle face,Vector3[] source,bool closed,JoinedWorld world)
        {
            if(Math.Abs(face.Area)>1e-12)return false;
            var corners=new[]{face.A,face.B,face.C};var a=face.A;var b=face.B;double longest=0;
            for(int i=0;i<3;i++)for(int j=i+1;j<3;j++)
            {double dx=(double)corners[j].x-corners[i].x,dz=(double)corners[j].z-corners[i].z,length=dx*dx+dz*dz;if(length>longest){longest=length;a=corners[i];b=corners[j];}}
            if(longest<1e-12)return false;const double fractionTolerance=0;
            double Parameter(Vector3 p)=>(((double)p.x-a.x)*(b.x-a.x)+((double)p.z-a.z)*(b.z-a.z))/longest;
            var sourceIntervals=new List<(double,double)>();
            for(int side=0;side<2;side++)for(int row=side+2;row<source.Length;row+=2)Original(source[row-2],source[row]);
            if(!closed){Original(source[0],source[1]);Original(source[source.Length-2],source[source.Length-1]);}
            if(!Covered(sourceIntervals))return false;
            var left=new List<(double,double)>();var right=new List<(double,double)>();var seen=new HashSet<int>();
            for(int z=world.Z(Math.Min(a.z,b.z))-1;z<=world.Z(Math.Max(a.z,b.z))+1;z++)for(int x=world.X(Math.Min(a.x,b.x))-1;x<=world.X(Math.Max(a.x,b.x))+1;x++)
                if(world.RibbonCells.TryGetValue(new CellKey(x,z),out var candidates))foreach(int candidate in candidates)if(seen.Add(candidate))
                {
                    var ribbon=world.RibbonTriangles[candidate];if(!Interval(ribbon,out double low,out double high)||!Below(ribbon,low,high))continue;
                    // Actual triangle area on both sides is required over each complete
                    // centre-line interval; mere point contact cannot hide a face.
                    double first=Cross(new Point(a),new Point(b),new Point(ribbon.A)),second=Cross(new Point(a),new Point(b),new Point(ribbon.B)),third=Cross(new Point(a),new Point(b),new Point(ribbon.C));
                    if(Math.Max(first,Math.Max(second,third))>0)left.Add((low,high));
                    if(Math.Min(first,Math.Min(second,third))<0)right.Add((low,high));
                }
            return Covered(left)&&Covered(right);
            void Original(Vector3 first,Vector3 last)
            {
                double start=Parameter(first),end=Parameter(last);var delta=b-a;
                var firstProjected=new Vector3(a.x+delta.x*(float)start,first.y,a.z+delta.z*(float)start);
                var lastProjected=new Vector3(a.x+delta.x*(float)end,last.y,a.z+delta.z*(float)end);
                if(Vector3.Distance(first,firstProjected)>1e-4||Vector3.Distance(last,lastProjected)>1e-4)return;
                sourceIntervals.Add((Math.Max(0,Math.Min(start,end)),Math.Min(1,Math.Max(start,end))));
            }
            bool Interval(Triangle triangle,out double low,out double high,double offset=0)
            {
                double nx=-((double)b.z-a.z)/Math.Sqrt(longest),nz=((double)b.x-a.x)/Math.Sqrt(longest);
                var firstPoint=new Point(a.x+nx*offset,a.z+nz*offset);var lastPoint=new Point(b.x+nx*offset,b.z+nz*offset);
                low=0;high=1;var vertices=new[]{new Point(triangle.A),new Point(triangle.B),new Point(triangle.C)};double sign=triangle.Area>=0?1:-1;
                for(int i=0;i<3;i++)
                {
                    double first=sign*Cross(vertices[i],vertices[(i+1)%3],firstPoint),last=sign*Cross(vertices[i],vertices[(i+1)%3],lastPoint);
                    if(first<0&&last<0)return false;
                    if((first<0)!=(last<0)){double t=first/(first-last);if(first<0)low=Math.Max(low,t);else high=Math.Min(high,t);}
                }
                return low<=high;
            }
            bool Below(Triangle ribbon,double low,double high)
            {
                // On a vertical triangle each upper-envelope piece is an original edge.
                // Check every clipped edge endpoint, including the middle vertex break.
                for(int i=0;i<3;i++)
                {
                    var first=corners[i];var last=corners[(i+1)%3];double start=Parameter(first),end=Parameter(last);
                    if(Math.Abs(start-end)<1e-12)
                    {if(start>=low&&start<=high&&!Under(first,Math.Max(first.y,last.y)))return false;continue;}
                    double from=Math.Max(low,Math.Min(start,end)),to=Math.Min(high,Math.Max(start,end));if(from>to)continue;
                    foreach(double t in new[]{from,to})
                    {double fraction=(t-start)/(end-start);var p=new Vector3((float)(first.x+((double)last.x-first.x)*fraction),0,(float)(first.z+((double)last.z-first.z)*fraction));if(!Under(p,first.y+((double)last.y-first.y)*fraction))return false;}
                }
                return true;
                bool Under(Vector3 point,double height)=>height<=ribbon.Height(new Point(point))+1e-4;
            }
            bool Covered(List<(double,double)> intervals)
            {
                intervals.Sort((first,last)=>first.Item1.CompareTo(last.Item1));double end=0;
                foreach(var interval in intervals){if(interval.Item2<interval.Item1)continue;if(interval.Item1>end+fractionTolerance)return false;end=Math.Max(end,interval.Item2);}return end>=1-fractionTolerance;
            }
        }

        internal static void AssertJoinedGeometry(ProductionVenueEnvironment environment,ProductionCoursePresentation course,
            string loopId,Vector3[] ribbon,bool closed,JoinedWorld world=null)
        {
            world=world??new JoinedWorld(environment,course);
            var ribbonCells=world.RibbonCells;var ribbonTriangles=world.RibbonTriangles;
            int X(double p)=>world.X(p);int Z(double p)=>world.Z(p);
            IEnumerable<Triangle> Over(Vector3 a,Vector3 b,Vector3 c)=>world.Over(a,b,c);
            var join=course.SkirtMesh(loopId);var points=join.vertices;var faces=join.triangles;var bounds=join.bounds;bounds.Expand(2e-4f);
            Assert.That(faces.Length,Is.GreaterThan(0));Assert.That(faces.Length%3,Is.Zero);
            double minimum=double.PositiveInfinity;long overlayVertices=0;
            // The clearance difference is affine on each overlap polygon. Its extrema
            // occur at these vertices, so this proves the whole face, not just mesh samples.
            for(int i=0;i<faces.Length;i+=3)
            {
                var shoulder=new Triangle(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
                var faceNormal=Vector3.Cross(shoulder.B-shoulder.A,shoulder.C-shoulder.A);
                Assert.That(float.IsFinite(faceNormal.x)&&float.IsFinite(faceNormal.y)&&float.IsFinite(faceNormal.z)&&faceNormal.sqrMagnitude>=1e-16f,Is.True,
                    "Every retained join face must be finite and nondegenerate: "+loopId+" face "+(i/3)+" "+shoulder.A+" / "+shoulder.B+" / "+shoulder.C+" cross="+faceNormal+" square="+faceNormal.sqrMagnitude.ToString("R",CultureInfo.InvariantCulture));
                if(Math.Abs(shoulder.Area)<1e-12)
                {
                    // A vertical triangle projects to a line. On every real terrain triangle,
                    // its lower envelope is one of its edges; checking all three clipped edges
                    // proves clearance throughout the complete face, including grid breaks.
                    AssertClearLine(shoulder.A,shoulder.B);AssertClearLine(shoulder.B,shoulder.C);AssertClearLine(shoulder.C,shoulder.A);
                    continue;
                }
                Assert.That(faceNormal.y,Is.GreaterThan(0),"A nonvertical shoulder face must face up.");
                double coverage=0;
                foreach(var terrain in Over(shoulder.A,shoulder.B,shoulder.C))
                {
                    var polygon=Overlay(shoulder,terrain);
                    for(int vertex=1;vertex<polygon.Count-1;vertex++)coverage+=Math.Abs(Cross(polygon[0],polygon[vertex],polygon[vertex+1]));
                    foreach(var point in polygon)
                    {
                        double gap=shoulder.Height(point)-terrain.Height(point);minimum=Math.Min(minimum,gap);overlayVertices++;
                        Assert.That(double.IsFinite(gap)&&gap>=-1e-4,Is.True,"Actual terrain buries a complete shoulder face at "+loopId+" / "+(i/3));
                    }
                }
                Assert.That(coverage,Is.EqualTo(Math.Abs(shoulder.Area)).Within(Math.Max(1e-6,Math.Abs(shoulder.Area)*1e-5)),"No projected shoulder face may leave actual terrain coverage.");
                var checkedRibbon=new HashSet<int>();
                for(int z=Z(Math.Min(shoulder.A.z,Math.Min(shoulder.B.z,shoulder.C.z)));z<=Z(Math.Max(shoulder.A.z,Math.Max(shoulder.B.z,shoulder.C.z)));z++)
                    for(int x=X(Math.Min(shoulder.A.x,Math.Min(shoulder.B.x,shoulder.C.x)));x<=X(Math.Max(shoulder.A.x,Math.Max(shoulder.B.x,shoulder.C.x)));x++)
                        if(ribbonCells.TryGetValue(new CellKey(x,z),out var candidates))foreach(int candidate in candidates)if(checkedRibbon.Add(candidate))
                        {
                            var overlap=Overlay(shoulder,ribbonTriangles[candidate]);double intrusion=0;
                            for(int vertex=1;vertex<overlap.Count-1;vertex++)intrusion+=Math.Abs(Cross(overlap[0],overlap[vertex],overlap[vertex+1]));
                            double perimeter=Vector3.Distance(shoulder.A,shoulder.B)+Vector3.Distance(shoulder.B,shoulder.C)+Vector3.Distance(shoulder.C,shoulder.A);
                            Assert.That(intrusion,Is.LessThanOrEqualTo(Math.Max(1e-6,perimeter*1e-4)),"Snow shoulders must stay outside the existing groomed ribbon within the float contact tolerance.");
                        }
            }
            foreach(var point in points)Assert.That(float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z)&&bounds.Contains(point),Is.True);
            foreach(var normal in join.normals)Assert.That(float.IsFinite(normal.x)&&float.IsFinite(normal.y)&&float.IsFinite(normal.z)&&normal.y>=-1e-5f&&normal.sqrMagnitude>.99f,Is.True);
            Assert.That(overlayVertices,Is.GreaterThan(0));

            // Canonicalize position seams independently of the production mesh's UV/index
            // policy, then require every exposed edge to meet either ribbon or actual ground.
            var canonical=new List<Vector3>();var buckets=new Dictionary<(long,long,long),List<int>>();var remap=new int[points.Length];
            const double weld=1e-4;
            for(int i=0;i<points.Length;i++)
            {
                var point=points[i];long x=(long)Math.Floor(point.x/weld),y=(long)Math.Floor(point.y/weld),z=(long)Math.Floor(point.z/weld);int found=-1;
                for(long dz=-1;dz<=1&&found<0;dz++)for(long dy=-1;dy<=1&&found<0;dy++)for(long dx=-1;dx<=1&&found<0;dx++)
                    if(buckets.TryGetValue((x+dx,y+dy,z+dz),out var candidates))foreach(int candidate in candidates)
                        if(Vector3.Distance(point,canonical[candidate])<=weld){found=candidate;break;}
                if(found<0){found=canonical.Count;canonical.Add(point);if(!buckets.TryGetValue((x,y,z),out var list)){list=new List<int>();buckets.Add((x,y,z),list);}list.Add(found);}
                remap[i]=found;
            }
            var edges=new Dictionary<(int,int),int>();
            var edgeFaces=new Dictionary<(int,int),List<Vector3>>();
            var rawEdges=new Dictionary<(int,int),(Vector3,Vector3)>();
            var hiddenCurtainEdges=new List<(Vector3,Vector3)>();
            var shoulderCells=new Dictionary<CellKey,List<Triangle>>();
            for(int i=0;i<faces.Length;i+=3)
            {
                var face=new Triangle(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
                for(int z=Z(Math.Min(face.A.z,Math.Min(face.B.z,face.C.z)));z<=Z(Math.Max(face.A.z,Math.Max(face.B.z,face.C.z)));z++)
                    for(int x=X(Math.Min(face.A.x,Math.Min(face.B.x,face.C.x)));x<=X(Math.Max(face.A.x,Math.Max(face.B.x,face.C.x)));x++)
                    {var key=new CellKey(x,z);if(!shoulderCells.TryGetValue(key,out var list)){list=new List<Triangle>();shoulderCells.Add(key,list);}list.Add(face);}
            }
            for(int i=0;i<faces.Length;i+=3)
            {
                var actualFace=new Triangle(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
                if(HiddenOriginalCurtain(actualFace,ribbon,closed,world))
                {
                    hiddenCurtainEdges.Add((actualFace.A,actualFace.B));hiddenCurtainEdges.Add((actualFace.B,actualFace.C));hiddenCurtainEdges.Add((actualFace.C,actualFace.A));continue;
                }
                if(remap[faces[i]]==remap[faces[i+1]]||remap[faces[i+1]]==remap[faces[i+2]]||remap[faces[i+2]]==remap[faces[i]])continue;
                for(int side=0;side<3;side++)
                {
                    int a=remap[faces[i+side]],b=remap[faces[i+(side+1)%3]];var key=(Math.Min(a,b),Math.Max(a,b));edges.TryGetValue(key,out int count);edges[key]=count+1;rawEdges[key]=(points[faces[i+side]],points[faces[i+(side+1)%3]]);
                    if(!edgeFaces.TryGetValue(key,out var list)){list=new List<Vector3>();edgeFaces.Add(key,list);}list.Add(canonical[remap[faces[i+(side+2)%3]]]);
                }
            }
            var edgeCells=new Dictionary<CellKey,List<(int,int)>>();
            foreach(var edge in edgeFaces)
            {
                var a=canonical[edge.Key.Item1];var b=canonical[edge.Key.Item2];
                for(int z=Z(Math.Min(a.z,b.z));z<=Z(Math.Max(a.z,b.z));z++)for(int x=X(Math.Min(a.x,b.x));x<=X(Math.Max(a.x,b.x));x++)
                {var cell=new CellKey(x,z);if(!edgeCells.TryGetValue(cell,out var list)){list=new List<(int,int)>();edgeCells.Add(cell,list);}list.Add(edge.Key);}
            }
            IEnumerable<(int,int)> NearbyEdges(Vector3 a,Vector3 b)
            {
                var seen=new HashSet<(int,int)>();
                for(int z=Z(Math.Min(a.z,b.z))-1;z<=Z(Math.Max(a.z,b.z))+1;z++)for(int x=X(Math.Min(a.x,b.x))-1;x<=X(Math.Max(a.x,b.x))+1;x++)
                    if(edgeCells.TryGetValue(new CellKey(x,z),out var list))foreach(var key in list)if(seen.Add(key))yield return key;
            }
            var topEdges=new List<(Vector3,Vector3)>();int groundEdges=0;
            foreach(var edge in edges)
            {
                Assert.That(edge.Value,Is.LessThanOrEqualTo(2),"A snow join must not publish duplicate/non-manifold faces: "+loopId+" "+canonical[edge.Key.Item1]+" / "+canonical[edge.Key.Item2]);if(edge.Value!=1)continue;
                var a=rawEdges[edge.Key].Item1;var b=rawEdges[edge.Key].Item2;
                if(OnGroomedLine(a,b))
                    topEdges.Add((a,b));
                else if(!RetainedHiddenCurtainCovers(a,b)&&!InteriorGroomedCoversVerticalEdge(a,b)&&!HasOppositeFaceCoverage(edge.Key,a,b)&&!HasHigherShoulderCoverage(a,b)){AssertGroundLine(a,b);groundEdges++;}
            }
            foreach(var edge in hiddenCurtainEdges)if(OnGroomedLine(edge.Item1,edge.Item2))topEdges.Add(edge);
            Assert.That(topEdges.Count,Is.GreaterThan(0));Assert.That(groundEdges,Is.GreaterThan(0));
            var topCells=new Dictionary<CellKey,List<(Vector3,Vector3)>>();
            foreach(var edge in topEdges)
                for(int z=Z(Math.Min(edge.Item1.z,edge.Item2.z));z<=Z(Math.Max(edge.Item1.z,edge.Item2.z));z++)for(int x=X(Math.Min(edge.Item1.x,edge.Item2.x));x<=X(Math.Max(edge.Item1.x,edge.Item2.x));x++)
                {var key=new CellKey(x,z);if(!topCells.TryGetValue(key,out var list)){list=new List<(Vector3,Vector3)>();topCells.Add(key,list);}list.Add(edge);}
            for(int side=0;side<2;side++)for(int row=side+2;row<ribbon.Length;row+=2)AssertRibbonLine(ribbon[row-2],ribbon[row]);
            if(!closed){AssertRibbonLine(ribbon[0],ribbon[1]);AssertRibbonLine(ribbon[ribbon.Length-2],ribbon[ribbon.Length-1]);}
            TestContext.WriteLine("{\"loop\":\""+loopId+"\",\"shoulder_triangles\":"+(faces.Length/3)+",\"overlay_vertices\":"+overlayVertices+
                ",\"minimum_shouldergap_m\":"+minimum.ToString("R",CultureInfo.InvariantCulture)+",\"ribbon_edges\":"+topEdges.Count+",\"ground_edges\":"+groundEdges+"}");

            bool HasOppositeFaceCoverage((int,int) key,Vector3 a,Vector3 b)
            {
                var intervals=new List<(double,double)>();var delta=b-a;double length=delta.sqrMagnitude;
                double fractionTolerance=2e-4/Math.Sqrt(length);
                if(length<1e-12)return true;var side=Vector3.Cross(delta,edgeFaces[key][0]-a);
                foreach(var otherKey in NearbyEdges(a,b))
                {
                    if(otherKey.Equals(key))continue;var first=canonical[otherKey.Item1];var last=canonical[otherKey.Item2];
                    double start=Vector3.Dot(first-a,delta)/length,end=Vector3.Dot(last-a,delta)/length;
                    double low=Math.Max(0,Math.Min(start,end)),high=Math.Min(1,Math.Max(start,end));if(low>high||Math.Abs(end-start)<1e-12)continue;
                    // Compare only the shared interval: extrapolating a tiny clipped edge
                    // to a distant original endpoint amplifies float rounding arbitrarily.
                    var otherDelta=last-first;
                    if(Vector3.Distance(a+delta*(float)low,first+otherDelta*(float)((low-start)/(end-start)))>2e-4||
                        Vector3.Distance(a+delta*(float)high,first+otherDelta*(float)((high-start)/(end-start)))>2e-4)continue;
                    bool opposite=false;foreach(var third in edgeFaces[otherKey])if(Vector3.Dot(side,Vector3.Cross(delta,third-a))<0){opposite=true;break;}
                    if(opposite)intervals.Add((Math.Max(0,Math.Min(start,end)),Math.Min(1,Math.Max(start,end))));
                }
                intervals.Sort((first,last)=>first.Item1.CompareTo(last.Item1));double covered=0;
                foreach(var interval in intervals){if(interval.Item1>covered+fractionTolerance)return false;covered=Math.Max(covered,interval.Item2);}return covered>=1-fractionTolerance;
            }
            bool HasHigherShoulderCoverage(Vector3 a,Vector3 b)
            {
                var intervals=new List<(double,double)>();
                double fractionTolerance=2e-4/Vector3.Distance(a,b);
                for(int z=Z(Math.Min(a.z,b.z))-1;z<=Z(Math.Max(a.z,b.z))+1;z++)for(int x=X(Math.Min(a.x,b.x))-1;x<=X(Math.Max(a.x,b.x))+1;x++)
                    if(shoulderCells.TryGetValue(new CellKey(x,z),out var candidates))foreach(var face in candidates)
                    {
                        if(Math.Abs(face.Area)<1e-12)continue;
                        if(!LineInterval(a,b,face,out double low,out double high)||high-low<1e-9)continue;
                        double Difference(double t)=>face.Height(new Point(a.x+((double)b.x-a.x)*t,a.z+((double)b.z-a.z)*t))-(a.y+((double)b.y-a.y)*t);
                        double first=Difference(low),last=Difference(high);if(first< -1e-4&&last< -1e-4)continue;
                        if((first< -1e-4)!=(last< -1e-4))
                        {double t=low+(high-low)*(first+1e-4)/(first-last);if(first< -1e-4)low=t;else high=t;}
                        var at=new Point(a.x+((double)b.x-a.x)*(low+high)*.5,a.z+((double)b.z-a.z)*(low+high)*.5);double sign=face.Area>=0?1:-1;
                        bool interior=sign*Cross(new Point(face.A),new Point(face.B),at)>1e-7&&sign*Cross(new Point(face.B),new Point(face.C),at)>1e-7&&sign*Cross(new Point(face.C),new Point(face.A),at)>1e-7;
                        if(interior||Math.Max(first,last)>1e-4)intervals.Add((low,high));
                    }
                intervals.Sort((first,last)=>first.Item1.CompareTo(last.Item1));double covered=0;
                foreach(var interval in intervals){if(interval.Item1>covered+fractionTolerance)return false;covered=Math.Max(covered,interval.Item2);}return covered>=1-fractionTolerance;
            }

            void AssertRibbonLine(Vector3 a,Vector3 b)
            {
                var delta=b-a;double length=delta.sqrMagnitude;if(length<1e-12)return;var intervals=new List<(double,double)>();
                var checkedEdges=new HashSet<(Vector3,Vector3)>();
                for(int z=Z(Math.Min(a.z,b.z))-1;z<=Z(Math.Max(a.z,b.z))+1;z++)for(int x=X(Math.Min(a.x,b.x))-1;x<=X(Math.Max(a.x,b.x))+1;x++)
                if(topCells.TryGetValue(new CellKey(x,z),out var candidates))foreach(var edge in candidates)if(checkedEdges.Add(edge))
                {
                    double first=Vector3.Dot(edge.Item1-a,delta)/length,last=Vector3.Dot(edge.Item2-a,delta)/length;
                    if(Vector3.Distance(edge.Item1,a+delta*(float)first)>2e-4||Vector3.Distance(edge.Item2,a+delta*(float)last)>2e-4)continue;
                    intervals.Add((Math.Max(0,Math.Min(first,last)),Math.Min(1,Math.Max(first,last))));
                }
                // Inside bends and connected loops may cover a nominal source edge on
                // both sides. Only those exact covered intervals waive an external join.
                var left=new List<(double,double)>();var right=new List<(double,double)>();
                foreach(var triangle in NearbyRibbon(a,b))
                {
                    if(!LineInterval(a,b,triangle,out double low,out double high)||!MatchesRibbonHeight(a,b,triangle,low,high))continue;
                    double sign=Cross(new Point(a),new Point(b),new Point((triangle.A+triangle.B+triangle.C)/3));
                    if(sign>1e-7)left.Add((low,high));else if(sign< -1e-7)right.Add((low,high));
                }
                foreach(var first in left)foreach(var last in right)
                {double low=Math.Max(first.Item1,last.Item1),high=Math.Min(first.Item2,last.Item2);if(high>=low)intervals.Add((low,high));}
                AssertCovered(intervals,"Every exact ribbon boundary segment must have a continuous join: "+loopId,2e-4/Math.Sqrt(length));
            }
            IEnumerable<Triangle> NearbyRibbon(Vector3 a,Vector3 b)
            {
                var seen=new HashSet<int>();
                for(int z=Z(Math.Min(a.z,b.z))-1;z<=Z(Math.Max(a.z,b.z))+1;z++)for(int x=X(Math.Min(a.x,b.x))-1;x<=X(Math.Max(a.x,b.x))+1;x++)
                    if(ribbonCells.TryGetValue(new CellKey(x,z),out var candidates))foreach(int candidate in candidates)if(seen.Add(candidate))yield return ribbonTriangles[candidate];
            }
            bool MatchesRibbonHeight(Vector3 a,Vector3 b,Triangle triangle,double low,double high)
            {
                foreach(double t in new[]{low,high})
                    if(Math.Abs(a.y+((double)b.y-a.y)*t-triangle.Height(new Point(a.x+((double)b.x-a.x)*t,a.z+((double)b.z-a.z)*t)))>1e-4)return false;
                return true;
            }
            bool RetainedHiddenCurtainCovers(Vector3 a,Vector3 b)
            {
                // Excluding a fully hidden original curtain from exposed topology must
                // not invent a boundary at its retained neighboring face. Use raw edges,
                // exact collinearity and opposite incidence; canonical welding never
                // supplies this contact proof, and partial/high curtains never enter it.
                double dx=(double)b.x-a.x,dy=(double)b.y-a.y,dz=(double)b.z-a.z;
                double length=dx*dx+dy*dy+dz*dz;if(length==0)return false;
                var intervals=new List<(double,double)>();
                foreach(var edge in hiddenCurtainEdges)
                {
                    double ex=(double)edge.Item2.x-edge.Item1.x,ey=(double)edge.Item2.y-edge.Item1.y,ez=(double)edge.Item2.z-edge.Item1.z;
                    if(dx*ex+dy*ey+dz*ez>=0)continue;
                    if(!Collinear(edge.Item1)||!Collinear(edge.Item2))continue;
                    double first=Parameter(edge.Item1),last=Parameter(edge.Item2);
                    intervals.Add((Math.Max(0,Math.Min(first,last)),Math.Min(1,Math.Max(first,last))));
                }
                intervals.Sort((first,last)=>first.Item1.CompareTo(last.Item1));double end=0;
                foreach(var interval in intervals){if(interval.Item2<interval.Item1)continue;if(interval.Item1>end)return false;end=Math.Max(end,interval.Item2);}return end>=1;
                bool Collinear(Vector3 p)
                {
                    double x=(double)p.x-a.x,y=(double)p.y-a.y,z=(double)p.z-a.z;
                    return dy*z-dz*y==0&&dz*x-dx*z==0&&dx*y-dy*x==0;
                }
                double Parameter(Vector3 p)=>(((double)p.x-a.x)*dx+((double)p.y-a.y)*dy+((double)p.z-a.z)*dz)/length;
            }
            bool InteriorGroomedCoversVerticalEdge(Vector3 a,Vector3 b)
            {
                // One exact projected point describes this entire vertical edge. Prove that
                // the union's tangent cones cover every direction, including shared triangle
                // diagonals, and that its height covers both endpoints. Boundary cones cannot
                // qualify unless other actual triangles complete the whole neighborhood.
                if(a.x!=b.x||a.z!=b.z)return false;var p=new Point(a);var cones=new List<List<(double,double)>>();var angles=new SortedSet<double>{0,Math.PI*2};
                foreach(var triangle in NearbyRibbon(a,b))
                {
                    if(Math.Max(a.y,b.y)>triangle.Height(p)+1e-4)continue;
                    double sign=triangle.Area>=0?1:-1;var corners=new[]{new Point(triangle.A),new Point(triangle.B),new Point(triangle.C)};var cone=new List<(double,double)>();bool contains=true;
                    for(int side=0;side<3;side++)
                    {
                        var first=corners[side];var last=corners[(side+1)%3];double value=sign*Cross(first,last,p);
                        if(value<0){contains=false;break;}
                        if(value==0)
                        {
                            double dx=last.X-first.X,dz=last.Z-first.Z;cone.Add((-sign*dz,sign*dx));
                            double angle=Math.Atan2(dz,dx);if(angle<0)angle+=Math.PI*2;angles.Add(angle);angles.Add((angle+Math.PI)%(Math.PI*2));
                        }
                    }
                    if(contains){if(cone.Count==0)return true;cones.Add(cone);}
                }
                var cuts=new List<double>(angles);
                for(int i=1;i<cuts.Count;i++)
                {
                    double angle=(cuts[i-1]+cuts[i])*.5,x=Math.Cos(angle),z=Math.Sin(angle);bool covered=false;
                    foreach(var cone in cones)
                    {bool includes=true;foreach(var halfplane in cone)if(halfplane.Item1*x+halfplane.Item2*z<0){includes=false;break;}if(includes){covered=true;break;}}
                    if(!covered)return false;
                }
                return cones.Count>0;
            }
            bool OnGroomedLine(Vector3 a,Vector3 b)
            {
                var intervals=new List<(double,double)>();
                foreach(var triangle in NearbyRibbon(a,b))if(LineInterval(a,b,triangle,out double low,out double high,1e-4)&&MatchesRibbonHeight(a,b,triangle,low,high))intervals.Add((low,high));
                intervals.Sort((first,last)=>first.Item1.CompareTo(last.Item1));double covered=0,tolerance=2e-4/Math.Max(2e-4,Vector3.Distance(a,b));
                foreach(var interval in intervals){if(interval.Item1>covered+tolerance)return false;covered=Math.Max(covered,interval.Item2);}return covered>=1-tolerance;
            }
            bool LineInterval(Vector3 a,Vector3 b,Triangle triangle,out double low,out double high,double contactDistance=0)
            {
                low=0;high=1;var p=new Point(a);var q=new Point(b);double sign=triangle.Area>=0?1:-1;
                var corners=new[]{new Point(triangle.A),new Point(triangle.B),new Point(triangle.C)};
                for(int side=0;side<3;side++)
                {
                    double first=sign*Cross(corners[side],corners[(side+1)%3],p),last=sign*Cross(corners[side],corners[(side+1)%3],q);
                    var edgeA=corners[side];var edgeB=corners[(side+1)%3];double tolerance=1e-7+contactDistance*Math.Sqrt((edgeB.X-edgeA.X)*(edgeB.X-edgeA.X)+(edgeB.Z-edgeA.Z)*(edgeB.Z-edgeA.Z));
                    if(first< -tolerance&&last< -tolerance)return false;
                    if(first<0&&first>=-tolerance)first=0;if(last<0&&last>=-tolerance)last=0;
                    if((first<0)!=(last<0)){double t=first/(first-last);if(first<0)low=Math.Max(low,t);else high=Math.Min(high,t);}
                }
                return low<=high+1e-9;
            }
            void AssertClearLine(Vector3 a,Vector3 b)
            {
                var intervals=new List<(double,double)>();
                foreach(var terrain in Over(a,b,b))
                {
                    if(!LineInterval(a,b,terrain,out double low,out double high))continue;
                    foreach(double t in new[]{low,high})
                    {
                        var at=new Point(a.x+((double)b.x-a.x)*t,a.z+((double)b.z-a.z)*t);
                        double gap=a.y+((double)b.y-a.y)*t-terrain.Height(at);minimum=Math.Min(minimum,gap);overlayVertices++;
                        Assert.That(double.IsFinite(gap)&&gap>=-1e-4,Is.True,"Actual terrain buries a complete retained vertical face: "+loopId);
                    }
                    intervals.Add((low,high));
                }
                AssertCovered(intervals,"Every retained vertical face must have complete actual terrain coverage.",2e-4/Math.Max(2e-4,Vector3.Distance(a,b)));
            }
            void AssertGroundLine(Vector3 a,Vector3 b)
            {
                var intervals=new List<(double,double)>();var p=new Point(a);var q=new Point(b);
                foreach(var terrain in Over(a,b,b))
                {
                    double low=0,high=1;bool covered=true;double sign=terrain.Area>=0?1:-1;
                    var corners=new[]{new Point(terrain.A),new Point(terrain.B),new Point(terrain.C)};
                    for(int side=0;side<3;side++)
                    {
                        double start=sign*Cross(corners[side],corners[(side+1)%3],p),end=sign*Cross(corners[side],corners[(side+1)%3],q);
                        if(start< -1e-7&&end< -1e-7){covered=false;break;}
                        if((start<0)!=(end<0)){double t=start/(start-end);if(start<0)low=Math.Max(low,t);else high=Math.Min(high,t);}
                    }
                    if(!covered||low>high+1e-9)continue;
                    foreach(double t in new[]{low,high})
                    {
                        var at=new Point(p.X+(q.X-p.X)*t,p.Z+(q.Z-p.Z)*t);double height=a.y+((double)b.y-a.y)*t;
                        Assert.That(height,Is.EqualTo(terrain.Height(at)).Within(1e-4),"Every exposed snow foot must meet the entire actual terrain segment, including diagonal breaks: "+loopId+" "+a+" / "+b);
                    }
                    intervals.Add((low,high));
                }
                AssertCovered(intervals,"Every exposed snow foot must have actual terrain beneath it.",2e-4/Math.Max(2e-4,Vector3.Distance(a,b)));
            }
            void AssertCovered(List<(double,double)> intervals,string reason,double tolerance)
            {
                intervals.Sort((a,b)=>a.Item1.CompareTo(b.Item1));double end=0;
                foreach(var interval in intervals){if(interval.Item2<interval.Item1)continue;Assert.That(interval.Item1,Is.LessThanOrEqualTo(end+tolerance),reason);end=Math.Max(end,interval.Item2);}
                Assert.That(end,Is.GreaterThanOrEqualTo(1-tolerance),reason);
            }
        }
        private static Mesh TestRibbon(params Vector3[] points)
        {
            var indices=new List<int>();for(int row=2;row<points.Length;row+=2)indices.AddRange(new[]{row-2,row,row-1,row-1,row,row+1});
            var mesh=new Mesh{name="Bounded shoulder contact fixture",vertices=points,triangles=indices.ToArray()};mesh.RecalculateBounds();return mesh;
        }
        private static ProductionVenueEnvironment TestEnvironment(GameObject root,Func<Vector3,float> height=null)
        {
            var fixture=new ProductionEnvironmentFixture(height??(p=>-.35f));var cameraObject=new GameObject("Contact fixture camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;var feed=root.AddComponent<RacePresentationFeed>();
            var environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);return environment;
        }
        private static Mesh PreparedContact(ProductionVenueEnvironment environment,Mesh[] ribbons,bool closed,out int slopes,out int walls)
        {
            // Exercise the native cold mesh builder with deliberately adversarial geometry.
            // Reflection keeps fixture injection out of the production API.
            const BindingFlags hidden=BindingFlags.NonPublic;var owner=typeof(ProductionCoursePresentation);
            object ground=Activator.CreateInstance(owner.GetNestedType("JoinGroundSurface",hidden),hidden|BindingFlags.Instance,null,
                new object[]{environment,environment.TerrainGridSpacing,environment.TerrainGridOrigin},null);
            object groomed=Activator.CreateInstance(owner.GetNestedType("JoinRibbonSurface",hidden),hidden|BindingFlags.Instance,null,
                new object[]{ribbons,environment.TerrainGridSpacing,environment.TerrainGridOrigin},null);
            var arguments=new object[]{ribbons[0],closed,ground,groomed,TerrainBlendDefinition.Load().edge_width_m,0f,0,0};
            var mesh=(Mesh)owner.GetMethod("BuildSkirt",hidden|BindingFlags.Static).Invoke(null,arguments);slopes=(int)arguments[6];walls=(int)arguments[7];return mesh;
        }
        private static ProductionCoursePresentation AssertPreparedContact(GameObject root,ProductionVenueEnvironment environment,Mesh[] ribbons,Mesh join,bool closed)
        {
            var holder=new GameObject("Prepared contact proof");holder.transform.SetParent(root.transform,false);var course=holder.AddComponent<ProductionCoursePresentation>();
            const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(ProductionCoursePresentation);
            type.GetField("meshes",hidden).SetValue(course,ribbons);type.GetField("skirts",hidden).SetValue(course,new[]{join});
            var ids=new string[ribbons.Length];for(int i=0;i<ids.Length;i++)ids[i]="fixture_"+i;type.GetField("loopIds",hidden).SetValue(course,ids);
            ((Dictionary<string,int>)type.GetField("indices",hidden).GetValue(course)).Add(ids[0],0);
            AssertJoinedGeometry(environment,course,ids[0],ribbons[0].vertices,closed);return course;
        }
        [UnityTest] public IEnumerator StraightShouldersKeepRecipeWidthAndCloseBothOpenEnds()
        {
            var root=new GameObject("Straight shoulder whole-face test");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(-2,0,16),new Vector3(2,0,16));
                var original=ribbon.vertices;join=PreparedContact(environment,new[]{ribbon},false,out int slopes,out int walls);
                Assert.That(slopes,Is.EqualTo(2));Assert.That(walls,Is.EqualTo(2));AssertPreparedContact(root,environment,new[]{ribbon},join,false);
                float width=TerrainBlendDefinition.Load().edge_width_m;Assert.That(join.bounds.min.x,Is.EqualTo(-2-width).Within(1e-5));Assert.That(join.bounds.max.x,Is.EqualTo(2+width).Within(1e-5));
                var vertices=join.vertices;var faces=join.triangles;int front=0,back=0;
                for(int i=0;i<faces.Length;i+=3)
                {
                    var a=vertices[faces[i]];var b=vertices[faces[i+1]];var c=vertices[faces[i+2]];var normal=Vector3.Cross(b-a,c-a);
                    if(a.z==0&&b.z==0&&c.z==0){Assert.That(normal.z,Is.LessThan(0),"Open and shoulder transition fronts must face outward.");front++;}
                    if(a.z==16&&b.z==16&&c.z==16){Assert.That(normal.z,Is.GreaterThan(0),"Open and shoulder transition backs must face outward.");back++;}
                }
                Assert.That(front,Is.GreaterThan(2));Assert.That(back,Is.GreaterThan(2));
                CollectionAssert.AreEqual(original,ribbon.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator StraightToBendTransitionsKeepFullTerrainContactWithoutHoles()
        {
            var root=new GameObject("Shoulder transition whole-face test");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(-2,0,-16),new Vector3(2,0,-16),new Vector3(-2,0,-8),new Vector3(2,0,-8),
                    new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(2,0,8),new Vector3(6,0,8));
                var original=ribbon.vertices;join=PreparedContact(environment,new[]{ribbon},false,out int slopes,out int walls);
                Assert.That(slopes,Is.GreaterThan(0));Assert.That(walls,Is.GreaterThan(2));AssertPreparedContact(root,environment,new[]{ribbon},join,false);
                CollectionAssert.AreEqual(original,ribbon.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator OtherGroomedLoopRejectsAnIntrudingShoulderWithoutChangingEitherRibbon()
        {
            var root=new GameObject("Venue-wide shoulder exclusion test");ProductionVenueEnvironment environment=null;Mesh ribbon=null,other=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(-2,0,16),new Vector3(2,0,16));
                other=TestRibbon(new Vector3(2.4f,0,6),new Vector3(6.4f,0,6),new Vector3(2.4f,0,10),new Vector3(6.4f,0,10));
                var original=ribbon.vertices;var otherOriginal=other.vertices;var ribbons=new[]{ribbon,other};join=PreparedContact(environment,ribbons,false,out int slopes,out int walls);
                Assert.That(slopes,Is.EqualTo(1));Assert.That(walls,Is.EqualTo(3));AssertPreparedContact(root,environment,ribbons,join,false);
                Assert.That(join.bounds.max.x,Is.EqualTo(2),"The whole intruding right panel must retain the original join.");
                CollectionAssert.AreEqual(original,ribbon.vertices);CollectionAssert.AreEqual(otherOriginal,other.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(other);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator TinyTangentChangesRejectUnequalOuterSeamsAndRetainWholeJoins()
        {
            var root=new GameObject("Tiny turn shoulder continuity regression");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(-2,0,-16),new Vector3(2,0,-16),new Vector3(-2,0,-8),new Vector3(2,0,-8),
                    new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(-1.992f,0,8),new Vector3(2.008f,0,8));
                var original=ribbon.vertices;join=PreparedContact(environment,new[]{ribbon},false,out int slopes,out int walls);
                Assert.That(slopes,Is.EqualTo(2),"Only the first exact straight panel on each side may taper.");
                Assert.That(walls,Is.EqualTo(6),"Both incident panels at an unequal outer seam retain their original joins.");
                AssertPreparedContact(root,environment,new[]{ribbon},join,false);CollectionAssert.AreEqual(original,ribbon.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator TerrainBoundaryRejectsAnUncoveredShoulderAndKeepsCompleteOriginalJoin()
        {
            var root=new GameObject("Bounded shoulder terrain coverage fallback");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(123.75f,0,0),new Vector3(127.75f,0,0),new Vector3(123.75f,0,16),new Vector3(127.75f,0,16));
                var original=ribbon.vertices;join=PreparedContact(environment,new[]{ribbon},false,out int slopes,out int walls);
                Assert.That(slopes,Is.EqualTo(1));Assert.That(walls,Is.EqualTo(3),"The incomplete outside footprint falls back before sampling any unavailable ground.");
                Assert.That(join.bounds.max.x,Is.EqualTo(127.75f));AssertPreparedContact(root,environment,new[]{ribbon},join,false);
                CollectionAssert.AreEqual(original,ribbon.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }

        [UnityTest] public IEnumerator ConvexTerrainRejectsABuriedTransitionCapWhileOriginalRibbonRemainsClear()
        {
            var root=new GameObject("Convex terrain transition cap regression");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root,p=>p.x==0&&p.z==0?-.03f:-3);
                ribbon=TestRibbon(new Vector3(.4f,0,0),new Vector3(4.4f,0,0),new Vector3(.4f,0,16),new Vector3(4.4f,0,16));var original=ribbon.vertices;
                join=PreparedContact(environment,new[]{ribbon},false,out int slopes,out int walls);
                Assert.That(slopes,Is.EqualTo(1),"The convex terrain crossing rejects the entire left taper before a buried transition cap is emitted.");Assert.That(walls,Is.EqualTo(3));
                var course=AssertPreparedContact(root,environment,new[]{ribbon},join,false);var world=new JoinedWorld(environment,course);var indices=ribbon.triangles;double minimum=double.PositiveInfinity;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var face=new Triangle(original[indices[i]],original[indices[i+1]],original[indices[i+2]]);
                    foreach(var terrain in world.Over(face.A,face.B,face.C))foreach(var point in Overlay(face,terrain))minimum=Math.Min(minimum,face.Height(point)-terrain.Height(point));
                }
                Assert.That(minimum,Is.GreaterThanOrEqualTo(.025),"The entire original ribbon remains clear; only the new outside cap chord is invalid.");
                CollectionAssert.AreEqual(original,ribbon.vertices);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        private static Mesh OriginalSelfOverlapRibbon()=>TestRibbon(
            new Vector3(4,0,3),new Vector3(4,0,-3),new Vector3(5,0,3),new Vector3(5,0,-3),new Vector3(6,0,3),new Vector3(6,0,-3),new Vector3(7,0,3),new Vector3(7,0,-3),
            new Vector3(5,0,0),new Vector3(11,0,0),new Vector3(5,0,1),new Vector3(11,0,1),new Vector3(5,0,2),new Vector3(11,0,2),new Vector3(5,0,3),new Vector3(11,0,3),new Vector3(5,0,4),new Vector3(11,0,4));
        [UnityTest] public IEnumerator RetainedOriginalSelfOverlapKeepsCompleteCertifiedCurtainContact()
        {
            var root=new GameObject("Retained original curtain whole-face contact");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=OriginalSelfOverlapRibbon();var original=ribbon.vertices;var indices=ribbon.triangles;
                join=PreparedContact(environment,new[]{ribbon},false,out _,out _);var course=AssertPreparedContact(root,environment,new[]{ribbon},join,false);
                var world=new JoinedWorld(environment,course);var hidden=new Triangle(new Vector3(6,0,3),new Vector3(6,-.35f,3),new Vector3(7,0,3));
                Assert.That(HiddenOriginalCurtain(hidden,original,false,world),Is.True,"The complete original curtain is under actual groomed surfaces on both sides.");
                CollectionAssert.AreEqual(original,ribbon.vertices);CollectionAssert.AreEqual(indices,ribbon.triangles);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator PartlyExposedAndHighCurtainsStillRejectRawTopologyAcrossCanonicalWelds()
        {
            var root=new GameObject("Exposed curtain strict certificate negatives");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null,badMesh=null;
            try
            {
                environment=TestEnvironment(root);ribbon=OriginalSelfOverlapRibbon();join=PreparedContact(environment,new[]{ribbon},false,out _,out _);
                var course=AssertPreparedContact(root,environment,new[]{ribbon},join,false);var world=new JoinedWorld(environment,course);
                foreach(bool high in new[]{false,true})
                {
                    var bad=new Triangle(new Vector3(high?6:4,high?.01f:0,3),new Vector3(high?6:4,-.35f,3),new Vector3(high?7:6,high?.01f:0,3));
                    Assert.That(HiddenOriginalCurtain(bad,ribbon.vertices,false,world),Is.False,high?"Above-groomed wall cannot be hidden.":"An exposed interval cannot be hidden by partial bilateral cover.");
                    var vertices=new List<Vector3>(join.vertices);var faces=new List<int>(join.triangles);var normals=new List<Vector3>(join.normals);var normal=Vector3.Cross(bad.B-bad.A,bad.C-bad.A).normalized;
                    for(int copy=0;copy<3;copy++)
                    {int at=vertices.Count;vertices.Add(bad.A);vertices.Add(bad.B+new Vector3(0,copy*.00001f,0));vertices.Add(bad.C);normals.Add(normal);normals.Add(normal);normals.Add(normal);faces.AddRange(new[]{at,at+1,at+2});}
                    badMesh=new Mesh{vertices=vertices.ToArray(),triangles=faces.ToArray(),normals=normals.ToArray()};badMesh.RecalculateBounds();
                    var failure=Assert.Throws<AssertionException>(()=>AssertPreparedContact(root,environment,new[]{ribbon},badMesh,false));
                    StringAssert.Contains("non-manifold",failure.Message,"A canonical weld may not hide a partially exposed or too-high original wall.");Object.Destroy(badMesh);badMesh=null;
                }
                yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(badMesh);Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ExactRetracedOriginalBoundaryPublishesOneWholeOutwardCurtain()
        {
            var root=new GameObject("Exact original curtain retrace regression");ProductionVenueEnvironment environment=null;Mesh ribbon=null,join=null;
            try
            {
                environment=TestEnvironment(root);ribbon=TestRibbon(new Vector3(0,0,3),new Vector3(0,0,-3),new Vector3(1,0,3),new Vector3(1,0,-3),new Vector3(2,0,3),new Vector3(2,0,-3),new Vector3(3,0,3),new Vector3(3,0,-3),new Vector3(4,0,3),new Vector3(4,0,-3),
                    new Vector3(3,0,-3),new Vector3(3,0,3),new Vector3(2,0,-3),new Vector3(2,0,3),new Vector3(1,0,-3),new Vector3(1,0,3),new Vector3(0,0,3),new Vector3(0,0,-3));
                var original=ribbon.vertices;var indices=ribbon.triangles;join=PreparedContact(environment,new[]{ribbon},true,out int slopes,out int walls);
                Assert.That(slopes,Is.Zero,"A retraced groomed surface must veto all overlapping candidate tapers.");Assert.That(walls,Is.EqualTo(12));AssertPreparedContact(root,environment,new[]{ribbon},join,true);
                var vertices=join.vertices;var faces=join.triangles;int exposed=0;
                for(int i=0;i<faces.Length;i+=3)
                {var a=vertices[faces[i]];var b=vertices[faces[i+1]];var c=vertices[faces[i+2]];if(a.z==3&&b.z==3&&c.z==3){Assert.That(Vector3.Cross(b-a,c-a).z,Is.GreaterThan(0),"The retained exposed union boundary keeps outward winding.");exposed++;}}
                Assert.That(exposed,Is.GreaterThan(0));CollectionAssert.AreEqual(original,ribbon.vertices);CollectionAssert.AreEqual(indices,ribbon.triangles);yield return null;
            }
            finally{environment?.Shutdown();Object.Destroy(join);Object.Destroy(ribbon);Object.Destroy(root);}
        }

        [UnityTest]
        public IEnumerator ActualRenderedTerrainRemainsBeneathEveryProductionRibbonTriangle()
        {
            const string prefix = "Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)
            {
                var asset = Resources.Load<TextAsset>(id);
                return asset != null ? asset.text : throw new FileNotFoundException(id);
            }
            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(Read(prefix + "manifest")), Read);
            var recipe = ProductionEnvironmentDefinition.FromJson(Read(prefix + "environment"));
            string trackBefore = plan.Track.ToJson(), visualBefore = plan.Visual.ToJson(), pathsBefore = plan.Paths.ToJson(), recipeBefore = recipe.ToJson();
            var root = new GameObject("Production complete terrain contact acceptance");
            ProductionVenueEnvironment view = null;
            ProductionCoursePresentation course = null;
            try
            {
                var cameraObject = new GameObject("Acceptance camera"); cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                var feed = root.AddComponent<RacePresentationFeed>(); view = root.AddComponent<ProductionVenueEnvironment>();
                view.Initialize(plan, recipe, Read, feed, camera);
                course = root.AddComponent<ProductionCoursePresentation>(); course.Initialize(plan);
                double spacing = recipe.terrain.grid_spacing_m;
                var cells = new Dictionary<CellKey, TerrainCell>();
                var sharedHeights = new Dictionary<CellKey, float>(); int sharedBorderChecks = 0;
                for (int tileIndex = 0; tileIndex < view.TerrainTileCount; tileIndex++)
                {
                    var vertices = view.TileMesh(tileIndex).vertices; var indices = view.TileMesh(tileIndex).triangles;
                    Vector3 center = recipe.terrain.tiles[tileIndex].center;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] += center;
                        var key = new CellKey((int)Math.Round(vertices[i].x / spacing), (int)Math.Round(vertices[i].z / spacing));
                        if (Math.Abs(vertices[i].x - key.X * spacing) >= 1e-5 || Math.Abs(vertices[i].z - key.Z * spacing) >= 1e-5)
                            Assert.Fail("Actual terrain vertex does not use the declared shared world grid.");
                        if (sharedHeights.TryGetValue(key, out float prior))
                        { Assert.That(vertices[i].y, Is.EqualTo(prior).Within(1e-5)); sharedBorderChecks++; }
                        else sharedHeights.Add(key, vertices[i].y);
                    }
                    // Index the renderer's actual triangles rather than assuming a diagonal or
                    // reconstructing the generator's height cap.
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        var triangle = new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                        var key = new CellKey(Cell(Math.Min(triangle.A.x, Math.Min(triangle.B.x, triangle.C.x)), spacing),
                            Cell(Math.Min(triangle.A.z, Math.Min(triangle.B.z, triangle.C.z)), spacing));
                        cells.TryGetValue(key, out var cell);
                        if (cell.Count >= 2) Assert.Fail("Terrain cells must not overlap between tiles.");
                        if (cell.Count == 0) cell.First = triangle; else cell.Second = triangle;
                        cell.Count++; cells[key] = cell;
                    }
                }
                Assert.That(sharedBorderChecks, Is.GreaterThan(0));
                foreach (var cell in cells.Values) if (cell.Count != 2) Assert.Fail("Every terrain grid cell requires two actual renderer triangles.");
                double minimum = double.PositiveInfinity, maximum = double.NegativeInfinity;
                long overlayVertices = 0; int ribbonTriangles = 0; string minimumAt = null, maximumAt = null;
                for (int loopIndex = 0; loopIndex < plan.Visual.Count; loopIndex++)
                {
                    string loopId = plan.Visual.Get(loopIndex).LoopId;
                    // Inspect the actual course renderer, including its joined closed-loop edge.
                    var mesh = course.LoopMesh(loopId);
                    {
                        var vertices = mesh.vertices; var indices = mesh.triangles;
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            var ribbon = new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                            if (Math.Abs(ribbon.Area) < 1e-12) continue;
                            ribbonTriangles++; long before = overlayVertices;
                            int x0 = Cell(Math.Min(ribbon.A.x, Math.Min(ribbon.B.x, ribbon.C.x)), spacing);
                            int x1 = Cell(Math.Max(ribbon.A.x, Math.Max(ribbon.B.x, ribbon.C.x)), spacing);
                            int z0 = Cell(Math.Min(ribbon.A.z, Math.Min(ribbon.B.z, ribbon.C.z)), spacing);
                            int z1 = Cell(Math.Max(ribbon.A.z, Math.Max(ribbon.B.z, ribbon.C.z)), spacing);
                            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                            {
                                if (!cells.TryGetValue(new CellKey(x, z), out var cell))
                                    Assert.Fail("Terrain does not cover the complete ribbon triangle bounds: " + loopId);
                                for (int side = 0; side < 2; side++)
                                {
                                    var terrain = side == 0 ? cell.First : cell.Second;
                                    foreach (var point in Overlay(ribbon, terrain))
                                    {
                                        double gap = ribbon.Height(point) - terrain.Height(point); overlayVertices++;
                                        if (gap < minimum) { minimum = gap; minimumAt = loopId + " triangle " + (i / 3) + " at " + point.X + "," + point.Z; }
                                        if (gap > maximum) { maximum = gap; maximumAt = loopId + " triangle " + (i / 3) + " at " + point.X + "," + point.Z; }
                                    }
                                }
                            }
                            if (overlayVertices <= before) Assert.Fail("Every nondegenerate ribbon triangle requires actual terrain coverage: " + loopId);
                        }
                    }
                }
                Assert.That(ribbonTriangles, Is.GreaterThan(0));
                TestContext.WriteLine("{\"ribbon_triangles\":" + ribbonTriangles + ",\"overlay_vertices\":" + overlayVertices +
                    ",\"shared_border_checks\":" + sharedBorderChecks + ",\"minimum_ribbon_gap_m\":" + minimum.ToString("R", CultureInfo.InvariantCulture) +
                    ",\"maximum_ribbon_gap_m\":" + maximum.ToString("R", CultureInfo.InvariantCulture) + "}");
                Assert.That(minimum, Is.GreaterThanOrEqualTo(.025), "Terrain protrudes into the rendered ribbon: " + minimumAt);
                Assert.That(double.IsFinite(maximum), Is.True, "Maximum gap must remain observable: " + maximumAt);
                course.JoinGround(view,recipe.terrain.grid_spacing_m);
                Assert.That(course.SnowShoulderWidthMeters,Is.EqualTo(TerrainBlendDefinition.Load().edge_width_m));
                Assert.That(course.TaperedSnowPanelCount,Is.GreaterThan(0),"Actual straight runs must use the existing recipe shoulder width.");
                Assert.That(course.VerticalSnowPanelCount,Is.GreaterThan(0),"Actual bends and connected corridors retain their proven joins.");
                var joinedWorld=new JoinedWorld(view,course);
                for(int i=0;i<course.LoopCount;i++)AssertJoinedGeometry(view,course,course.LoopId(i),course.LoopMesh(i).vertices,plan.Track.GetLoop(course.LoopId(i)).Closed,joinedWorld);
                Assert.That(plan.Track.ToJson(), Is.EqualTo(trackBefore)); Assert.That(plan.Visual.ToJson(), Is.EqualTo(visualBefore));
                Assert.That(plan.Paths.ToJson(), Is.EqualTo(pathsBefore)); Assert.That(recipe.ToJson(), Is.EqualTo(recipeBefore));
                course.Shutdown(); view.Shutdown(); yield return null;
            }
            finally { course?.Shutdown(); view?.Shutdown(); Object.Destroy(root); }
        }
    }
}
