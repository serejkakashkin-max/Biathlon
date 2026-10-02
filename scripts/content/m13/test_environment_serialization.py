"""Serialization precision must not detach explicit forest feet from terrain."""
import unittest
from environment_serialization import grounded_tree_position


class ForestSerializationTests(unittest.TestCase):
    def test_serialized_coordinates_determine_ground_height(self):
        # A clipped terrain triangle can be locally steeper than the Main.
        sample = lambda x, z: 100 + 4 * x + 4 * z
        x, z = .00000049, .00000049
        old = {'x': round(x, 6), 'y': round(sample(x, z), 6), 'z': round(z, 6)}
        self.assertGreater(abs(old['y'] - sample(old['x'], old['z'])), 2e-6)
        position = grounded_tree_position(x, z, sample)
        self.assertEqual(position, {'x': 0., 'y': 100., 'z': 0.})
        self.assertLessEqual(abs(position['y'] - sample(position['x'], position['z'])), .5e-6)
        self.assertEqual(grounded_tree_position(x, z, sample), position)


if __name__ == '__main__':
    unittest.main()
