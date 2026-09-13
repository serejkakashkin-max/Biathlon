using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>
    /// Original, unbranded development athlete. Geometry is presentation art, in metres, +Y up/+Z forward.
    /// The reusable prefab is preferred in races; the procedural path also makes asset regeneration explicit.
    /// No simulation values, imported designs, colliders or root motion are used by this builder.
    /// </summary>
    public static class OriginalAthleteRigBuilder
    {
        public const string ResourcePath = "Biathlon/Athletes/OriginalAthlete";
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.Ordinal);

        public static AthleteRigBinding Create(Transform parent)
        {
            var prefab = Resources.Load<AthleteRigBinding>(ResourcePath);
            return prefab != null ? UnityEngine.Object.Instantiate(prefab, parent, false) : CreateDevelopment(parent);
        }

        public static AthleteRigBinding CreateDevelopment(Transform parent)
        {
            var root = Child("Original development athlete", parent, Vector3.zero);
            var bones = new Transform[(int)AthleteBone.Count];
            var sockets = new Transform[(int)AthleteSocket.Count];
            Bone(AthleteBone.Hips, root, new Vector3(0, .99f, 0));
            Bone(AthleteBone.Spine, bones[(int)AthleteBone.Hips], new Vector3(0, .17f, 0));
            Bone(AthleteBone.Chest, bones[(int)AthleteBone.Spine], new Vector3(0, .18f, 0));
            Bone(AthleteBone.Neck, bones[(int)AthleteBone.Chest], new Vector3(0, .22f, 0));
            Bone(AthleteBone.Head, bones[(int)AthleteBone.Neck], new Vector3(0, .09f, 0));
            Limb(false); Limb(true);
            Socket(AthleteSocket.RifleBack, AthleteBone.Chest, new Vector3(.035f, -.17f, -.165f), Quaternion.Euler(-90, 0, -12));
            Socket(AthleteSocket.RifleAim, AthleteBone.Chest, new Vector3(.12f, .08f, -.08f), Quaternion.identity);
            Socket(AthleteSocket.CameraTarget, AthleteBone.Chest, new Vector3(0, .04f, 0), Quaternion.identity);

            var suit = Material("TechnicalNavy", new Color(.028f, .10f, .18f), .20f);
            var accent = Material("TechnicalTeal", new Color(.035f, .53f, .56f), .23f);
            var bib = Material("BibTextile", new Color(.86f, .90f, .87f), .10f);
            var rubber = Material("GloveBootRubber", new Color(.033f, .043f, .051f), .28f);
            var skin = Material("Skin", new Color(.62f, .38f, .26f), .26f);
            var metal = Material("MatteComposite", new Color(.065f, .085f, .10f), .36f, .28f);
            var lens = Material("SportLens", new Color(.075f, .58f, .74f), .91f, .72f);
            var body = new List<BodyPiece>();

            Add(AthleteBone.Hips, "Pelvis", suit, Profile("Pelvis", new[] { R(-.11f,.11f,.080f), R(-.055f,.145f,.10f), R(.045f,.15f,.102f), R(.15f,.139f,.087f) }));
            Add(AthleteBone.Spine, "Waist", suit, Profile("Waist", new[] { R(-.04f,.137f,.086f), R(.05f,.148f,.09f), R(.15f,.174f,.10f), R(.22f,.188f,.108f) }));
            Add(AthleteBone.Chest, "Thorax", suit, Profile("Thorax", new[] { R(-.035f,.171f,.10f), R(.06f,.196f,.112f), R(.145f,.201f,.11f), R(.185f,.151f,.081f), R(.21f,.068f,.064f) }));
            // Bib is a cloth shell over the ribcage, with original solid panels instead of reference logos.
            Add(AthleteBone.Chest, "Bib", bib, Profile("Bib", new[] { R(-.08f,.161f,.103f), R(.015f,.185f,.116f), R(.125f,.198f,.119f), R(.177f,.144f,.09f) }));
            Add(AthleteBone.Spine, "BibHem", accent, Profile("BibHem", new[] { R(.085f,.164f,.102f), R(.11f,.17f,.105f), R(.13f,.175f,.108f) }));
            Add(AthleteBone.Neck, "Collar", suit, Profile("Collar", new[] { R(-.015f,.066f,.063f), R(.072f,.057f,.057f) }));
            Add(AthleteBone.Head, "Face", skin, Profile("Face", new[] { R(-.017f,.048f,.054f,.014f), R(.025f,.065f,.07f,.016f), R(.075f,.074f,.071f,.009f), R(.135f,.071f,.074f), R(.17f,.046f,.052f), R(.183f,.011f,.02f) }, 16));
            Add(AthleteBone.Head, "Cap", suit, Profile("Cap", new[] { R(.09f,.076f,.078f), R(.145f,.068f,.074f), R(.18f,.040f,.049f), R(.19f,.009f,.012f) },16));
            Add(AthleteBone.Head, "Headband", accent, Profile("Headband", new[] { R(.087f,.077f,.079f), R(.113f,.077f,.079f) },16));
            Add(AthleteBone.Head, "Eyewear", lens, Ellipsoid("Lens", new Vector3(.074f,.026f,.021f)), new Vector3(0,.078f,.069f));
            Add(AthleteBone.Head, "Nose", skin, Ellipsoid("Nose", new Vector3(.015f,.029f,.018f)), new Vector3(0,.040f,.083f));

            foreach (bool right in new[] { false, true })
            {
                var upperArm = right ? AthleteBone.RightUpperArm : AthleteBone.LeftUpperArm;
                var lowerArm = right ? AthleteBone.RightLowerArm : AthleteBone.LeftLowerArm;
                var hand = right ? AthleteBone.RightHand : AthleteBone.LeftHand;
                var thigh = right ? AthleteBone.RightUpperLeg : AthleteBone.LeftUpperLeg;
                var shin = right ? AthleteBone.RightLowerLeg : AthleteBone.LeftLowerLeg;
                var foot = right ? AthleteBone.RightFoot : AthleteBone.LeftFoot;
                Add(upperArm,"Sleeve",suit,Profile("Sleeve",new[] { R(-.305f,.043f,.047f),R(-.20f,.051f,.060f),R(-.09f,.066f,.070f),R(-.025f,.074f,.072f),R(.035f,.055f,.058f) }));
                Add(upperArm,"SleeveBand",accent,Profile("SleeveBand",new[] {R(-.18f,.056f,.063f),R(-.12f,.063f,.069f)}));
                Add(lowerArm,"Forearm",suit,Profile("Forearm",new[] {R(-.28f,.031f,.036f),R(-.20f,.038f,.043f),R(-.08f,.048f,.052f),R(.022f,.044f,.047f)}));
                Add(hand,"Glove",rubber,Profile("Glove",new[] {R(-.105f,.024f,.028f,.009f),R(-.080f,.033f,.035f,.011f),R(-.020f,.034f,.038f),R(.028f,.033f,.036f)}));
                Add(hand,"GloveCuff",accent,Profile("GloveCuff",new[] {R(.005f,.035f,.038f),R(.025f,.035f,.038f)}));
                Add(thigh,"Thigh",suit,Profile("Thigh",new[] {R(-.46f,.047f,.057f,.008f),R(-.34f,.056f,.069f),R(-.18f,.075f,.087f),R(-.035f,.09f,.09f),R(.045f,.084f,.086f)}));
                Add(thigh,"OuterThighPanel",accent,Ellipsoid("ThighPanel",new Vector3(.006f,.155f,.06f)),new Vector3(right?.075f:-.075f,-.18f,0));
                Add(shin,"LowerLeg",suit,Profile("LowerLeg",new[] {R(-.39f,.031f,.035f),R(-.29f,.034f,.042f,-.006f),R(-.15f,.051f,.064f,-.014f),R(-.06f,.052f,.061f),R(.024f,.046f,.054f)}));
                Add(shin,"ShinPanel",accent,Ellipsoid("ShinPanel",new Vector3(.030f,.135f,.006f)),new Vector3(0,-.15f,.053f));
                Add(foot,"BootShell",rubber,Profile("BootShell",new[] {R(-.06f,.048f,.112f,.047f),R(-.025f,.050f,.116f,.043f),R(.035f,.044f,.076f,.012f),R(.14f,.038f,.044f)}));
                Add(foot,"BootSole",metal,Ellipsoid("BootSole",new Vector3(.049f,.015f,.122f)),new Vector3(0,-.061f,.047f));
                Add(foot,"BootClosure",accent,Ellipsoid("BootClosure",new Vector3(.046f,.014f,.056f)),new Vector3(0,.064f,0));
            }
            // Harness remains with the thorax; its simple original straps are independent from the rifle prop.
            foreach(float side in new[] { -.13f,.13f })
            {
                Add(AthleteBone.Chest,"FrontHarness",rubber,Ellipsoid("Harness",new Vector3(.018f,.139f,.009f)),new Vector3(side,.04f,.110f));
                Add(AthleteBone.Chest,"BackHarness",rubber,Ellipsoid("Harness",new Vector3(.018f,.139f,.009f)),new Vector3(side,.04f,-.114f));
            }
            SkinBody(root,bones,body);
            Equipment(root,sockets,rubber,metal,accent,bib);
            var binding = root.gameObject.AddComponent<AthleteRigBinding>();
            binding.Configure(root,bones,sockets);
            root.gameObject.AddComponent<AthleteLocomotionDriver>();
            root.gameObject.AddComponent<AthleteRiflePresentation>();
            root.gameObject.AddComponent<AthleteShootingPose>();
            root.gameObject.AddComponent<AthleteGroundContact>();
            return binding;

            void Bone(AthleteBone id,Transform ancestor,Vector3 at) => bones[(int)id]=Child(id.ToString(),ancestor,at);
            void Socket(AthleteSocket id,AthleteBone bone,Vector3 at,Quaternion rotation)
            {
                var t=Child(id.ToString(),bones[(int)bone],at);t.localRotation=rotation;sockets[(int)id]=t;
            }
            void Limb(bool right)
            {
                float sign=right?1:-1;
                var arm=right?AthleteBone.RightUpperArm:AthleteBone.LeftUpperArm;
                var forearm=right?AthleteBone.RightLowerArm:AthleteBone.LeftLowerArm;
                var hand=right?AthleteBone.RightHand:AthleteBone.LeftHand;
                var thigh=right?AthleteBone.RightUpperLeg:AthleteBone.LeftUpperLeg;
                var shin=right?AthleteBone.RightLowerLeg:AthleteBone.LeftLowerLeg;
                var foot=right?AthleteBone.RightFoot:AthleteBone.LeftFoot;
                Bone(arm,bones[(int)AthleteBone.Chest],new Vector3(sign*.214f,.151f,0));
                Bone(forearm,bones[(int)arm],new Vector3(0,-.29f,0));
                Bone(hand,bones[(int)forearm],new Vector3(0,-.27f,0));
                Bone(thigh,bones[(int)AthleteBone.Hips],new Vector3(sign*.105f,-.05f,0));
                Bone(shin,bones[(int)thigh],new Vector3(0,-.44f,0));
                Bone(foot,bones[(int)shin],new Vector3(0,-.38f,0));
                Socket(right?AthleteSocket.RightHandGrip:AthleteSocket.LeftHandGrip,hand,new Vector3(0,-.056f,.026f),Quaternion.identity);
                Socket(right?AthleteSocket.RightSkiBinding:AthleteSocket.LeftSkiBinding,foot,new Vector3(0,-.107f,.02f),Quaternion.identity);
            }
            void Add(AthleteBone bone,string label,Material material,Mesh mesh,Vector3 offset=default)
                => body.Add(new BodyPiece((int)bone,material,mesh,Matrix4x4.Translate(offset)));
        }

        private static void Equipment(Transform root,Transform[] sockets,Material rubber,Material metal,Material accent,Material pale)
        {
            Transform leftPole = null, rightPole = null;
            foreach(bool right in new[] {false,true})
            {
                var ski=Child(right?"Right ski":"Left ski",sockets[(int)(right?AthleteSocket.RightSkiBinding:AthleteSocket.LeftSkiBinding)],Vector3.zero);
                var skiParts=new Parts();
                skiParts.Add(SkiMesh(),accent);
                skiParts.Add(Ellipsoid("SkiBinding",new Vector3(.032f,.022f,.085f)),rubber,new Vector3(0,.023f,.014f));
                skiParts.Add(Ellipsoid("SkiTipStripe",new Vector3(.017f,.003f,.24f)),pale,new Vector3(0,.018f,.66f));
                skiParts.Render(ski,"SkiComplete");
                var pole=Child(right?"Right pole":"Left pole",sockets[(int)(right?AthleteSocket.RightHandGrip:AthleteSocket.LeftHandGrip)],Vector3.zero);
                if (right) rightPole = pole; else leftPole = pole;
                pole.localRotation=Quaternion.Euler(47,0,0);
                var poleParts=new Parts();
                poleParts.Add(Profile("PoleShaft",new[]{R(-1.36f,.006f,.006f),R(-.11f,.007f,.007f)} ,8),metal);
                poleParts.Add(Profile("PoleGrip",new[]{R(-.12f,.015f,.015f),R(.025f,.015f,.015f)},8),rubber);
                poleParts.Add(Profile("PoleBasket",new[]{R(-1.305f,.034f,.034f),R(-1.29f,.034f,.034f)},8),rubber);
                poleParts.Add(Profile("PoleAccent",new[]{R(-.38f,.008f,.008f),R(-.15f,.008f,.008f)},8),accent);
                poleParts.Render(pole,"PoleComplete");
            }
            // Prop frame: buttstock at Z=0, muzzle at Z=1.04; RifleBack socket turns +Z upward.
            var rifle=Child("Original rifle",sockets[(int)AthleteSocket.RifleBack],Vector3.zero);
            var rifleParts=new Parts();
            rifleParts.Add(Ellipsoid("RifleStock",new Vector3(.031f,.056f,.17f)),metal,new Vector3(0,-.014f,.16f));
            rifleParts.Add(Ellipsoid("RifleButt",new Vector3(.039f,.084f,.021f)),rubber,new Vector3(0,0,.015f));
            rifleParts.Add(Ellipsoid("RifleCheekRest",new Vector3(.033f,.024f,.10f)),rubber,new Vector3(0,.04f,.14f));
            rifleParts.Add(Ellipsoid("RifleForeEnd",new Vector3(.027f,.033f,.17f)),metal,new Vector3(0,-.007f,.50f));
            rifleParts.Add(Profile("RifleBarrel",new[]{R(0,.011f,.011f),R(.62f,.011f,.011f)},10),metal,new Vector3(0,.038f,.42f),Quaternion.Euler(90,0,0));
            rifleParts.Add(Ellipsoid("RifleSight",new Vector3(.024f,.024f,.02f)),metal,new Vector3(0,.065f,1.0f));
            rifleParts.Add(Ellipsoid("RifleGrip",new Vector3(.024f,.064f,.030f)),rubber,new Vector3(0,-.068f,.34f));
            rifleParts.Add(Ellipsoid("RifleTrim",new Vector3(.033f,.012f,.075f)),accent,new Vector3(0,-.034f,.52f));
            rifleParts.Render(rifle,"RifleComplete");
            root.gameObject.AddComponent<AthleteEquipmentBinding>().Configure(rifle,leftPole,rightPole);
        }

        private readonly struct BodyPiece
        {
            public readonly int Bone; public readonly Material Material; public readonly Mesh Mesh; public readonly Matrix4x4 Matrix;
            public BodyPiece(int bone,Material material,Mesh mesh,Matrix4x4 matrix){Bone=bone;Material=material;Mesh=mesh;Matrix=matrix;}
        }

        private static void SkinBody(Transform root,Transform[] bones,List<BodyPiece> pieces)
        {
            var palette=new List<Material>();foreach(var p in pieces)if(!palette.Contains(p.Material))palette.Add(p.Material);
            if(!meshes.TryGetValue("AthleteBody",out var bodyMesh)||bodyMesh==null)
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();
                var indices=new List<int>[palette.Count];for(int i=0;i<indices.Length;i++)indices[i]=new List<int>();
                foreach(var p in pieces)
                {
                    var matrix=root.worldToLocalMatrix*bones[p.Bone].localToWorldMatrix*p.Matrix;
                    int first=vertices.Count;var pv=p.Mesh.vertices;var pn=p.Mesh.normals;var puv=p.Mesh.uv;
                    for(int i=0;i<pv.Length;i++)
                    {
                        vertices.Add(matrix.MultiplyPoint3x4(pv[i]));normals.Add(matrix.MultiplyVector(pn[i]).normalized);uv.Add(puv[i]);
                        weights.Add(new BoneWeight{boneIndex0=p.Bone,weight0=1});
                    }
                    var target=indices[palette.IndexOf(p.Material)];foreach(int index in p.Mesh.triangles)target.Add(first+index);
                }
                bodyMesh=new Mesh{name="AthleteBody"};bodyMesh.SetVertices(vertices);bodyMesh.SetNormals(normals);bodyMesh.SetUVs(0,uv);
                bodyMesh.boneWeights=weights.ToArray();var bind=new Matrix4x4[bones.Length];for(int i=0;i<bind.Length;i++)bind[i]=bones[i].worldToLocalMatrix*root.localToWorldMatrix;
                bodyMesh.bindposes=bind;bodyMesh.subMeshCount=indices.Length;for(int i=0;i<indices.Length;i++)bodyMesh.SetTriangles(indices[i],i);
                bodyMesh.RecalculateBounds();meshes["AthleteBody"]=bodyMesh;
            }
            var renderer=Child("Athlete body",root,Vector3.zero).gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh=bodyMesh;renderer.sharedMaterials=palette.ToArray();renderer.bones=bones;renderer.rootBone=bones[(int)AthleteBone.Hips];
            renderer.quality=SkinQuality.Bone1;renderer.updateWhenOffscreen=false;
            // Includes prone/extended pole-drive poses without relying on offscreen skin updates.
            renderer.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(3.5f,3.6f,4));
        }

        private sealed class Parts
        {
            private readonly List<BodyPiece> pieces=new List<BodyPiece>();
            public void Add(Mesh mesh,Material material,Vector3 offset=default,Quaternion rotation=default)
            {
                if(rotation.Equals(default(Quaternion)))rotation=Quaternion.identity;
                pieces.Add(new BodyPiece(0,material,mesh,Matrix4x4.TRS(offset,rotation,Vector3.one)));
            }
            public void Render(Transform parent,string id)
            {
                var palette=new List<Material>();foreach(var p in pieces)if(!palette.Contains(p.Material))palette.Add(p.Material);
                if(!meshes.TryGetValue(id,out var mesh)||mesh==null)
                {
                    var sub=new List<Mesh>();
                    foreach(var material in palette)
                    {
                        var combinations=new List<CombineInstance>();foreach(var p in pieces)if(p.Material==material)combinations.Add(new CombineInstance{mesh=p.Mesh,transform=p.Matrix});
                        var part=new Mesh();part.CombineMeshes(combinations.ToArray(),true,true);sub.Add(part);
                    }
                    var combine=new CombineInstance[sub.Count];for(int i=0;i<sub.Count;i++)combine[i]=new CombineInstance{mesh=sub[i],transform=Matrix4x4.identity};
                    mesh=new Mesh{name=id};mesh.CombineMeshes(combine,false,false);meshes[id]=mesh;
                    foreach(var temporary in sub)Release(temporary);
                }
                parent.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=parent.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=palette.ToArray();renderer.shadowCastingMode=ShadowCastingMode.On;
            }
        }

        private static Transform Child(string name,Transform parent,Vector3 at)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;
        }
        private static Material Material(string id,Color color,float smoothness,float metallic=0)
        {
            if(materials.TryGetValue(id,out var cached)&&cached!=null)return cached;
            var source=Resources.Load<Material>("Biathlon/GreyboxLit");
            if(source==null)throw new InvalidOperationException("Original athlete requires the build-retained Biathlon/GreyboxLit material.");
            var material=new Material(source){name=id,color=color,enableInstancing=true};
            material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Metallic",metallic);materials[id]=material;return material;
        }
        private static Vector4 R(float y,float xRadius,float zRadius,float zOffset=0)=>new Vector4(y,xRadius,zRadius,zOffset);

        private static Mesh Ellipsoid(string id,Vector3 radii)
        {
            const int rings=8;var profile=new Vector4[rings+1];
            for(int i=0;i<=rings;i++)
            {
                float angle=-Mathf.PI*.5f+Mathf.PI*i/rings;float width=Mathf.Max(.015f,Mathf.Cos(angle));
                profile[i]=R(Mathf.Sin(angle)*radii.y,width*radii.x,width*radii.z);
            }
            return Profile(id,profile,12);
        }

        private static Mesh Profile(string id,Vector4[] rings,int sides=12)
        {
            if(meshes.TryGetValue(id,out var cached)&&cached!=null)return cached;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int ring=0;ring<rings.Length;ring++)
            {
                var r=rings[ring];
                for(int side=0;side<=sides;side++)
                {
                    float angle=side*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Sin(angle)*r.y,r.x,Mathf.Cos(angle)*r.z+r.w));uv.Add(new Vector2((float)side/sides,(float)ring/(rings.Length-1)));
                    if(ring==rings.Length-1||side==sides)continue;
                    int a=ring*(sides+1)+side,b=a+1,c=a+sides+1,d=c+1;
                    triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);
                }
            }
            int lower=vertices.Count;vertices.Add(new Vector3(0,rings[0].x,rings[0].w));uv.Add(Vector2.zero);
            int upper=vertices.Count;vertices.Add(new Vector3(0,rings[rings.Length-1].x,rings[rings.Length-1].w));uv.Add(Vector2.one);
            int last=(rings.Length-1)*(sides+1);
            for(int side=0;side<sides;side++)
            {
                triangles.Add(lower);triangles.Add(side+1);triangles.Add(side);
                triangles.Add(upper);triangles.Add(last+side);triangles.Add(last+side+1);
            }
            var mesh=new Mesh{name=id};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[id]=mesh;return mesh;
        }

        private static Mesh SkiMesh()
        {
            const string id="SkiGeometry";if(meshes.TryGetValue(id,out var cached)&&cached!=null)return cached;
            float[] z={-.85f,-.79f,0,.76f,.94f,1.03f};float[] halfWidth={.021f,.024f,.023f,.026f,.019f,.007f};float[] y={.008f,0,0,.004f,.040f,.085f};
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int i=0;i<z.Length;i++)
            {
                v.Add(new Vector3(-halfWidth[i],y[i],z[i]));v.Add(new Vector3(halfWidth[i],y[i],z[i]));v.Add(new Vector3(-halfWidth[i],y[i]-.013f,z[i]));v.Add(new Vector3(halfWidth[i],y[i]-.013f,z[i]));
                for(int j=0;j<4;j++)uv.Add(new Vector2(j%2,(float)i/(z.Length-1)));
                if(i==0)continue;int a=(i-1)*4,b=i*4;
                Face(a,b,a+1,b+1);Face(a+3,b+3,a+2,b+2);Face(a+2,b+2,a,b);Face(a+1,b+1,a+3,b+3);
            }
            Face(2,0,3,1);int end=v.Count-4;Face(end,end+2,end+1,end+3);
            var mesh=new Mesh{name=id};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[id]=mesh;return mesh;
            void Face(int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(c);t.Add(b);t.Add(d);}
        }
        private static void Release(UnityEngine.Object item)
        {
            if(Application.isPlaying)UnityEngine.Object.Destroy(item);else UnityEngine.Object.DestroyImmediate(item);
        }
    }
}
