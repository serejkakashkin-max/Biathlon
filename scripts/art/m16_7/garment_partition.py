"""Shared suit/bib topology keeps layered cloth coherent under linear skinning."""
import bpy
from mathutils import Vector

def partition_cloth(surface,fields):
    mesh=surface.data;mesh.calc_loop_triangles();uv=mesh.uv_layers.active
    vertices=[];faces=[];inside_faces=[];coordinates=[];weights=[];lookup={}
    def corner(index,loop):
        vertex=mesh.vertices[index];return (vertex.co.copy(),uv.data[loop].uv.copy(),{surface.vertex_groups[g.group].name:g.weight for g in vertex.groups})
    def mix(a,b,t):
        names=set(a[2])|set(b[2]);return (a[0].lerp(b[0],t),a[1].lerp(b[1],t),{n:a[2].get(n,0)*(1-t)+b[2].get(n,0)*t for n in names})
    def clip(poly,field,positive):
        result=[]
        for first,last in zip(poly,poly[1:]+poly[:1]):
            fa,fb=field(first[0]),field(last[0]);a=fa>=0 if positive else fa<=0;b=fb>=0 if positive else fb<=0
            if a:result.append(first)
            if a!=b:
                lo,hi=0.,1.
                for _ in range(24):
                    middle=(lo+hi)/2;fm=field(first[0].lerp(last[0],middle))
                    if (fm>=0)==(fa>=0):lo=middle
                    else:hi=middle
                result.append(mix(first,last,(lo+hi)/2))
        return result
    def store(poly,inside=False):
        ids=[]
        for point,tex,skin in poly:
            key=tuple(round(float(v),7)for v in (*point,*tex))
            if key not in lookup:
                lookup[key]=len(vertices);vertices.append(point);coordinates.append(tex)
                best=sorted(skin.items(),key=lambda p:p[1],reverse=True)[:4];total=sum(v for _,v in best);weights.append({n:v/total for n,v in best})
            ids.append(lookup[key])
        for i in range(1,len(ids)-1):
            triangle=(ids[0],ids[i],ids[i+1])
            if len(set(triangle))==3 and (vertices[triangle[1]]-vertices[triangle[0]]).cross(vertices[triangle[2]]-vertices[triangle[0]]).length>1e-12:
                faces.append(triangle)
                if inside:inside_faces.append(triangle)
    for face in mesh.loop_triangles:
        poly=[corner(index,loop)for index,loop in zip(face.vertices,face.loops)]
        for field in fields:
            outside=clip(poly,field,False)
            if len(outside)>=3:store(outside)
            poly=clip(poly,field,True)
            if len(poly)<3:break
        else:store(poly,True)
    covered=set(inside_faces)
    new=bpy.data.meshes.new('Suit with shared sewn bib topology');new.from_pydata(vertices,[],faces);new.update()
    flags=new.attributes.new('Covered_By_Bib','BOOLEAN','FACE')
    for index,face in enumerate(faces):flags.data[index].value=face in covered
    for material in mesh.materials:new.materials.append(material)
    surface.data=new;surface.vertex_groups.clear()
    groups={n:surface.vertex_groups.new(name=n)for n in sorted({n for values in weights for n in values})}
    for i,values in enumerate(weights):
        for n,value in values.items():groups[n].add([i],value,'REPLACE')
    layer=new.uv_layers.new(name='TailoredUV')
    for face in new.polygons:
        face.use_smooth=True
        for loop in face.loop_indices:layer.data[loop].uv=coordinates[new.loops[loop].vertex_index]
    ids=sorted({k for face in inside_faces for k in face});reverse={k:i for i,k in enumerate(ids)}
    return [vertices[k] for k in ids],[[reverse[k]for k in face]for face in inside_faces],[weights[k]for k in ids],[new.vertices[k].normal.copy()for k in ids]

def remove_covered_suit(surface):
    """Keep an editable hidden lining, exclude covered cloth from visible export."""
    import bmesh
    reference=surface.copy();reference.data=surface.data.copy();bpy.context.collection.objects.link(reference)
    reference.name='Suit_Bib_Lining_Reference';reference.hide_render=True;reference.hide_viewport=True
    reference['authoring_reference_only']=True
    bm=bmesh.new();bm.from_mesh(surface.data);layer=bm.faces.layers.int.get('Covered_By_Bib')
    # BOOLEAN attributes are exposed as integer layers in BMesh.
    if layer is None:
        flags=surface.data.attributes['Covered_By_Bib'];bm.faces.ensure_lookup_table()
        selected=[bm.faces[i]for i,row in enumerate(flags.data)if row.value]
    else:selected=[face for face in bm.faces if face[layer]]
    count=len(selected);bmesh.ops.delete(bm,geom=selected,context='FACES');bm.to_mesh(surface.data);bm.free();surface.data.update()
    return {'covered_suit_triangles_removed':count,'reference':'Suit_Bib_Lining_Reference','reference_in_runtime':False}

def retain_covered_suit(surface):
    """The race bib is removable; its underlying suit remains a complete shell."""
    flags=surface.data.attributes['Covered_By_Bib']
    return {'covered_suit_triangles_removed':0,'covered_suit_triangles_retained':sum(row.value for row in flags.data),'reference_in_runtime':False,'method':'complete_suit_beneath_separate_pullover'}
