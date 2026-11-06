"""Original solid, cap-fitted mask webbing. Lens/frame remain CC-BY derivatives."""
import bpy,math,subprocess
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def finish_mask(parts,cap,mesh,tube,block,rigid,materials,out,settings):
    graph=bpy.context.evaluated_depsgraph_get();evaluated=cap.evaluated_get(graph);data=evaluated.to_mesh()
    tree=BVHTree.FromPolygons([cap.matrix_world@v.co for v in data.vertices],[tuple(f.vertices)for f in data.polygons]);evaluated.to_mesh_clear()
    elastic=bpy.data.materials['PULSE_Mask_Woven_Strap'];accent=bpy.data.materials['PULSE_Mask_Strap_Accent'];rim=bpy.data.materials['PULSE_Mask_Graphite_Frame']
    image_path=out/'Textures/Mask_PULSE_Strap_BaseColor_2K.png';root=Path(__file__).resolve().parents[3]
    subprocess.run(['powershell','-NoProfile','-File',str(Path(__file__).with_name('draw_mask_webbing.ps1')),'-Output',str(image_path),'-FontPath',str(root/'Assets/Resources/Biathlon/UI/Fonts/Roboto.ttf')],check=True,capture_output=True)
    image=bpy.data.images.load(str(image_path));node=elastic.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;elastic.node_tree.links.new(node.outputs['Color'],elastic.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    half=settings['mask_strap_width_m']/2;thickness=settings['mask_strap_thickness_m'];gap=settings['mask_strap_cap_gap_m'];height=settings['mask_strap_rear_height_m']
    def on_cap(theta,z,offset):
        radial=Vector((math.cos(theta),math.sin(theta),0));origin=Vector((0,.032,z));hit=tree.ray_cast(origin,radial,.25)
        if hit[0] is None:raise RuntimeError('Mask strap misses final cap surface at '+str((theta,z)))
        return hit[0]+radial*offset
    # One continuous path: right frame, right side, rear hemisphere, left frame.
    sections=[]
    for side in [1,-1]:
        theta=0 if side>0 else math.pi;end=on_cap(theta,height,gap+thickness/2);start=Vector((side*.092,-.084,1.687));control=Vector((side*.094,-.017,1.706))
        connector=[]
        for j in range(25):
            t=j/24;centre=(1-t)**2*start+2*(1-t)*t*control+t*t*end
            connector.append((centre,None,t,theta))
        if side>0:sections.extend(connector[:-1])
        else:left=list(reversed(connector[:-1]))
    for j in range(129):
        theta=j/128*math.pi;sections.append((on_cap(theta,height,gap+thickness/2),theta,1,theta))
    sections.extend(left)
    vertices=[];uv=[];faces=[];indices=[];lengths=[0.0]
    for a,b in zip(sections,sections[1:]):lengths.append(lengths[-1]+(b[0]-a[0]).length)
    for j,(centre,theta,t,side_theta)in enumerate(sections):
        radial=Vector((math.cos(side_theta),math.sin(side_theta),0))
        for dz,depth in [(-half,0),(half,0),(half,thickness),(-half,thickness)]:
            if theta is not None:p=on_cap(theta,height+dz,gap+depth)
            else:
                p=centre+Vector((0,0,dz))+radial*(depth-thickness/2)
                # Blend connector edge to the exact cap edge at its final end.
                target=on_cap(side_theta,height+dz,gap+depth)
                linear=on_cap(side_theta,height,gap+thickness/2)+Vector((0,0,dz))+radial*(depth-thickness/2)
                p+=(target-linear)*t*t
            vertices.append(p);uv.append((lengths[j]/lengths[-1],0 if dz<0 else 1))
        if j:
            for k in range(4):faces.append(((j-1)*4+k,(j-1)*4+(k+1)%4,j*4+(k+1)%4,j*4+k));indices.append(0 if k in [0,2] else 1)
    faces.extend([(3,2,1,0),tuple(range(len(vertices)-4,len(vertices)))]);indices.extend([1,1])
    strap=mesh('Mask_PULSE_Fitted_Elastic_Strap',vertices,faces,[elastic,accent],indices,uv);strap['construction']='closed_solid_cap_fitted_webbing';strap['thickness_m']=thickness;rigid(strap,'Head');parts.append(strap)
    # A curved adjustment frame with a middle crossbar, fitted to the same cap.
    theta=1.19;centre=on_cap(theta,height,gap+thickness+.002);tangent=Vector((-math.sin(theta),math.cos(theta),0));radial=Vector((math.cos(theta),math.sin(theta),0))
    outline=[(-.018,-.014),(.018,-.014),(.018,.014),(-.018,.014),(-.018,-.014)]
    for label,points in [('Frame',outline),('Crossbar',[(0,-.014),(0,.014)])]:
        coords=[]
        for x,z in points:
            p=centre+tangent*x+Vector((0,0,z));angle=math.atan2(p.y-.032,p.x);coords.append(on_cap(angle,p.z,gap+thickness+.0025))
        obj=tube('Mask_PULSE_Adjuster_'+label,coords,.0017,rim,'Head');parts.append(obj)
    for side in [-1,1]:
        clip=block('Mask_PULSE_Temple_Clip',(.092*side,-.081,1.687),(.008,.012,.027),rim,.002,'Head');parts.append(clip)
    for obj in parts:obj.hide_render=True
    return {'construction':'closed_solid_cap_fitted_webbing','width_m':half*2,'thickness_m':thickness,'cap_gap_m':gap,'solid_vertices':len(vertices),'buckle':'curved_adjuster_with_crossbar','texture':'Mask_PULSE_Strap_BaseColor_2K.png'}
