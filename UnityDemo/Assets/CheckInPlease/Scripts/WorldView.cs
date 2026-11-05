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
    // Painted environments with real-time characters. Art, UI and walk masks share a 1600x900 canvas.
    public sealed class WorldView : MonoBehaviour
    {
        public readonly List<Hotspot> spots=new List<Hotspot>();
        readonly Queue<Vector3> path=new Queue<Vector3>();
        readonly List<Vector2[]> blocked=new List<Vector2[]>();
        readonly Dictionary<string,Texture2D> backgrounds=new Dictionary<string,Texture2D>();
        readonly RenderTexture[] portraits=new RenderTexture[3];
        Vector2[] walkMask;
        Transform roomRoot,actor,legA,legB,body;
        Camera view;
        Material black,cloth,paper,iron,plateMat,shadowMat;
        Action arrived;
        public string Area {get;private set;}
        public bool Walking {get{return path.Count>0;}}
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
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Painted environment plate";
            Destroy(quad.GetComponent<Collider>());quad.transform.SetParent(view.transform,false);
            quad.transform.localPosition=new Vector3(0,0,70);quad.transform.localScale=new Vector3(22.4f,12.6f,1);
            plateMat=new Material(Resources.Load<Shader>("HotelPlate"));quad.GetComponent<Renderer>().sharedMaterial=plateMat;
            foreach(var key in new[]{"Lobby","GuestRoom","Office"})
            {var tex=Resources.Load<Texture2D>("Art/"+key);if(tex==null)throw new InvalidOperationException("Missing environment: "+key);backgrounds.Add(key,tex);}
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
        public Texture Background {get{return plateMat.mainTexture;}}
        Vector3 At(float x,float y)
        {var ray=view.ViewportPointToRay(new Vector3(x/1600,1-y/900,0));new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float d);return ray.GetPoint(d);}
        public Vector2 Project(Vector3 p)
        {var v=view.WorldToViewportPoint(p);return new Vector2(v.x*1600,(1-v.y)*900);}
        static Vector2[] Poly(params float[] xy)
        {var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
        static bool Inside(Vector2 p,Vector2[] polygon)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {var a=polygon[i];var b=polygon[j];if(((a.y>p.y)!=(b.y>p.y))&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}
            return inside;
        }
        bool Clear(Vector2 p)
        {if(!Inside(p,walkMask))return false;foreach(var b in blocked)if(Inside(p,b))return false;return true;}
        void Spot(string label,string action,float x,float y,float ax,float ay)
        {spots.Add(new Hotspot(label,action,At(x,y),At(ax,ay)));}
        float ScaleForHeight(float pixels)
        {float height=Mathf.Abs(Project(Vector3.up*1.89f).y-Project(Vector3.zero).y);return pixels/height;}
        public void Build(string area,GameModel game)
        {
            if(roomRoot!=null){roomRoot.gameObject.SetActive(false);Destroy(roomRoot.gameObject);}
            roomRoot=new GameObject("Scene: "+area).transform;roomRoot.SetParent(transform);
            Area=area;spots.Clear();path.Clear();blocked.Clear();arrived=null;
            plateMat.mainTexture=backgrounds[area=="lobby"?"Lobby":area=="office"?"Office":"GuestRoom"];
            Vector2 start;
            if(area=="lobby")
            {
                walkMask=Poly(465,495,570,455,1080,555,1160,490,1490,500,1530,570,1370,635,1280,600,1190,712,970,765,930,860,490,860,505,655);
                Spot("前台 · 审查住客","review",930,385,800,600);Spot("办公室","office",610,310,550,500);Spot("客房走廊","rooms",1465,390,1390,555);
                start=new Vector2(700,740);
                if(game.Current!=null&&game.State.phase==Shift.Reception)
                {Person(At(1070,635),game.Current.portrait,false,roomRoot,ScaleForHeight(178));blocked.Add(Poly(1020,612,1120,612,1120,665,1020,665));}
            }
            else if(area=="office")
            {
                walkMask=Poly(320,590,430,530,460,565,1030,625,1100,590,1230,605,1270,555,1480,585,1400,855,390,850,300,790);
                Spot("整理档案 / 举报","dossier",790,443,800,700);Spot("返回大厅","lobby",285,682,395,760);start=new Vector2(600,760);
            }
            else
            {
                walkMask=Poly(390,610,500,585,540,660,780,690,805,650,990,610,1055,650,1000,715,995,825,1160,855,1080,880,425,880);
                Spot("书桌 · 搜查","search0",825,385,635,735);Spot("行李 · 搜查","search1",1110,760,940,805);Spot("返回大厅","lobby",305,690,465,800);
                start=new Vector2(570,800);
            }
            // Each background has its own camera distance. Match the keeper to furniture, not screen percentage globally.
            float desiredHeight=area=="lobby"?160:area=="office"?245:295;
            actor=Person(At(start.x,start.y),0,true,roomRoot,ScaleForHeight(desiredHeight));actor.rotation=Quaternion.Euler(0,-20,0);
            CharacterHeightPixels=Mathf.Abs(Project(actor.position+Vector3.up*1.89f*actor.localScale.y).y-Project(actor.position).y);
            ReachableHotspots=0;foreach(var spot in spots)if(FindPath(actor.position,spot.approach)!=null)ReachableHotspots++;
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
        const int Columns=80,Rows=45;
        Vector2 Cell(int index){return new Vector2(index%Columns*20+10,index/Columns*20+10);}
        int Nearest(Vector2 p)
        {int best=-1;float distance=float.MaxValue;for(int i=0;i<Columns*Rows;i++)if(Clear(Cell(i))){float d=(Cell(i)-p).sqrMagnitude;if(d<distance){best=i;distance=d;}}return best;}
        List<Vector3> FindPath(Vector3 from,Vector3 to)
        {
            int start=Nearest(Project(from)),goal=Nearest(Project(to));if(start<0||goal<0)return null;
            var previous=new int[Columns*Rows];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&previous[goal]<0)
            {
                int v=queue.Dequeue();foreach(int delta in new[]{-Columns,Columns,-1,1})
                {int n=v+delta;if(n<0||n>=previous.Length||Math.Abs(n%Columns-v%Columns)>1||previous[n]>=0||!Clear(Cell(n)))continue;previous[n]=v;queue.Enqueue(n);}
            }
            if(previous[goal]<0)return null;
            var result=new List<Vector3>();for(int n=goal;n!=start;n=previous[n]){var p=Cell(n);result.Add(At(p.x,p.y));}result.Reverse();return result;
        }
        public void WalkTo(Vector3 destination,Action onArrival=null)
        {
            var route=FindPath(actor.position,destination);if(route==null)return;path.Clear();foreach(var p in route)path.Enqueue(p);arrived=onArrival;
            if(path.Count==0){var callback=arrived;arrived=null;callback?.Invoke();}
        }
        public void GroundClick(Vector2 canvas){if(Clear(canvas))WalkTo(At(canvas.x,canvas.y));}
        public void Tick(bool paused)
        {
            FrameCamera();if(actor==null||paused)return;
            if(path.Count==0){actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.Euler(0,-20,0),Time.deltaTime*5);return;}
            var target=path.Peek();var direction=target-actor.position;actor.position=Vector3.MoveTowards(actor.position,target,3.5f*Time.deltaTime);
            if(direction.sqrMagnitude>.001f)actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.LookRotation(-direction),Time.deltaTime*12);
            float swing=Mathf.Sin(Time.time*12);legA.localRotation=Quaternion.Euler(swing*17,0,0);legB.localRotation=Quaternion.Euler(-swing*17,0,0);body.localPosition=Vector3.up*Mathf.Abs(swing)*.018f;
            if((target-actor.position).sqrMagnitude<.002f)
            {path.Dequeue();if(path.Count==0){legA.localRotation=legB.localRotation=Quaternion.identity;body.localPosition=Vector3.zero;var callback=arrived;arrived=null;callback?.Invoke();}}
        }
        void OnDestroy(){foreach(var rt in portraits)if(rt!=null){rt.Release();Destroy(rt);}}
    }
}
