"""Native stadium bounds regression against every actual paid Woodland loop."""
import copy
import json
import os
from pathlib import Path
import unittest

from test_environment_geometry import helpers
import dress_woodland_course as dressing

GEN=helpers('generate_environment.py')
PROJECT=Path(__file__).resolve().parents[3]
FIXTURE_RESOURCES=Path(os.environ.get('BIATHLON_STADIUM_FIXTURE_RESOURCES',str(PROJECT/'Assets/Resources'))).resolve()
PREFIX='Biathlon/Venues/woodland_basin_v1/'


def policy():
    recipe=dressing.read_json(Path(__file__).with_name('recipes')/'woodland_basin_short_environment_recipe_v1.json')
    return {'stadium_placement':copy.deepcopy(recipe['stadium_placement'])}


def fixture_document(resource):
    path=(FIXTURE_RESOURCES/(resource+'.json')).resolve()
    if not path.is_relative_to(FIXTURE_RESOURCES):raise ValueError('Bounded fixture resource required')
    return dressing.read_json(path)


def actual_inputs():
    environment=fixture_document(PREFIX+'environment')
    # The old layout is a regression input even after the shipped layout changes.
    stands=[dict(item,position={'x':-80+20*index,'y':0,'z':-50},yaw_deg=180,scale=1)
            for index,item in enumerate(environment['assets'][:9])]
    screen=[copy.deepcopy(item) for item in environment['mesh_assets'] if item['id'].startswith('event_screen_')]
    for item in screen:item.update(position={'x':112,'y':0,'z':-45},yaw_deg=-90,scale=1)
    grid={};spacing=environment['terrain']['grid_spacing_m']
    for tile in environment['terrain']['tiles']:
        heights=fixture_document(tile['height_resource'])
        for iz in range(tile['rows']):
            for ix in range(tile['columns']):
                point=(tile['center']['x']-tile['size']['x']/2+ix*spacing,tile['center']['z']-tile['size']['z']/2+iz*spacing)
                height=heights['heights_m'][iz*tile['columns']+ix]
                if point in grid and grid[point]!=height:raise ValueError('Exact source terrain borders required')
                grid[point]=height
    return stands,screen,environment['route_corridors'],grid,spacing


class StadiumPlacementTests(unittest.TestCase):
    def test_section_poses_are_copied_and_only_existing_finite_sections_are_accepted(self):
        source=policy();copied=GEN['stadium_placement_overrides'](source)
        copied['grandstand_sections']['grandstand_01']['z']=0
        self.assertEqual(source['stadium_placement']['grandstand_sections']['grandstand_01']['z'],-60)
        for sections in (None,[],{'new_stand':{'x':0,'z':0}},{'grandstand_01':{'x':True,'z':0}},{'grandstand_01':{'x':0,'z':float('nan')}},{'grandstand_01':{'x':0,'z':0,'y':1}}):
            invalid=policy();invalid['stadium_placement']['grandstand_sections']=sections
            with self.subTest(sections=sections),self.assertRaises(ValueError):GEN['stadium_placement_overrides'](invalid)

    def test_omitted_section_table_retains_the_exact_legacy_row_policy(self):
        source=policy();source['stadium_placement'].pop('grandstand_sections')
        copied=GEN['stadium_placement_overrides'](source)
        self.assertEqual(copied,source['stadium_placement']);self.assertNotIn('grandstand_sections',copied)

    def test_missing_option_keeps_exact_legacy_placement_serialization(self):
        stands=[{'id':'legacy','position':{'x':1,'y':0,'z':2}}];screen=[{'arbitrary':'untouched'}]
        before=json.dumps([stands,screen],separators=(',',':'))
        self.assertIsNone(GEN['apply_stadium_placements']({},stands,screen,Path('missing'),[],{},2))
        self.assertEqual(json.dumps([stands,screen],separators=(',',':')),before)

    def test_policy_copies_nested_values_and_rejects_invalid_schema_pose_or_clearance(self):
        original=policy();copied=GEN['stadium_placement_overrides'](original)
        copied['grandstand_row']['z']=0
        self.assertEqual(original['stadium_placement']['grandstand_row']['z'],-72)
        for field,value in (('schema_version',True),('schema_version',2),('id','../bad'),
                            ('minimum_paid_clearance_m',2.9),('minimum_paid_clearance_m',float('nan')),
                            ('maximum_foundation_variation_m',.251)):
            invalid=policy();invalid['stadium_placement'][field]=value
            with self.subTest(field=field,value=value),self.assertRaises(ValueError):
                GEN['stadium_placement_overrides'](invalid)
        for pose,field,value in (('grandstand_row','z',float('inf')),('grandstand_row','pitch_m',False),
                                ('event_screen','yaw_deg',361),('event_screen','y',0)):
            invalid=policy();invalid['stadium_placement'][pose][field]=value
            with self.subTest(pose=pose,field=field),self.assertRaises(ValueError):
                GEN['stadium_placement_overrides'](invalid)
        with self.assertRaises(ValueError):GEN['stadium_placement_overrides']({'stadium_placement':None})

    def test_original_row_and_event_screen_each_reject_without_partial_pose_mutation(self):
        for row_z,screen_x in ((-50,128),(-72,112)):
            stands,screen,corridors,grid,spacing=actual_inputs();before=copy.deepcopy([stands,screen])
            recipe=policy();recipe['stadium_placement']['grandstand_row']['z']=row_z
            recipe['stadium_placement']['event_screen']['x']=screen_x
            with self.subTest(row_z=row_z,screen_x=screen_x),self.assertRaisesRegex(ValueError,'paid route'):
                GEN['apply_stadium_placements'](recipe,stands,screen,PROJECT,corridors,grid,spacing)
            self.assertEqual([stands,screen],before)

    def test_all_nine_prefabs_and_three_screen_parts_use_native_full_bounds_actual_terrain(self):
        stands,screen,corridors,grid,spacing=actual_inputs()
        proof=GEN['apply_stadium_placements'](policy(),stands,screen,PROJECT,corridors,grid,spacing)
        self.assertEqual(proof['full_paid_loop_count'],len(corridors))
        self.assertEqual(len(corridors),37,"Include the separate start spur in every full-course clearance check.")
        self.assertEqual(proof['prefab_count'],9);self.assertEqual(proof['screen_part_count'],3)
        self.assertEqual(len(proof['witnesses']),10)
        self.assertFalse(proof['authoritative_geometry_changed'])
        self.assertTrue(proof['native_source_sha256'])
        for witness in proof['witnesses']:
            self.assertGreaterEqual(witness['minimum_paid_clearance_m'],3)
            self.assertLessEqual(witness['foundation_variation_m'],.25)
            bottom=witness['position']['y']+witness['native_bounds'][0][1]
            self.assertAlmostEqual(bottom,max(value['terrain_y'] for value in witness['foundation_samples']))
            self.assertEqual(len(witness['world_footprint']),4)
        for item in stands+screen:self.assertEqual(item['scale'],1)
        self.assertEqual(stands[0]['position']['z'],-60)
        self.assertEqual({item['position']['z'] for item in stands[1:]},{-72})
        self.assertEqual({item['position']['x'] for item in screen},{128})
        self.assertEqual(len({json.dumps(item['position'],sort_keys=True) for item in screen}),1)
        self.assertEqual(stands[0]['position']['x'],-55)
        self.assertEqual(stands[-1]['position']['x'],105)

    def test_native_footprints_reject_crowd_blocks_intersecting_each_other(self):
        stands,screen,corridors,grid,spacing=actual_inputs();before=copy.deepcopy([stands,screen])
        recipe=policy();recipe['stadium_placement']['grandstand_row'].update(start_x=-15,pitch_m=10)
        with self.assertRaisesRegex(ValueError,'footprints overlap'):
            GEN['apply_stadium_placements'](recipe,stands,screen,PROJECT,corridors,grid,spacing)
        self.assertEqual([stands,screen],before)

    def test_optional_terrain_policy_preserves_legacy_defaults_and_detaches_authored_bounds(self):
        self.assertEqual(GEN['terrain_stadium_policy']({}),((-180,-95,130,62),40))
        self.assertEqual(GEN['terrain_stadium_policy']({'terrain':{'snow_shoulders_resource':'unrelated'}}),((-180,-95,130,62),40))
        recipe=dressing.read_json(Path(__file__).with_name('recipes')/'woodland_basin_short_environment_recipe_v1.json')
        flat,blend=GEN['terrain_stadium_policy'](recipe)
        self.assertEqual(flat,(-65,-95,130,62));self.assertEqual(blend,10)
        recipe['terrain']['stadium_flat_bounds'][0]=0
        self.assertEqual(flat,(-65,-95,130,62))

    def test_terrain_override_rejects_nonfinite_inverted_or_unbounded_blend(self):
        for value in (None,{},True,4,'bad',[0,0,0,2],[0,4,2,3],[0,0,True,2],[0,0,float('inf'),2],[0,0,float('nan'),2],[0,0,2]):
            with self.subTest(bounds=value),self.assertRaises(ValueError):
                GEN['terrain_stadium_policy']({'terrain':{'stadium_flat_bounds':value}})
        for value in (True,None,float('nan'),float('inf'),0,80.01,'10'):
            with self.subTest(blend=value),self.assertRaises(ValueError):
                GEN['terrain_stadium_policy']({'terrain':{'stadium_blend_m':value}})
        for value in (1,80):self.assertEqual(GEN['terrain_stadium_policy']({'terrain':{'stadium_blend_m':value}})[1],value)
        for value in (None,[],True):
            with self.subTest(terrain=value),self.assertRaises(ValueError):GEN['terrain_stadium_policy']({'terrain':value})

    def test_original_stadium_flattening_erased_actual_positive_return_height(self):
        visual=fixture_document(PREFIX+'visual')
        knots=next(loop['knots'] for loop in visual['loops'] if loop['loop_id']=='main')
        witnesses=[knot for knot in knots if -180<=knot['x']<-75 and -95<=knot['z']<=62 and knot['y']>1]
        self.assertTrue(witnesses,'The actual current return must include an authored positive-Y witness outside the compact stadium.')
        knot=max(witnesses,key=lambda value:value['y'])
        namespace=helpers('generate_environment.py')
        namespace.update(nearest_main=lambda x,z:(0,knot['y'],3),rise=max(k['y'] for k in knots),
                         base_north=min(k['z'] for k in knots if abs(k['y'])<1e-8),
                         base_south=max(k['z'] for k in knots if abs(k['y']-max(v['y'] for v in knots))<1e-8),SURFACE_OFFSET=-.03)
        namespace['FLAT'],namespace['STADIUM_BLEND']=namespace['terrain_stadium_policy']({})
        erased=namespace['surface_height'](knot['x'],knot['z'])
        recipe=dressing.read_json(Path(__file__).with_name('recipes')/'woodland_basin_short_environment_recipe_v1.json')
        namespace['FLAT'],namespace['STADIUM_BLEND']=namespace['terrain_stadium_policy'](recipe)
        kept=namespace['surface_height'](knot['x'],knot['z'])
        self.assertAlmostEqual(erased,-.03)
        self.assertAlmostEqual(kept-erased,knot['y'])
        _,_,_,grid,spacing=actual_inputs()
        actual=dressing.ground(grid,spacing,knot['x'],knot['z'])
        self.assertGreater(actual,knot['y']-.3,'Native terrain must retain the current graded return within its conservative ribbon-clearance lowering budget; metadata alone cannot pass.')
        self.assertLess(actual,knot['y'],'The terrain stays below the groomed course surface.')


if __name__=='__main__':unittest.main()
