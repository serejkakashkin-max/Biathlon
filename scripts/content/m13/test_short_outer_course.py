"""Owner-shortened paid geometry, protected stadium and legacy byte contracts."""
from pathlib import Path
import copy
import hashlib
import json
import math
import subprocess
import sys
import tempfile
import unittest
from short_outer_course import build_short_outer_course

SCRIPT = Path(__file__).resolve().parent
PROJECT = SCRIPT.parents[2]
PREFIX = Path('resources/Biathlon/Venues/woodland_basin_v1')


class ShortOuterCourseTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix='biathlon-short-course-')
        cls.root = Path(cls.temporary.name)
        cls.short_recipe = SCRIPT/'recipes/woodland_basin_short_course_recipe_v1.json'
        cls.format_recipe = SCRIPT/'recipes/woodland_basin_short_formats_recipe_v1.json'
        cls.recipe = json.loads(cls.short_recipe.read_text(encoding='utf-8-sig'))
        cls.generate('generate_venue01.py','--recipe',cls.short_recipe,'--output-root',cls.root/'short')
        cls.generate('generate_venue01.py','--recipe',cls.short_recipe,'--output-root',cls.root/'repeat')
        cls.generate('generate_venue01.py','--recipe',SCRIPT/'recipes/woodland_basin_compact_range_recipe_v1.json',
                '--output-root',cls.root/'legacy-current')
        cls.generate('generate_format_branches.py','--recipe',cls.format_recipe,
                '--base-root',cls.root/'short','--output-root',cls.root/'formats')
        cls.generate('verify_format_branches.py','--base-root',cls.root/'short','--output-root',cls.root/'formats')

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    @staticmethod
    def generate(script,*arguments):
        result = subprocess.run([sys.executable,str(SCRIPT/script),'--project-root',str(PROJECT),
                                 *map(str,arguments)],capture_output=True,text=True)
        if result.returncode:
            raise AssertionError(result.stdout+result.stderr)

    def read(self,name,resource):
        return json.loads((self.root/name/PREFIX/(resource+'.json')).read_text(encoding='utf-8-sig'))

    def test_actual_paid_main_is_1200m_and_exact_xyz_not_visual_scale(self):
        track,visual = self.read('short','track'),self.read('short','visual')
        loops = {loop['id']:loop for loop in track['loops']}
        maps = {loop['loop_id']:loop for loop in visual['loops']}
        self.assertEqual(sum(round(segment['length_m']*1000) for segment in loops['main']['segments']),1200000)
        for loop_id,loop in loops.items():
            knots = maps[loop_id]['knots']
            paid = 0
            for a,b,segment in zip(knots,knots[1:],loop['segments']):
                self.assertEqual(round(a['distance_m']*1000),paid)
                paid += round(segment['length_m']*1000)
                self.assertEqual(round(b['distance_m']*1000),paid)
                self.assertLess(abs(math.dist(tuple(a[k] for k in ('x','y','z')),
                    tuple(b[k] for k in ('x','y','z')))-segment['length_m']),1e-8)
                horizontal=math.hypot(b['x']-a['x'],b['z']-a['z'])
                self.assertAlmostEqual(segment['start_grade'],(b['y']-a['y'])/horizontal,places=12)

    def test_first_turn_and_real_early_downhill_and_negative_grade_bend(self):
        main=next(loop for loop in self.read('short','track')['loops'] if loop['id']=='main')
        self.assertEqual(main['segments'][0]['length_m'],60)
        distance=0
        early_downhill=False
        negative_bend=False
        for segment in main['segments']:
            if distance<146 and segment['start_grade']<-.01:early_downhill=True
            if (segment['semantic_zone']=='descent_technical' and segment['start_grade']<-.01
                    and abs(segment['curvature_per_m'])>.01):negative_bend=True
            self.assertLessEqual(abs(segment['start_grade']),.25)
            distance+=segment['length_m']
        self.assertTrue(early_downhill,'Short climb/decline must occur before the format window.')
        self.assertTrue(negative_bend,'A technical descent must have actual negative XYZ grade.')

    def test_exterior_smooth_knots_and_no_giant_same_heading_straight(self):
        main=next(loop for loop in self.read('short','track')['loops'] if loop['id']=='main')
        visual=next(loop for loop in self.read('short','visual')['loops'] if loop['loop_id']=='main')
        longest=run=0.
        previous=None
        for a,b,segment in zip(visual['knots'],visual['knots'][1:],main['segments']):
            if a['distance_m']>=995.548:break
            direction=(b['x']-a['x'],b['z']-a['z'])
            if previous is not None:
                angle=abs(math.atan2(previous[0]*direction[1]-previous[1]*direction[0],
                                     previous[0]*direction[0]+previous[1]*direction[1]))
                self.assertLess(math.degrees(angle),6.2)
                run=run+segment['length_m'] if angle<1e-7 else segment['length_m']
            else:run=segment['length_m']
            longest=max(longest,run)
            previous=direction
        self.assertLessEqual(longest,140.001)

    def test_existing_private_penalty_exit_and_start_world_preserve_geometry(self):
        for resource,key in [('track','id'),('visual','loop_id')]:
            before={loop[key]:loop for loop in self.read('legacy-current',resource)['loops']}
            after={loop[key]:loop for loop in self.read('short',resource)['loops']}
            shared=before.keys()-{'main'}
            self.assertEqual(len(shared),33)
            for loop_id in shared:
                if resource=='track' and loop_id.startswith('mat_'):
                    self.assertTrue(all(edge['width_m']==2.5 for edge in before[loop_id]['segments']))
                    self.assertTrue(all(edge['width_m']==3 for edge in after[loop_id]['segments']))
                    original={**before[loop_id], 'segments':[
                        {k:v for k,v in edge.items() if k!='width_m'} for edge in before[loop_id]['segments']]}
                    widened={**after[loop_id], 'segments':[
                        {k:v for k,v in edge.items() if k!='width_m'} for edge in after[loop_id]['segments']]}
                    self.assertEqual(original,widened,loop_id)
                else:self.assertEqual(before[loop_id],after[loop_id],loop_id)
        before,after=(self.read(name,'manifest')['range_layout'] for name in ('legacy-current','short'))
        self.assertEqual({k:v for k,v in before.items() if k not in ('entry','exit')},
                         {k:v for k,v in after.items() if k not in ('entry','exit')})
        self.assertEqual(after['lane_pitch_m'],3)
        self.assertEqual(after['apron_depth_m'],12)
        self.assertEqual(len(self.read('short','paths')['lanes']),30)

    def test_straight_format_windows_keep_exact_planes_and_paid_lengths(self):
        audit=json.loads((self.root/'short/audit.json').read_text())['short_outer_course']
        visual=next(loop for loop in self.read('short','visual')['loops'] if loop['loop_id']=='main')
        for name,window in audit['format_windows'].items():
            self.assertEqual(round((window['end_m']-window['start_m'])*1000),140000)
            knots=[k for k in visual['knots'] if window['start_m']<=k['distance_m']<=window['end_m']]
            self.assertEqual(len(knots),3)
            self.assertEqual(len({k['x'] for k in knots}),1,'Quantisation must not move a straight midpoint.')
            self.assertAlmostEqual(math.dist(tuple(knots[0][k] for k in ('x','y','z')),
                tuple(knots[-1][k] for k in ('x','y','z'))),140,places=10)

    def test_all_three_formats_pay_declared_game_distances_and_preserve_shooting(self):
        expected={'mass_start':(6000,5,30,[0,0,1,1]),'sprint':(4200.001,3,40,[0,1]),
                  'pursuit':(5000,5,40,[0,0,1,1])}
        proof=json.loads((self.root/'formats/formats-verification.json').read_text())
        self.assertEqual({item['format']:item['distance_m'] for item in proof['formats']},
                         {key:row[0] for key,row in expected.items()})
        for key,(distance,laps,count,stances) in expected.items():
            profile=self.read('formats',key+'_profile')
            self.assertEqual((profile['distance_m'],profile['laps'],profile['field_capacity']),
                             (distance,laps,count))
            self.assertEqual(profile['rules_revision'],'owner_compressed_gameplay_distance_v1')
            self.assertEqual(profile['penalty_loop_m'],150)
            self.assertEqual(profile['shots_per_bout'],5)
            self.assertEqual([bout['stance'] for bout in profile['shooting_bouts']],stances)
        self.assertEqual(proof['maximum_directed_seam_world_gap_m'],0)
        self.assertEqual(proof['unchanged_base_loop_count'],34)
        self.assertEqual(proof['unchanged_lane_path_count'],30)

    def test_independent_repeated_generation_is_byte_identical(self):
        def inventory(root):
            return {path.relative_to(root).as_posix():hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in root.rglob('*.json')}
        self.assertEqual(inventory(self.root/'short/resources'),inventory(self.root/'repeat/resources'))
        self.assertEqual((self.root/'short/audit.json').read_bytes(),(self.root/'repeat/audit.json').read_bytes())

    def test_invalid_incomplete_or_nonfinite_recipe_is_rejected(self):
        original=self.recipe['short_outer_course']
        for name,value in [('curve_samples',15),('curve_samples',True),('corner_radius_m',0),
                           ('leg_side_sway_m',-1),('launch_straight_m',float('nan')),
                           ('format_window_m',140.001),('depth_bounds_m',[300,220])]:
            changed=copy.deepcopy(original)
            changed[name]=value
            with self.assertRaises((ValueError,AssertionError),msg=name):
                build_short_outer_course(changed,-138.5,54.5,-8.693252614,-2.585252614,995548)
        changed=copy.deepcopy(original)
        del changed['id']
        with self.assertRaises(ValueError):
            build_short_outer_course(changed,-138.5,54.5,-8.693252614,-2.585252614,995548)

    def test_legacy_checkpoint_resource_bytes_remain_exact(self):
        fixture=json.loads((SCRIPT/'fixtures/short-course-legacy-byte-baseline.json').read_text())
        self.assertEqual(fixture['schema_version'],1)
        recipe=self.root/'legacy-recipe.json'
        recipe.write_text(json.dumps(fixture['geometry_recipe']))
        self.generate('generate_venue01.py','--recipe',recipe,
                 '--output-root',self.root/'legacy-baseline')
        self.generate('generate_format_branches.py','--recipe',SCRIPT/'recipes/woodland_basin_formats_recipe_v1.json',
                 '--base-root',self.root/'legacy-baseline','--output-root',self.root/'legacy-formats')
        for generated,reference in [('legacy-baseline','base'),('legacy-formats','formats')]:
            after=self.root/generated/'resources'
            inventory={path.relative_to(after).as_posix():hashlib.sha256(path.read_bytes()).hexdigest()
                       for path in after.rglob('*.json')}
            self.assertEqual(fixture['resources_sha256'][reference],inventory)


if __name__=='__main__':
    unittest.main()
