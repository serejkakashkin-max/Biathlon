"""Evidence-contract regressions for the outside-Assets WP175 planner."""
from copy import deepcopy
import hashlib
from pathlib import Path
import tempfile
import unittest

import wp175_weather_matrix as matrix


PROJECT = Path(__file__).resolve().parents[3]


class WeatherMatrixPlanTests(unittest.TestCase):
    def setUp(self):
        self.recipe = matrix.read_json(matrix.DEFAULT_RECIPE)

    def test_resolves_canonical_sources_without_claiming_execution(self):
        plan, documents = matrix.resolve(PROJECT, self.recipe)
        self.assertEqual(len(plan["visual_rows"]), 5)
        self.assertEqual(len(plan["physical_conditions"]), 4)
        self.assertEqual(len(documents), 9)
        self.assertEqual(plan["native_cases_executed"], 0)
        self.assertEqual(plan["physical_races_executed"], 0)
        self.assertFalse(plan["performance_qualified"])
        self.assertFalse(plan["wp175_complete"])
        for source in plan["source_sha256"]:
            self.assertEqual(source["sha256"], hashlib.sha256((PROJECT / source["path"]).read_bytes()).hexdigest())

    def test_generated_environment_changes_only_identity_status_and_preset_references(self):
        original = matrix.read_json(matrix.resource_file(PROJECT, self.recipe["resources"]["environment"]))
        before = deepcopy(original)
        _, documents = matrix.resolve(PROJECT, self.recipe)
        for row in self.recipe["visual_rows"]:
            generated = deepcopy(documents["environments/" + row["id"] + ".json"])
            self.assertEqual(generated["lighting"]["weather_preset"], row["weather_preset"])
            self.assertEqual(generated["lighting"]["preset_id"], row["lighting_preset"])
            generated["id"] = original["id"]
            generated["status"] = original["status"]
            generated["lighting"]["weather_preset"] = original["lighting"]["weather_preset"]
            generated["lighting"]["preset_id"] = original["lighting"]["preset_id"]
            self.assertEqual(generated, original)
        self.assertEqual(original, before)

    def test_conditions_bind_every_actual_track_surface_and_duplicate_no_coefficients(self):
        track = matrix.read_json(matrix.resource_file(PROJECT, self.recipe["resources"]["track"]))
        surfaces = {segment["snow_profile_id"] for loop in track["loops"] for segment in loop["segments"]}
        _, documents = matrix.resolve(PROJECT, self.recipe)
        for profile in self.recipe["physical_snow_profiles"]:
            conditions = documents["conditions/" + profile + ".json"]
            self.assertEqual(set(conditions), {"schema_version", "id", "surfaces"})
            self.assertEqual({row["surface_id"] for row in conditions["surfaces"]}, surfaces)
            for row in conditions["surfaces"]:
                self.assertEqual(set(row), {"surface_id", "profile_id"})
                self.assertEqual(row["profile_id"], profile)

    def test_incomplete_or_duplicate_visual_coverage_is_rejected(self):
        for mutation in (lambda rows: rows.pop(), lambda rows: rows.append(deepcopy(rows[0]))):
            recipe = deepcopy(self.recipe)
            mutation(recipe["visual_rows"])
            with self.assertRaises(ValueError):
                matrix.assert_recipe(recipe)

    def test_numeric_tuning_in_reference_rows_is_rejected(self):
        self.recipe["visual_rows"][0]["light_intensity"] = 3
        with self.assertRaisesRegex(ValueError, "duplicated numeric tuning"):
            matrix.assert_recipe(self.recipe)

    def test_unknown_or_duplicate_physical_profile_is_rejected(self):
        for profile in ("unknown", "packed_neutral"):
            recipe = deepcopy(self.recipe)
            recipe["physical_snow_profiles"][-1] = profile
            with self.assertRaises(ValueError):
                matrix.assert_recipe(recipe)

    def test_fake_performance_or_plan_acceptance_is_rejected(self):
        for key in ("planned_matrix_qualifies_wp175", "hidden_player_qualifies_sustained_fps_or_gpu", "synthetic_frame_timings_qualify_performance"):
            recipe = deepcopy(self.recipe)
            recipe["evidence_policy"][key] = True
            with self.assertRaisesRegex(ValueError, "Evidence policy"):
                matrix.assert_recipe(recipe)

    def test_fabricated_pursuit_qualification_and_wrong_roster_are_rejected(self):
        recipe = deepcopy(self.recipe)
        recipe["race_formats"][1]["qualification"] = "fabricated_gaps"
        with self.assertRaisesRegex(ValueError, "actual source Sprint"):
            matrix.assert_recipe(recipe)
        recipe = deepcopy(self.recipe)
        recipe["race_formats"][1]["field_count"] = 60
        with self.assertRaisesRegex(ValueError, "field counts"):
            matrix.assert_recipe(recipe)

    def test_second_production_venue_and_unsafe_resource_paths_are_rejected(self):
        self.recipe["venue_id"] = "pine_ridge_v1"
        with self.assertRaisesRegex(ValueError, "Woodland only"):
            matrix.assert_recipe(self.recipe)
        for resource in ("Biathlon/../secret", "Biathlon\\catalog", "C:/secret", "Biathlon/foo.json", "Biathlon//foo"):
            with self.subTest(resource=resource), self.assertRaises(ValueError):
                matrix.resource_file(PROJECT, resource)

    def test_plan_outputs_cannot_enter_assets(self):
        for target in (PROJECT / "Assets", PROJECT / "Assets/Resources/Biathlon/wp175_should_not_exist"):
            with self.assertRaisesRegex(ValueError, "outside Assets"):
                matrix.write_plan(PROJECT, self.recipe, target)

    def test_disk_plan_is_explicitly_unverified_and_uses_all_native_views(self):
        with tempfile.TemporaryDirectory(prefix="biathlon-wp175-plan-") as directory:
            target = Path(directory)
            plan = matrix.write_plan(PROJECT, self.recipe, target)
            self.assertEqual(matrix.read_json(target / "plan.json"), plan)
            for row in plan["visual_rows"]:
                self.assertEqual(row["status"], "NOT_EXECUTED")
                self.assertEqual(len(row["required_view_ids"]), 6)
                self.assertTrue((target / row["environment_file"]).is_file())
            for row in plan["physical_conditions"]:
                self.assertEqual(row["status"], "NOT_EXECUTED")
                self.assertTrue((target / row["conditions_file"]).is_file())


if __name__ == "__main__":
    unittest.main()
