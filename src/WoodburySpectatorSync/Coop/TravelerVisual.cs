using System;
using System.Collections.Generic;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    // Original, code-built low-poly clothing and face geometry. One skinned mesh,
    // stable bone bindings and a small per-instance palette; no borrowed game assets.
    internal sealed class TravelerVisual : MonoBehaviour
    {
        private readonly List<Transform> _bones = new List<Transform>();
        private readonly Dictionary<string,int> _ids = new Dictionary<string,int>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<BoneWeight> _weights = new List<BoneWeight>();
        private readonly List<int>[] _triangles = new List<int>[10];
        private SkinnedMeshRenderer _renderer;
        private Mesh _mesh;
        private Material[] _materials;
        private Matrix4x4[] _bind;
        private CharacterProfile _profile;
        private bool _initialized;
        public CharacterProfile Profile => _profile;
        public int VertexCount => _mesh != null ? _mesh.vertexCount : 0;
        public static readonly Color[] SkinColors = Colors("F1C6A7","DCAA83","BA825A","94603E","70472F","462E23");
        public static readonly Color[] HairColors = Colors("211C1C","513527","8C482D","C9A25F","B4B6B5");
        public static readonly Color[] TopColors = Colors("344D43","A44F35","29394F","B5A17D","644657","68757E","C9CECA","B48638");
        public static readonly Color[] PantColors = Colors("383D43","354C64","525B42","8C7B5C","5C4537","25282B");
        public static readonly Color[] BootColors = Colors("604533","26282A","9E8463","5F3234");
        private static Color[] Colors(params string[] hex)
        { var colors=new Color[hex.Length]; for(int i=0;i<hex.Length;i++) { uint n=Convert.ToUInt32(hex[i],16); colors[i]=new Color((n>>16&255)/255f,(n>>8&255)/255f,(n&255)/255f); } return colors; }

        public static TravelerVisual Create(Transform parent, CharacterProfile profile)
        {
            var root=new GameObject("Winter traveler"); root.transform.SetParent(parent,false);
            var visual=root.AddComponent<TravelerVisual>(); visual.Initialize(); visual.Apply(profile); return visual;
        }
        private void Bone(string name,string parent,Vector3 position)
        {
            var bone=new GameObject(name).transform; bone.SetParent(parent==null?transform:_bones[_ids[parent]],false);
            bone.position=transform.TransformPoint(position); _ids.Add(name,_bones.Count); _bones.Add(bone);
        }
        private void Initialize()
        {
            Bone("Hips",null,new Vector3(0,.94f,0)); Bone("Spine","Hips",new Vector3(0,1.12f,0));
            Bone("Chest","Spine",new Vector3(0,1.35f,0)); Bone("Neck","Chest",new Vector3(0,1.52f,0)); Bone("Head","Neck",new Vector3(0,1.67f,0));
            for(int side=-1;side<=1;side+=2)
            {
                string s=side<0?"Left":"Right";
                Bone(s+"Arm","Chest",new Vector3(side*.25f,1.4f,0));
                Bone(s+"ForeArm",s+"Arm",new Vector3(side*.29f,1.1f,0)); Bone(s+"Hand",s+"ForeArm",new Vector3(side*.32f,.85f,.015f));
                Bone(s+"UpLeg","Hips",new Vector3(side*.105f,.92f,0));
                Bone(s+"Leg",s+"UpLeg",new Vector3(side*.11f,.52f,-.01f)); Bone(s+"Foot",s+"Leg",new Vector3(side*.11f,.10f,0));
            }
            _bind=new Matrix4x4[_bones.Count]; for(int i=0;i<_bones.Count;i++) _bind[i]=_bones[i].worldToLocalMatrix*transform.localToWorldMatrix;
            _renderer=gameObject.AddComponent<SkinnedMeshRenderer>(); _renderer.bones=_bones.ToArray(); _renderer.rootBone=_bones[0];
            _renderer.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(2,3,2)); _renderer.updateWhenOffscreen=true;
            _materials=new Material[10]; var shader=Shader.Find("Standard")??Shader.Find("Legacy Shaders/Diffuse");
            if(shader==null) throw new InvalidOperationException("No character shader available");
            for(int i=0;i<_materials.Length;i++) { _materials[i]=new Material(shader){name="Traveler palette "+i}; if(_materials[i].HasProperty("_Glossiness")) _materials[i].SetFloat("_Glossiness",.12f); _triangles[i]=new List<int>(); }
            _renderer.sharedMaterials=_materials;
            _mesh=new Mesh {name="Woodbury traveler skin"}; _renderer.sharedMesh=_mesh;
        }
        public void Apply(CharacterProfile profile)
        {
            if(!profile.IsValid || (_initialized && _profile.Equals(profile))) return;
            _profile=profile; _initialized=true;
            Color top=TopColors[profile.Top], hair=HairColors[profile.HairColor];
            var palette=new[] {top,PantColors[profile.Pants],BootColors[profile.Boots],SkinColors[profile.Skin],hair,new Color(.65f,.66f,.62f),new Color(.91f,.92f,.89f),new Color(.08f,.085f,.08f),new Color(.33f,.14f,.12f),Color.Lerp(top,Color.white,.3f)};
            for(int i=0;i<palette.Length;i++) _materials[i].color=palette[i];
            _vertices.Clear(); _weights.Clear(); foreach(var triangles in _triangles) triangles.Clear();
            Body(profile); Face(profile);
            // Unity caches GPU skinning buffers by mesh identity. Reusing Clear() on
            // a rendered mesh with a different vertex count can stop rendering.
            var previous=_mesh;
            _mesh=new Mesh {name="Woodbury traveler skin"};
            _mesh.SetVertices(_vertices); _mesh.boneWeights=_weights.ToArray(); _mesh.bindposes=_bind;
            _mesh.subMeshCount=_triangles.Length; for(int i=0;i<_triangles.Length;i++) _mesh.SetTriangles(_triangles[i],i);
            _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            _renderer.sharedMesh=null; _renderer.sharedMesh=_mesh;
            if(previous!=null) Destroy(previous);
        }
        private void Body(CharacterProfile p)
        {
            // Continuous garment surfaces blend across the joints; rigid decorative
            // pieces sit on top without exposing gaps when elbows and knees bend.
            Ellipsoid("Hips",new Vector3(0,.95f,0),new Vector3(.19f,.15f,.13f),1);
            Tube(new[]{"Spine","Spine","Chest","Chest","Chest"},
                new[]{new Vector3(0,.95f,0),new Vector3(0,1.12f,0),new Vector3(0,1.28f,0),new Vector3(0,1.41f,0),new Vector3(0,1.50f,0)},
                new[]{new Vector2(.20f,.138f),new Vector2(.205f,.143f),new Vector2(.235f,.15f),new Vector2(.245f,.14f),new Vector2(.085f,.075f)},0);
            Ellipsoid("Neck",new Vector3(0,1.535f,0),new Vector3(.065f,.075f,.065f),3);
            Box("Chest",new Vector3(0,1.37f,.143f),new Vector3(.012f,.27f,.008f),5);
            for(int side=-1;side<=1;side+=2)
            {
                string s=side<0?"Left":"Right";
                Tube(new[]{s+"ForeArm",s+"ForeArm",s+"Arm",s+"Arm",s+"Arm"},
                    new[]{new Vector3(side*.32f,.87f,.01f),new Vector3(side*.30f,1.03f,.004f),new Vector3(side*.29f,1.1f,0),new Vector3(side*.265f,1.3f,0),new Vector3(side*.245f,1.425f,0)},
                    new[]{new Vector2(.059f,.067f),new Vector2(.071f,.077f),new Vector2(.075f,.08f),new Vector2(.087f,.093f),new Vector2(.072f,.078f)},0);
                Ellipsoid(s+"ForeArm",new Vector3(side*.32f,.87f,.01f),new Vector3(.07f,.035f,.077f),9);
                Ellipsoid(s+"Hand",new Vector3(side*.325f,.805f,.02f),new Vector3(.042f,.053f,.029f),3);
                for(int finger=0;finger<4;finger++) Ellipsoid(s+"Hand",new Vector3(side*(.294f+finger*.02f),.752f+Mathf.Abs(finger-1.5f)*.005f,.025f),new Vector3(.010f,.035f,.013f),3,8,6);
                Ellipsoid(s+"Hand",new Vector3(side*.282f,.81f,.045f),new Vector3(.019f,.04f,.021f),3);
                Tube(new[]{s+"Leg",s+"Leg",s+"UpLeg",s+"UpLeg",s+"UpLeg"},
                    new[]{new Vector3(side*.11f,.16f,0),new Vector3(side*.11f,.41f,-.01f),new Vector3(side*.11f,.52f,-.01f),new Vector3(side*.11f,.72f,0),new Vector3(side*.105f,.94f,0)},
                    new[]{new Vector2(.066f,.075f),new Vector2(.074f,.085f),new Vector2(.08f,.088f),new Vector2(.094f,.109f),new Vector2(.098f,.115f)},1);
                Ellipsoid(s+"Foot",new Vector3(side*.11f,.125f,.015f),new Vector3(.081f,.12f,.10f),2);
                Ellipsoid(s+"Foot",new Vector3(side*.11f,.065f,.075f),new Vector3(.088f,.064f,.15f),2);
                Box(s+"Foot",new Vector3(side*.11f,.025f,.07f),new Vector3(.16f,.025f,.24f),7);
                for(int n=0;n<3;n++) Box(s+"Foot",new Vector3(side*.11f,.125f,.085f+n*.023f),new Vector3(.09f,.009f,.009f),5);
            }
            if(p.Outfit==0)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Chest",new Vector3(side*.12f,1.35f,.137f),new Vector3(.105f,.12f,.025f),9);
                    Box("Spine",new Vector3(side*.125f,1.075f,.125f),new Vector3(.11f,.10f,.025f),9);
                    Ellipsoid("Chest",new Vector3(side*.065f,1.49f,.04f),new Vector3(.065f,.045f,.095f),9);
                }
            }
            else if(p.Outfit==1)
            {
                for(int n=0;n<4;n++) Ellipsoid(n<2?"Spine":"Chest",new Vector3(0,1.02f+n*.13f,0),new Vector3(n==3?.255f:.225f,.078f,.163f),n%2==0?0:9);
                Box("Chest",new Vector3(0,1.35f,.167f),new Vector3(.016f,.28f,.012f),5);
            }
            else
            {
                Ellipsoid("Chest",new Vector3(0,1.44f,-.10f),new Vector3(.15f,.14f,.09f),9);
                Box("Spine",new Vector3(0,1.09f,.133f),new Vector3(.22f,.12f,.025f),9);
                for(int s=-1;s<=1;s+=2) Box("Chest",new Vector3(s*.038f,1.36f,.15f),new Vector3(.007f,.17f,.007f),6);
            }
            if(p.Accessory==2)
            {
                Ellipsoid("Chest",new Vector3(0,1.51f,0),new Vector3(.12f,.06f,.11f),9);
                Box("Chest",new Vector3(.085f,1.34f,.16f),new Vector3(.07f,.27f,.025f),9);
            }
        }
        private void Face(CharacterProfile p)
        {
            float width=p.Face==1?.108f:p.Face==2?.13f:.115f, height=p.Face==3?.174f:p.Face==2?.143f:.16f;
            var center=new Vector3(0,1.68f,.005f);
            Ellipsoid("Head",center,new Vector3(width,height,.117f),3,12,10);
            Ellipsoid("Head",new Vector3(0,1.645f,.10f),new Vector3(width*.72f,.07f,.034f),3);
            Ellipsoid("Head",new Vector3(0,1.681f,.119f),new Vector3(p.Face==1?.027f:.021f,.041f,p.Face==3?.047f:.032f),3);
            for(int side=-1;side<=1;side+=2)
            {
                Ellipsoid("Head",new Vector3(side*(width+.002f),1.684f,0),new Vector3(.024f,.042f,.028f),3);
                Ellipsoid("Head",new Vector3(side*.047f,1.716f,.106f),new Vector3(.029f,.014f,.013f),6);
                Ellipsoid("Head",new Vector3(side*.046f,1.716f,.118f),new Vector3(.011f,.012f,.006f),7);
                Box("Head",new Vector3(side*.047f,1.745f,.108f),new Vector3(.052f,.011f,.012f),4);
            }
            Box("Head",new Vector3(0,1.612f,.121f),new Vector3(.052f,.009f,.007f),8);
            if(p.Hair!=4)
            {
                Ellipsoid("Head",new Vector3(0,1.68f+height*.66f,-.019f),new Vector3(width*1.06f,height*.46f,.112f),p.Hair==3?9:4);
                if(p.Hair==1) Ellipsoid("Head",new Vector3(-.043f,1.79f,.064f),new Vector3(.084f,.037f,.066f),4);
                if(p.Hair==2)
                {
                    Ellipsoid("Head",new Vector3(0,1.68f,-.065f),new Vector3(width*1.08f,.135f,.068f),4);
                    for(int side=-1;side<=1;side+=2) Ellipsoid("Head",new Vector3(side*(width-.006f),1.69f,-.015f),new Vector3(.032f,.135f,.071f),4);
                }
                if(p.Hair==3) Ellipsoid("Head",new Vector3(0,1.795f,-.005f),new Vector3(width*1.12f,.032f,.126f),0);
            }
            if(p.Accessory==1)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    float x=side*.051f;
                    Box("Head",new Vector3(x,1.736f,.128f),new Vector3(.077f,.009f,.008f),7);
                    Box("Head",new Vector3(x,1.696f,.128f),new Vector3(.077f,.008f,.008f),7);
                    for(int edge=-1;edge<=1;edge+=2) Box("Head",new Vector3(x+edge*.034f,1.716f,.128f),new Vector3(.008f,.04f,.008f),7);
                }
                Box("Head",new Vector3(0,1.725f,.13f),new Vector3(.026f,.008f,.009f),7);
            }
            if(p.Accessory==3) Ellipsoid("Head",new Vector3(0,1.582f,.069f),new Vector3(width*.81f,.05f,.066f),4);
        }
        private void Vertex(Vector3 point,int bone)
        { _vertices.Add(point); _weights.Add(new BoneWeight {boneIndex0=bone,weight0=1}); }
        private void Tube(string[] bones,Vector3[] centers,Vector2[] radii,int material)
        {
            const int segments=16;
            int start=_vertices.Count; var tri=_triangles[material];
            for(int ring=0;ring<centers.Length;ring++)
            {
                int bone=_ids[bones[ring]], previous=ring>0?_ids[bones[ring-1]]:bone;
                for(int x=0;x<=segments;x++)
                {
                    float angle=2*Mathf.PI*x/segments;
                    _vertices.Add(centers[ring]+new Vector3(Mathf.Cos(angle)*radii[ring].x,0,Mathf.Sin(angle)*radii[ring].y));
                    _weights.Add(previous==bone?new BoneWeight{boneIndex0=bone,weight0=1}:new BoneWeight{boneIndex0=bone,weight0=.5f,boneIndex1=previous,weight1=.5f});
                }
            }
            for(int ring=0;ring<centers.Length-1;ring++) for(int x=0;x<segments;x++)
            {
                int a=start+ring*(segments+1)+x,b=a+segments+1;
                tri.Add(a);tri.Add(b);tri.Add(a+1);tri.Add(a+1);tri.Add(b);tri.Add(b+1);
            }
            int bottom=_vertices.Count; Vertex(centers[0],_ids[bones[0]]);
            int top=_vertices.Count; Vertex(centers[centers.Length-1],_ids[bones[bones.Length-1]]);
            for(int x=0;x<segments;x++)
            {
                tri.Add(bottom);tri.Add(start+x);tri.Add(start+x+1);
                int a=start+(centers.Length-1)*(segments+1)+x;
                tri.Add(top);tri.Add(a+1);tri.Add(a);
            }
        }
        private void Ellipsoid(string bone,Vector3 center,Vector3 size,int material,int segments=12,int rings=8)
        {
            int start=_vertices.Count,id=_ids[bone];
            for(int y=0;y<=rings;y++)
            {
                float latitude=Mathf.PI*y/rings;
                for(int x=0;x<=segments;x++)
                {
                    float longitude=2*Mathf.PI*x/segments;
                    Vertex(center+Vector3.Scale(size,new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude))),id);
                }
            }
            var tri=_triangles[material];
            for(int y=0;y<rings;y++) for(int x=0;x<segments;x++)
            {
                int a=start+y*(segments+1)+x,b=a+segments+1;
                tri.Add(a);tri.Add(a+1);tri.Add(b);tri.Add(a+1);tri.Add(b+1);tri.Add(b);
            }
        }
        private void Box(string bone,Vector3 center,Vector3 size,int material)
        {
            int start=_vertices.Count,id=_ids[bone];
            for(int i=0;i<8;i++) Vertex(center+Vector3.Scale(size,new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f)),id);
            int[] indices={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};
            foreach(int index in indices) _triangles[material].Add(start+index);
        }
        private void OnDestroy()
        { if(_mesh!=null) Destroy(_mesh); if(_materials!=null) foreach(var material in _materials) if(material!=null) Destroy(material); }
    }
}
