"""Optional fog recipes preserve the canonical generator's absent-field path."""
import json
from pathlib import Path
import unittest
from test_environment_geometry import GEN


class EnvironmentFogRecipeTests(unittest.TestCase):
    def test_absent_scale_adds_no_default_property(self):
        self.assertEqual(GEN['lighting_overrides']({}), {})
        legacy={'lighting':{'preset_id':'dawn','weather_preset':'clear'}}
        overrides=GEN['lighting_overrides'](legacy)
        self.assertEqual(overrides,legacy['lighting'])
        overrides['preset_id']='clear_cold'
        self.assertEqual(legacy['lighting']['preset_id'],'dawn')

    def test_scale_bounds_reject_nonfinite_and_non_numeric_values(self):
        for scale in (1,2.7,3):
            self.assertEqual(GEN['lighting_overrides']({'lighting':{'fog_distance_scale':scale}})['fog_distance_scale'],scale)
        for scale in (0,.99,3.01,float('nan'),float('inf'),-float('inf'),True,'2.7',None):
            with self.subTest(scale=scale),self.assertRaises(ValueError):
                GEN['lighting_overrides']({'lighting':{'fog_distance_scale':scale}})

    def test_woodland_recipe_changes_only_the_explicit_atmosphere_property(self):
        recipe=json.loads((Path(__file__).parent/'recipes/woodland_basin_environment_recipe_v1.json').read_text(encoding='utf-8'))
        self.assertEqual(recipe['schema_version'],1)
        self.assertEqual(recipe['venue_id'],'woodland_basin_v1')
        self.assertEqual(GEN['lighting_overrides'](recipe),{'fog_distance_scale':2.7})
        self.assertNotIn('forest',recipe)


if __name__=='__main__':unittest.main()
