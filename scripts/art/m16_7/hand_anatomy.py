"""Measured palmar pad patches on the unwarped CC0 anatomical source."""
import argparse,json,sys
from pathlib import Path
from mathutils import Vector

def add_palmar_pads(metadata,mesh):
    metadata['hand_carpal_guard']={'start_m':.015,'end_m':.040,'purpose':'Preserve the wrist seam while authoring thumb opposition; smooth corrective skinning reaches full articulation in the distal palm.'}
    from glove_shell import nail_cover
    overrides,shell_receipt=nail_cover(metadata,mesh)
    metadata['glove_shell_vertex_overrides']=overrides;metadata['glove_shell_receipt']=shell_receipt
    centres={};normals={};patches={};pad_samples={};surface_samples={}
    def sample(k):
        vertex=mesh.vertices[k]
        return {'position':list(vertex.co),'normal':list(vertex.normal),'weights':metadata['finger_weights'][k]}
    for prefix,suffix,side in [('Left','.L',1),('Right','.R',-1)]:
        bones=metadata['finger_bones'];wrist=Vector(metadata['heads'][prefix+'Hand'])
        longitudinal=(Vector(bones['finger3-1'+suffix]['head'])-wrist).normalized()
        across=Vector(bones['finger5-1'+suffix]['head'])-Vector(bones['finger2-1'+suffix]['head'])
        palmar=across.cross(longitudinal).normalized()
        if palmar.x*side>0:palmar=-palmar
        for digit in range(1,6):
            name=f'finger{digit}-3{suffix}'
            indices=[v.index for v in mesh.vertices if metadata['finger_weights'][v.index].get(name,0)>.7 and v.normal.dot(palmar)>.2]
            if len(indices)<3:raise RuntimeError('No measured palmar distal patch for '+name)
            centres[name]=list(sum((mesh.vertices[k].co for k in indices),Vector())/len(indices))
            normals[name]=list(sum((mesh.vertices[k].normal for k in indices),Vector()).normalized())
            patches[name]=indices
            pad_samples[name]=[sample(k) for k in indices]
            surface=[v.index for v in mesh.vertices if sum(value for key,value in metadata['finger_weights'][v.index].items() if key.startswith(f'finger{digit}-'))>.7]
            count=min(48,len(surface))
            selected=[surface[round(i*(len(surface)-1)/max(1,count-1))] for i in range(count)]
            surface_samples[name]=[sample(k) for k in sorted(set(selected))]
    metadata.update(finger_palmar_pad_centres=centres,finger_palmar_pad_normals=normals,finger_palmar_pad_vertex_ids=patches,
                    finger_palmar_pad_samples=pad_samples,finger_surface_samples=surface_samples)
    palmar_ids={k for patch in patches.values() for k in patch}
    shell_receipt['palmar_pad_vertices_modified']=len(palmar_ids.intersection(map(int,overrides)))
    if shell_receipt['palmar_pad_vertices_modified']:
        raise RuntimeError('Dorsal glove shell must not move a palmar contact patch')
    cages={}
    for prefix,side in [('Left',1),('Right',-1)]:
        wrist=Vector(metadata['heads'][prefix+'Hand']);direction=(wrist-Vector(metadata['heads'][prefix+'ForeArm'])).normalized()
        keep={v.index for v in mesh.vertices if (v.co-wrist).dot(direction)>-.008 and side*v.co.x>.41}
        faces=[list(face.vertices) for face in mesh.polygons if all(k in keep for k in face.vertices)]
        indices=sorted({k for face in faces for k in face});reverse={k:i for i,k in enumerate(indices)}
        cages[prefix]={'source_vertex_ids':indices,'positions':[overrides.get(str(k),list(mesh.vertices[k].co)) for k in indices],
                       'faces':[[reverse[k] for k in face] for face in faces],
                       'weights':[metadata['finger_weights'][k] for k in indices]}
    metadata['hand_cages']=cages

if __name__=='__main__':
    import bpy
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    p=argparse.ArgumentParser();p.add_argument('--body',required=True);p.add_argument('--metadata',required=True)
    a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);path=Path(a.metadata)
    bpy.ops.wm.open_mainfile(filepath=str(Path(a.body).resolve()))
    metadata=json.loads(path.read_text());add_palmar_pads(metadata,bpy.data.objects['MakeHuman_Athletic_Fitted'].data)
    temporary=path.with_suffix('.json.authoring-tmp');temporary.write_text(json.dumps(metadata,indent=2)+'\n');temporary.replace(path)
    print('Measured ten palmar distal patches:',{k:len(v) for k,v in metadata['finger_palmar_pad_vertex_ids'].items()})
