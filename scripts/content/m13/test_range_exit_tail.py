"""Counted downstream range space separates live guards without shrinking them."""
from pathlib import Path
import json
import subprocess
import sys
import unittest

PROJECT = Path(__file__).resolve().parents[3]
SCRIPTS = Path(__file__).resolve().parent
ARTIFACTS = PROJECT / "artifacts/wp169o/exit-tail-tests"


class RangeExitTailTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.recipe = json.loads((SCRIPTS / "recipes/woodland_basin_geometry_recipe_v1.json").read_text(encoding="utf-8-sig"))
        cls.roots = {}
        for name, extension in (("default", 0), ("extended", cls.recipe["exit_tail_extension_m"])):
            root = ARTIFACTS / name
            root.mkdir(parents=True, exist_ok=True)
            recipe = dict(cls.recipe)
            recipe.pop("exit_tail_extension_m", None)
            if extension:
                recipe["exit_tail_extension_m"] = extension
            recipe_path = root / "input-recipe.json"
            recipe_path.write_text(json.dumps(recipe), encoding="utf-8")
            subprocess.run([sys.executable, str(SCRIPTS / "generate_venue01.py"), "--project-root", str(PROJECT),
                            "--recipe", str(recipe_path), "--output-root", str(root)], check=True, capture_output=True, text=True)
            cls.roots[name] = root

    def read(self, name, resource):
        return json.loads((self.roots[name] / "resources/Biathlon/Venues/woodland_basin_v1" / f"{resource}.json").read_text())

    def test_counted_paths_and_standard_lap_preserve_every_mat_stop(self):
        for name in self.roots:
            track = self.read(name, "track")
            loops = {loop["id"]: loop for loop in track["loops"]}
            self.assertEqual(round(sum(segment["length_m"] * 1000 for segment in loops["main"]["segments"])), 3000000)
            self.assertEqual(round(sum(segment["length_m"] * 1000 for segment in loops["penalty_150"]["segments"])), 150000)
            paths = self.read(name, "paths")
            interval = round((paths["exit_distance_m"] - paths["entry_distance_m"]) * 1000)
            for lane in paths["lanes"]:
                self.assertEqual(sum(round((s["end_m"] - s["start_m"]) * 1000) for s in lane["slices"]), interval)
                self.assertEqual(lane["mat_anchor"]["distance_m"], 13.704)
            self.assertEqual(paths["exit_distance_m"], 2950)

    def test_guards_and_private_body_corridors_do_not_shrink(self):
        before = self.read("default", "junctions")
        after = self.read("extended", "junctions")
        self.assertEqual([j["id"] for j in before["junctions"]], [j["id"] for j in after["junctions"]])
        for a, b in zip(before["junctions"], after["junctions"]):
            for p, q in zip(a["ports"], b["ports"]):
                self.assertEqual(p["guard_before_m"], q["guard_before_m"])
                self.assertEqual(p["guard_after_m"], q["guard_after_m"])
        before_loops = {loop["id"]: loop for loop in self.read("default", "track")["loops"]}
        for loop in self.read("extended", "track")["loops"]:
            if loop["id"].startswith("mat_"):
                self.assertEqual(loop, before_loops[loop["id"]])
        before_visual = {loop["loop_id"]: loop for loop in self.read("default", "visual")["loops"]}
        for loop in self.read("extended", "visual")["loops"]:
            if loop["loop_id"].startswith("mat_"):
                self.assertEqual(loop, before_visual[loop["loop_id"]])

    def test_last_merge_and_rejoin_body_guards_have_a_real_clear_interval(self):
        body = json.loads((PROJECT / "Assets/Resources/Biathlon/occupancy_provisional_v1.json").read_text(encoding="utf-8-sig"))
        half = (body["body_length_m"] + body["personal_clearance_m"]) / 2
        gaps = {}
        for name in self.roots:
            regions = {j["id"]: j for j in self.read(name, "junctions")["junctions"]}
            merge = next(p for p in regions["merge_30"]["ports"] if p["anchor"]["loop_id"] == "exit_aisle")
            rejoin = next(p for p in regions["range_rejoin"]["ports"] if p["anchor"]["loop_id"] == "exit_aisle")
            gaps[name] = (rejoin["anchor"]["distance_m"] - rejoin["guard_before_m"] - half) - (
                merge["anchor"]["distance_m"] + merge["guard_after_m"] + half)
        self.assertLess(gaps["default"], 0, "The historical geometry reproduces the live overlapping-region cause.")
        self.assertGreater(gaps["extended"], 0)
        self.assertAlmostEqual(gaps["extended"] - gaps["default"], self.recipe["exit_tail_extension_m"])

    def test_negative_tail_is_rejected_in_the_recipe(self):
        root = ARTIFACTS / "invalid"
        root.mkdir(parents=True, exist_ok=True)
        recipe = dict(self.recipe, exit_tail_extension_m=-1)
        path = root / "input-recipe.json"
        path.write_text(json.dumps(recipe), encoding="utf-8")
        result = subprocess.run([sys.executable, str(SCRIPTS / "generate_venue01.py"), "--project-root", str(PROJECT),
                                 "--recipe", str(path), "--output-root", str(root)], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)


if __name__ == "__main__":
    unittest.main()
