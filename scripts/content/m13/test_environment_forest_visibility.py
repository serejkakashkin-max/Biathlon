"""Production visibility opt-in must not regenerate placement/identity tuning."""
import json
from pathlib import Path
import unittest
from test_environment_geometry import GEN


class EnvironmentForestVisibilityTests(unittest.TestCase):
    def test_absent_policy_preserves_legacy_path_and_explicit_policy_is_detached(self):
        self.assertIsNone(GEN['forest_high_visibility']({}))
        policy={'schema_version':1,'id':'production_high_fog_visibility_v1','mode':'fog_frustum_v1'}
        recipe={'forest_high_visibility':policy}
        copied=GEN['forest_high_visibility'](recipe)
        self.assertEqual(copied,policy)
        copied['id']='different'
        self.assertEqual(policy['id'],'production_high_fog_visibility_v1')
        self.assertNotIn('forest',recipe)

    def test_unknown_version_mode_identity_or_unbound_tuning_rejects(self):
        valid={'schema_version':1,'id':'production_high_fog_visibility_v1','mode':'fog_frustum_v1'}
        for field,values in {'schema_version':[0,2,True,None,1.0],'mode':['radius','',None],
                             'id':['','..','x/y','x\\y','../x','foo.bar','x'*257,None]}.items():
            for value in values:
                with self.subTest(field=field,value=value),self.assertRaises(ValueError):
                    GEN['forest_high_visibility']({'forest_high_visibility':dict(valid,**{field:value})})
        for value in (None,True,[],dict(valid,cull_m=600)):
            with self.subTest(value=value),self.assertRaises(ValueError):
                GEN['forest_high_visibility']({'forest_high_visibility':value})

    def test_woodland_opt_in_changes_no_identity_or_density_parameters(self):
        recipe=json.loads((Path(__file__).parent/'recipes/woodland_basin_environment_recipe_v1.json').read_text(encoding='utf-8'))
        self.assertNotIn('forest',recipe)
        self.assertEqual(GEN['lighting_overrides'](recipe),{'fog_distance_scale':2.7})
        self.assertEqual(GEN['forest_high_visibility'](recipe),{
            'schema_version':1,'id':'woodland_basin_high_fog_visibility_v1','mode':'fog_frustum_v1'})


if __name__=='__main__':unittest.main()
