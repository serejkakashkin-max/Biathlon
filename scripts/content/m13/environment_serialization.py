"""Quantize an explicit tree's X/Z before sampling its serialized terrain."""


def grounded_tree_position(x, z, sample_ground):
    x, z = round(x, 6), round(z, 6)
    return {'x': x, 'y': round(sample_ground(x, z), 6), 'z': z}
