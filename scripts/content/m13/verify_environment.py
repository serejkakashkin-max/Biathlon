"""Independent serialized-data acceptance checks; no Unity/Assets mutation."""
from pathlib import Path
import argparse,hashlib,json,math,struct

def single(value):return struct.unpack('f',struct.pack('f',value))[0]

def actual_ribbon_triangles(track,visual):
    """Independently reconstruct the published TrackRibbonBuilder row contract."""
    for physical in track['loops']:
        candidates=[loop for loop in visual['loops'] if loop['loop_id']==physical['id']]
        if not candidates:continue
        loop=candidates[0];knots=loop['knots'];length_mm=sum(round(segment['length_m']*1000) for segment in physical['segments'])
        length=length_mm/1000;coordinates={0.,length};segment_ends=[];cursor=0
        for segment in physical['segments']:
            coordinates.add(cursor/1000);cursor+=round(segment['length_m']*1000);segment_ends.append(cursor)
        coordinates.update(knot['distance_m'] for knot in knots)
        count=math.ceil(length/visual['sample_spacing_m'])
        coordinates.update(length*step/count for step in range(1,count));rows=[]
        for coordinate in sorted(coordinates):
            index=1
            while index<len(knots)-1 and knots[index]['distance_m']<=coordinate:index+=1
            start,end=knots[index-1],knots[index];fraction=(coordinate-start['distance_m'])/(end['distance_m']-start['distance_m'])
            center=tuple(single(start[axis]+fraction*(end[axis]-start[axis])) for axis in ('x','y','z'))
            dx,dz=end['x']-start['x'],end['z']-start['z'];norm=math.sqrt(dx*dx+dz*dz)
            sample_mm=round(round(coordinate,3)*1000)
            width_index=next((i for i,end_mm in enumerate(segment_ends) if sample_mm<end_mm),len(segment_ends)-1)
            half=single(physical['segments'][width_index]['width_m']/2)
            perpendicular=(single(single(dz/norm)*half),0.,single(single(-dx/norm)*half))
            rows.append((coordinate,tuple(single(center[i]-perpendicular[i]) for i in range(3)),tuple(single(center[i]+perpendicular[i]) for i in range(3))))
        for i in range(1,len(rows)):
            before,after=rows[i-1],rows[i]
            yield physical['id'],before[0],after[0],(before[1],after[1],before[2])
            yield physical['id'],before[0],after[0],(before[2],after[1],after[2])

def signed_area(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])

def intersect_triangles(subject,clip):
    """Sutherland-Hodgman overlay using the terrain's actual triangle diagonal."""
    sign=1 if signed_area(*clip)>0 else -1;polygon=subject[:]
    for edge in range(3):
        if not polygon:break
        a,b=clip[edge],clip[(edge+1)%3];result=[]
        for i,current in enumerate(polygon):
            prior=polygon[i-1];p=sign*signed_area(a,b,prior);q=sign*signed_area(a,b,current)
            if (p>=-1e-9)!=(q>=-1e-9):
                t=p/(p-q);result.append((prior[0]+t*(current[0]-prior[0]),prior[1]+t*(current[1]-prior[1])))
            if q>=-1e-9:result.append(current)
        polygon=result
    return polygon

def plane_height(triangle,point):
    a,b,c=triangle;x,z=point;area=signed_area((a[0],a[2]),(b[0],b[2]),(c[0],c[2]))
    u=((x-a[0])*(c[2]-a[2])-(z-a[2])*(c[0]-a[0]))/area
    v=((b[0]-a[0])*(z-a[2])-(b[2]-a[2])*(x-a[0]))/area
    return a[1]+u*(b[1]-a[1])+v*(c[1]-a[1])

def verify_full_ribbon(grid,spacing,track,visual):
    """Exact affine extrema over all terrain/ribbon intersection polygons."""
    minimum=float('inf');maximum=-minimum;minimum_at=maximum_at=None;triangles=vertices=buried=0;per_loop={}
    for loop,start,end,ribbon in actual_ribbon_triangles(track,visual):
        footprint=[(p[0],p[2]) for p in ribbon]
        if abs(signed_area(*footprint))<1e-12:continue
        triangles+=1;triangle_min=float('inf');local=per_loop.setdefault(loop,{'minimum_gap_m':float('inf'),'maximum_gap_m':-float('inf')})
        x_begin=math.floor(min(p[0] for p in footprint)/spacing);x_end=math.floor(max(p[0] for p in footprint)/spacing)
        z_begin=math.floor(min(p[1] for p in footprint)/spacing);z_end=math.floor(max(p[1] for p in footprint)/spacing)
        for iz in range(z_begin,z_end+1):
            for ix in range(x_begin,x_end+1):
                x,z=ix*spacing,iz*spacing
                for cell in (((x,z),(x,z+spacing),(x+spacing,z)),((x+spacing,z),(x,z+spacing),(x+spacing,z+spacing))):
                    overlap=intersect_triangles(footprint,list(cell))
                    if not overlap:continue
                    terrain=[(px,single(grid[(px,pz)]),pz) for px,pz in cell]
                    for point in overlap:
                        vertices+=1;height=plane_height(ribbon,point);gap=height-plane_height(terrain,point);triangle_min=min(triangle_min,gap)
                        local['minimum_gap_m']=min(local['minimum_gap_m'],gap);local['maximum_gap_m']=max(local['maximum_gap_m'],gap)
                        witness={'loop_id':loop,'row_start_distance_m':start,'row_end_distance_m':end,'world':{'x':point[0],'y':height,'z':point[1]}}
                        if gap<minimum:minimum=gap;minimum_at=witness
                        if gap>maximum:maximum=gap;maximum_at=witness
        if triangle_min< -1e-6:buried+=1
    return {'method':'exact affine extrema of every runtime ribbon/terrain triangle overlay','ribbon_triangle_count':triangles,
        'overlay_vertex_count':vertices,'buried_ribbon_triangle_count':buried,'minimum_ribbon_minus_terrain_m':minimum,'maximum_ribbon_minus_terrain_m':maximum,
        'minimum_gap_location':minimum_at,'maximum_gap_location':maximum_at,'per_loop':per_loop}

SCRIPT_DIRECTORY=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root',type=Path,default=SCRIPT_DIRECTORY.parents[2])
parser.add_argument('--output-root',type=Path,default=None)
parser.add_argument('--venue-id',default='woodland_basin_v1')
arguments=parser.parse_args()
project=arguments.project_root.resolve()
HERE=(arguments.output_root if arguments.output_root is not None else project/'artifacts/wp169i/draft').resolve()
RESOURCES=HERE/'resources';VENUE=arguments.venue_id;PREFIX='Biathlon/Venues/'+VENUE
def read(resource):return json.loads((RESOURCES/(resource+'.json')).read_text(encoding='utf-8-sig'))
environment=read(PREFIX+'/environment');visual=read(PREFIX+'/visual');track=read(PREFIX+'/track')
assert environment['schema_version']==1 and environment['venue_id']==VENUE
grid={};duplicate_border_checks=0;spacing=environment['terrain']['grid_spacing_m']
for tile in environment['terrain']['tiles']:
    heights=read(tile['height_resource']);columns=heights['columns'];rows=heights['rows']
    assert heights['schema_version']==1 and heights['venue_id']==environment['venue_id'] and heights['tile_id']==tile['id']
    assert columns==tile['columns'] and rows==tile['rows'] and columns*rows==len(heights['heights_m']) and columns*rows<100000
    x0=tile['center']['x']-tile['size']['x']/2;z0=tile['center']['z']-tile['size']['z']/2
    for iz in range(rows):
        for ix in range(columns):
            key=(x0+ix*spacing,z0+iz*spacing);y=heights['heights_m'][iz*columns+ix]
            assert math.isfinite(y)
            if key in grid:assert grid[key]==y;duplicate_border_checks+=1
            grid[key]=y
def ground(x,z):
    x0=math.floor(x/spacing)*spacing;z0=math.floor(z/spacing)*spacing;fx=(x-x0)/spacing;fz=(z-z0)/spacing
    a=grid[(x0,z0)];b=grid[(x0,z0+spacing)];c=grid[(x0+spacing,z0)];d=grid[(x0+spacing,z0+spacing)]
    return a+(c-a)*fx+(b-a)*fz if fx+fz<=1 else d+(b-d)*(1-fx)+(c-d)*(1-fz)
definitions={loop['id']:loop for loop in track['loops']};segments=[]
corridors={corridor['loop_id']:corridor for corridor in environment['route_corridors']}
assert len(corridors)==len(environment['route_corridors'])==len(visual['loops'])
width_samples=0
for loop in visual['loops']:
    corridor=corridors[loop['loop_id']]
    assert corridor['source_visual_resource']==PREFIX+'/visual' and corridor['knots']==loop['knots']
    assert len(corridor['widths_m'])==len(loop['knots'])
    authored=definitions[loop['loop_id']]['segments'];endpoints=[];total=0
    for segment in authored:
        total+=round(segment['length_m']*1000);endpoints.append(total)
    for knot,width in zip(loop['knots'],corridor['widths_m']):
        coordinate=round(knot['distance_m']*1000)
        candidate=next((i for i,end in enumerate(endpoints) if coordinate<end),len(authored)-1)
        assert width==authored[candidate]['width_m'];width_samples+=1
    for a,b,definition in zip(loop['knots'],loop['knots'][1:],definitions[loop['loop_id']]['segments']):
        segments.append((a,b,definition['width_m']/2))
apron=environment['forest']['firing_and_stadium_apron_exclusion'];native_radius=environment['forest']['conservative_all_species_canopy_radius_m']
vegetation=json.loads((project/'Assets/Resources/Biathlon/vegetation_v1.json').read_text(encoding='utf-8-sig'))
min_gap=float('inf');max_height_error=0;tree_count=0;ids=set();pine_count=exposed_tree_count=0
identity=environment['forest'].get('original_venue_identity')
for group in environment['forest']['groups']:
    definition=read(group['instances_resource']);instances=definition['instances']
    assert definition['schema_version']==1 and definition['venue_id']==environment['venue_id'] and definition['group_id']==group['id']
    assert len(instances)==group['count']<=environment['forest']['max_instances_per_group']
    for instance in instances:
        assert instance['id'] not in ids;ids.add(instance['id']);tree_count+=1
        x,y,z=(instance['position'][axis] for axis in ('x','y','z'))
        assert all(map(math.isfinite,(x,y,z,instance['scale'],instance['yaw_deg'],instance['density_rank'])))
        assert instance['species'] in ('fir','pine') and .5<=instance['scale']<=1.5 and 0<=instance['density_rank']<1
        pine_count+=instance['species']=='pine'
        if identity:
            assert identity['minimum_scale']-1e-6<=instance['scale']<=identity['maximum_scale']+1e-6
            exposed_tree_count+=y>identity['exposed_height_m']
        max_height_error=max(max_height_error,abs(y-ground(x,z)))
        canopy=native_radius*instance['scale']+vegetation['wind_amplitude_m']
        if identity:
            for clearing in identity['explicit_clearings']:
                assert math.hypot(x-clearing['x'],z-clearing['z'])>=clearing['radius_m']+canopy-2e-6,(instance['id'],clearing['id'])
        assert not(apron['min_x']-canopy<=x<=apron['max_x']+canopy and apron['min_z']-canopy<=z<=apron['max_z']+canopy)
        for a,b,half in segments:
            required=half+canopy+environment['forest']['additional_canopy_corridor_clearance_m']
            # Bounds rejection only; the final nearby check uses the exact segment.
            if x<min(a['x'],b['x'])-required or x>max(a['x'],b['x'])+required or z<min(a['z'],b['z'])-required or z>max(a['z'],b['z'])+required:continue
            dx,dz=b['x']-a['x'],b['z']-a['z'];t=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/(dx*dx+dz*dz)))
            gap=math.hypot(x-a['x']-dx*t,z-a['z']-dz*t)-half-canopy
            assert gap>=environment['forest']['additional_canopy_corridor_clearance_m']-2e-6,(instance['id'],gap)
            min_gap=min(min_gap,gap)
assert max_height_error<=2e-6,max_height_error
geometry=json.loads((project/'Assets/Resources/Biathlon/target_geometry_v1.json').read_text(encoding='utf-8-sig'))
stadium=json.loads((project/'Assets/Resources/Biathlon/stadium_art_v1.json').read_text(encoding='utf-8-sig'))
assert len(environment['range']['banks'])==30
physical_mat=read(PREFIX+'/manifest')['range_layout'].get('mat_definition')
firing_datum_depth=0 if physical_mat is None else physical_mat['athlete_stop_depth_m']
for bank in environment['range']['banks']:
    for axis in ('x','y','z'):
        expected=bank['mat_position'][axis]+environment['range']['forward'][axis]*(geometry['distance_m']+firing_datum_depth)+(stadium['target_height_m'] if axis=='y' else 0)
        assert abs(bank['position'][axis]-expected)<1e-6
assert environment['range']['target_distance_m']==geometry['distance_m'] and environment['range']['target_height_m']==stadium['target_height_m']
mesh_minus_route=[]
for a,b,_ in segments:
    samples=max(1,math.ceil((b['distance_m']-a['distance_m'])/2))
    for i in range(samples+1):
        t=i/samples;x=a['x']+(b['x']-a['x'])*t;z=a['z']+(b['z']-a['z'])*t;y=a['y']+(b['y']-a['y'])*t
        mesh_minus_route.append(ground(x,z)-y)
assert max(mesh_minus_route)<=1e-5,'Terrain must not bury the exact authored ribbon.'
full_ribbon=verify_full_ribbon(grid,spacing,track,visual)
clearance=-environment['terrain']['height_recipe']['surface_offset_m']
assert clearance>0 and full_ribbon['ribbon_triangle_count']>0
assert full_ribbon['buried_ribbon_triangle_count']==0,full_ribbon
assert full_ribbon['minimum_ribbon_minus_terrain_m']>=clearance-1e-4,full_ribbon
report={'schema_version':1,'id':VENUE[:-3]+'_environment_serialized_verification_v1','status':'PASS_STATIC_DATA_RUNTIME_VISUAL_AND_PERFORMANCE_PENDING',
    'tile_count':len(environment['terrain']['tiles']),'coincident_terrain_border_checks':duplicate_border_checks,'unique_world_height_vertices':len(grid),
    'authored_corridor_width_sample_count':width_samples,
    'forest_group_count':len(environment['forest']['groups']),'tree_count':tree_count,'maximum_tree_to_triangle_height_error_m':max_height_error,
    'minimum_checked_full_canopy_corridor_gap_m':None if not math.isfinite(min_gap) else min_gap,'target_count':len(environment['range']['banks']),
    'course_surface_samples':len(mesh_minus_route),'minimum_terrain_minus_route_height_m':min(mesh_minus_route),'maximum_terrain_minus_route_height_m':max(mesh_minus_route),
    'full_runtime_ribbon_triangle_clearance':full_ribbon,
    'environment_sha256':hashlib.sha256((RESOURCES/(PREFIX+'/environment.json')).read_bytes()).hexdigest()}
if identity:
    report['original_venue_identity']={'pine_count':pine_count,'fir_count':tree_count-pine_count,'actual_pine_fraction':pine_count/tree_count,
        'explicit_clearing_count':len(identity['explicit_clearings']),'exposed_tree_count':exposed_tree_count,'clearing_canopy_and_scale_checks':'PASS'}
(HERE/'environment-verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
