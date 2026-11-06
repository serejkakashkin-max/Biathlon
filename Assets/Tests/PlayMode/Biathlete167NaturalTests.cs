#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167NaturalTests
    {
        private const string Prefab="Assets/Biathlon/Characters/Biathlete_16_7/Prefabs/Biathlete_16_7_Natural.prefab";
        private static readonly HumanBodyBones[] Limbs={HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        private static readonly AthleteBone[] SourceLimbs={AthleteBone.LeftUpperArm,AthleteBone.LeftLowerArm,AthleteBone.LeftHand,
            AthleteBone.RightUpperArm,AthleteBone.RightLowerArm,AthleteBone.RightHand,
            AthleteBone.LeftUpperLeg,AthleteBone.LeftLowerLeg,AthleteBone.LeftFoot,
            AthleteBone.RightUpperLeg,AthleteBone.RightLowerLeg,AthleteBone.RightFoot};
        private Biathlete167Rig rig;
        private Biathlete167Motion motion;
        [Serializable] private sealed class GeometryReceipt { public int active_lod0_triangles; public string export_fbx_sha256; public float tuck_power_grip_obliquity_degrees,tuck_upper_grip_offset_m; }
        [Serializable] private sealed class ContactReceipt { public float tuck_power_grip_obliquity_degrees,tuck_upper_grip_offset_m; }
        [SetUp] public void Create()
        {
            rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)).GetComponent<Biathlete167Rig>();
            rig.transform.SetPositionAndRotation(new Vector3(2,.3f,-5),Quaternion.Euler(0,27,0));
            motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);
        }
        [TearDown] public void Destroy(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}

        [Test] public void CandidateUsesNaturalAnatomyAndTheProductionMotionPipeline()
        {
            Assert.That(rig.IsNaturalAnatomy,Is.True);rig.Validate();
            Assert.That(motion.MainReference,Is.Not.Null);
            Assert.That(motion.MainReference.SourceDriver,Is.TypeOf<AthleteHybridLocomotionDriver>());
            Assert.That(motion.MainReference.SourceRig.transform.IsChildOf(rig.transform),Is.False);
            Assert.That(rig.GetComponentsInChildren<LODGroup>(true).Length,Is.EqualTo(1));
            Assert.That(rig.GetComponentsInChildren<Collider>(true),Is.Empty);
            int budget=0;
            foreach(var renderer in rig.Lods.GetLODs()[0].renderers)
            {if(!renderer.gameObject.activeInHierarchy)continue;var skin=renderer as SkinnedMeshRenderer;var mesh=skin!=null?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;budget+=mesh.triangles.Length/3;Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));}
            Assert.That(budget,Is.InRange(60000,200000));
            var receipt=JsonUtility.FromJson<GeometryReceipt>(File.ReadAllText("Assets/Biathlon/Characters/Biathlete_16_7/Documentation/natural-asset-manifest.json"));
            var definition=JsonUtility.FromJson<ContactReceipt>(rig.ContactDefinition.text);
            Assert.That(receipt.tuck_power_grip_obliquity_degrees,Is.EqualTo(definition.tuck_power_grip_obliquity_degrees).Within(.00001f),"Grip authoring settings require a matching model export.");
            Assert.That(receipt.tuck_upper_grip_offset_m,Is.EqualTo(definition.tuck_upper_grip_offset_m).Within(.000001f));
            using(var sha=SHA256.Create())
            {
                var hash=sha.ComputeHash(File.ReadAllBytes("Assets/Biathlon/Characters/Biathlete_16_7/Models/Biathlete_16_7_Natural.fbx"));
                Assert.That(BitConverter.ToString(hash).Replace("-","").ToLowerInvariant(),Is.EqualTo(receipt.export_fbx_sha256),"The imported candidate and its generation receipt must be from the same export.");
            }
            // Unity removes degenerate source triangles on import; the export
            // count is an upper bound, not the measured runtime triangle count.
            Assert.That(budget,Is.InRange(Mathf.FloorToInt(receipt.active_lod0_triangles*.99f),receipt.active_lod0_triangles));
            float pivotDistance=Vector3.Distance(rig.Bone(HumanBodyBones.Neck).position,rig.Bone(HumanBodyBones.Head).position);
            Assert.That(pivotDistance,Is.InRange(.085f,.115f));
            foreach(bool right in new[]{false,true})
            {
                var hand=rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                Assert.That(Vector3.Distance(hand.position,rig.GripContact(right).position),Is.InRange(.03f,.16f),"The actual grip frame must be inside the anatomical hand, not at the model origin.");
            }
            motion.EvaluateCycle(.25f);
            foreach(var renderer in motion.MainReference.SourceRig.GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False);
        }

        [Test] public void ExposedNeckAndJawHaveNoOpenSkinEdges()
        {
            var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();var mesh=skin.sharedMesh;
            int material=Array.FindIndex(skin.sharedMaterials,m=>m.name=="Anatomical_Skin_CC0_2K");Assert.That(material,Is.GreaterThanOrEqualTo(0));
            var points=mesh.vertices;var welded=new Dictionary<Vector3Int,int>();var ids=new int[points.Length];var unique=new List<Vector3>();
            for(int i=0;i<points.Length;i++)
            {
                var p=rig.transform.InverseTransformPoint(skin.transform.TransformPoint(points[i]));
                var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                if(!welded.TryGetValue(key,out int id)){id=unique.Count;welded.Add(key,id);unique.Add(p);}ids[i]=id;
            }
            var edges=new Dictionary<(int,int),int>();var triangles=mesh.GetTriangles(material);
            for(int i=0;i<triangles.Length;i+=3)for(int edge=0;edge<3;edge++)
            {
                int a=ids[triangles[i+edge]],b=ids[triangles[i+(edge+1)%3]];var key=a<b?(a,b):(b,a);
                edges.TryGetValue(key,out int count);edges[key]=count+1;
            }
            float exposedBoundary=0;
            foreach(var edge in edges)if(edge.Value==1)
            {
                var a=unique[edge.Key.Item1];var b=unique[edge.Key.Item2];float height=(a.y+b.y)*.5f;
                // The hidden neckline cut, eyelids and mouth are outside this band.
                if(height>1.535f&&height<1.595f)exposedBoundary+=Vector3.Distance(a,b);
            }
            Assert.That(exposedBoundary,Is.LessThan(.001f),"Exposed cervical skin contains a cut or a hole.");
        }

        [TestCase(SkiMotionState.Flat)] [TestCase(SkiMotionState.Uphill)] [TestCase(SkiMotionState.MaxSprint)] [TestCase(SkiMotionState.Tuck)]
        [TestCase(SkiMotionState.SteepUphill)] [TestCase(SkiMotionState.FastSkate)] [TestCase(SkiMotionState.NoPole)] [TestCase(SkiMotionState.DoublePole)]
        [TestCase(SkiMotionState.CornerLeft)] [TestCase(SkiMotionState.CornerRight)] [TestCase(SkiMotionState.Fatigued)] [TestCase(SkiMotionState.SlowStop)]
        public void LimbsFollowTheActualMainAthleteWithoutStretchOrRootDrift(SkiMotionState state)
        {
            var rootPosition=rig.transform.position;var rootRotation=rig.transform.rotation;
            var lengths=new float[8];
            for(int i=0;i<4;i++)for(int j=0;j<2;j++)lengths[i*2+j]=Vector3.Distance(rig.Bone(Limbs[i*3+j]).position,rig.Bone(Limbs[i*3+j+1]).position);
            motion.MainReference.State=state;motion.MainReference.ContactAdaptationEnabled=false;
            for(int sample=0;sample<48;sample++)
            {
                motion.EvaluateCycle(sample/48f);
                Assert.That(motion.MainReference.SourceDriver.MotionState,Is.EqualTo(state));
                for(int i=0;i<4;i++)for(int j=0;j<2;j++)
                {
                    int k=i*3+j;
                    Vector3 actual=rig.Bone(Limbs[k+1]).position-rig.Bone(Limbs[k]).position;
                    Vector3 expected=motion.MainReference.SourceRig.Bone(SourceLimbs[k+1]).position-motion.MainReference.SourceRig.Bone(SourceLimbs[k]).position;
                    Assert.That(Vector3.Dot(actual.normalized,expected.normalized),Is.GreaterThan(.9999f),state+" / "+Limbs[k]+" / "+sample);
                    Assert.That(actual.magnitude,Is.EqualTo(lengths[i*2+j]).Within(.00003f));
                }
                Assert.That(rig.transform.position,Is.EqualTo(rootPosition));Assert.That(Quaternion.Angle(rig.transform.rotation,rootRotation),Is.LessThan(.001f));
                Assert.That(rig.Animator.applyRootMotion,Is.False);
            }
        }

        [Test] public void ScrubbingDoesNotDependOnThePreviouslyViewedFrame()
        {
            motion.EvaluateCycle(.375f);var transforms=rig.Animator.GetComponentsInChildren<Transform>(true);
            var positions=transforms.Select(t=>t.position).ToArray();var rotations=transforms.Select(t=>t.rotation).ToArray();
            motion.EvaluateCycle(.91f);motion.EvaluateCycle(.05f);motion.EvaluateCycle(3.375f);
            for(int i=0;i<transforms.Length;i++)
            {Assert.That(Vector3.Distance(positions[i],transforms[i].position),Is.LessThan(.00003f),transforms[i].name);Assert.That(Quaternion.Angle(rotations[i],transforms[i].rotation),Is.LessThan(.08f),transforms[i].name);}
        }

        [Test] public void MainMotionDoesNotSwivelThePoleHandleThroughTheGlove()
        {
            var left=rig.Bone(HumanBodyBones.LeftHand).GetComponentInChildren<MeshFilter>().transform;
            var right=rig.Bone(HumanBodyBones.RightHand).GetComponentInChildren<MeshFilter>().transform;
            var lp=left.localPosition;var lr=left.localRotation;var rp=right.localPosition;var rr=right.localRotation;
            for(int i=0;i<48;i++)
            {
                motion.EvaluateCycle(i/48f);
                Assert.That(Vector3.Distance(left.localPosition,lp),Is.LessThan(.00001f));Assert.That(Vector3.Distance(right.localPosition,rp),Is.LessThan(.00001f));
                Assert.That(Quaternion.Angle(left.localRotation,lr),Is.LessThan(.06f));Assert.That(Quaternion.Angle(right.localRotation,rr),Is.LessThan(.06f));
            }
        }

        [TestCase(SkiMotionState.Flat)][TestCase(SkiMotionState.Uphill)][TestCase(SkiMotionState.Tuck)]
        [TestCase(SkiMotionState.MaxSprint)][TestCase(SkiMotionState.CornerLeft)][TestCase(SkiMotionState.CornerRight)]
        [TestCase(SkiMotionState.SteepUphill)][TestCase(SkiMotionState.FastSkate)][TestCase(SkiMotionState.NoPole)]
        [TestCase(SkiMotionState.DoublePole)][TestCase(SkiMotionState.Fatigued)][TestCase(SkiMotionState.SlowStop)]
        public void ContactAdaptationKeepsWristsBoundedAndToesInTheirBindings(SkiMotionState state)
        {
            motion.MainReference.State=state;
            var hands=new[]{rig.Bone(HumanBodyBones.LeftHand),rig.Bone(HumanBodyBones.RightHand)};
            var lower=new[]{rig.Bone(HumanBodyBones.LeftLowerArm),rig.Bone(HumanBodyBones.RightLowerArm)};
            var neutral=new[]{hands[0].InverseTransformDirection(hands[0].position-lower[0].position),hands[1].InverseTransformDirection(hands[1].position-lower[1].position)};
            float heelRise=0;
            for(int i=0;i<96;i++)
            {
                motion.EvaluateCycle(i/96f);
                for(int side=0;side<2;side++)
                    Assert.That(Vector3.Angle(hands[side].TransformDirection(neutral[side]),hands[side].position-lower[side].position),Is.LessThanOrEqualTo(state==SkiMotionState.Tuck?40.1f:25.1f),state+" / "+i+" / wrist "+side);
                Assert.That(Vector3.Distance(rig.GripContact(false).position,rig.GripContact(true).position),Is.InRange(.20f,.90f),state+" / grip spacing");
                Assert.That(motion.MainReference.MaximumBindingResidualMeters,Is.LessThan(.0001f),state+" / toe anchor");
                Assert.That(motion.MainReference.MaximumHeelRiseDegrees,Is.InRange(0,38.01f));
                heelRise=Mathf.Max(heelRise,motion.MainReference.MaximumHeelRiseDegrees);
                if(state==SkiMotionState.Tuck)
                {
                    Assert.That(Vector3.Dot(rig.GripContact(false).up,-rig.transform.forward),Is.GreaterThan(.90f),"Tuck poles must trail backwards, not point upward.");
                    Assert.That(Vector3.Dot(rig.GripContact(true).up,-rig.transform.forward),Is.GreaterThan(.90f));
                }
            }
            if(state==SkiMotionState.Flat)Assert.That(heelRise,Is.GreaterThan(1),"The trailing heel must be allowed to lift off the ski.");
        }

        [Test] public void TuckGripHasPalmarSideUpAndPolesTrailAroundTheArms()
        {
            motion.MainReference.State=SkiMotionState.Tuck;motion.EvaluateCycle(0);
            foreach(bool right in new[]{false,true})
            {
                var hand=rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var shoulder=rig.transform.InverseTransformPoint(rig.Bone(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm).position);
                var grip=rig.GripContact(right);var p=rig.transform.InverseTransformPoint(grip.position);var d=rig.transform.InverseTransformDirection(grip.up);float side=Mathf.Sign(shoulder.x);
                var nodes=rig.GetComponentsInChildren<Transform>(true);string prefix=right?"Right":"Left";
                var physicalPalm=(nodes.Single(t=>t.name==prefix+"_Palm_Side").position-nodes.Single(t=>t.name==prefix+"_Dorsal_Side").position).normalized;
                Assert.That(Vector3.Dot(physicalPalm,rig.transform.right*side),Is.GreaterThan(.15f),"The full power-grip palms face outward/up while the knuckles and four fingers wrap the handle.");
                Assert.That(Vector3.Dot(physicalPalm,rig.transform.up),Is.GreaterThan(.30f),"Actual palm side must turn upward toward the chin. The oblique pole-frame X axis is not the anatomical palm normal.");
                Assert.That(d.z,Is.LessThan(-.90f));Assert.That(d.y,Is.GreaterThan(-.1f));float t=(shoulder.z-p.z)/d.z;Assert.That(t,Is.GreaterThan(.1f));
                var shaft=p+d*t;
                Assert.That(shaft.y,Is.LessThan(shoulder.y-.03f),"The shafts must stay below the shoulder pivots, inside the arms.");
                Assert.That(shaft.x*side,Is.LessThan(shoulder.x*side+.02f),"The shaft must pass inside the arm, close to the body.");
            }
        }

        [Test] public void PoleShaftsStayOutsideBothForearmsAcrossAllProductionStates()
        {
            foreach(SkiMotionState state in Enum.GetValues(typeof(SkiMotionState)))
            for(int phase=0;phase<96;phase++)
            {
                motion.MainReference.State=state;motion.EvaluateCycle(phase/96f);
                foreach(bool poleRight in new[]{false,true})foreach(bool armRight in new[]{false,true})
                {
                    var grip=rig.GripContact(poleRight);
                    var a=rig.Bone(armRight?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm).position;
                    var b=rig.Bone(armRight?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position;
                    Assert.That(SegmentDistance(grip.position+grip.up*.05f,grip.position+grip.up*1.46f,a,b),Is.GreaterThan(.025f),state+" / "+phase+" / pole "+poleRight+" / arm "+armRight);
                }
            }
        }

        [Test] public void ImportedGripFramesPointToTheActualPalmarSide()
        {
            var transforms=rig.GetComponentsInChildren<Transform>(true);
            foreach(bool right in new[]{false,true})
            {
                string prefix=right?"Right":"Left";
                var palm=transforms.Single(t=>t.name==prefix+"_Palm_Side");var dorsal=transforms.Single(t=>t.name==prefix+"_Dorsal_Side");
                var physical=(palm.position-dorsal.position).normalized;
                Assert.That(Vector3.Dot(rig.GripContact(right).right,physical),Is.GreaterThan(.9f),"FBX handedness must preserve the palmar side of the planting grip.");
                Assert.That(Vector3.Dot(rig.TuckGripContact(right).right,physical),Is.GreaterThan(.7f),"Tuck contact must lie on the palmar side too.");
                var reversed=rig.GripContact(right).rotation*Quaternion.Euler(0,180,0)*Vector3.right;
                Assert.That(Vector3.Dot(reversed,physical),Is.LessThan(-.9f),"This gate must reject the earlier 180-degree mirrored export.");
            }
        }

        [Test] public void GripVariantsSwitchTogetherWithTheirActualPoleContacts()
        {
            var gloves=rig.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.EndsWith("_Glove",StringComparison.Ordinal)).ToArray();
            Assert.That(gloves.Length,Is.EqualTo(4));
            foreach(var state in new[]{SkiMotionState.Tuck,SkiMotionState.Flat,SkiMotionState.Tuck})
            {
                motion.MainReference.State=state;motion.EvaluateCycle(.25f);
                bool tucked=state==SkiMotionState.Tuck;
                foreach(var glove in gloves)Assert.That(glove.gameObject.activeSelf,Is.EqualTo(glove.name.Contains("Tuck")==tucked));
                foreach(bool right in new[]{false,true})
                {
                    var hand=rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                    var pole=hand.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(m=>m.sharedMesh.bounds.size.magnitude).First();
                    var contact=rig.GripContact(right);var axis=contact.up;
                    // The actual mesh tip, not only a frame, must be on this ray.
                    float near=float.PositiveInfinity,far=float.NegativeInfinity;int shaftVertices=0;
                    foreach(var vertex in pole.sharedMesh.vertices)
                    {
                        var delta=pole.transform.TransformPoint(vertex)-contact.position;float t=Vector3.Dot(delta,axis);
                        if(t>.15f&&t<1.44f){Assert.That(Vector3.Cross(delta,axis).magnitude,Is.LessThan(.05f));shaftVertices++;}
                        near=Mathf.Min(near,t);far=Mathf.Max(far,t);
                    }
                    Assert.That(shaftVertices,Is.GreaterThan(20));Assert.That(far,Is.GreaterThan(1.44f));Assert.That(near,Is.LessThan(-.05f));
                }
            }
        }

        [Test] public void TuckGlovesKeepAVisibleGapBetweenTheHands()
        {
            motion.MainReference.State=SkiMotionState.Tuck;
            for(int phase=0;phase<4;phase++)
            {
                motion.EvaluateCycle(phase/4f);
                var gloves=rig.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.EndsWith("Tuck_Glove",StringComparison.Ordinal)).ToArray();
                Assert.That(gloves.Length,Is.EqualTo(2));
                Assert.That(SeparatingGap(gloves[0],gloves[1]),Is.GreaterThanOrEqualTo(.003f),"The actual posed hand bounds must be separated, including the wrist loops.");
            }
        }

        private static float SeparatingGap(MeshFilter a,MeshFilter b)
        {
            var axesA=new[]{a.transform.right,a.transform.up,a.transform.forward};var axesB=new[]{b.transform.right,b.transform.up,b.transform.forward};
            var axes=axesA.Concat(axesB).Concat(axesA.SelectMany(x=>axesB.Select(y=>Vector3.Cross(x,y)))).ToArray();
            float gap=float.NegativeInfinity;
            foreach(var axis in axes)
            {
                if(axis.sqrMagnitude<.000001f)continue;var direction=axis.normalized;
                void Project(MeshFilter filter,out float min,out float max)
                {
                    min=float.PositiveInfinity;max=float.NegativeInfinity;var bounds=filter.sharedMesh.bounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var local=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                        float projection=Vector3.Dot(filter.transform.TransformPoint(local),direction);min=Mathf.Min(min,projection);max=Mathf.Max(max,projection);
                    }
                }
                Project(a,out float minA,out float maxA);Project(b,out float minB,out float maxB);
                gap=Mathf.Max(gap,Mathf.Max(minA-maxB,minB-maxA));
            }
            return gap;
        }

        [Test] public void TuckShaftsDoNotPierceTheActualSkinnedAthlete()
        {
            motion.MainReference.State=SkiMotionState.Tuck;
            var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();var baked=new Mesh();
            try {for(int phase=0;phase<4;phase++)
            {
                motion.EvaluateCycle(phase/4f);skin.BakeMesh(baked);var vertices=baked.vertices;var triangles=baked.triangles;
                for(int i=0;i<vertices.Length;i++)vertices[i]=skin.transform.TransformPoint(vertices[i]);
                foreach(bool right in new[]{false,true})
                {
                    var grip=rig.GripContact(right);var across=Vector3.ProjectOnPlane(rig.transform.right,grip.up).normalized;var vertical=Vector3.Cross(grip.up,across);
                    // Centre plus eight surface rays, 7 mm from the axis. The
                    // shaft radius is 4.3 mm: this also checks a small clearance.
                    for(int ray=0;ray<9;ray++)
                    {
                        float angle=(ray-1)*Mathf.PI/4;var offset=ray==0?Vector3.zero:(across*Mathf.Cos(angle)+vertical*Mathf.Sin(angle))*.007f;
                        var start=grip.position+grip.up*.05f+offset;
                        for(int i=0;i<triangles.Length;i+=3)
                        {
                            var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                            var edge=b-a;var other=c-a;var cross=Vector3.Cross(grip.up,other);float det=Vector3.Dot(edge,cross);
                            if(Mathf.Abs(det)<1e-9f)continue;
                            var delta=start-a;float u=Vector3.Dot(delta,cross)/det;if(u<0||u>1)continue;
                            var q=Vector3.Cross(delta,edge);float v=Vector3.Dot(grip.up,q)/det;if(v<0||u+v>1)continue;
                            float distance=Vector3.Dot(other,q)/det;
                            Assert.That(distance<=0||distance>=1.45f,Is.True,"Actual body intersection: side="+right+", phase="+phase+", ray="+ray+", distance="+distance+", triangle="+i);
                        }
                    }
                }
            }}finally{Object.DestroyImmediate(baked);}
        }

        [Test] public void TuckElbowsRemainTuckedRatherThanFlaredOutsideTheShoulders()
        {
            motion.MainReference.State=SkiMotionState.Tuck;motion.EvaluateCycle(0);
            foreach(bool right in new[]{false,true})
            {
                var shoulder=rig.transform.InverseTransformPoint(rig.Bone(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm).position);
                var elbow=rig.transform.InverseTransformPoint(rig.Bone(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm).position);
                Assert.That(Mathf.Abs(elbow.x),Is.LessThan(Mathf.Abs(shoulder.x)+.04f),"Anatomical elbow must stay close to the body, not swing outside the shoulder.");
            }
        }

        private static float SegmentDistance(Vector3 p,Vector3 q,Vector3 a,Vector3 b)
        {
            var u=q-p;var v=b-a;var w=p-a;float aa=Vector3.Dot(u,u),bb=Vector3.Dot(v,v),ab=Vector3.Dot(u,v),aw=Vector3.Dot(u,w),bw=Vector3.Dot(v,w);
            float denominator=aa*bb-ab*ab;float x=denominator>.00000001f?Mathf.Clamp01((ab*bw-bb*aw)/denominator):0;
            float y=(ab*x+bw)/bb;
            if(y<0){y=0;x=Mathf.Clamp01(-aw/aa);}else if(y>1){y=1;x=Mathf.Clamp01((ab-aw)/aa);}
            return Vector3.Distance(p+u*x,a+v*y);
        }

        [Test] public void WarmedRetargetingReusesItsCachedObjects()
        {
            foreach(SkiMotionState state in Enum.GetValues(typeof(SkiMotionState)))
            {
                motion.MainReference.State=state;
                for(int i=0;i<120;i++)motion.EvaluateCycle(i/120f);
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<120;i++)motion.EvaluateCycle(i/120f);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero,state.ToString());
            }
        }
    }
}
#endif
