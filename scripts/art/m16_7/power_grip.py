"""Offline anatomical power-grip posing. No runtime simulation dependency."""
import math,itertools
import numpy as np
from mathutils import Vector,Matrix
from hand_surface import HandSurface

_cache={}

def solve_finger(metadata,side,digit,centre,shaft,palmar):
    key=(metadata['id'],side,digit,tuple(centre),tuple(shaft),tuple(palmar))
    if key in _cache:return _cache[key]
    suffix='.L' if side>0 else '.R';bones=metadata['finger_bones']
    names=[f'finger{digit}-{j}{suffix}' for j in range(1,4)]
    heads=[Vector(bones[name]['head']) for name in names]
    tails=[Vector(bones[name]['tail']) for name in names]
    from equipment_refine import HANDLE_PROFILE
    metacarpal=bones[names[0]]['parent'];meta_head=Vector(bones[metacarpal]['head']);meta_delta=Vector(bones[metacarpal]['tail'])-meta_head
    cup_axis=meta_delta.cross(palmar).normalized()
    if cup_axis.cross(meta_delta).dot(palmar)<0:cup_axis=-cup_axis
    surface=HandSurface(metadata,side,digit,[metacarpal]+names)
    origin=np.asarray(centre);shaft_array=np.asarray(shaft);profile=np.asarray(HANDLE_PROFILE)
    axes=[]
    for start,end in zip(heads,tails):
        direction=(end-start).normalized();axis=direction.cross(palmar).normalized()
        if axis.cross(direction).dot(palmar)<0:axis=-axis
        axes.append(axis)
    # MCP adduction lets the four fingers converge on a diagonal handle.
    # PIP/DIP remain simple flexion hinges; they never twist sideways.
    cup_max=20 if digit==5 else 15 if digit==4 else 0
    limits=[(0,100),(55,115),(25,85),(-20,20),(0,cup_max)]
    if digit==5:limits[3]=(-15,20) if side>0 else (-20,15)
    def evaluate(angles,details=False):
        cup=Matrix.Translation(meta_head)@Matrix.Rotation(math.radians(angles[4]),4,cup_axis)@Matrix.Translation(-meta_head)
        matrix=cup@Matrix.Translation(heads[0])@Matrix.Rotation(math.radians(angles[3]),4,palmar)@Matrix.Translation(-heads[0]);matrices={metacarpal:cup};chain=[matrix@heads[0]]
        for name,head,tail,axis,flex in zip(names,heads,tails,axes,angles[:3]):
            matrix=matrix@Matrix.Translation(head)@Matrix.Rotation(math.radians(flex),4,axis)@Matrix.Translation(-head)
            matrices[name]=matrix;chain.append(matrix@tail)
        skin_pad,pad_normal,points=surface.sample([matrices[metacarpal]]+[matrices[name] for name in names])
        delta=skin_pad-origin;t=float(delta@shaft_array);radial=delta-shaft_array*t;radius=float(np.linalg.norm(radial));radial/=max(radius,1e-12)
        along=max(-.070,min(-.045 if digit==2 else .040,t));target_radius=float(np.interp(along,profile[:,0],profile[:,1]))+.0002
        slot={2:-.060,3:-.035,4:-.010,5:.016}[digit]
        slot_error=max(0,abs(t-slot)-.004)
        clearance=radius-target_radius;facing=float(pad_normal@radial)
        deltas=points-origin;axial=deltas@shaft_array;radii=np.linalg.norm(deltas-axial[:,None]*shaft_array,axis=1)
        inside=(axial>-.075)&(axial<.045);depth=np.maximum(0,np.interp(axial,profile[:,0],profile[:,1])-.001-radii)
        penalty=float((depth[inside]**2).sum())
        score=clearance**2+(t-along)**2+slot_error**2+.0001*max(0,facing+.85)**2+.5*penalty+1e-9*sum((a-b)**2 for a,b in zip(angles,[65,90,45,0,cup_max/2]))
        if details:return matrices,chain,{'angles':list(angles[:3]),'mcp_adduction_degrees':angles[3],
            'metacarpal_cupping_degrees':angles[4],'measured_palmar_pad_centre':list(skin_pad),'palmar_pad_axis_radius_m':radius,
            'palmar_pad_target_error_m':math.hypot(clearance,t-along),'palmar_pad_faces_handle':-facing,'sampled_skin_penetration_cost':penalty}
        return score
    candidates=sorted(itertools.product([0,20,40,65,90],[60,85,110],[30,55,80],[-15,0,15],sorted(set([0,cup_max/2,cup_max]))),key=evaluate)[:5]
    best=None;best_score=float('inf')
    for initial in candidates:
        angles=list(initial);score=evaluate(angles)
        for step in [12,6,3,1]:
            for iteration in range(12):
                changed=False
                for joint in range(5):
                    for sign in [-1,1]:
                        trial=angles.copy();trial[joint]=max(limits[joint][0],min(limits[joint][1],trial[joint]+sign*step));value=evaluate(trial)
                        if value<score:angles=trial;score=value;changed=True
                if not changed:break
        if score<best_score:best=angles;best_score=score
    result=evaluate(best,True);_cache[key]=result
    return result
