"""Thin glove shell covers bare-skin nail recesses without moving palmar pads."""
from mathutils import Vector

def nail_cover(metadata,mesh):
    positions=[v.co.copy() for v in mesh.vertices];neighbours=[set() for _ in positions]
    for edge in mesh.edges:
        a,b=edge.vertices;neighbours[a].add(b);neighbours[b].add(a)
    normals=[v.normal.copy() for v in mesh.vertices];strength={}
    for prefix,suffix,side in [('Left','.L',1),('Right','.R',-1)]:
        bones=metadata['finger_bones'];wrist=Vector(metadata['heads'][prefix+'Hand'])
        longitudinal=(Vector(bones['finger3-1'+suffix]['head'])-wrist).normalized()
        across=Vector(bones['finger5-1'+suffix]['head'])-Vector(bones['finger2-1'+suffix]['head'])
        palmar=across.cross(longitudinal).normalized()
        if palmar.x*side>0:palmar=-palmar
        for digit in range(1,6):
            for vertex in mesh.vertices:
                weight=metadata['finger_weights'][vertex.index].get(f'finger{digit}-3{suffix}',0)
                facing=-vertex.normal.dot(palmar)
                if weight>.55 and facing>.25:
                    strength[vertex.index]=min(1,(weight-.55)/.25)*min(1,(facing-.25)/.4)
    changed=positions.copy()
    for _ in range(3):
        updated=changed.copy()
        for k,weight in strength.items():
            average=sum((changed[j] for j in neighbours[k]),Vector())/len(neighbours[k])
            fill=max(0,(average-changed[k]).dot(normals[k]))*.65*weight
            updated[k]=changed[k]+normals[k]*min(.0007,fill)
        changed=updated
    for k,weight in strength.items():changed[k]+=normals[k]*(.0007*weight)
    overrides={str(k):list(changed[k]) for k in strength if (changed[k]-positions[k]).length>1e-7}
    return overrides,{'modified_vertices':len(overrides),'maximum_outward_offset_m':max(((changed[k]-positions[k]).length for k in strength),default=0),
                      'palmar_pad_vertices_modified':0,'purpose':'Cover the dorsal nail recesses with a thin glove shell; original anatomy remains unchanged.'}
