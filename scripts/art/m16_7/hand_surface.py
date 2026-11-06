"""Exact one-level Catmull-Clark surface stencils for offline glove fitting."""
import numpy as np

def combine(terms):
    result={}
    for row,weight in terms:
        for key,value in row.items():result[key]=result.get(key,0)+weight*value
    return result

class HandSurface:
    def __init__(self,metadata,side,digit,names):
        prefix='Left' if side>0 else 'Right';suffix='.L' if side>0 else '.R'
        cage=metadata['hand_cages'][prefix];self.rest=np.asarray(cage['positions'],dtype=np.float64)
        faces=cage['faces'];n=len(self.rest);edges={};adj_faces=[[] for _ in range(n)];adj_edges=[[] for _ in range(n)]
        face_rows=[{k:1/len(face) for k in face} for face in faces]
        for fi,face in enumerate(faces):
            for k in face:adj_faces[k].append(fi)
            for a,b in zip(face,face[1:]+face[:1]):edges.setdefault(tuple(sorted((a,b))),[]).append(fi)
        edge_keys=list(edges);edge_map={edge:i for i,edge in enumerate(edge_keys)}
        for ei,(a,b) in enumerate(edge_keys):adj_edges[a].append(ei);adj_edges[b].append(ei)
        rows=[]
        for vi in range(n):
            boundary=[edge_keys[ei][1] if edge_keys[ei][0]==vi else edge_keys[ei][0] for ei in adj_edges[vi] if len(edges[edge_keys[ei]])==1]
            if boundary:rows.append({vi:.75,**{k:.25/len(boundary) for k in boundary}});continue
            count=len(adj_faces[vi]);terms=[({vi:1},(count-3)/count)]
            terms.extend((face_rows[fi],1/(count*count)) for fi in adj_faces[vi])
            terms.extend(({a:.5,b:.5},2/(count*len(adj_edges[vi]))) for a,b in (edge_keys[ei] for ei in adj_edges[vi]))
            rows.append(combine(terms))
        for edge in edge_keys:
            a,b=edge;neighbours=edges[edge]
            rows.append(combine([({a:.5,b:.5},1)] if len(neighbours)==1 else [({a:.5,b:.5},.5)]+[(face_rows[fi],.25) for fi in neighbours]))
        rows.extend(face_rows)
        quads=[]
        for fi,face in enumerate(faces):
            for i,k in enumerate(face):
                following=n+edge_map[tuple(sorted((k,face[(i+1)%len(face)])))];previous=n+edge_map[tuple(sorted((face[i-1],k)))];centre=n+len(edge_keys)+fi
                quads.append((k,following,centre,previous))
        self.quads=np.asarray(quads,dtype=np.int32)
        # Blender's default Subsurf places refined vertices on the limit
        # surface. Plain one-level cage points differ by over 1 mm at tips.
        neighbours=[set() for _ in rows];opposites=[[] for _ in rows];refined_edges={}
        for face in quads:
            for i,k in enumerate(face):
                neighbours[k].update([face[(i-1)%4],face[(i+1)%4]])
                opposites[k].append(face[(i+2)%4])
            for a,b in zip(face,face[1:]+face[:1]):refined_edges[tuple(sorted((a,b)))]=refined_edges.get(tuple(sorted((a,b))),0)+1
        boundary=[[] for _ in rows]
        for (a,b),count in refined_edges.items():
            if count==1:boundary[a].append(b);boundary[b].append(a)
        limit_rows=[]
        for k,row in enumerate(rows):
            if boundary[k]:
                limit_rows.append(combine([(row,2/3)]+[(rows[j],1/(3*len(boundary[k]))) for j in boundary[k]]))
            else:
                count=len(neighbours[k]);denominator=count*(count+5)
                limit_rows.append(combine([(row,count*count/denominator)]+[(rows[j],4/denominator) for j in neighbours[k]]+[(rows[j],1/denominator) for j in opposites[k]]))
        maximum=max(map(len,limit_rows));self.indices=np.zeros((len(rows),maximum),dtype=np.int32);self.coefficients=np.zeros((len(rows),maximum))
        for i,row in enumerate(limit_rows):
            for j,(key,value) in enumerate(row.items()):self.indices[i,j]=key;self.coefficients[i,j]=value
        self.weights=np.asarray([[row.get(name,0) for name in names] for row in cage['weights']]);self.remainder=1-self.weights.sum(axis=1)
        wrist=np.asarray(metadata['heads'][prefix+'Hand']);longitudinal=np.asarray(metadata['finger_bones']['finger3-1'+suffix]['head'])-wrist;longitudinal/=np.linalg.norm(longitudinal)
        guard=metadata['hand_carpal_guard'];factor=np.clip(((self.rest-wrist)@longitudinal-guard['start_m'])/(guard['end_m']-guard['start_m']),0,1)
        self.preservation=factor*factor*(3-2*factor)
        influence=(self.weights.sum(axis=1)[self.indices]*self.coefficients).sum(axis=1)
        self.collision_vertices=influence>.02;self.collision_faces=(influence[self.quads]>.02).any(axis=1)
        reverse={k:i for i,k in enumerate(cage['source_vertex_ids'])}
        self.pad=np.asarray([reverse[k] for k in metadata['finger_palmar_pad_vertex_ids'][f'finger{digit}-3{suffix}']])
        self.pad_faces=np.isin(self.quads[:,0],self.pad)
        adjacency=[];corners=[];destinations=[]
        for destination,vertex in enumerate(self.pad):
            fi,corner=np.where(self.quads==vertex)
            adjacency.extend(fi);corners.extend(corner);destinations.extend([destination]*len(fi))
        self.pad_adjacency=np.asarray(adjacency);self.pad_corners=np.asarray(corners);self.pad_destinations=np.asarray(destinations)

    def surface(self,matrices):
        posed=self.rest*self.remainder[:,None]
        for i,matrix in enumerate(matrices):
            m=np.asarray(matrix,dtype=np.float64)
            posed+=(self.rest@m[:3,:3].T+m[:3,3])*self.weights[:,i,None]
        posed=self.rest+(posed-self.rest)*self.preservation[:,None]
        return (posed[self.indices]*self.coefficients[:,:,None]).sum(axis=1)

    def sample(self,matrices):
        return self.sample_surface(self.surface(matrices))

    def sample_surface(self,surface,accurate_normal=False):
        faces=surface[self.quads[self.pad_faces]]
        normals=np.cross(faces[:,1]-faces[:,0],faces[:,2]-faces[:,0])+np.cross(faces[:,2]-faces[:,0],faces[:,3]-faces[:,0])
        normal=normals.sum(axis=0);normal/=max(np.linalg.norm(normal),1e-12)
        if accurate_normal:
            faces=surface[self.quads[self.pad_adjacency]]
            face_normals=np.cross(faces[:,1]-faces[:,0],faces[:,2]-faces[:,0])+np.cross(faces[:,2]-faces[:,0],faces[:,3]-faces[:,0])
            face_normals/=np.maximum(np.linalg.norm(face_normals,axis=1),1e-12)[:,None]
            row=np.arange(len(faces));centre=faces[row,self.pad_corners]
            before=faces[row,(self.pad_corners-1)%4]-centre;after=faces[row,(self.pad_corners+1)%4]-centre
            cosine=(before*after).sum(axis=1)/np.maximum(np.linalg.norm(before,axis=1)*np.linalg.norm(after,axis=1),1e-12)
            angles=np.arccos(np.clip(cosine,-1,1));vertex_normals=np.zeros((len(self.pad),3))
            np.add.at(vertex_normals,self.pad_destinations,face_normals*angles[:,None])
            vertex_normals/=np.maximum(np.linalg.norm(vertex_normals,axis=1),1e-12)[:,None]
            normal=vertex_normals.mean(axis=0);normal/=max(np.linalg.norm(normal),1e-12)
        quads=surface[self.quads[self.collision_faces]]
        collision=np.concatenate([surface[self.collision_vertices]]+[
            quads[:,0]*((1-u)*(1-v))+quads[:,1]*(u*(1-v))+quads[:,2]*(u*v)+quads[:,3]*((1-u)*v)
            for u,v in [(0,.25),(0,.5),(0,.75),(.25,0),(.5,0),(.75,0),(.25,.25),(.25,.75),(.75,.25),(.75,.75),(.5,.5)]])
        return surface[self.pad].mean(axis=0),normal,collision
