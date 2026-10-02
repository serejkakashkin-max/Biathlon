"""Retain complete physical guard coverage while joining one overlapping site."""
from pathlib import Path
import json
import subprocess
import sys
import tempfile
import unittest

PROJECT = Path(__file__).resolve().parents[3]
SCRIPTS = Path(__file__).resolve().parent
PREFIX = Path("resources/Biathlon/Venues/woodland_basin_v1")


class CompoundRangeRejoinTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix="biathlon-compound-rejoin-")
        cls.root = Path(cls.temp.name)
        cls.recipe = json.loads((SCRIPTS / "recipes/woodland_basin_geometry_recipe_v1.json").read_text(encoding="utf-8-sig"))
        cls.roots = {}
        for name, flag in (("omitted", None), ("separate", False), ("compound", True)):
            recipe = dict(cls.recipe)
            recipe.pop("compound_range_rejoin", None)
            if flag is not None:
                recipe["compound_range_rejoin"] = flag
            path = cls.root / (name + ".json")
            path.write_text(json.dumps(recipe), encoding="utf-8")
            output = cls.root / name
            result = cls.generate(path, output)
            if result.returncode:
                raise AssertionError(result.stdout + result.stderr)
            cls.roots[name] = output

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    @staticmethod
    def generate(recipe, output):
        return subprocess.run([sys.executable, str(SCRIPTS / "generate_venue01.py"), "--project-root", str(PROJECT),
                               "--recipe", str(recipe), "--output-root", str(output)], capture_output=True, text=True)

    def read(self, name, resource):
        return json.loads((self.roots[name] / PREFIX / (resource + ".json")).read_text(encoding="utf-8-sig"))

    def files(self, name):
        root = self.roots[name] / "resources"
        return {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*.json")}

    def test_omitted_defaults_to_exact_separate_resource_bytes(self):
        self.assertEqual(self.files("omitted"), self.files("separate"))
        regions = {j["id"] for j in self.read("omitted", "junctions")["junctions"]}
        self.assertIn("bypass_turn", regions)

    def test_compound_changes_only_junctions_and_preserves_all_original_ports(self):
        before, after = self.files("separate"), self.files("compound")
        self.assertEqual(before.keys(), after.keys())
        self.assertEqual([key for key in before if before[key] != after[key]], [(PREFIX / "junctions.json").relative_to("resources").as_posix()])
        old = self.read("separate", "junctions")["junctions"]
        new = self.read("compound", "junctions")["junctions"]
        ports = lambda regions: {p["id"]: p for j in regions for p in j["ports"]}
        self.assertEqual(ports(old), ports(new))
        self.assertEqual(len(new), len(old) - 1)
        old_regions, new_regions = ({j["id"]: j for j in regions} for regions in (old, new))
        self.assertEqual(new_regions["range_rejoin"]["ports"], old_regions["range_rejoin"]["ports"] + old_regions["bypass_turn"]["ports"])
        for key in old_regions.keys() - {"range_rejoin", "bypass_turn"}:
            self.assertEqual(old_regions[key], new_regions[key])

    def test_real_stalled_points_share_one_full_body_guard_only_when_compound(self):
        body = json.loads((PROJECT / "Assets/Resources/Biathlon/occupancy_provisional_v1.json").read_text(encoding="utf-8-sig"))
        half_nm = round((body["body_length_m"] + body["personal_clearance_m"]) * 500000000)
        self.assertEqual(half_nm, 750000000)
        def touches(junction, loop, local, length):
            for port in junction["ports"]:
                if port["anchor"]["loop_id"] != loop:
                    continue
                anchor = round(port["anchor"]["distance_m"] * 1000000000)
                before = round(port["guard_before_m"] * 1000000000) + half_nm
                after = round(port["guard_after_m"] * 1000000000) + half_nm
                if any(anchor - before <= local + shift <= anchor + after for shift in (-length, 0, length)):
                    return True
            return False
        for name in ("separate", "compound"):
            regions = self.read(name, "junctions")["junctions"]
            main = {j["id"] for j in regions if touches(j, "main", 2931249724957, 3000000000000)}
            penalty = {j["id"] for j in regions if touches(j, "penalty_150", 149999542189, 150000000000)}
            self.assertEqual(main & penalty, {"range_rejoin"} if name == "compound" else set())
            self.assertEqual(main, {"range_rejoin"} if name == "compound" else {"bypass_turn"})
            self.assertEqual(penalty, {"range_rejoin"})

    def test_malformed_flag_rejects_before_creating_or_mutating_output(self):
        for number, value in enumerate((None, 0, 1, "true", [], {})):
            with self.subTest(value=value):
                recipe = self.root / ("invalid" + str(number) + ".json")
                recipe.write_text(json.dumps(dict(self.recipe, compound_range_rejoin=value)), encoding="utf-8")
                absent = self.root / ("absent" + str(number))
                self.assertNotEqual(self.generate(recipe, absent).returncode, 0)
                self.assertFalse(absent.exists())
                existing = self.root / ("existing" + str(number))
                existing.mkdir()
                sentinel = existing / "presentation-resource-recipe.json"
                sentinel.write_bytes(b"accepted existing output")
                self.assertNotEqual(self.generate(recipe, existing).returncode, 0)
                self.assertEqual(list(existing.iterdir()), [sentinel])
                self.assertEqual(sentinel.read_bytes(), b"accepted existing output")


if __name__ == "__main__":
    unittest.main()
