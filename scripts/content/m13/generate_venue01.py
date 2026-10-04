"""Draft original Venue01 resources outside Assets. No Unity or source mutation.

Every authored edge is a straight chord with exact whole-millimetre travel; arc
radii are derived from chord lengths. Elevation is solved from two exact slope
edge lengths whose horizontal difference matches the12m aisle separation.
"""
from pathlib import Path
import argparse, json, math, shutil, hashlib, re
from exact_polyline import ridge_span

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root', type=Path, default=SCRIPT_DIRECTORY.parents[2])
parser.add_argument('--output-root', type=Path, default=None)
parser.add_argument('--recipe', type=Path, default=SCRIPT_DIRECTORY/'recipes/woodland_basin_geometry_recipe_v1.json')
arguments = parser.parse_args()
PROJECT = arguments.project_root.resolve()
HERE = (arguments.output_root if arguments.output_root is not None else PROJECT/'artifacts/wp169i/draft').resolve()
RESOURCES = HERE / "resources"
RECIPE = json.loads(arguments.recipe.read_text(encoding='utf-8-sig'))
assert RECIPE['schema_version']==1
compact_range = RECIPE.get("compact_range", False)
if type(compact_range) is not bool:
    raise ValueError("compact_range must be an explicit boolean")
compound_range_rejoin = RECIPE.get("compound_range_rejoin", False)
if type(compound_range_rejoin) is not bool:
    raise ValueError("compound_range_rejoin must be an explicit boolean when supplied")
# Reject malformed optional admission topology before creating or replacing output.
HERE.mkdir(parents=True, exist_ok=True)
VENUE=RECIPE.get('venue_id','woodland_basin_v1')
assert VENUE.replace('_','').isalnum() and VENUE.endswith('_v1')
LABEL=VENUE[:-3]
PREFIX='Biathlon/Venues/'+VENUE
TRACK_ID=RECIPE.get('track_id',LABEL+'_track_v1')
presentation_recipe=SCRIPT_DIRECTORY/'presentation-resource-recipe.json'
if presentation_recipe.resolve()!=(HERE/presentation_recipe.name).resolve():
    if VENUE=='woodland_basin_v1':shutil.copyfile(presentation_recipe,HERE/presentation_recipe.name)
    else:
        presentation=json.loads(presentation_recipe.read_text(encoding='utf-8-sig'))
        presentation['id']=LABEL+'_presentation_resource_recipe_v1';presentation['venue_manifest_resource']=PREFIX+'/manifest'
        (HERE/presentation_recipe.name).write_text(json.dumps(presentation,indent=2)+'\n',encoding='utf-8')

def compact_left_entry(value):
    # Stable lane 1 remains on the right as viewed toward the target bank.
    if isinstance(value,str):
        return re.sub(r'(?<![A-Za-z0-9])(?:mat|lane|fork|outbound_cross|merge|mat_turn)_(\d{2})(?!\d)',
                      lambda m:m.group(0)[:-2]+f'{RECIPE["lane_count"]+1-int(m.group(1)):02d}',value)
    if isinstance(value,list):return [compact_left_entry(x) for x in value]
    if isinstance(value,dict):
        result={k:compact_left_entry(v) for k,v in value.items()}
        if set(('x','y','z'))<=value.keys():result['x']=-value['x']
        if 'curvature_per_m' in value:result['curvature_per_m']=-value['curvature_per_m']
        if 'yaw_deg' in value:result['yaw_deg']=-value['yaw_deg']
        return result
    return value

def write(relative, value):
    if compact_range:
        value=compact_left_entry(value)
        if relative.endswith('/paths'):value['lanes'].reverse()
        if relative.endswith('/manifest'):
            value['range_layout']['apron_depth_m']=RECIPE['apron_depth_m']
            value['range_layout']['mat_definition']=RECIPE['mat_definition']
    path = RESOURCES / (relative + ".json")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    return path

def mm(value):
    result = round(value * 1000)
    assert abs(value - result / 1000) < 1e-7, (value, result)
    return result

def anchor(loop, distance):
    return {"loop_id": loop, "distance_m": mm(distance) / 1000}

class Polyline:
    def __init__(self, loop, point, width):
        self.loop = loop
        self.knots = [{"distance_m": 0., "x": point[0], "y": point[1], "z": point[2]}]
        self.segments = []
        self.width = width
        self.distance_mm = 0

    def edge(self, point, distance, zone="range_approach", curvature=0., exposure=.25, draft=False, sector=None):
        a = self.knots[-1]
        actual = math.dist((a["x"], a["y"], a["z"]), point)
        length_mm = mm(distance)
        assert abs(actual - distance) < 1e-8, (self.loop, point, actual, distance)
        horizontal = math.hypot(point[0] - a["x"], point[2] - a["z"])
        grade = (point[1] - a["y"]) / horizontal
        self.segments.append({"id": f"edge_{len(self.segments):03d}", "length_m": length_mm / 1000,
            "start_grade": grade, "end_grade": grade, "curvature_per_m": curvature,
            "width_m": self.width, "snow_profile_id": "snow_test_packed", "wind_exposure": exposure,
            "terrain_muscle_factor": 1.18 if grade > .02 else 1.,
            "draft_allowed": draft, "overtake_allowed": draft,
            "risk_level": .25 if curvature else .05,
            "semantic_zone": zone})
        overrides=RECIPE.get('course_sectors',{}).get(sector,{})
        assert set(overrides)<= {'width_m','wind_exposure','draft_allowed','overtake_allowed','risk_level','semantic_zone'}
        self.segments[-1].update(overrides)
        self.distance_mm += length_mm
        self.knots.append({"distance_m": self.distance_mm / 1000,
            "x": point[0], "y": point[1], "z": point[2]})

    def arc(self, center, radius, start, end, steps, chord, zone="range_approach", exposure=.25, sector=None):
        direction = 1 if end > start else -1
        for index in range(1, steps + 1):
            angle = start + (end - start) * index / steps
            point = (center[0] + radius * math.cos(angle), center[1], center[2] + radius * math.sin(angle))
            self.edge(point, chord, zone, direction / radius, exposure,sector=sector)

    def definition(self, kind="race", closed=False):
        return {"id": self.loop, "kind": kind, "closed": closed, "segments": self.segments}

    def visual(self):
        return {"loop_id": self.loop, "knots": self.knots}

    def at(self, distance):
        for a, b in zip(self.knots, self.knots[1:]):
            if a["distance_m"] <= distance <= b["distance_m"]:
                t = (distance - a["distance_m"]) / (b["distance_m"] - a["distance_m"])
                return tuple(a[key] + (b[key] - a[key]) * t for key in ("x", "y", "z"))
        raise ValueError(distance)

    def heading(self, distance):
        for a, b in zip(self.knots, self.knots[1:]):
            if a["distance_m"] <= distance < b["distance_m"] or distance == self.distance_mm/1000 and b is self.knots[-1]:
                dx,dz=b["x"]-a["x"],b["z"]-a["z"]
                magnitude=math.hypot(dx,dz)
                return dx/magnitude,dz/magnitude
        raise ValueError(distance)

q = RECIPE["private_quarter_segments"]
chord = RECIPE["private_arc_chord_m"]
radius = chord / (2 * math.sin(math.pi / (4 * q)))
stop_z = -RECIPE["athlete_stop_depth_m"] if "athlete_stop_depth_m" in RECIPE else 0.
inbound = RECIPE["private_inbound_straight_m"]
outbound = RECIPE["private_outbound_straight_m"]
entry_z = stop_z - inbound - 2 * radius
exit_z = stop_z - outbound - 2 * radius
private_length = inbound + outbound + 4 * q * chord
private_stop = q * chord + inbound + q * chord
lane_count = RECIPE["lane_count"]
pitch = RECIPE["lane_pitch_m"]
side = RECIPE["side_margin_m"]
base_aisle_length = (lane_count - 1) * pitch + 2 * side
# Optional counted space after the last mat merge separates it from the common
# rejoin guard. Keep mat positions/pitch and incoming staging unchanged. The
# longer overlay starts earlier within the same exact lap, with no path rescale.
exit_tail_extension = RECIPE.get("exit_tail_extension_m", 0)
assert exit_tail_extension >= 0
mm(exit_tail_extension)
aisle_length = base_aisle_length + exit_tail_extension
right = base_aisle_length / 2
left = right - aisle_length
overlay_length = aisle_length + private_length
overlay_exit = RECIPE["lap_length_m"] - RECIPE["finish_lead_m"]
overlay_entry = overlay_exit - overlay_length

def private_u(loop, x, width):
    line = Polyline(loop, (x, 0, entry_z), width)
    line.arc((x, 0, entry_z + radius), radius, -math.pi/2, -math.pi, q, chord)
    if inbound > 0: line.edge((x-radius, 0, stop_z-radius), inbound)
    line.arc((x, 0, stop_z-radius), radius, math.pi, 0, 2*q, chord)
    line.edge((x+radius, 0, exit_z + radius), outbound)
    line.arc((x, 0, exit_z + radius), radius, 0, -math.pi/2, q, chord)
    # Exact coincidence at seams, not residual trigonometric drift.
    line.knots[-1].update(x=x, y=0., z=exit_z)
    line.knots[q+(1 if inbound > 0 else 0)+q].update(x=x, y=0., z=stop_z)
    assert line.distance_mm == mm(private_length)
    return line

private = [private_u(f"mat_{i+1:02d}", (lane_count*.5-.5-i)*pitch, RECIPE["private_width_m"]) for i in range(lane_count)]
bypass = private_u("bypass", left, RECIPE["main_width_m"])

oq = RECIPE["outer_quarter_segments"]
ochord = RECIPE["outer_arc_chord_m"]
outer_radius = ochord / (2 * math.sin(math.pi / (4 * oq)))
climb = RECIPE["climb_travel_m"]
descent = RECIPE["descent_travel_m"]
separation = entry_z - exit_z
climb_horizontal = (descent**2 - climb**2 - separation**2) / (2 * separation)
rise = math.sqrt(climb**2 - climb_horizontal**2)
outer_arc_total = 4 * oq * ochord
bottom = (overlay_entry - climb - descent - outer_arc_total + aisle_length + RECIPE["finish_lead_m"]) / 2
bottom = mm(bottom) / 1000
west = left - RECIPE["finish_lead_m"] - RECIPE["outer_west_straight_m"]
ridge=None
if 'plateau_controls' in RECIPE:
    bottom,ridge,ridge_chords=ridge_span(RECIPE['plateau_controls'],bottom*2,rise,exit_z-outer_radius-climb_horizontal-outer_radius,RECIPE['plateau_chord_real_slack_m'])
east = west + bottom
top_straight = east - right
south_z = exit_z - outer_radius - climb_horizontal
main = Polyline("main", (left-RECIPE["finish_lead_m"], 0, exit_z), RECIPE["main_width_m"])
main.edge((west, 0, exit_z), RECIPE["outer_west_straight_m"], "stadium", draft=True)
main.arc((west, 0, exit_z-outer_radius), outer_radius, math.pi/2, math.pi, oq, ochord, "rolling",sector='lower_turn')
climb_start=main.distance_mm/1000
main.edge((west-outer_radius, rise, south_z), climb, "climb_long", exposure=.45, draft=True,sector='climb')
climb_end=main.distance_mm/1000
main.arc((west, rise, south_z), outer_radius, math.pi, math.pi*1.5, oq, ochord, "rolling", .6,sector='crest_turn')
ridge_start=main.distance_mm/1000
if ridge is None:main.edge((east, rise, south_z-outer_radius), bottom, "flat", exposure=.95, draft=True)
else:
    ridge=[(x+west,y,z) for x,y,z in ridge]
    for index,(point,chord_mm) in enumerate(zip(ridge[1:],ridge_chords)):
        a=main.knots[-1];dx,dz=point[0]-a['x'],point[2]-a['z'];horizontal=math.hypot(dx,dz)
        previous=(a['x']-main.knots[-2]['x'],a['z']-main.knots[-2]['z'])
        curvature=math.atan2(previous[0]*dz-previous[1]*dx,previous[0]*dx+previous[1]*dz)/horizontal
        main.edge(point,chord_mm/1000,'rolling',curvature,.8,sector=RECIPE['plateau_sectors'][index])
ridge_end=main.distance_mm/1000
main.arc((east, rise, south_z), outer_radius, -math.pi/2, 0, oq, ochord, "descent_technical", .75,sector='descent_turn')
descent_start=main.distance_mm/1000
main.edge((east+outer_radius, 0, entry_z-outer_radius), descent, "descent_fast", exposure=.6, draft=True,sector='descent')
descent_end=main.distance_mm/1000
main.arc((east, 0, entry_z-outer_radius), outer_radius, 0, math.pi/2, oq, ochord, "descent_technical", .35,sector='recovery_turn')
main.edge((right, 0, entry_z), top_straight, "range_approach", exposure=.2, draft=True,sector='recovery')
assert main.distance_mm == mm(overlay_entry), (main.distance_mm, overlay_entry)
# Break shared aisle at every whole-mm fork and crossing coordinate for exact static samples.
cuts = {0., aisle_length}
for i in range(lane_count):
    fork = side + i*pitch
    cuts.add(fork)
    cuts.add(round(fork-radius, 3))
for coordinate in sorted(cuts)[1:]:
    current = main.distance_mm/1000-overlay_entry
    main.edge((right-coordinate, 0, entry_z), round(coordinate-current, 3))
for a, b, segment in zip(bypass.knots, bypass.knots[1:], bypass.segments):
    main.edge((b["x"], b["y"], b["z"]), segment["length_m"], curvature=segment["curvature_per_m"])
assert main.distance_mm == mm(overlay_exit)
main.edge((left-RECIPE["finish_lead_m"], 0, exit_z), RECIPE["finish_lead_m"], "stadium", draft=True)
main.knots[-1].update(x=main.knots[0]["x"], y=main.knots[0]["y"], z=main.knots[0]["z"])
assert main.distance_mm == mm(RECIPE["lap_length_m"])
exit_aisle = Polyline("exit_aisle", (right, 0, exit_z), RECIPE["main_width_m"])
for coordinate in sorted({0., aisle_length, *(side+i*pitch for i in range(lane_count))})[1:]:
    current = exit_aisle.distance_mm/1000
    exit_aisle.edge((right-coordinate, 0, exit_z), coordinate-current)

penalty_steps = RECIPE["penalty_segments"]
penalty_chord = RECIPE["penalty_length_m"] / penalty_steps
penalty_radius = penalty_chord / (2*math.sin(math.pi/penalty_steps))
penalty = Polyline("penalty_150", (left, 0, exit_z), RECIPE["penalty_width_m"])
penalty.arc((left, 0, exit_z-penalty_radius), penalty_radius, math.pi/2, math.pi*2.5, penalty_steps, penalty_chord, "penalty_loop")
penalty.knots[-1].update(x=left, y=0., z=exit_z)
start_q = RECIPE["start_quarter_segments"]
start_chord = RECIPE["start_arc_chord_m"]
start_radius = start_chord / (2 * math.sin(math.pi / (4 * start_q)))
start_straight = RECIPE["start_approach_length_m"] - start_q * start_chord - RECIPE["start_terminal_straight_m"]
start_x = main.knots[0]["x"]
start_arc_x = start_x + RECIPE["start_terminal_straight_m"]
# The grid approaches from the north of the finish lead rather than sharing an
# invisible duplicate forty metres of the main loop. Only the final local merge
# overlaps the westbound finish corridor; that overlap is explicitly guarded.
start = Polyline("start_approach", (start_arc_x+start_radius, 0, exit_z+start_radius+start_straight), RECIPE["main_width_m"])
start.edge((start_arc_x+start_radius, 0, exit_z+start_radius), start_straight, "stadium", draft=True)
start.arc((start_arc_x, 0, exit_z+start_radius), start_radius, 0, -math.pi/2, start_q, start_chord, "stadium")
start.knots[-1].update(x=start_arc_x,y=0.,z=exit_z)
start.edge((start_x,0,exit_z),RECIPE["start_terminal_straight_m"],"stadium",draft=True)
start.knots[-1].update(x=start_x, y=0., z=exit_z)
assert start.distance_mm == mm(RECIPE["start_approach_length_m"])
all_lines = [main, *private, exit_aisle, penalty, start]
line_map = {line.loop: line for line in all_lines}

junctions, connections, purpose = [], [], {}
def port(pid, loop, distance, before, after):
    return {"id": pid, "anchor": anchor(loop, distance), "guard_before_m": before, "guard_after_m": after}
def region(rid, ports, why):
    junctions.append({"id": rid, "ports": ports}); purpose[rid] = why
def seam(cid, rid, source, target):
    connections.append({"id": cid, "junction_id": rid, "from_port_id": source, "to_port_id": target, "kind": "coincident_seam"})
for i, line in enumerate(private):
    name = line.loop; ordinal=i+1; fork=side+i*pitch; cross=round(fork-radius,3)
    rid=f"fork_{ordinal:02d}"
    region(rid,[port(rid+"_main","main",overlay_entry+fork,4,4),port(rid+"_private",name,0,0,7)],"one_mat_fork")
    seam(rid+"_connection",rid,rid+"_main",rid+"_private")
    rid=f"outbound_cross_{ordinal:02d}"; outgoing=round(q*chord+inbound+2*q*chord+inbound+radius,3)
    region(rid,[port(rid+"_main","main",overlay_entry+cross,4,4),port(rid+"_private",name,outgoing,4,4)],"private_return_crosses_entry_aisle")
    rid=f"merge_{ordinal:02d}"
    region(rid,[port(rid+"_private",name,private_length,7,0),port(rid+"_exit","exit_aisle",fork,4,4)],"one_mat_exit_merge")
    seam(rid+"_connection",rid,rid+"_private",rid+"_exit")
    rid=f"mat_turn_{ordinal:02d}"
    if compact_range:
        turn_anchor=max(5.,min(private_length-5.,private_stop))
        region(rid,[port(rid+"_incoming",name,turn_anchor,5,5),port(rid+"_outgoing",name,turn_anchor,5,5)],"private_turn_only_no_other_lane")
    else:
        region(rid,[port(rid+"_incoming",name,q*chord+9,5,5),port(rid+"_outgoing",name,q*chord+9+2*q*chord,5,5)],"private_turn_only_no_other_lane")

rejoin_ports=[port("rejoin_exit","exit_aisle",aisle_length,18,0),port("rejoin_main","main",overlay_exit,18,18),port("rejoin_penalty","penalty_150",0,18,18)]
bypass_ports=[port("bypass_in","main",overlay_entry+aisle_length,7.9,25),port("bypass_out","main",overlay_exit-10,25,25)]
if compound_range_rejoin:
    # These full guards overlap on Main. One conservative region prevents two
    # incumbents owning opposite halves of the same site and waiting for each
    # other. Keep every original port/anchor/guard and all directed seams exact.
    rejoin_ports.extend(bypass_ports)
region("range_rejoin",rejoin_ports,"compound_common_exit_penalty_and_outer_bypass" if compound_range_rejoin else "common_exit_and_penalty_access")
seam("range_rejoin_connection","range_rejoin","rejoin_exit","rejoin_main")
seam("range_penalty_enter","range_rejoin","rejoin_exit","rejoin_penalty")
seam("range_penalty_enter_main","range_rejoin","rejoin_main","rejoin_penalty")
seam("range_penalty_leave","range_rejoin","rejoin_penalty","rejoin_main")
if not compound_range_rejoin:
    region("bypass_turn",bypass_ports,"outer_bypass_only_not_mat_forks")
if not compact_range:
    region("penalty_tail_cross_mat_30",[port("penalty_tail_mat","mat_30",38.8,2,.608),port("penalty_tail_loop","penalty_150",139,3,3)],"local_penalty_overlap_with_last_mat_departure_only")
region("start_join",[port("start_join_end","start_approach",RECIPE["start_approach_length_m"],18,0),port("start_join_main","main",0,18,2)],"single_local_start_to_main_merge")
seam("start_join_connection","start_join","start_join_end","start_join_main")

paths=[]
for i, line in enumerate(private):
    fork=side+i*pitch
    paths.append({"id":f"lane_{i+1:02d}","slices":[{"loop_id":"main","start_m":overlay_entry,"end_m":overlay_entry+fork},
        {"loop_id":line.loop,"start_m":0,"end_m":private_length},{"loop_id":"exit_aisle","start_m":fork,"end_m":aisle_length}],"mat_anchor":anchor(line.loop,private_stop)})
track={"schema_version":1,"id":TRACK_ID,"authoring_limits":{"max_abs_grade":.25,"max_abs_curvature_per_m":RECIPE.get("max_abs_curvature_per_m",1),"min_width_m":1,"max_width_m":20},
    "loops":[line.definition("penalty" if line is penalty else "race",line is main or line is penalty) for line in all_lines],
    "checkpoints":[{"id":"climb_top","anchor":anchor("main",main.knots[oq+2]["distance_m"])},{"id":"plateau_exit","anchor":anchor("main",main.knots[oq*2+3]["distance_m"])}],
    "range":{"id":LABEL+"_range","entry":anchor("main",overlay_entry+5),"firing_line":anchor("main",overlay_entry+side+private_stop),"exit":anchor("main",overlay_exit-5)},
    "penalty":{"loop_id":"penalty_150","entry":anchor("main",overlay_exit),"rejoin":anchor("main",overlay_exit)}}
if ridge is not None:
    track['checkpoints'][1]['anchor']=anchor('main',ridge_end)
write(PREFIX+"/track",track)
write(PREFIX+"/visual",{"schema_version":1,"id":LABEL+"_visual_v1","track_id":TRACK_ID,"sample_spacing_m":2,"loops":[line.visual() for line in all_lines]})
write(PREFIX+"/paths",{"schema_version":1,"id":LABEL+"_paths_v1","track_id":TRACK_ID,"main_loop_id":"main","entry_distance_m":overlay_entry,"exit_distance_m":overlay_exit,"lanes":paths})
write(PREFIX+"/junctions",{"schema_version":1,"id":LABEL+"_junctions_v1","track_id":TRACK_ID,"junctions":junctions})
write(PREFIX+"/connections",{"schema_version":1,"id":LABEL+"_connections_v1","junction_definition_id":LABEL+"_junctions_v1","connections":connections})

art=[("grandstand","Biathlon/Stadium/G2Grandstand/Grandstand",0,{"x":15,"y":0,"z":-45},90),
     ("event_dressing","Biathlon/Stadium/G2Event/Event",0,{"x":0,"y":0,"z":-80},0)]
manifest={"schema_version":2,"id":VENUE,"track_resource":PREFIX+"/track","visual_resource":PREFIX+"/visual","paths_resource":PREFIX+"/paths",
    "start_approach_resource":PREFIX+"/start_approach",
    "start_continuation_resource":PREFIX+"/start_continuation",
    "occupancy_resource":"Biathlon/occupancy_provisional_v1","junctions_resource":PREFIX+"/junctions","connections_resource":PREFIX+"/connections",
    "range_layout":{"approach":"side_entry","entry":anchor("main",overlay_entry),"exit":anchor("main",overlay_exit),"firing_origin":{"x":0,"y":0,"z":0},"forward":{"x":0,"y":0,"z":1},"lane_pitch_m":pitch,"apron_depth_m":30},
    "cameras":[{"id":"range_overview","anchor":anchor("main",overlay_entry+98),"offset":{"x":20,"y":20,"z":-30},"look_offset":{"x":0,"y":1,"z":20},"fov_deg":52},
        {"id":"forest_climb","anchor":anchor("main",450),"offset":{"x":-12,"y":5,"z":-10},"look_offset":{"x":0,"y":1,"z":0},"fov_deg":48},
        {"id":"plateau_broadcast","anchor":anchor("main",1300),"offset":{"x":10,"y":4,"z":-12},"look_offset":{"x":0,"y":1,"z":0},"fov_deg":48}],
    "ambience":[{"id":"stadium_crowd","anchor":anchor("main",overlay_entry+98),"event_id":"ambience_stadium_crowd","radius_m":120},{"id":"forest_wind","anchor":anchor("main",600),"event_id":"ambience_forest_wind","radius_m":100}],
    "assets":[{"id":aid,"resource":resource,"anchor":anchor("main",overlay_entry+d),"offset":offset,"yaw_deg":yaw} for aid,resource,d,offset,yaw in art]}
if ridge is not None:
    manifest['cameras'][1]['anchor']=anchor('main',round((climb_start+climb_end)/2,3))
    manifest['cameras'][2]['id']='ridge_broadcast';manifest['cameras'][2]['anchor']=anchor('main',round(ridge_start+(ridge_end-ridge_start)*.25,3))
    manifest['ambience'][1]['anchor']=anchor('main',round(climb_start+(climb_end-climb_start)*.35,3))
write(PREFIX+"/manifest",manifest)
write(PREFIX+"/start_approach",{"schema_version":1,"id":LABEL+"_start_approach_v1","track_id":TRACK_ID,"loop_id":start.loop,"route_entry_loop_id":"main"})
main_prefix_point=main.at(10)
write(PREFIX+"/start_continuation",{"schema_version":1,"id":LABEL+"_start_continuation_v1","track_id":TRACK_ID,
    "start_approach_id":LABEL+"_start_approach_v1","connection_id":"start_join_connection","occupancy_definition_id":"occupancy_provisional_v1",
    "validation_policy_resource":"Biathlon/Venues/production_venue_validation_v1",
    "corridors":[{"loop_id":"start_approach","knots":start.knots},{"loop_id":"main","knots":[main.knots[0],
        {"distance_m":10,"x":main_prefix_point[0],"y":main_prefix_point[1],"z":main_prefix_point[2]}]}]})
profile={"schema_version":1,"id":LABEL+"_mass_start_men_v1","rules_revision":"production_distance_structure_v1","format":2,"category":0,"distance_m":15000,"laps":5,"start_type":2,"start_interval_s":0,"field_capacity":30,"shots_per_bout":5,"penalty_loop_m":150,
    "shooting_bouts":[{"id":f"bout_{i+1}","after_lap":i+1,"stance":0 if i<2 else 1} for i in range(4)]}
write(PREFIX+"/mass_start_profile",profile)
write(PREFIX+"/mass_start_route",{"schema_version":1,"id":LABEL+"_mass_start_route_v1","track_id":TRACK_ID,"competition_profile_id":profile["id"],"laps":[{"id":f"lap_{i+1}","loop_ids":["main"]} for i in range(5)]})
for relative in ("Biathlon/occupancy_provisional_v1.json","Biathlon/Venues/production_venue_validation_v1.json"):
    destination=RESOURCES/relative;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(PROJECT/"Assets/Resources"/relative,destination)
for _,resource,_,_,_ in art:
    destination=RESOURCES/(resource+".prefab");destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(PROJECT/"Assets/Resources"/(resource+".prefab"),destination)
max_edge_error=max(abs(math.dist((a["x"],a["y"],a["z"]),(b["x"],b["y"],b["z"]))-segment["length_m"]) for line in all_lines for a,b,segment in zip(line.knots,line.knots[1:],line.segments))
for path in paths:
    assert sum(mm(s["end_m"])-mm(s["start_m"]) for s in path["slices"])==mm(overlay_length)
    assert path["slices"][0]["end_m"]-path["slices"][0]["start_m"]>=RECIPE["minimum_seam_clearance_m"]
    assert path["slices"][-1]["end_m"]-path["slices"][-1]["start_m"]>=RECIPE["minimum_seam_clearance_m"]
for junction in junctions:
    if purpose[junction["id"]] in ("one_mat_fork","private_return_crosses_entry_aisle","one_mat_exit_merge"):
        for p in junction["ports"]:
            if p["anchor"]["loop_id"].startswith("mat_"):
                if not compact_range: assert not p["anchor"]["distance_m"]-p["guard_before_m"]<=private_stop<=p["anchor"]["distance_m"]+p["guard_after_m"]

def region_contains(junction, loop, coordinate):
    length = line_map[loop].distance_mm / 1000
    closed = loop in ("main", "penalty_150")
    for p in junction["ports"]:
        if p["anchor"]["loop_id"] != loop:
            continue
        for shift in (-1, 0, 1) if closed else (0,):
            at = p["anchor"]["distance_m"] + shift * length
            if at-p["guard_before_m"]-1e-9 <= coordinate <= at+p["guard_after_m"]+1e-9:
                return True
    return False

mat_members = {j["id"]:[line.loop for line in private if region_contains(j, line.loop, private_stop)] for j in junctions}
fork_members = {j["id"]:[p["id"] for p in paths if region_contains(j, p["slices"][0]["loop_id"], p["slices"][0]["end_m"])] for j in junctions}
assert max(map(len, mat_members.values())) == 1
if not compact_range: assert max(map(len, fork_members.values())) <= 1, {key:value for key,value in fork_members.items() if len(value)>1}
occupancy = json.loads((RESOURCES/"Biathlon/occupancy_provisional_v1.json").read_text(encoding="utf-8-sig"))
body_width_with_clearance = occupancy["body_width_m"] + occupancy["personal_clearance_m"]
private_width_with_clearance = RECIPE["private_width_m"] + occupancy["personal_clearance_m"]
minimum_mat_center_separation = min(math.dist(a.at(private_stop), b.at(private_stop)) for i,a in enumerate(private) for b in private[i+1:])
neighbor_private_corridor_bound_gap = pitch - (2*radius+private_width_with_clearance)
assert minimum_mat_center_separation > body_width_with_clearance
if not compact_range: assert neighbor_private_corridor_bound_gap > 0
seam_gaps=[]
port_map={p["id"]:p for j in junctions for p in j["ports"]}
for c in connections:
    a,b=port_map[c["from_port_id"]]["anchor"],port_map[c["to_port_id"]]["anchor"]
    seam_gaps.append(math.dist(line_map[a["loop_id"]].at(a["distance_m"]), line_map[b["loop_id"]].at(b["distance_m"])))
assert max(seam_gaps) < 1e-8

def body_rectangle(position, forward):
    half_length=(occupancy["body_length_m"]+occupancy["personal_clearance_m"])/2
    half_width=body_width_with_clearance/2
    fx,fz=forward;rx,rz=fz,-fx;x,_,z=position
    return [(x+f*fx*half_length+r*rx*half_width,z+f*fz*half_length+r*rz*half_width) for f,r in ((1,1),(-1,1),(-1,-1),(1,-1))]

def separated(a,b):
    for polygon in (a,b):
        for p,q in zip(polygon,polygon[1:]+polygon[:1]):
            axis=(q[1]-p[1],p[0]-q[0])
            aproj=[x*axis[0]+z*axis[1] for x,z in a]
            bproj=[x*axis[0]+z*axis[1] for x,z in b]
            if max(aproj)<min(bproj)-1e-10 or max(bproj)<min(aproj)-1e-10:
                return True
    return False

mat_bodies=[body_rectangle(line.at(private_stop),(0,1)) for line in private]
assert all(separated(a,b) for i,a in enumerate(mat_bodies) for b in mat_bodies[i+1:])
grid_definition=json.loads((PROJECT/"Assets/Resources/Biathlon/mass_start_grid_provisional_v1.json").read_text(encoding="utf-8-sig"))
grid_bodies=[]
for i in range(lane_count):
    row,column=divmod(i,grid_definition["columns"])
    d=RECIPE["start_approach_length_m"]-row*grid_definition["row_spacing_m"]
    line=main if row==0 else start
    at=0 if row==0 else d
    center=line.at(at);forward=line.heading(at)
    lateral=(column-(grid_definition["columns"]-1)/2)*grid_definition["column_spacing_m"]
    center=(center[0]+lateral*forward[1],center[1],center[2]-lateral*forward[0])
    grid_bodies.append(body_rectangle(center,forward))
assert all(separated(a,b) for i,a in enumerate(grid_bodies) for b in grid_bodies[i+1:])
audit={"schema_version":1,"id":LABEL+"_geometry_draft_audit_v1","status":"DRAFT_STATIC_VALIDATION_PENDING","recipe":RECIPE,
    "loop_count":len(all_lines),"lane_count":len(paths),"main_length_m":main.distance_mm/1000,"penalty_length_m":penalty.distance_mm/1000,
    "private_length_m":private_length,"private_firing_m":private_stop,"range_interval_m":[overlay_entry,overlay_exit],"climb_rise_m":rise,
    "entry_z":entry_z,"exit_z":exit_z,"maximum_world_edge_error_m":max_edge_error,"guard_region_count":len(junctions),"connection_count":len(connections),
    "guard_purposes":purpose,"guard_mat_members":mat_members,"guard_main_fork_members":fork_members,
    "maximum_mat_stops_per_exclusive_region":max(map(len,mat_members.values())),"maximum_forks_per_exclusive_region":max(map(len,fork_members.values())),
    "minimum_mat_center_separation_m":minimum_mat_center_separation,"stationary_mat_body_gap_m":minimum_mat_center_separation-body_width_with_clearance,
    "neighbor_private_corridor_bound_gap_m":neighbor_private_corridor_bound_gap,"maximum_directed_seam_world_gap_m":max(seam_gaps),
    "start_approach_length_m":start.distance_mm/1000,"start_approach_radius_m":start_radius,"start_guard_length_m":18,
    "start_seam_tangent":list(start.heading(start.distance_mm/1000)),"main_start_tangent":list(main.heading(0)),
    "stationary_mat_oriented_body_pair_count":len(mat_bodies)*(len(mat_bodies)-1)//2,"stationary_mat_oriented_body_checks":"PASS",
    "mass_start_grid_oriented_body_pair_count":len(grid_bodies)*(len(grid_bodies)-1)//2,"mass_start_grid_oriented_body_checks":"PASS_WITH_CANONICAL_GRID_AND_OCCUPANCY",
    "grid_definition_resource":"Biathlon/mass_start_grid_provisional_v1","grid_definition_sha256":hashlib.sha256((PROJECT/"Assets/Resources/Biathlon/mass_start_grid_provisional_v1.json").read_bytes()).hexdigest(),
    "lane_assignment_and_runtime":"NOT_PROVED", "simultaneous_mat_static_body_envelope":"PASS_WITH_CANONICAL_OCCUPANCY_WIDTH_AND_SHOOTING_FORWARD", "visual_acceptance":"PENDING",
    "source_file_hashes":{str(p.relative_to(RESOURCES)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in RESOURCES.rglob('*') if p.is_file() and '/environment/' not in str(p.relative_to(RESOURCES)).replace('\\','/') and p.name!='environment.json'}}
if ridge is not None:
    audit['milestones_m']={'climb_start':climb_start,'climb_end':climb_end,'ridge_start':ridge_start,'ridge_end':ridge_end,'descent_start':descent_start,'descent_end':descent_end}
    audit['plateau_span_m']=bottom;audit['plateau_actual_travel_m']=sum(ridge_chords)/1000
    audit['plateau_control_points_world']=ridge
(HERE/"recipe.json").write_text(json.dumps(RECIPE,indent=2)+"\n",encoding="utf-8")
(HERE/"audit.json").write_text(json.dumps(audit,indent=2)+"\n",encoding="utf-8")

# Standalone scientific/engineering plot; no imported images or Unity mutation.
def plot_svg(path, bounds, title):
    x0,z0,x1,z1=bounds;width,height=1000,650;padding=40
    scale=min((width-2*padding)/(x1-x0),(height-2*padding)/(z1-z0))
    def xy(x,z):return padding+(x-x0)*scale,height-padding-(z-z0)*scale
    output=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="1000" height="650" fill="#edf4fa"/>',f'<text x="30" y="25" font-family="sans-serif" font-size="18">{title}</text>']
    colors={"main":"#285f91","exit_aisle":"#087f8c","penalty_150":"#b66a14","start_approach":"#6a43a1"}
    for line in all_lines:
        points=' '.join(f'{xy(k["x"],k["z"])[0]:.3f},{xy(k["x"],k["z"])[1]:.3f}' for k in line.knots)
        color=colors.get(line.loop,"#b14b62")
        output.append(f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{max(1,line.width*scale):.3f}" opacity=".65"/>')
    for i,line in enumerate(private):
        x,_,z=line.at(private_stop);px,py=xy(x,z)
        output.append(f'<circle cx="{px:.3f}" cy="{py:.3f}" r="{max(2,.6*scale):.3f}" fill="#102632"/>')
        if i%5==0:
            output.append(f'<text x="{px:.3f}" y="{py-7:.3f}" font-family="sans-serif" font-size="10">{i+1}</text>')
    output.append('<text x="30" y="632" font-family="sans-serif" font-size="13">Blue main | Rose private mats | Teal shared exit | Orange penalty | Purple start</text></svg>')
    path.write_text('\n'.join(output)+'\n',encoding="utf-8")
plot_svg(HERE/"stadium-plan.svg",(-160,-80,115,15),"Venue01: 30-mat side-entry stadium geometry (metres)")
plot_svg(HERE/"course-plan.svg",(-410,-720,490,25),"Venue01: exact 3 km lap, 150 m penalty, 40 m start approach")
print(json.dumps({k:audit[k] for k in ("main_length_m","penalty_length_m","lane_count","private_length_m","maximum_world_edge_error_m","guard_region_count","connection_count")},indent=2))
