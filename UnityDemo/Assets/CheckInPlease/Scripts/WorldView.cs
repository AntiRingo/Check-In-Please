using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheckInPlease
{
    public sealed class Hotspot
    {
        public string label,action;
        public Vector3 point,approach;
        public Hotspot(string l,string a,Vector3 p,Vector3 goal){label=l;action=a;point=p;approach=goal;}
    }
    // One three-storey hotel, viewed through a fixed side-overhead camera. UI uses a 1600x900 canvas.
    public sealed partial class WorldView : MonoBehaviour
    {
        public readonly List<Hotspot> spots=new List<Hotspot>();
        readonly Queue<Vector3> path=new Queue<Vector3>();
        readonly RenderTexture[] portraits=new RenderTexture[3];
        Transform roomRoot,actor,legA,legB,body;
        Camera view;
        Material black,cloth,paper,iron,shadowMat;
        Action arrived;
        public string Area {get;private set;}
        public bool Walking {get{return ManualMoving||path.Count>0||IsTravelling;}}
        public bool ManualMoving {get;private set;}
        public bool ArtReady {get;private set;}
        public int ReachableHotspots {get;private set;}
        public float CharacterHeightPixels {get;private set;}
        public Vector3 PlayerPosition {get{return actor==null?Vector3.zero:actor.position;}}
        static Color C(string h){ColorUtility.TryParseHtmlString(h,out var c);return c;}
        Material Mat(string name,string hex)
        {var m=new Material(Resources.Load<Shader>("HotelMatte"));m.name=name;m.color=C(hex);return m;}
        public void Setup(Font font)
        {
            black=Mat("Charcoal silhouette","#131c1d");cloth=Mat("Ash grey wool","#626761");
            paper=Mat("Bone collar","#c0bca9");iron=Mat("Winter cap","#303b3b");
            var cameraObject=new GameObject("Fixed scene camera");cameraObject.transform.SetParent(transform);
            view=cameraObject.AddComponent<Camera>();view.tag="MainCamera";view.orthographic=true;
            view.orthographicSize=6.3f;view.nearClipPlane=.1f;view.farClipPlane=100;
            view.backgroundColor=C("#0b1011");view.clearFlags=CameraClearFlags.SolidColor;
            view.transform.position=new Vector3(10,15,-21);view.transform.LookAt(Vector3.zero);
            view.cullingMask=~(1<<30);cameraObject.AddComponent<AudioListener>();FrameCamera();
            ArtReady=true;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=C("#a5adab");RenderSettings.fog=false;
            var lightObject=new GameObject("Northern character light");lightObject.transform.SetParent(transform);
            var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=C("#b7c3c1");sun.intensity=.65f;
            sun.shadows=LightShadows.None;lightObject.transform.rotation=Quaternion.Euler(35,-40,0);QualitySettings.antiAliasing=4;
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);var px=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {float d=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;px[y*128+x]=new Color(.02f,.03f,.03f,Mathf.Pow(Mathf.Clamp01(1-d),1.2f)*.65f);}
            texture.SetPixels(px);texture.Apply();shadowMat=new Material(Resources.Load<Shader>("HotelOverlay"));shadowMat.mainTexture=texture;
            CreatePortraits();
        }
        void FrameCamera()
        {
            float actual=Screen.width/(float)Mathf.Max(1,Screen.height),target=16f/9f;view.aspect=target;
            if(actual>target){float w=target/actual;view.rect=new Rect((1-w)/2,0,w,1);}
            else{float h=actual/target;view.rect=new Rect(0,(1-h)/2,1,h);}
        }
        public Texture PortraitTexture(int variant){return portraits[Mathf.Clamp(variant,0,2)];}
        Vector3 At(float x,float y)
        {var ray=view.ViewportPointToRay(new Vector3(x/1600,1-y/900,0));new Plane(Vector3.up,new Vector3(0,HotelLayout.Height(CurrentFloor),0)).Raycast(ray,out float d);return ray.GetPoint(d);}
        public Vector2 Project(Vector3 p)
        {var v=view.WorldToViewportPoint(p);return new Vector2(v.x*1600,(1-v.y)*900);}
        public void Build(string area,GameModel game)
        {
            if(buildingRoot!=null){buildingRoot.gameObject.SetActive(false);Destroy(buildingRoot.gameObject);}
            ReleaseFloor();path.Clear();arrived=null;ManualMoving=false;spots.Clear();BuildHotel(area,game);
        }
        GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 size,Material m,Transform parent)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;
            o.GetComponent<Renderer>().sharedMaterial=m;Destroy(o.GetComponent<Collider>());return o;
        }
        Transform Coat(Transform parent,int variant)
        {
            int segments=variant==0?20:8;
            float[] ys=variant==0?new[]{.38f,.58f,.96f,1.22f}:new[]{.30f,.65f,1.15f,1.28f};
            float[] widths=variant==0?new[]{.54f,.57f,.46f,.27f}:new[]{.39f,.37f,.27f,.22f};
            var v=new Vector3[segments*4+2];var tris=new List<int>();
            for(int r=0;r<4;r++)for(int s=0;s<segments;s++)
            {float a=s*Mathf.PI*2/segments;v[r*segments+s]=new Vector3(Mathf.Cos(a)*widths[r],ys[r],Mathf.Sin(a)*widths[r]*.59f);}
            v[v.Length-2]=new Vector3(0,ys[0],0);v[v.Length-1]=new Vector3(0,ys[3],0);
            for(int r=0;r<3;r++)for(int s=0;s<segments;s++)
            {int a=r*segments+s,b=r*segments+(s+1)%segments,c=a+segments,d=b+segments;tris.AddRange(new[]{a,c,b,b,c,d});}
            for(int s=0;s<segments;s++){int n=(s+1)%segments;tris.AddRange(new[]{v.Length-2,s,n,v.Length-1,3*segments+n,3*segments+s});}
            var mesh=new Mesh {vertices=v,triangles=tris.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject("Tailored symbolic overcoat");o.transform.SetParent(parent,false);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=black;return o.transform;
        }
        Transform Person(Vector3 p,int variant,bool player,Transform parent,float scale)
        {
            var root=new GameObject(player?"Hotel keeper":"Guest silhouette").transform;root.SetParent(parent);root.position=p;root.localScale=Vector3.one*scale;
            var coat=Coat(root,variant);
            var a=Primitive("Left boot",PrimitiveType.Cube,new Vector3(-.18f,.18f,-.02f),new Vector3(.25f,.36f,.36f),black,root).transform;
            var b=Primitive("Right boot",PrimitiveType.Cube,new Vector3(.18f,.18f,-.02f),new Vector3(.25f,.36f,.36f),black,root).transform;
            Primitive("Featureless head",PrimitiveType.Sphere,new Vector3(0,1.53f,0),new Vector3(.76f,.72f,.62f),black,root);
            foreach(int i in new[]{-1,1})Primitive("Eye slit",PrimitiveType.Cube,new Vector3(i*.14f,1.55f,-.306f),new Vector3(.10f,.028f,.015f),paper,root);
            if(variant==0)
            {
                var collar=new GameObject("Pale triangular collar");collar.transform.SetParent(root,false);
                var mesh=new Mesh {vertices=new[]{new Vector3(-.23f,1.27f,-.21f),new Vector3(.23f,1.27f,-.21f),new Vector3(0,.97f,-.33f)},triangles=new[]{0,1,2}};
                mesh.RecalculateNormals();collar.AddComponent<MeshFilter>().sharedMesh=mesh;collar.AddComponent<MeshRenderer>().sharedMaterial=paper;
            }
            else if(variant==1)
            {
                var scarf=Primitive("Wool scarf",PrimitiveType.Cube,new Vector3(0,1.27f,-.1f),new Vector3(.78f,.21f,.5f),cloth,root);scarf.transform.localRotation=Quaternion.Euler(0,0,-12);
                Primitive("Shoulder strap",PrimitiveType.Cube,new Vector3(.16f,.84f,-.24f),new Vector3(.08f,.68f,.04f),cloth,root);
                Primitive("Square shoulder bag",PrimitiveType.Cube,new Vector3(.22f,.53f,-.29f),new Vector3(.44f,.29f,.13f),cloth,root);
            }
            else
            {
                Primitive("Winter cap",PrimitiveType.Cube,new Vector3(0,1.81f,0),new Vector3(.82f,.17f,.66f),iron,root);
                Primitive("Cap brim",PrimitiveType.Cube,new Vector3(0,1.74f,-.25f),new Vector3(.79f,.07f,.35f),iron,root);
                Primitive("Briefcase",PrimitiveType.Cube,new Vector3(.43f,.44f,0),new Vector3(.15f,.44f,.45f),cloth,root);
            }
            if(player){legA=a;legB=b;body=coat;}else root.rotation=Quaternion.Euler(0,-18,0);
            var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Contact shadow";Destroy(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root,false);shadow.transform.localPosition=new Vector3(0,.008f,0);shadow.transform.localRotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(1.6f,1.2f,1);
            shadow.GetComponent<Renderer>().sharedMaterial=shadowMat;return root;
        }
        void CreatePortraits()
        {
            var stage=new GameObject("Portrait studio").transform;stage.SetParent(transform);stage.position=new Vector3(300,0,0);
            var cameraObject=new GameObject("Portrait camera");var camera=cameraObject.AddComponent<Camera>();cameraObject.transform.SetParent(stage,false);
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.91f;camera.aspect=384f/448f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,0);
            camera.cullingMask=1<<30;camera.nearClipPlane=.1f;camera.farClipPlane=10;
            cameraObject.transform.localPosition=new Vector3(.1f,1.22f,-4);cameraObject.transform.LookAt(stage.position+Vector3.up*1.05f);
            for(int i=0;i<3;i++)
            {
                var person=Person(stage.position,i,false,stage,1);person.rotation=Quaternion.Euler(0,-8,0);
                foreach(var t in person.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                portraits[i]=new RenderTexture(384,448,24,RenderTextureFormat.ARGB32);portraits[i].Create();camera.targetTexture=portraits[i];camera.Render();person.gameObject.SetActive(false);
            }
            camera.targetTexture=null;
        }
        List<Vector3> FindPath(Vector3 from,Vector3 to){return FloorPath(from,to);}
        public bool WalkTo(Vector3 destination,Action onArrival=null)
        {
            if(Climbing)return false;
            var route=FindPath(actor.position,destination);if(route==null)return false;
            path.Clear();foreach(var p in route)path.Enqueue(p);arrived=onArrival;
            if(path.Count==0){var callback=arrived;arrived=null;callback?.Invoke();}
            return true;
        }
        public void GroundClick(Vector2 canvas)
        {
            if(IsTravelling)return;
            var p=At(canvas.x,canvas.y);if(FloorClear(p))WalkTo(p);
        }
        void AnimateWalking(Vector3 direction)
        {
            var facing=new Vector3(-direction.x,0,-direction.z);
            if(facing.sqrMagnitude>.001f)actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.LookRotation(facing),Time.deltaTime*12);
            float swing=Mathf.Sin(Time.time*12);legA.localRotation=Quaternion.Euler(swing*17,0,0);legB.localRotation=Quaternion.Euler(-swing*17,0,0);body.localPosition=Vector3.up*Mathf.Abs(swing)*.018f;
        }
        void RestPose(){legA.localRotation=legB.localRotation=Quaternion.identity;body.localPosition=Vector3.zero;}
        void MoveByKeyboard(Vector2 input)
        {
            // Use camera-relative ground axes, so W always moves toward the top of the picture.
            var right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
            var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
            input=Vector2.ClampMagnitude(input,1);
            var delta=(right*input.x+forward*input.y)*(3.5f*Mathf.Min(Time.deltaTime,.1f));
            var start=actor.position;
            // Sweep small steps through the same walkable geometry used by mouse navigation.
            int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.06f));var step=delta/steps;
            for(int i=0;i<steps;i++)
            {
                var next=actor.position+step;
                if(FloorClear(next)){actor.position=next;continue;}
                next=actor.position+new Vector3(step.x,0,0);if(FloorClear(next))actor.position=next;
                next=actor.position+new Vector3(0,0,step.z);if(FloorClear(next))actor.position=next;
            }
            ManualMoving=(actor.position-start).sqrMagnitude>.000001f;
            if(ManualMoving)AnimateWalking(actor.position-start);else RestPose();
        }
        public void Tick(bool paused,Vector2 movement=default(Vector2))
        {
            ManualMoving=false;
            FrameCamera();TickFloor();UpdateHotelCamera();if(actor==null)return;
            if(paused){RestPose();return;}
            if(movement.sqrMagnitude>.001f&&!Climbing)
            {
                path.Clear();arrived=null;CancelFloorTravel();MoveByKeyboard(movement);return;
            }
            if(path.Count==0){RestPose();actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.Euler(0,-20,0),Time.deltaTime*5);return;}
            var target=path.Peek();var direction=target-actor.position;actor.position=Vector3.MoveTowards(actor.position,target,3.5f*Time.deltaTime);
            AnimateWalking(direction);
            if((target-actor.position).sqrMagnitude<.002f)
            {path.Dequeue();if(path.Count==0){RestPose();var callback=arrived;arrived=null;callback?.Invoke();}}
        }
        void OnDestroy(){ReleaseFloor();foreach(var rt in portraits)if(rt!=null){rt.Release();Destroy(rt);}}
    }
}
