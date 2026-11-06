"""Opposition at the thumb base, fitted to the actual subdivided pad surface."""
import math,itertools
import numpy as np
from mathutils import Vector,Matrix
from hand_surface import HandSurface
_cache={}

def solve_thumb(metadata,side,centre,shaft,palmar,axis_band=(-.065,-.048)):
    key=(metadata['id'],side,tuple(centre),tuple(shaft),tuple(palmar),tuple(axis_band))
    if key in _cache:return _cache[key]
    from equipment_refine import HANDLE_PROFILE
    suffix='.L' if side>0 else '.R';bones=metadata['finger_bones'];names=[f'finger1-{j}{suffix}' for j in [1,2,3]]
    heads=[Vector(bones[name]['head']) for name in names];tails=[Vector(bones[name]['tail']) for name in names]
    axes=[]
    for start,end in zip(heads,tails):
        d=(end-start).normalized();axis=d.cross(palmar).normalized()
        if axis.cross(d).dot(palmar)<0:axis=-axis
        axes.append(axis)
    thumb_axis=(tails[0]-heads[0]).normalized();surface=HandSurface(metadata,side,1,names)
    origin=np.asarray(centre);shaft_array=np.asarray(shaft);profile=np.asarray(HANDLE_PROFILE)
    limits=[(-50,65),(-10,85),(0,85),(-70,70),(-65,65)]
    def evaluate(a,details=False):
        matrix=Matrix.Translation(heads[0])@Matrix.Rotation(math.radians(a[3]),4,palmar)@Matrix.Rotation(math.radians(a[4]),4,thumb_axis)@Matrix.Translation(-heads[0]);matrices={}
        for name,head,axis,angle in zip(names,heads,axes,a[:3]):
            matrix=matrix@Matrix.Translation(head)@Matrix.Rotation(math.radians(angle),4,axis)@Matrix.Translation(-head);matrices[name]=matrix
        pad,normal,points=surface.sample([matrices[name] for name in names])
        delta=pad-origin;t=float(delta@shaft_array);radial=delta-shaft_array*t;r=float(np.linalg.norm(radial));radial/=max(r,1e-12)
        along=max(axis_band[0],min(axis_band[1],t));target_radius=float(np.interp(along,profile[:,0],profile[:,1]))+.0004
        d=points-origin;axial=d@shaft_array;radii=np.linalg.norm(d-axial[:,None]*shaft_array,axis=1);inside=(axial>-.075)&(axial<.045)
        depth=np.maximum(0,np.interp(axial,profile[:,0],profile[:,1])-.001-radii);collision=float((depth[inside]**2).sum())
        facing=float(normal@radial)
        score=(r-target_radius)**2+(t-along)**2+.00015*max(0,facing+.8)**2+.5*collision+1e-9*sum((v-target)**2 for v,target in zip(a,[20,30,35,0,0]))
        if details:return matrices,{'joint_angles_degrees':list(a),'measured_palmar_pad_centre':list(pad),'measured_palmar_pad_normal':list(normal),'palmar_pad_handle_error_m':math.hypot(r-target_radius,t-along),'pad_faces_handle':-facing,'sampled_skin_penetration_cost':collision}
        return score
    seeds=sorted(itertools.product([-30,10,50],[0,40,80],[15,45,75],[-50,0,50],[-50,0,50]),key=evaluate)[:4]
    best=None;score=float('inf')
    for initial in seeds:
        angles=list(initial);value=evaluate(angles)
        for step in [15,7,3,1,.5,.25]:
            for iteration in range(15):
                changed=False
                for j in range(5):
                    for direction in [-1,1]:
                        trial=angles.copy();trial[j]=max(limits[j][0],min(limits[j][1],trial[j]+step*direction));candidate=evaluate(trial)
                        if candidate<value:angles=trial;value=candidate;changed=True
                if not changed:break
        if value<score:best=angles;score=value
    result=evaluate(best,True);_cache[key]=result;return result
