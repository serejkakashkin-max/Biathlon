"""Generate read-only-derived Venue01 presentation data outside Unity Assets.

Terrain samples are world Y in row-major Z/X order. Separate physical loops
remain separate corridor arrays. All forest trees are explicit world instances.
"""
from pathlib import Path
import argparse, bisect, hashlib, json, math, re, struct
from corridor_sampling import authored_corridor_widths

def float32(value):
    """Match the Vector3 conversion and arithmetic used by TrackRibbonBuilder."""
    return struct.unpack('f',struct.pack('f',value))[0]

def ribbon_triangles(track,visual):
    """Every actual ribbon row, including outgoing turns and terminal seams."""
    definitions={loop['id']:loop for loop in track['loops']}
    for loop in visual['loops']:
        authored=definitions[loop['loop_id']];knots=loop['knots']
        total_mm=0;ends=[];distances={0.,knots[-1]['distance_m']}
        for segment in authored['segments']:
            distances.add(total_mm/1000);total_mm+=round(segment['length_m']*1000);ends.append(total_mm)
        length=total_mm/1000;steps=math.ceil(length/visual['sample_spacing_m'])
        distances.update(length*i/steps for i in range(1,steps))
        distances.update(knot['distance_m'] for knot in knots)
        knot_distances=[knot['distance_m'] for knot in knots];rows=[]
        for distance in sorted(distances):
            index=min(bisect.bisect_right(knot_distances,distance),len(knots)-1)
            a,b=knots[index-1],knots[index];fraction=(distance-a['distance_m'])/(b['distance_m']-a['distance_m'])
            center=[float32(a[axis]+(b[axis]-a[axis])*fraction) for axis in ('x','y','z')]
            dx,dz=b['x']-a['x'],b['z']-a['z'];norm=math.hypot(dx,dz)
            width_index=min(bisect.bisect_right(ends,round(round(distance,3)*1000)),len(ends)-1)
            half=float32(authored['segments'][width_index]['width_m']/2)
            side=[float32(float32(dz/norm)*half),0.,float32(float32(-dx/norm)*half)]
            rows.append((distance,[float32(center[i]-side[i]) for i in range(3)],[float32(center[i]+side[i]) for i in range(3)]))
        for prior,current in zip(rows,rows[1:]):
            for triangle in ((prior[1],current[1],prior[2]),(prior[2],current[1],current[2])):
                yield loop['loop_id'],prior[0],current[0],triangle

def cross_xz(a,b,p):
    return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])

def clipped_polygon(polygon,triangle):
    """Convex triangle overlay; extrema of the height difference are vertices."""
    orientation=1 if cross_xz(triangle[0],triangle[1],triangle[2])>=0 else -1
    for a,b in zip(triangle,triangle[1:]+triangle[:1]):
        if not polygon:return []
        output=[];prior=polygon[-1];prior_side=orientation*cross_xz(a,b,prior)
        for point in polygon:
            side=orientation*cross_xz(a,b,point)
            if (side>=-1e-9)!=(prior_side>=-1e-9):
                fraction=prior_side/(prior_side-side)
                output.append((prior[0]+(point[0]-prior[0])*fraction,prior[1]+(point[1]-prior[1])*fraction))
            if side>=-1e-9:output.append(point)
            prior=point;prior_side=side
        polygon=output
    return polygon

def triangle_height(triangle,x,z):
    a,b,c=triangle;det=(b[0]-a[0])*(c[2]-a[2])-(b[2]-a[2])*(c[0]-a[0])
    if abs(det)<1e-12:return None
    u=((x-a[0])*(c[2]-a[2])-(z-a[2])*(c[0]-a[0]))/det
    v=((b[0]-a[0])*(z-a[2])-(b[2]-a[2])*(x-a[0]))/det
    return a[1]+u*(b[1]-a[1])+v*(c[1]-a[1])

def cap_terrain_below_ribbons(grid,spacing,triangles,clearance):
    """Lower only vertices of intersecting cells with a clearance violation.

    Each original terrain triangle needs a uniform lowering equal to its worst
    overlay violation. Giving all three vertices at least that lowering also
    covers vertices outside the ribbon and protects every interpolated interior.
    Neighboring triangles can only lower the final surface further.
    """
    reductions={};checked=0;overlays=0
    for _,_,_,ribbon in triangles:
        if triangle_height(ribbon,ribbon[0][0],ribbon[0][2]) is None:continue
        checked+=1;footprint=[(p[0],p[2]) for p in ribbon]
        min_x=math.floor(min(p[0] for p in ribbon)/spacing)*spacing;max_x=math.floor(max(p[0] for p in ribbon)/spacing)*spacing
        min_z=math.floor(min(p[2] for p in ribbon)/spacing)*spacing;max_z=math.floor(max(p[2] for p in ribbon)/spacing)*spacing
        for x in range(min_x,max_x+1,spacing):
            for z in range(min_z,max_z+1,spacing):
                for cell in ([(x,z),(x,z+spacing),(x+spacing,z)],[(x+spacing,z),(x,z+spacing),(x+spacing,z+spacing)]):
                    overlap=clipped_polygon(footprint,cell)
                    if not overlap:continue
                    overlays+=1;terrain=[(px,grid[(px,pz)],pz) for px,pz in cell]
                    lowering=max(triangle_height(terrain,px,pz)-triangle_height(ribbon,px,pz)+clearance for px,pz in overlap)
                    if lowering<=1e-7:continue
                    for vertex in cell:reductions[vertex]=max(reductions.get(vertex,0.),lowering)
    for vertex,lowering in reductions.items():grid[vertex]=math.floor((grid[vertex]-lowering)*1e6)/1e6
    return {'ribbon_triangle_count':checked,'terrain_ribbon_overlay_count':overlays,'lowered_world_grid_vertex_count':len(reductions),
        'maximum_world_grid_vertex_lowering_m':max(reductions.values(),default=0.),'minimum_ribbon_clearance_m':clearance,
        'method':'exact runtime ribbon/terrain triangle overlay; conservative uniform triangle lowering'}

def lighting_overrides(recipe):
    """Optional atmosphere tuning affects only the explicitly selected venue."""
    lighting=recipe.get('lighting',{})
    if not isinstance(lighting,dict):raise ValueError('Environment lighting overrides must be an object.')
    if 'fog_distance_scale' in lighting:
        scale=lighting['fog_distance_scale']
        if isinstance(scale,bool) or not isinstance(scale,(int,float)) or not math.isfinite(scale) or not 1<=scale<=3:
            raise ValueError('Fog distance scale must be finite and within 1–3.')
    return dict(lighting)

def forest_high_visibility(recipe):
    """Independent optional renderer policy; never activates forest identity tuning."""
    policy=recipe.get('forest_high_visibility')
    if policy is None:
        if 'forest_high_visibility' in recipe:raise ValueError('High forest visibility policy must be an object.')
        return None
    if not isinstance(policy,dict) or set(policy)!={'schema_version','id','mode'}:
        raise ValueError('High forest visibility policy requires version, identity and mode only.')
    if type(policy['schema_version']) is not int or policy['schema_version']!=1 or policy['mode']!='fog_frustum_v1':
        raise ValueError('Unsupported High forest visibility policy.')
    identity=policy['id']
    if not isinstance(identity,str) or len(identity)>256 or not re.fullmatch(r'[A-Za-z0-9_-]+',identity):
        raise ValueError('Stable single-component High forest visibility identity required.')
    return dict(policy)

SCRIPT_DIRECTORY=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root',type=Path,default=SCRIPT_DIRECTORY.parents[2])
parser.add_argument('--output-root',type=Path,default=None)
parser.add_argument('--venue-id',default='woodland_basin_v1')
parser.add_argument('--recipe',type=Path,default=None)
arguments=parser.parse_args()
PROJECT=arguments.project_root.resolve()
HERE=(arguments.output_root if arguments.output_root is not None else PROJECT/'artifacts/wp169i/draft').resolve()
RESOURCES=HERE/'resources'
VENUE=arguments.venue_id;PREFIX='Biathlon/Venues/'+VENUE
assert VENUE.replace('_','').isalnum() and VENUE.endswith('_v1')
ENVIRONMENT_RECIPE={} if arguments.recipe is None else json.loads(arguments.recipe.read_text(encoding='utf-8-sig'))
if ENVIRONMENT_RECIPE:assert ENVIRONMENT_RECIPE['schema_version']==1 and ENVIRONMENT_RECIPE['venue_id']==VENUE
forest_recipe=ENVIRONMENT_RECIPE.get('forest',{})
environment_lighting=lighting_overrides(ENVIRONMENT_RECIPE)
environment_high_visibility=forest_high_visibility(ENVIRONMENT_RECIPE)
def read(relative):return json.loads((RESOURCES/(relative+'.json')).read_text(encoding='utf-8-sig'))
def project_read(relative):return json.loads((PROJECT/'Assets/Resources'/(relative+'.json')).read_text(encoding='utf-8-sig'))
def write(relative,data):
    destination=RESOURCES/(relative+'.json');destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_text(json.dumps(data,allow_nan=False,separators=(',',':'))+'\n',encoding='utf-8')
def point(x,y,z):return {'x':round(x,6),'y':round(y,6),'z':round(z,6)}
def smooth(t):t=max(0.,min(1.,t));return t*t*(3-2*t)
def unit(ix,iz,channel):
    digest=hashlib.sha256(f'{VENUE}:forest:{ix}:{iz}:{channel}'.encode()).digest()
    return int.from_bytes(digest[:8],'big')/2**64

track=read(PREFIX+'/track');visual=read(PREFIX+'/visual');paths=read(PREFIX+'/paths');manifest=read(PREFIX+'/manifest')
vegetation=project_read('Biathlon/vegetation_v1');composition=project_read('Biathlon/g2_forest_composition_v1')
composition_resource='Biathlon/g2_forest_composition_v1'
if 'pine_fraction' in forest_recipe:
    composition={'schema_version':1,'id':VENUE[:-3]+'_forest_composition_v1','pine_fraction':forest_recipe['pine_fraction'],'species_seed':forest_recipe['species_seed']}
    assert 0<=composition['pine_fraction']<=1
    composition_resource=PREFIX+'/forest_composition';write(composition_resource,composition)
forest_minimum_scale=forest_recipe.get('minimum_scale',vegetation['minimum_scale'])
forest_maximum_scale=forest_recipe.get('maximum_scale',vegetation['maximum_scale'])
assert .5<=forest_minimum_scale<=forest_maximum_scale<=1.5
stadium=project_read('Biathlon/stadium_art_v1');target=project_read('Biathlon/target_geometry_v1')
native_canopy_bounds={}
for species in ('Fir','Pine'):
    mesh=PROJECT/'Assets/Resources/Biathlon/Vegetation'/('G2'+species)/(species+'LOD0.asset')
    bounds=re.search(r'm_LocalAABB:\s*m_Center: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}\s*m_Extent: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',mesh.read_text(encoding='utf-8-sig'))
    assert bounds is not None,mesh
    cx,cy,cz,ex,ey,ez=map(float,bounds.groups())
    native_canopy_bounds[species.lower()]=math.hypot(abs(cx)+ex,abs(cz)+ez)
CANOPY_BOUND=max(native_canopy_bounds.values())
tracks={loop['id']:loop for loop in track['loops']}
maps={loop['loop_id']:loop for loop in visual['loops']}
edges=[]
for loop in visual['loops']:
    definitions=tracks[loop['loop_id']]['segments']
    for a,b,definition in zip(loop['knots'],loop['knots'][1:],definitions):
        dx,dz=b['x']-a['x'],b['z']-a['z'];squared=dx*dx+dz*dz
        edges.append({'loop_id':loop['loop_id'],'a':a,'b':b,'dx':dx,'dz':dz,'squared':squared,'half_width':definition['width_m']/2})
height_loop_ids=['main']+[loop for loop in ('pursuit_shortcut','sprint_extension') if loop in tracks]
main_edges=[edge for edge in edges if edge['loop_id'] in height_loop_ids]
def sample_edge(edge,x,z):
    a=edge['a'];t=max(0.,min(1.,((x-a['x'])*edge['dx']+(z-a['z'])*edge['dz'])/edge['squared']))
    px,pz=a['x']+t*edge['dx'],a['z']+t*edge['dz']
    return (x-px)**2+(z-pz)**2,a['y']+t*(edge['b']['y']-a['y'])
def nearest_main(x,z):
    best=float('inf');height=half=0
    for edge in main_edges:
        squared,y=sample_edge(edge,x,z)
        if squared<best:best=squared;height=y;half=edge['half_width']
    return math.sqrt(best),height,half

# Spatial index for every protected physical corridor, including private mats,
# penalty, shared exit and start. It is conservative at endpoints and never
# concatenates unrelated loop endpoints into a phantom edge.
CELL=32
index={}
MAX_CANOPY=CANOPY_BOUND*forest_maximum_scale+vegetation['wind_amplitude_m']+3
for edge in edges:
    a,b=edge['a'],edge['b'];margin=edge['half_width']+MAX_CANOPY
    for iz in range(math.floor((min(a['z'],b['z'])-margin)/CELL),math.floor((max(a['z'],b['z'])+margin)/CELL)+1):
        for ix in range(math.floor((min(a['x'],b['x'])-margin)/CELL),math.floor((max(a['x'],b['x'])+margin)/CELL)+1):
            index.setdefault((ix,iz),[]).append(edge)
def corridor_gap(x,z,canopy):
    best=float('inf')
    for edge in index.get((math.floor(x/CELL),math.floor(z/CELL)),[]):
        squared,_=sample_edge(edge,x,z)
        best=min(best,math.sqrt(squared)-edge['half_width']-canopy)
    return best

clearings=[]
for clearing in forest_recipe.get('clearings',[]):
    knots=maps[clearing['loop_id']]['knots'];distance=clearing['distance_m']
    a,b=next((a,b) for a,b in zip(knots,knots[1:]) if a['distance_m']<=distance<=b['distance_m'])
    t=(distance-a['distance_m'])/(b['distance_m']-a['distance_m']);dx,dz=b['x']-a['x'],b['z']-a['z'];length=math.hypot(dx,dz)
    clearings.append({'id':clearing['id'],'x':a['x']+dx*t+dz/length*clearing['lateral_m'],'z':a['z']+dz*t-dx/length*clearing['lateral_m'],'radius_m':clearing['radius_m']})

TILE=128;SPACING=2;MARGIN=120;SURFACE_OFFSET=-.03
knots=[k for loop in visual['loops'] for k in loop['knots']]
firing_origin=manifest['range_layout']['firing_origin'];range_forward=manifest['range_layout']['forward']
target_points=[point((len(paths['lanes'])/2-.5-i)*manifest['range_layout']['lane_pitch_m'],stadium['target_height_m'],target['distance_m']) for i in range(len(paths['lanes']))]
all_points=knots+target_points
minimum_x=math.floor((min(p['x'] for p in all_points)-MARGIN)/TILE)*TILE
maximum_x=math.ceil((max(p['x'] for p in all_points)+MARGIN)/TILE)*TILE
minimum_z=math.floor((min(p['z'] for p in all_points)-MARGIN)/TILE)*TILE
maximum_z=math.ceil((max(p['z'] for p in all_points)+MARGIN)/TILE)*TILE
rise=max(k['y'] for k in maps['main']['knots'])
level_top=[k for k in maps['main']['knots'] if abs(k['y']-rise)<1e-8]
base_south=max(k['z'] for k in level_top)
base_north=min(k['z'] for k in maps['main']['knots'] if abs(k['y'])<1e-8)
FLAT=(-180,-95,130,62)
def surface_height(x,z):
    distance,y,half=nearest_main(x,z)
    base=rise*max(0.,min(1.,(base_north-z)/(base_north-base_south)))
    weight=smooth((distance-half-4)/100)
    height=y*(1-weight)+base*weight
    away=math.hypot(max(FLAT[0]-x,0,x-FLAT[2]),max(FLAT[1]-z,0,z-FLAT[3]))
    return height*smooth(away/40)+SURFACE_OFFSET

print('Generating shared world terrain samples...',flush=True)
height_cache={}
for z in range(minimum_z,maximum_z+1,SPACING):
    for x in range(minimum_x,maximum_x+1,SPACING):
        height_cache[(x,z)]=round(surface_height(x,z),6)
print('Protecting the complete runtime groomed ribbon triangles...',flush=True)
terrain_cap=cap_terrain_below_ribbons(height_cache,SPACING,ribbon_triangles(track,visual),-SURFACE_OFFSET)
def mesh_height(x,z):
    x0=math.floor(x/SPACING)*SPACING;z0=math.floor(z/SPACING)*SPACING
    fx=(x-x0)/SPACING;fz=(z-z0)/SPACING
    a=height_cache[(x0,z0)];b=height_cache[(x0,z0+SPACING)]
    c=height_cache[(x0+SPACING,z0)];d=height_cache[(x0+SPACING,z0+SPACING)]
    return a+(c-a)*fx+(b-a)*fz if fx+fz<=1 else d+(b-d)*(1-fx)+(c-d)*(1-fz)

tiles=[];columns=TILE//SPACING+1
for z in range(minimum_z,maximum_z,TILE):
    for x in range(minimum_x,maximum_x,TILE):
        tile_id=f'tile_{(x-minimum_x)//TILE:02d}_{(z-minimum_z)//TILE:02d}'
        heights=[height_cache[(x+ix*SPACING,z+iz*SPACING)] for iz in range(columns) for ix in range(columns)]
        resource=PREFIX+'/environment/terrain/'+tile_id
        write(resource,{'schema_version':1,'id':VENUE+'_'+tile_id+'_heights_v1','venue_id':VENUE,'tile_id':tile_id,'columns':columns,'rows':columns,'heights_m':heights})
        tiles.append({'id':tile_id,'center':point(x+TILE/2,0,z+TILE/2),'size':{'x':TILE,'y':0,'z':TILE},'columns':columns,'rows':columns,'height_resource':resource})
assert columns*columns<100000

print('Generating dense route-aware explicit forest groups...',flush=True)
groups={};tree_count=pine_count=0;min_gap=float('inf');forest_spacing=forest_recipe.get('grid_spacing_m',6)
assert 4<=forest_spacing<=12
for iz in range(math.ceil(minimum_z/forest_spacing)+1,math.floor(maximum_z/forest_spacing)-1):
    for ix in range(math.ceil(minimum_x/forest_spacing)+1,math.floor(maximum_x/forest_spacing)-1):
        x=(ix+(unit(ix,iz,0)-.5)*.5)*forest_spacing
        z=(iz+(unit(ix,iz,1)-.5)*.5)*forest_spacing
        if forest_recipe:
            # New explicit recipes ground and validate the serialized position.
            # Keep the accepted default Venue01 resource bytes unchanged.
            x,z=round(x,6),round(z,6)
        scale=forest_minimum_scale+(forest_maximum_scale-forest_minimum_scale)*unit(ix,iz,2)
        canopy=CANOPY_BOUND*scale+vegetation['wind_amplitude_m']
        # The complete firing corridor, target line, rear apron, crowd and event
        # area remain clear; individual corridor tests also protect the start.
        if FLAT[0]-canopy<=x<=FLAT[2]+canopy and FLAT[1]-canopy<=z<=FLAT[3]+canopy:continue
        distance,_,_=nearest_main(x,z)
        if distance>150:continue
        gap=corridor_gap(x,z,canopy)
        if gap<3:continue
        if any(math.hypot(x-c['x'],z-c['z'])<c['radius_m']+canopy for c in clearings):continue
        if forest_recipe and mesh_height(x,z)>forest_recipe['exposed_height_m'] and unit(ix,iz,6)>=forest_recipe['exposed_density_fraction']:continue
        group_id=f'forest_{math.floor((x-minimum_x)/TILE):02d}_{math.floor((z-minimum_z)/TILE):02d}'
        species='pine' if unit(ix,iz,5)<composition['pine_fraction'] else 'fir'
        if forest_recipe:
            from environment_serialization import grounded_tree_position
            position=grounded_tree_position(x,z,mesh_height)
        else:position=point(x,mesh_height(x,z),z)
        instance={'id':f'tree_{ix+10000}_{iz+10000}','position':position,'scale':round(scale,6),'yaw_deg':round(unit(ix,iz,3)*360,6),'density_rank':round(unit(ix,iz,4),6),'species':species}
        # Rounding must not turn a valid density rank into the forbidden1 value.
        instance['density_rank']=min(instance['density_rank'],.999999)
        groups.setdefault(group_id,[]).append(instance);tree_count+=1;pine_count+=species=='pine';min_gap=min(min_gap,gap)
forest_groups=[]
for group_id,instances in sorted(groups.items()):
    assert len(instances)<=vegetation['max_instances']
    gx,gz=map(int,group_id.split('_')[1:]);cx=minimum_x+(gx+.5)*TILE;cz=minimum_z+(gz+.5)*TILE
    resource=PREFIX+'/environment/forest/'+group_id
    write(resource,{'schema_version':1,'id':VENUE+'_'+group_id+'_instances_v1','venue_id':VENUE,'group_id':group_id,'instances':instances})
    forest_groups.append({'id':group_id,'center':point(cx,mesh_height(cx,cz),cz),'size':{'x':TILE,'y':50,'z':TILE},'count':len(instances),'instances_resource':resource})

target_banks=[]
for i,lane in enumerate(paths['lanes']):
    a=lane['mat_anchor'];mat=next(k for k in maps[a['loop_id']]['knots'] if abs(k['distance_m']-a['distance_m'])<1e-8)
    target_banks.append({'id':f'target_bank_{i+1:02d}','lane_id':lane['id'],'mat_anchor':a,'mat_position':point(mat['x'],mat['y'],mat['z']),
        'position':point(mat['x']+range_forward['x']*target['distance_m'],mat['y']+stadium['target_height_m'],mat['z']+range_forward['z']*target['distance_m']),
        'yaw_deg':math.degrees(math.atan2(range_forward['x'],range_forward['z'])),'scale':1,'initial_stance':'Standing','initial_closed_mask':0})
stands=[{'id':f'grandstand_{i+1:02d}','resource':'Biathlon/Stadium/G2Grandstand/Grandstand','position':point(-80+i*20,0,-50),'yaw_deg':180,'scale':1} for i in range(9)]
screen_parts=[{'id':'event_screen_'+name,'mesh_resource':'Biathlon/Stadium/G2Event/'+name,'material_resource':'Biathlon/Stadium/G2Event/'+material,'position':point(112,0,-45),'yaw_deg':-90,'scale':1} for name,material in (('ScreenFrame','Charcoal'),('ScreenSteel','Steel'),('EventPanel','Display'))]
banner_parts=[]
for i,(x,z,yaw) in enumerate(((-110,8,90),(110,8,-90),(-160,-40,0),(-140,-40,0))):
    for mesh,material in (('BannerPole','Steel'),('BannerFabric','EventTeal')):
        banner_parts.append({'id':f'banner_{i+1:02d}_{mesh}','mesh_resource':'Biathlon/Stadium/G2Event/'+mesh,'material_resource':'Biathlon/Stadium/G2Event/'+material,'position':point(x,0,z),'yaw_deg':yaw,'scale':1})
lodge_parts=[]
for suffix in ('wood','woodLight','snow','metal','glass','dark','concrete','accent'):
    mapped=suffix in ('dark','concrete')
    lodge_parts.append({'id':'timing_lodge_'+suffix,'mesh_resource':'Biathlon/Stadium/G2Event/Mapped_TimingLodge_'+suffix if mapped else 'Biathlon/Stadium/TimingLodge_'+suffix,
        'material_resource':'Biathlon/Stadium/G2Event/corrugated_iron' if suffix=='dark' else 'Biathlon/Stadium/G2Event/concrete' if suffix=='concrete' else 'Biathlon/Snow/cold' if suffix=='snow' else 'Biathlon/Stadium/'+suffix,
        'position':point(-171,0,15),'yaw_deg':0,'scale':1})
finish_parts=[]
for suffix in ('white','snow','metal','dark','concrete','accent'):
    finish_parts.append({'id':'finish_gantry_'+suffix,'mesh_resource':'Biathlon/Stadium/G2Event/Mapped_FinishGantry_concrete' if suffix=='concrete' else 'Biathlon/Stadium/FinishGantry_'+suffix,
        'material_resource':'Biathlon/Stadium/G2Event/concrete' if suffix=='concrete' else 'Biathlon/Snow/cold' if suffix=='snow' else 'Biathlon/Stadium/'+suffix,
        'position':point(maps['main']['knots'][0]['x'],0,maps['main']['knots'][0]['z']),'yaw_deg':-90,'scale':1})
environment={'schema_version':1,'id':VENUE+'_environment_v1','venue_id':VENUE,'status':'DRAFT_PRESENTATION_DATA_UNITY_IMPORT_RUNTIME_AND_VISUAL_ACCEPTANCE_PENDING',
    'manifest_resource':PREFIX+'/manifest','visual_resource':PREFIX+'/visual','track_resource':PREFIX+'/track',
    'terrain':{'bounds':{'center':point((minimum_x+maximum_x)/2,0,(minimum_z+maximum_z)/2),'size':{'x':maximum_x-minimum_x,'y':0,'z':maximum_z-minimum_z}},
        'tile_size_m':TILE,'grid_spacing_m':SPACING,'blend_resource':'Biathlon/terrain_blend_v1','material_resource':'Biathlon/Terrain/G2Snow/WinterBlend','tiles':tiles,
        'height_recipe':{'id':'nearest_main_course_height_with_basin_blend_v1','source_visual_resource':PREFIX+'/visual','source_loop_id':'main','source_knots':maps['main']['knots'],
            'height_control_points':[{'z':base_north,'y':0},{'z':base_south,'y':rise}],'route_edge_keep_m':4,'offroute_blend_m':100,'surface_offset_m':SURFACE_OFFSET,
            'stadium_flat_bounds':{'min_x':FLAT[0],'min_z':FLAT[1],'max_x':FLAT[2],'max_z':FLAT[3]},'stadium_blend_m':40,
            'ribbon_clearance_cap':terrain_cap,
            'description':'Nearest main edge supplies the basin base surface; exact runtime ribbon/terrain triangle overlays lower affected grid triangles to retain the authored surface offset beneath every groomed interior and edge. Base samples outside affected course cells remain unchanged; tree heights use the final terrain triangles. No physics changes.'}},
    'route_corridors':[{'loop_id':loop['loop_id'],'source_visual_resource':PREFIX+'/visual','knots':loop['knots'],'widths_m':authored_corridor_widths(tracks[loop['loop_id']],loop)} for loop in visual['loops']],
    'forest':{'definition_resource':'Biathlon/vegetation_v1','composition_resource':composition_resource,'groups':forest_groups,'instance_space':'world','max_instances_per_group':vegetation['max_instances'],
        'species_resources':[{'id':name.lower(),'material_resource':'Biathlon/Vegetation/G2'+name+'/'+name,'lod_mesh_resources':['Biathlon/Vegetation/G2'+name+'/'+name+'LOD'+str(i) for i in range(3)],
            'derivation_record':'docs/licensing/g2_visual/'+name.lower()+'-derivation.json'} for name in ('Fir','Pine')],
        'grid_spacing_m':forest_spacing,'additional_canopy_corridor_clearance_m':3,'native_canopy_bound_radius_m':native_canopy_bounds,'conservative_all_species_canopy_radius_m':CANOPY_BOUND,
        'corridor_clearance_includes':'every physical loop width plus yaw-independent radial bound from native mesh AABB, instance scale and canonical visual wind amplitude','firing_and_stadium_apron_exclusion':{'min_x':FLAT[0],'min_z':FLAT[1],'max_x':FLAT[2],'max_z':FLAT[3]}},
    'range':{'geometry_resource':'Biathlon/target_geometry_v1','stadium_art_resource':'Biathlon/stadium_art_v1','bank_resource':'Biathlon/Stadium/TargetBank','forward':range_forward,'origin':firing_origin,
        'target_distance_m':target['distance_m'],'target_height_m':stadium['target_height_m'],'banks':target_banks,'mats':{'width_m':stadium['mat_width_m'],'length_m':stadium['mat_length_m'],'generate_from_banks_mat_positions':True},
        'backstop':{'position':point(0,1.9,target['distance_m']+2),'size':{'x':len(paths['lanes'])*manifest['range_layout']['lane_pitch_m']+4,'y':3.8,'z':.5},'material_resource':'Biathlon/Stadium/G2Event/concrete','generate_original_mesh':True}},
    'assets':stands,'mesh_assets':screen_parts+banner_parts+lodge_parts+finish_parts,
    'crowd':{'mode':'owner_approved_simplified','seat_binding':'SpectatorSeating on each existing unit-scale grandstand','resource_root':'Biathlon/Crowd','restricted_resources_allowed':False},
    'lighting':{'resource':'Biathlon/g2_lighting_visual_v1','preset_id':'clear_cold','weather_resource':'Biathlon/weather_visual_v1','weather_preset':'clear','sky_material_resource':'Biathlon/Weather/G2Lighting/BenchmarkSky','visual_wind':{'x':.75,'y':.2},'temperature_c':-8},
    'license_recipe':'presentation-resource-recipe.json'}
if forest_recipe:
    environment['forest']['original_venue_identity']={'recipe_id':ENVIRONMENT_RECIPE['id'],'explicit_clearings':clearings,
        'minimum_scale':forest_minimum_scale,'maximum_scale':forest_maximum_scale,'exposed_height_m':forest_recipe['exposed_height_m'],'exposed_density_fraction':forest_recipe['exposed_density_fraction']}
environment['lighting'].update(environment_lighting)
if environment_high_visibility is not None:
    environment['forest']['high_visibility']=environment_high_visibility
if len(height_loop_ids)>1:
    height_recipe=environment['terrain']['height_recipe']
    height_recipe['id']='nearest_counted_course_height_with_basin_blend_v1'
    height_recipe['source_loop_ids']=height_loop_ids
    height_recipe['source_corridors']=[{'loop_id':loop,'knots':maps[loop]['knots']} for loop in height_loop_ids]
    height_recipe['description']='Nearest actual Main/format-branch edge supplies the basin base surface. Exact runtime ribbon/terrain triangle overlays conservatively lower affected grid triangles beneath every groomed interior and edge. Original Main derives the off-course basin profile. Final tree heights use the corrected triangles; no physics changes.'
write(PREFIX+'/environment',environment)
(HERE/'environment.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')

# Check every actual native resource rather than trusting the recipe strings.
native_refs=set()
def collect(node):
    if isinstance(node,dict):
        for key,value in node.items():
            if key.endswith('resource') and isinstance(value,str) and value.startswith('Biathlon/') and not value.startswith(PREFIX):native_refs.add(value)
            if key.endswith('resources') and isinstance(value,list):
                for resource in value:
                    if isinstance(resource,str) and resource.startswith('Biathlon/') and not resource.startswith(PREFIX):native_refs.add(resource)
            collect(value)
    elif isinstance(node,list):
        for value in node:collect(value)
collect(environment)
missing=[resource for resource in sorted(native_refs) if not any((PROJECT/'Assets/Resources'/(resource+extension)).exists() for extension in ('.json','.prefab','.mat','.asset'))]
assert not missing,missing
alignment_errors=[mesh_height(k['x'],k['z'])-k['y'] for loop in visual['loops'] for k in loop['knots']]
maximum_analytic_error=max(abs(surface_height(k['x'],k['z'])-SURFACE_OFFSET-k['y']) for loop in visual['loops'] for k in loop['knots'])
assert maximum_analytic_error<1e-8,maximum_analytic_error
audit={'schema_version':1,'id':VENUE+'_environment_draft_audit_v1','venue_id':VENUE,'status':'DRAFT_STATIC_PRESENTATION_DATA_CHECKS_PASS_UNITY_AND_VISUAL_PENDING',
    'terrain_tile_count':len(tiles),'terrain_vertices_per_tile':columns*columns,'shared_grid_vertex_count':len(height_cache),'bounds':environment['terrain']['bounds'],
    'forest_group_count':len(forest_groups),'forest_instance_count':tree_count,'fir_instance_count':tree_count-pine_count,'pine_instance_count':pine_count,
    'maximum_forest_group_count':max(len(instances) for instances in groups.values()),'minimum_canopy_to_corridor_edge_gap_m':min_gap,'target_bank_count':len(target_banks),'target_distance_m':target['distance_m'],
    'native_resource_checks':'PASS','native_resource_count':len(native_refs),'tree_height_source':'barycentric interpolation of the same generated terrain triangles',
    'minimum_mesh_minus_route_knot_height_m':min(alignment_errors),'maximum_mesh_minus_route_knot_height_m':max(alignment_errors),'maximum_analytic_course_height_error_m':maximum_analytic_error,
    'tree_corridor_and_apron_checks':'PASS_ALL_PHYSICAL_LOOPS_NO_CONCATENATED_PHANTOM_EDGES','terrain_seams':'shared global samples; neighboring border rows/columns identical',
    'source_hashes':{relative:hashlib.sha256((RESOURCES/(relative+'.json')).read_bytes()).hexdigest() for relative in (PREFIX+'/track',PREFIX+'/visual',PREFIX+'/paths',PREFIX+'/manifest')},
    'generated_resource_hashes':{str(path.relative_to(RESOURCES)).replace('\\','/'):hashlib.sha256(path.read_bytes()).hexdigest() for path in (RESOURCES/PREFIX/'environment').rglob('*.json')},
    'environment_resource_sha256':hashlib.sha256((RESOURCES/(PREFIX+'/environment.json')).read_bytes()).hexdigest(),'native_canopy_bound_radius_m':native_canopy_bounds,
    'native_resource_sha256':{resource:hashlib.sha256(next((PROJECT/'Assets/Resources'/(resource+extension)) for extension in ('.json','.prefab','.mat','.asset') if (PROJECT/'Assets/Resources'/(resource+extension)).exists()).read_bytes()).hexdigest() for resource in sorted(native_refs)},
    'runtime_and_visual_acceptance':'PENDING','lod_performance':'NOT_MEASURED','authoritative_gameplay_changed':False}
audit['height_source_loop_ids']=height_loop_ids
audit['ribbon_clearance_cap']=terrain_cap
audit['corridor_width_sample_count']=sum(len(c['widths_m']) for c in environment['route_corridors'])
audit['corridor_width_sampling']='TrackSampler Reject mode: outgoing segment at interior boundaries, last segment at terminal knot'
if ENVIRONMENT_RECIPE:
    audit['environment_identity_recipe_sha256']=hashlib.sha256(arguments.recipe.read_bytes()).hexdigest()
if forest_recipe:
    audit['original_venue_identity']=environment['forest']['original_venue_identity']
(HERE/'environment-audit.json').write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8')
print(json.dumps({key:audit[key] for key in ('terrain_tile_count','terrain_vertices_per_tile','forest_group_count','forest_instance_count','maximum_forest_group_count','minimum_canopy_to_corridor_edge_gap_m','target_bank_count','native_resource_count')},indent=2),flush=True)
