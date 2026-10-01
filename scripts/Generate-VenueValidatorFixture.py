"""Small geometric test fixture, not a release venue or replacement for existing races."""
import json, math
from pathlib import Path
out=Path(__file__).resolve().parent.parent/'tests/fixtures/venues/validator_valid'
out.mkdir(parents=True,exist_ok=True)
def put(name,value):(out/(name+'.json')).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def anchor(loop,d):return dict(loop_id=loop,distance_m=d)
def point(d,x,z):return dict(distance_m=d,x=x,y=0,z=z)
def segment(id,length,zone='stadium',curve=0):return dict(id=id,length_m=length,start_grade=0,end_grade=0,curvature_per_m=curve,width_m=6,snow_profile_id='packed',wind_exposure=1,terrain_muscle_factor=1,draft_allowed=True,overtake_allowed=True,risk_level=0,semantic_zone=zone)
length=100.499
main=[segment('range',length,'range_approach')]+[segment('return_'+str(i),100) for i in range(3)]
n=48;radius=150/(2*n*math.sin(math.pi/n))
track=dict(schema_version=1,id='validator_track_v1',authoring_limits=dict(max_abs_grade=.25,max_abs_curvature_per_m=1,min_width_m=1,max_width_m=20),loops=[dict(id='main',kind='race',closed=True,segments=main),dict(id='lane_1',kind='race',closed=False,segments=[segment('range',length,'range_approach')]),dict(id='penalty',kind='penalty',closed=True,segments=[segment('penalty',150,'penalty_loop',1/radius)])],checkpoints=[],range=dict(id='range',entry=anchor('main',1),firing_line=anchor('main',50.249),exit=anchor('main',100)),penalty=dict(loop_id='penalty',entry=anchor('main',length),rejoin=anchor('main',length)))
put('track',track)
knots=[point(0,-50,-5),point(50.249,0,0),point(length,50,-5)]
penalty=[point(i*150/n,50+radius*(1-math.cos(2*math.pi*i/n)),-5+radius*math.sin(2*math.pi*i/n)) for i in range(n)]
penalty.append(point(150,50,-5))
put('visual',dict(schema_version=1,id='validator_visual_v1',track_id=track['id'],sample_spacing_m=2,loops=[dict(loop_id='main',knots=knots+[point(length+100,50,-105),point(length+200,-50,-105),point(length+300,-50,-5)]),dict(loop_id='lane_1',knots=knots),dict(loop_id='penalty',knots=penalty)]))
put('lanes',dict(schema_version=1,id='validator_lanes_v1',track_id=track['id'],main_loop_id='main',entry_distance_m=0,exit_distance_m=length,lane_loop_ids=['lane_1']))
def port(id,loop,d,before,after):return dict(id=id,anchor=anchor(loop,d),guard_before_m=before,guard_after_m=after)
put('junctions',dict(schema_version=1,id='validator_junctions_v1',track_id=track['id'],junctions=[dict(id='entry',ports=[port('main_in','main',0,1,1),port('lane_in','lane_1',0,0,1)]),dict(id='exit',ports=[port('main_out','main',length,1,1),port('lane_out','lane_1',length,1,0),port('penalty_out','penalty',0,1,1)])]))
def connection(id,j,a,b):return dict(id=id,junction_id=j,from_port_id=a,to_port_id=b,kind='coincident_seam')
put('connections',dict(schema_version=1,id='validator_connections_v1',junction_definition_id='validator_junctions_v1',connections=[connection('enter','entry','main_in','lane_in'),connection('leave','exit','lane_out','main_out'),connection('penalty_entry','exit','lane_out','penalty_out'),connection('penalty_exit','exit','penalty_out','main_out')]))
def xyz(x=0,y=0,z=0):return dict(x=x,y=y,z=z)
put('manifest',dict(schema_version=1,id='validator_venue_v1',track_resource='track',visual_resource='visual',lanes_resource='lanes',junctions_resource='junctions',connections_resource='connections',range_layout=dict(approach='side_entry',entry=anchor('main',0),exit=anchor('main',length),firing_origin=xyz(),forward=xyz(0,0,1),lane_pitch_m=3,apron_depth_m=10),cameras=[dict(id='range_camera',anchor=anchor('main',50.249),offset=xyz(10,3,-10),look_offset=xyz(),fov_deg=50)],ambience=[dict(id='crowd',anchor=anchor('main',50.249),event_id='ambience_crowd',radius_m=40)],assets=[dict(id='target',resource='target',anchor=anchor('main',50.249),offset=xyz(0,1,50),yaw_deg=0)]))
print(out)
