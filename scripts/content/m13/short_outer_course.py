"""Recipe-bound short Woodland exterior; range/start/penalty geometry is separate.

All paid edges remain exact XYZ whole-millimetre chords.  The two explicit
straight windows are retained for the existing guarded format branch solver.
The solve changes the forest depth, never metres, skier speed or stadium scale.
"""
import math
from exact_polyline import exact_paired_chords


def _bezier(a, b, c, d, steps):
    points = []
    for index in range(steps + 1):
        t = index / steps
        s = 1 - t
        points.append(tuple(s ** 3 * a[j] + 3 * s * s * t * b[j]
                            + 3 * s * t * t * c[j] + t ** 3 * d[j]
                            for j in range(3)))
    points[0], points[-1] = a, d
    return points


def build_short_outer_course(spec, start_x, entry_right_x, exit_z, entry_z, exterior_mm):
    required = {"schema_version", "id", "launch_straight_m", "corner_radius_m",
                "return_straight_m", "format_window_m", "curve_samples",
                "leg_side_sway_m", "quantization_slack_m", "heights_m",
                "depth_bounds_m"}
    if set(spec) != required or spec["schema_version"] != 1 or not spec["id"]:
        raise ValueError("short_outer_course requires its complete versioned recipe")
    steps = spec["curve_samples"]
    if type(steps) is not int or not 8 <= steps <= 128 or steps % 2:
        raise ValueError("curve_samples must be an even integer in [8,128]")
    heights = spec["heights_m"]
    if set(heights) != {"lower_corner", "early_rise_control", "climb_window_start",
                       "climb_window_end", "crest", "ridge_dip", "summit",
                       "descent_corner", "descent_window_end", "recovery_corner"}:
        raise ValueError("all short-course height controls are required")
    numbers = [spec[key] for key in ("launch_straight_m", "corner_radius_m",
               "return_straight_m", "format_window_m", "leg_side_sway_m",
               "quantization_slack_m")] + list(heights.values()) + spec["depth_bounds_m"]
    if any(type(value) not in (int, float) or not math.isfinite(value) for value in numbers):
        raise ValueError("short-course dimensions must be finite numbers")
    if any(spec[key] <= 0 for key in ("launch_straight_m", "corner_radius_m",
                                     "return_straight_m", "format_window_m", "quantization_slack_m")):
        raise ValueError("short-course distances must be positive")
    if spec["leg_side_sway_m"] < 0 or spec["quantization_slack_m"] > .1:
        raise ValueError("invalid short-course sway or quantization slack")
    launch_mm = round(spec["launch_straight_m"] * 1000)
    return_mm = round(spec["return_straight_m"] * 1000)
    if abs(launch_mm / 1000 - spec["launch_straight_m"]) > 1e-10 or abs(return_mm / 1000 - spec["return_straight_m"]) > 1e-10:
        raise ValueError("launch/return distances must be whole millimetres")
    target_mm = exterior_mm - launch_mm - return_mm
    if target_mm <= 0:
        raise ValueError("no paid exterior curve budget remains")
    r, paid_window = spec["corner_radius_m"], spec["format_window_m"]
    west = start_x - spec["launch_straight_m"]
    left = west - r
    return_x = entry_right_x + spec["return_straight_m"]
    right = return_x + r
    sway = spec["leg_side_sway_m"]
    h = heights
    window_l = math.sqrt(paid_window**2-(h["climb_window_end"]-h["climb_window_start"])**2)
    window_r = math.sqrt(paid_window**2-(h["descent_window_end"]-h["descent_corner"])**2)
    # Standard cubic quarter-circle approximation.  The paid geometry below is
    # the generated polyline itself, not this idealised curve's nominal length.
    k = 4 * (math.sqrt(2) - 1) / 3

    def raw(depth):
        south = exit_z - depth
        left_leg = depth - 2 * r
        right_leg = depth + entry_z - exit_z - 2 * r
        if min(left_leg-window_l, right_leg-window_r) <= 10:
            raise ValueError("forest depth does not fit guarded format windows")
        la, ra = (left_leg - window_l) / 2, (right_leg - window_r) / 2
        left0, left1 = exit_z - r, south + r
        right0, right1 = south + r, entry_z - r
        grade_l = (h["climb_window_end"] - h["climb_window_start"]) / window_l
        grade_r = (h["descent_window_end"] - h["descent_corner"]) / window_r
        parts = []
        def curve(name, a, b, c, d):
            parts.append((name, _bezier(a, b, c, d, steps)))
        def straight(name, a, b):
            # One exact paid straight is subdivided only at its midpoint to fit
            # the shared paired-chord contract; no cosmetic spline is used.
            parts.append((name, [a, tuple((x + y) / 2 for x, y in zip(a, b)), b]))
        def leg(name, a, d, offset, middle_y, start_grade=0., end_grade=0.):
            middle = (a[0]+offset, middle_y, (a[2]+d[2])/2)
            span = abs(d[2]-a[2])
            dz = d[2]-a[2]
            first = _bezier(a, (a[0],a[1]+start_grade*span/6,a[2]+dz/6),
                (middle[0],middle[1],middle[2]-dz/6), middle, steps)
            second = _bezier(middle, (middle[0],middle[1],middle[2]+dz/6),
                (d[0],d[1]-end_grade*span/6,d[2]-dz/6),d,steps)
            parts.append((name,first+second[1:]))
        curve("early_turn", (west, 0., exit_z), (west-k*r, 0., exit_z),
              (left, h["lower_corner"], left0+k*r), (left, h["lower_corner"], left0))
        leg("early_rolling", (left,h["lower_corner"],left0),
            (left,h["climb_window_start"],left0-la),-sway,h["early_rise_control"],end_grade=grade_l)
        straight("climb_window", (left, h["climb_window_start"], left0-la),
                 (left, h["climb_window_end"], left0-la-window_l))
        leg("crest_climb", (left,h["climb_window_end"],left0-la-window_l),
            (left,h["crest"],left1),-sway,(h["climb_window_end"]+h["crest"])/2,start_grade=grade_l)
        curve("crest_turn", (left, h["crest"], left1), (left, h["crest"], left1-k*r),
              (left+r-k*r, h["crest"], south), (left+r, h["crest"], south))
        middle = (left + right) / 2
        ridge_half = middle - (left+r)
        curve("ridge_descent", (left+r, h["crest"], south),
              (left+r+ridge_half/3, h["crest"], south),
              (middle-ridge_half/3, h["ridge_dip"], south-sway),
              (middle, h["ridge_dip"], south-sway))
        curve("ridge_climb", (middle, h["ridge_dip"], south-sway),
              (middle+ridge_half/3, h["ridge_dip"], south-sway),
              (right-r-ridge_half/3, h["summit"], south), (right-r, h["summit"], south))
        curve("technical_descent_turn", (right-r, h["summit"], south),
              (right-r+k*r, h["summit"], south), (right, h["descent_corner"], right0-k*r),
              (right, h["descent_corner"], right0))
        leg("descent_roll",(right,h["descent_corner"],right0),
            (right,h["descent_corner"],right0+ra),sway,h["descent_corner"],end_grade=grade_r)
        straight("descent_window", (right, h["descent_corner"], right0+ra),
                 (right, h["descent_window_end"], right0+ra+window_r))
        leg("late_rolling",(right,h["descent_window_end"],right0+ra+window_r),
            (right,h["recovery_corner"],right1),sway,(h["descent_window_end"]+h["recovery_corner"])/2,start_grade=grade_r)
        curve("recovery_turn", (right, h["recovery_corner"], right1),
              (right, h["recovery_corner"], right1+k*r), (return_x+k*r, 0., entry_z),
              (return_x, 0., entry_z))
        points = parts[0][1][:]
        sections = [(parts[0][0], 0, len(points)-1)]
        for name, section in parts[1:]:
            begin = len(points) - 1
            points.extend(section[1:])
            sections.append((name, begin, len(points)-1))
        return points, sections

    low, high = spec["depth_bounds_m"]
    if not 0 < low < high:
        raise ValueError("depth_bounds_m must be a positive increasing pair")
    target = target_mm / 1000 - spec["quantization_slack_m"]
    def total(depth):
        points, _ = raw(depth)
        return sum(math.dist(a, b) for a, b in zip(points, points[1:]))
    if not total(low) < target < total(high):
        raise ValueError("paid curve budget cannot fit within the recipe depth bounds")
    for _ in range(80):
        middle = (low + high) / 2
        if total(middle) < target:
            low = middle
        else:
            high = middle
    depth = (low + high) / 2
    raw_points, sections = raw(depth)
    # The two straight windows have exact paid XYZ length already.  Their odd
    # controls must never absorb a quantisation remainder: that would introduce
    # a false sideways knot and invalidate the guarded format solver's plane.
    paid_window_mm = round(paid_window*1000)
    if abs(paid_window_mm/1000-paid_window) > 1e-10 or paid_window_mm % 2:
        raise ValueError("format_window_m must have an even whole-millimetre length")
    budgets = []
    for name, begin, end in sections:
        distance = sum(math.dist(a,b) for a,b in zip(raw_points[begin:end],raw_points[begin+1:end+1]))
        budget = paid_window_mm if name.endswith('_window') else math.floor(distance*1000)
        if not name.endswith('_window'):
            minimum = sum(max(math.floor(math.dist(raw_points[index],raw_points[index+1])*1000)
                              + math.floor(math.dist(raw_points[index+1],raw_points[index+2])*1000),
                              math.ceil(math.dist(raw_points[index],raw_points[index+2])*1000-1e-8))
                          for index in range(begin,end,2))
            budget = max(budget,minimum)
        budgets.append([name, begin, end, budget, distance*1000-budget])
    remaining = target_mm-sum(part[3] for part in budgets)
    if remaining < 0:
        raise AssertionError("curve section budgets exceed their paid exterior")
    candidates = sorted((part for part in budgets if not part[0].endswith('_window')),
                        key=lambda part:-part[4])
    for index in range(remaining):
        candidates[index % len(candidates)][3] += 1
    points, lengths = [], []
    for name, begin, end, budget, _ in budgets:
        section = raw_points[begin:end+1]
        if name.endswith('_window'):
            paid = [budget//2,budget//2]
        else:
            section, paid = exact_paired_chords(section,budget)
        points.extend(section if not points else section[1:])
        lengths.extend(paid)
    distances = [launch_mm]
    for paid in lengths:
        distances.append(distances[-1] + paid)
    labels = [None] * len(lengths)
    windows = {}
    milestones = {}
    for name, begin, end in sections:
        labels[begin:end] = [name] * (end-begin)
        milestones[name] = {"start_m": distances[begin]/1000, "end_m": distances[end]/1000}
        if name.endswith("_window"):
            windows[name] = milestones[name]
    points = [(start_x, 0., exit_z)] + points + [(entry_right_x, 0., entry_z)]
    lengths = [launch_mm] + lengths + [return_mm]
    labels = ["stadium_launch"] + labels + ["range_recovery"]
    if sum(lengths) != exterior_mm:
        raise AssertionError("short exterior lost paid travel")
    return points, lengths, labels, {"depth_m": depth, "milestones": milestones,
           "format_windows": windows, "maximum_height_m": max(p[1] for p in points),
           "first_turn_start_m": launch_mm/1000, "exterior_distance_m": exterior_mm/1000}
