"""Optional authored traffic policy binding; reject invalid authority before writes."""
import copy
import unittest
from bind_compact_range import bind_departure_policy


class DeparturePolicyTests(unittest.TestCase):
    def fixture(self):
        apron = {"schema_version": 2, "regions": [{"junction_id": "old_guard"}]}
        paths = {"lanes": [{"slices": [{"loop_id": "main"}, {"loop_id": "exit"}]}]}
        track = {"penalty": {"loop_id": "penalty"}}
        junctions = {"junctions": [{"id": "merge", "ports": [
            {"anchor": {"loop_id": x}} for x in ("main", "exit", "penalty")]}]}
        recipe = {"parallel_post_shoot_departures": True, "penalty_world_merge_junctions": ["merge"]}
        return apron, junctions, paths, track, recipe

    def test_missing_policy_keeps_legacy_bytes(self):
        args = list(self.fixture()); args[-1] = {}
        before = copy.deepcopy(args[0]); bind_departure_policy(*args)
        self.assertEqual(before, args[0])

    def test_explicit_policy_keeps_geometry_and_guards_and_copies_ids(self):
        args = self.fixture(); before = copy.deepcopy(args[1:4]); bind_departure_policy(*args)
        self.assertEqual(before, args[1:4])
        self.assertEqual(args[0]["regions"], [{"junction_id": "old_guard"}])
        self.assertEqual(args[0]["penalty_world_merge_junctions"], ["merge"])
        self.assertIsNot(args[0]["penalty_world_merge_junctions"], args[-1]["penalty_world_merge_junctions"])

    def test_invalid_authority_cannot_partially_mutate_apron(self):
        for patch in ({"parallel_post_shoot_departures": 1}, {"parallel_post_shoot_departures": False},
                      {"penalty_world_merge_junctions": []}, {"penalty_world_merge_junctions": ["merge", "merge"]},
                      {"penalty_world_merge_junctions": ["unknown"]}):
            args = self.fixture(); args[-1].update(patch); before = copy.deepcopy(args[0])
            with self.assertRaises(ValueError): bind_departure_policy(*args)
            self.assertEqual(before, args[0])
        args = self.fixture(); args[0]["schema_version"] = 1
        with self.assertRaises(ValueError): bind_departure_policy(*args)

    def test_wrong_world_family_rejected(self):
        args = self.fixture(); args[1]["junctions"][0]["ports"][0]["anchor"]["loop_id"] = "unrelated"
        with self.assertRaises(ValueError): bind_departure_policy(*args)


if __name__ == "__main__": unittest.main()
