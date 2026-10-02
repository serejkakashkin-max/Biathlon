"""Exact whole-mm paired chords for new recipe-controlled ridge geometry."""
import math

def exact_paired_chords(points,target_mm):
    assert len(points)%2==1 and len(points)>=3
    def sub(a,b):return tuple(x-y for x,y in zip(a,b))
    def add(a,b):return tuple(x+y for x,y in zip(a,b))
    def mul(a,n):return tuple(x*n for x in a)
    def dot(a,b):return sum(x*y for x,y in zip(a,b))
    def unit(a):return mul(a,1/math.sqrt(dot(a,a)))
    pairs=[]
    for i in range(0,len(points)-2,2):
        a,p,b=points[i:i+3];l1,l2=math.dist(a,p),math.dist(p,b);distance=math.dist(a,b)
        pairs.append({'i':i,'budget':max(math.floor(l1*1000)+math.floor(l2*1000),math.ceil(distance*1000-1e-8)),
            'raw':(l1+l2)*1000,'l1':l1,'l2':l2})
    remainder=target_mm-sum(pair['budget'] for pair in pairs);assert remainder>=0
    order=sorted(pairs,key=lambda pair:-(pair['raw']-pair['budget']))
    for i in range(remainder):order[i%len(order)]['budget']+=1
    result=points[:];lengths=[]
    for pair in pairs:
        i=pair['i'];a,old,b=points[i:i+3];distance=math.dist(a,b)
        first=round(pair['budget']*pair['l1']/(pair['l1']+pair['l2']));second=pair['budget']-first;l1,l2=first/1000,second/1000
        assert abs(l1-l2)<=distance+1e-9 and distance<=l1+l2+1e-9
        axis=unit(sub(b,a));axial=(l1*l1-l2*l2+distance*distance)/(2*distance);projection=add(a,mul(axis,axial))
        perpendicular=sub(old,add(a,mul(axis,dot(sub(old,a),axis))))
        if math.dist(perpendicular,(0,0,0))<1e-8:
            perpendicular=(-axis[2],0,axis[0]);perpendicular=sub(perpendicular,mul(axis,dot(perpendicular,axis)))
        result[i+1]=add(projection,mul(unit(perpendicular),math.sqrt(max(0,l1*l1-axial*axial))));lengths.extend((first,second))
    assert sum(lengths)==target_mm
    assert max(abs(math.dist(a,b)-n/1000) for a,b,n in zip(result,result[1:],lengths))<1e-8
    return result,lengths

def ridge_span(controls,combined_length,start_y,start_z,real_slack):
    """Solve plateau travel plus the connected return span, then quantize chords."""
    assert controls[0]=={'fraction':0,'z_offset_m':0,'y_offset_m':0}
    assert controls[-1]=={'fraction':1,'z_offset_m':0,'y_offset_m':0}
    assert all(a['fraction']<b['fraction'] for a,b in zip(controls,controls[1:]))
    def points(span):return [(c['fraction']*span,start_y+c['y_offset_m'],start_z+c['z_offset_m']) for c in controls]
    def total(span):return sum(math.dist(a,b) for a,b in zip(points(span),points(span)[1:]))+span
    low,high=1.,combined_length;target=combined_length-real_slack
    assert total(low)<target<total(high)
    for _ in range(80):
        middle=(low+high)/2
        if total(middle)<target:low=middle
        else:high=middle
    span_mm=round((low+high)/2*1000);plateau_mm=round(combined_length*1000)-span_mm
    path,chords=exact_paired_chords(points(span_mm/1000),plateau_mm)
    return span_mm/1000,path,chords
