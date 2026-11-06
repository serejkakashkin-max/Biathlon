"""A race bib is a separate garment with thickness and a complete suit beneath it."""
import bpy,bmesh,argparse,sys,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
bib=bpy.data.objects['Woven_Bib_27'];suit=bpy.data.objects['RaceSuit_Continuous_Tailored_Shell'];bm=bmesh.new();bm.from_mesh(bib.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
boundary=sum(not e.is_manifold for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free()
tree=BVHTree.FromObject(suit,bpy.context.evaluated_depsgraph_get());gaps=[tree.find_nearest(v.co)[3]for v in bib.data.vertices];flags=suit.data.attributes.get('Covered_By_Bib');covered=sum(row.value for row in flags.data)if flags else 0
rows=[{'name':'separate_closed_fabric_shell','pass':boundary==0 and volume>1e-5,'nonmanifold_edges':boundary,'volume_m3':volume},{'name':'visible_layer_ease','pass':min(gaps)>.0025 and max(gaps)<.035,'minimum_gap_m':min(gaps),'maximum_gap_m':max(gaps)},{'name':'complete_suit_under_bib','pass':covered>100,'covered_suit_faces_retained':covered}]
report={'schema_version':1,'id':'m167_separate_pullover_bib_gate','pass':all(r['pass']for r in rows),'rows':rows};Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Bib is still a sewn-in patch or suit underlay is absent')
