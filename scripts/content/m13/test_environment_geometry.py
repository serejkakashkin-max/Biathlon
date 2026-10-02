"""Fast geometry regressions; no Unity Assets, CLI output, or draft mutation."""
import ast
from pathlib import Path
import unittest


def helpers(filename):
    # The authoring CLIs deliberately remain executable scripts. Load only their
    # pure geometry functions so tests never execute generation or verification.
    path=Path(__file__).with_name(filename)
    source=ast.parse(path.read_text(encoding='utf-8-sig'))
    nodes=[node for node in source.body if isinstance(node,(ast.Import,ast.FunctionDef))]
    namespace={};exec(compile(ast.Module(body=nodes,type_ignores=[]),str(path),'exec'),namespace)
    return namespace


GEN=helpers('generate_environment.py')
VERIFY=helpers('verify_environment.py')


def fixture():
    track={'loops':[{'id':'test','segments':[{'length_m':8.,'width_m':4.}]}]}
    visual={'sample_spacing_m':2.,'loops':[{'loop_id':'test','knots':[
        {'distance_m':0.,'x':0.,'y':10.,'z':0.},
        {'distance_m':8.,'x':8.,'y':10.,'z':0.}]}]}
    grid={(x,z):9.97 for x in range(-4,13,2) for z in range(-6,9,2)}
    return track,visual,grid


class EnvironmentGeometryTests(unittest.TestCase):
    def test_safe_centerline_does_not_hide_buried_lateral_edge(self):
        track,visual,grid=fixture()
        grid={point:height+.15*point[1] for point,height in grid.items()}
        # Both authored center knots retain 3cm, but the complete 4m ribbon
        # intersects terrain by 27cm on one edge.
        self.assertAlmostEqual(10-grid[(0,0)],.03)
        before=VERIFY['verify_full_ribbon'](grid,2,track,visual)
        self.assertEqual(before['minimum_ribbon_minus_terrain_m'],10-GEN['float32'](10.27))
        self.assertGreater(before['buried_ribbon_triangle_count'],0)
        original=grid.copy()
        cap=GEN['cap_terrain_below_ribbons'](grid,2,GEN['ribbon_triangles'](track,visual),.03)
        after=VERIFY['verify_full_ribbon'](grid,2,track,visual)
        self.assertGreater(cap['lowered_world_grid_vertex_count'],0)
        self.assertGreaterEqual(after['minimum_ribbon_minus_terrain_m'],.03-1e-6)
        self.assertEqual(after['buried_ribbon_triangle_count'],0)
        for point,height in original.items():
            self.assertLessEqual(grid[point],height)
            if abs(point[1])>=6:self.assertEqual(grid[point],height)

    def test_interior_peak_and_shared_grid_border_are_protected(self):
        track,visual,grid=fixture();grid[(2,0)]=10.27
        self.assertAlmostEqual(10-grid[(0,0)],.03)
        self.assertAlmostEqual(10-grid[(8,0)],.03)
        before=VERIFY['verify_full_ribbon'](grid,2,track,visual)
        self.assertEqual(before['minimum_ribbon_minus_terrain_m'],10-GEN['float32'](10.27))
        GEN['cap_terrain_below_ribbons'](grid,2,GEN['ribbon_triangles'](track,visual),.03)
        after=VERIFY['verify_full_ribbon'](grid,2,track,visual)
        self.assertGreaterEqual(after['minimum_ribbon_minus_terrain_m'],.03-1e-6)
        # A tiled serialization repeats one global sample on both border sides.
        left=[grid[(x,z)] for z in range(-6,9,2) for x in range(-4,3,2)]
        right=[grid[(x,z)] for z in range(-6,9,2) for x in range(2,13,2)]
        self.assertEqual(left[3::4],right[0::6])
        self.assertEqual(grid[(12,8)],9.97)

    def test_closed_terminal_row_keeps_last_width_and_float32_geometry(self):
        import math
        track={'loops':[{'id':'closed','segments':[{'length_m':3.,'width_m':2.},
            {'length_m':3.,'width_m':4.},{'length_m':3.,'width_m':6.}]}]}
        points=[(0.,0.),(3.,0.),(3.,3.),(0.,0.)]
        knots=[{'distance_m':i*3.,'x':x,'y':10.,'z':z} for i,(x,z) in enumerate(points)]
        visual={'sample_spacing_m':2.,'loops':[{'loop_id':'closed','knots':knots}]}
        generated=list(GEN['ribbon_triangles'](track,visual))
        checked=list(VERIFY['actual_ribbon_triangles'](track,visual))
        normalize=lambda records:[(loop,start,end,tuple(tuple(p) for p in tri)) for loop,start,end,tri in records]
        self.assertEqual(normalize(generated),normalize(checked))
        end=generated[-1][3];left,right=end[1],end[2]
        self.assertAlmostEqual(math.hypot(left[0]-right[0],left[2]-right[2]),6.,places=6)
        for record in generated:
            for vertex in record[3]:
                for coordinate in vertex:self.assertEqual(coordinate,GEN['float32'](coordinate))
        grid={(x,z):9.99 for x in range(-8,13,2) for z in range(-8,13,2)}
        GEN['cap_terrain_below_ribbons'](grid,2,iter(generated),.03)
        after=VERIFY['verify_full_ribbon'](grid,2,track,visual)
        self.assertGreaterEqual(after['minimum_ribbon_minus_terrain_m'],.03-1e-6)
        self.assertEqual(after['buried_ribbon_triangle_count'],0)


if __name__=='__main__':unittest.main()
