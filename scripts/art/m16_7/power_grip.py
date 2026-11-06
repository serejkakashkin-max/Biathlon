"""Offline anatomical power-grip posing. No runtime simulation dependency."""
import math,itertools
from mathutils import Vector,Matrix

_cache={}
PAD_RADII={2:.023,3:.019,4:.0195,5:.018}
BONE_RADII={2:.025,3:.0175,4:.018,5:.0175}

def solve_finger(metadata,side,digit,centre,shaft,palmar):
    key=(metadata['id'],side,digit,tuple(centre),tuple(shaft),PAD_RADII[digit],BONE_RADII[digit])
    if key in _cache:return _cache[key]
    suffix='.L' if side>0 else '.R';bones=metadata['finger_bones']
    names=[f'finger{digit}-{j}{suffix}' for j in range(1,4)]
    heads=[Vector(bones[name]['head']) for name in names]
    tails=[Vector(bones[name]['tail']) for name in names]
    pad=Vector(metadata['finger_pad_centres'][names[-1]])
    wrist=Vector(metadata['heads']['LeftHand' if side>0 else 'RightHand'])
    longitudinal=(Vector(bones['finger3-1'+suffix]['head'])-wrist).normalized()
    forward=(longitudinal-shaft*longitudinal.dot(shaft)).normalized()
    angle=math.radians({2:40,3:20,4:35,5:65}[digit])
    radial=palmar*math.cos(angle)+forward*math.sin(angle)
    along=max(-.068,min(.030,(heads[0]-centre).dot(shaft)))
    target=centre+shaft*along+radial*PAD_RADII[digit]
    bone_radius=BONE_RADII[digit]
    axes=[]
    for start,end in zip(heads,tails):
        direction=(end-start).normalized();axis=direction.cross(palmar).normalized()
        if axis.cross(direction).dot(palmar)<0:axis=-axis
        axes.append(axis)
    limits=[(0,100),(0,115),(0,80)]
    def evaluate(angles,details=False):
        matrix=Matrix.Identity(4);matrices={};chain=[heads[0]]
        for name,head,tail,axis,flex in zip(names,heads,tails,axes,angles):
            matrix=matrix@Matrix.Translation(head)@Matrix.Rotation(math.radians(flex),4,axis)@Matrix.Translation(-head)
            matrices[name]=matrix;chain.append(matrix@tail)
        skin_pad=matrix@pad;points=chain[:-1]+[skin_pad]
        penalty=0
        for start,end in zip(points,points[1:]):
            for k in range(5):
                point=start.lerp(end,k/4);t=max(-.075,min(.045,(point-centre).dot(shaft)))
                distance=(point-centre-shaft*t).length
                penalty+=max(0,bone_radius-distance)**2
        score=(skin_pad-target).length_squared+30*penalty+1e-10*sum((a-b)**2 for a,b in zip(angles,[60,90,50]))
        if details:return matrices,chain,{'angles':list(angles),'skin_pad_target':list(target),'skin_pad_error_m':(skin_pad-target).length,'bone_clearance_penalty':penalty}
        return score
    candidates=sorted(itertools.product([20,50,80],[45,80,110],[15,45,75]),key=evaluate)[:3]
    best=None;best_score=float('inf')
    for initial in candidates:
        angles=list(initial);score=evaluate(angles)
        for step in [12,6,3,1]:
            for iteration in range(12):
                changed=False
                for joint in range(3):
                    for sign in [-1,1]:
                        trial=angles.copy();trial[joint]=max(limits[joint][0],min(limits[joint][1],trial[joint]+sign*step));value=evaluate(trial)
                        if value<score:angles=trial;score=value;changed=True
                if not changed:break
        if score<best_score:best=angles;best_score=score
    result=evaluate(best,True);_cache[key]=result
    return result
