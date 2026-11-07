using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheckInPlease
{
    public sealed partial class WorldView
    {
        public bool IsHotelFloor { get { return CurrentFloor > 1; } }
        public int SelectedRoom { get; private set; } = 201;
        GameModel floorGame;
        readonly List<Material> floorMaterials = new List<Material>();
        readonly List<Mesh> floorMeshes = new List<Mesh>();
        List<Rect> floorObstacles = new List<Rect>();
        readonly Dictionary<int,List<Material>> observedMaterials = new Dictionary<int,List<Material>>();
        readonly Dictionary<int,Transform> doorPivots = new Dictionary<int,Transform>();
        readonly Dictionary<int,Transform> cameraPivots = new Dictionary<int,Transform>();
        readonly Dictionary<int,Transform> cameraBodies = new Dictionary<int,Transform>();
        readonly Dictionary<string,Material> materialCache = new Dictionary<string,Material>();
        const float FloorStep=.28f;
        const int FloorColumns=126,FloorRows=54;
        Font floorFont;
        Material signMaterial;
        Material shell,trim,wood,metal,red,plaster;
        public RoomObservation SelectedObservation { get { return floorGame?.Observation(SelectedRoom); } }
        public string MountLabel { get { return new[]{"卧室后角","书房后角","起居室前角"}[SelectedObservation.mount]; } }
        void ReleaseFloor()
        {
            foreach(var m in floorMaterials)if(m!=null)Destroy(m);
            foreach(var mesh in floorMeshes)if(mesh!=null)Destroy(mesh);
            floorMaterials.Clear();floorMeshes.Clear();floorObstacles.Clear();observedMaterials.Clear();
            doorPivots.Clear();cameraPivots.Clear();cameraBodies.Clear();materialCache.Clear();signMaterial=null;floorGame=null;ReleaseHotel();
        }
        Material Surface(string name,string hex,int pattern,int room=0)
        {
            string key=name+"/"+room;if(materialCache.TryGetValue(key,out var existing))return existing;
            var m=new Material(Resources.Load<Shader>("HotelRoom"));m.name=key;m.color=C(hex);
            m.SetFloat("_Pattern",pattern);m.SetFloat("_RoomCenter",room==0?-100:RoomSight.Center(room));
            m.SetFloat("_Floor",room==0?0:HotelLayout.RoomFloor(room));
            m.SetVector("_RoomSize",new Vector4(RoomSight.RoomWidth*.5f,RoomSight.Depth,0,0));
            var partitions=new Vector4[SuiteLayout.Walls.Length];
            for(int i=0;i<partitions.Length;i++){var w=SuiteLayout.Walls[i];partitions[i]=new Vector4(w.x1,w.z1,w.x2,w.z2);}
            m.SetVectorArray("_Partitions",partitions);
            m.SetTexture("_SurfaceAtlas",Resources.Load<Texture2D>("Art/RoomSurfaces"));
            floorMaterials.Add(m);materialCache.Add(key,m);
            if(room!=0){if(!observedMaterials.ContainsKey(room))observedMaterials[room]=new List<Material>();observedMaterials[room].Add(m);}
            return m;
        }
        GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {return Primitive(name,PrimitiveType.Cube,p,size,mat,parent??roomRoot);}
        GameObject Ball(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {return Primitive(name,PrimitiveType.Sphere,p,size,mat,parent??roomRoot);}
        void Rod(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent=null)
        {
            Vector3 d=b-a;var rod=Primitive(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius,d.magnitude*.5f,radius),mat,parent??roomRoot);
            rod.transform.localRotation=Quaternion.FromToRotation(Vector3.up,d);
        }
        void Ring(Vector3 p,float radius,float tube,Material mat,Transform parent=null)
        {
            const int segments=16;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Rod("Iron hoop",p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),p+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius),tube,mat,parent);
            }
        }
        void Sign(string text,Vector3 p,float size,Transform parent=null)
        {
            var o=new GameObject("Room plaque "+text);o.transform.SetParent(parent??roomRoot,false);o.transform.localPosition=p;
            o.transform.localRotation=Quaternion.identity;
            var mesh=o.AddComponent<TextMesh>();mesh.text=text;mesh.font=floorFont;mesh.fontSize=80;
            mesh.characterSize=size*.7f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=C("#d1c4a0");
            if(signMaterial==null){signMaterial=new Material(Resources.Load<Shader>("HotelText"));floorMaterials.Add(signMaterial);}
            signMaterial.mainTexture=floorFont.material.mainTexture;o.GetComponent<Renderer>().sharedMaterial=signMaterial;
        }
        void BuildGuestLevel(int floor)
        {
            Box("Building foundation",new Vector3(0,-.2f,3.2f),new Vector3(30.95f,.4f,15.0f),trim);
            Box("Corridor tiles",new Vector3(0,-.025f,-2.1f),new Vector3(30.6f,.08f,4.2f),Surface("Slate tiles","#565c56",5));
            Box("Corridor runner",new Vector3(0,.028f,-2.1f),new Vector3(30.1f,.025f,1.6f),Surface("Worn corridor runner","#633a32",4));
            Box("Near cutaway parapet",new Vector3(0,.18f,-4.25f),new Vector3(30.9f,.36f,.18f),trim);
            foreach(int n in HotelLayout.RoomsOn(floor))MakeRoom(n);
            Box("Corridor east end",new Vector3(15.35f,.38f,-2.1f),new Vector3(.22f,.76f,4.3f),plaster);
            Box("Corridor end coping",new Vector3(15.35f,.78f,-2.1f),new Vector3(.27f,.08f,4.35f),shell);
        }
        void MakeRoom(int room)
        {
            float cx=RoomSight.Center(room);
            var floor=Surface("Guest oak floor","#77644e",1,room);
            var wall=Surface("Peeling grey green plaster","#727b6b",0,room);
            var darkWall=Surface("Lower painted wall","#424d47",0,room);
            var oak=Surface("Worn furniture oak","#6d5037",1,room);
            var blackIron=Surface("Room iron","#353e37",3,room);
            var linen=Surface("Linen folds","#ada895",2,room);
            var blanket=Surface("Red wool blanket","#6a4138",2,room);
            var ivory=Surface("Paper and porcelain","#c6baa0",2,room);
            var brass=Surface("Tarnished brass","#a08c5d",3,room);
            Box("Room floor "+room,new Vector3(cx,0,5.3f),new Vector3(9.98f,.1f,10.6f),floor);
            Box("Back wall interior",new Vector3(cx,1.5f,10.62f),new Vector3(10.1f,3,.2f),wall);
            Box("Back wall dado",new Vector3(cx,.55f,10.48f),new Vector3(10.0f,1.1f,.06f),darkWall);
            Box("Back wall rail",new Vector3(cx,1.12f,10.43f),new Vector3(10.05f,.07f,.09f),oak);
            Box("Back wall cornice",new Vector3(cx,2.85f,10.43f),new Vector3(10.05f,.12f,.12f),oak);
            Box("Back coping",new Vector3(cx,3.05f,10.62f),new Vector3(10.35f,.12f,.32f),shell);
            for(int side=-1;side<=1;side+=2)
            {
                float x=cx+side*5.02f;
                // Low partitions are deliberate cutaways; the full-height back wall keeps the silhouette.
                Box("Partition inner face",new Vector3(x,.72f,5.3f),new Vector3(.18f,1.44f,10.6f),wall);
                Box("Partition coping",new Vector3(x,1.48f,5.3f),new Vector3(.25f,.1f,10.75f),shell);
                Box("Rear stone post",new Vector3(x,1.52f,10.6f),new Vector3(.32f,3.05f,.32f),shell);
                float frontX=cx+side*2.94f;
                Box("Corridor front wall",new Vector3(frontX,.62f,0),new Vector3(4.36f,1.24f,.22f),plaster);
                Box("Corridor red dado",new Vector3(frontX,.34f,-.14f),new Vector3(4.36f,.63f,.04f),red);
                Box("Front cutaway cap",new Vector3(frontX,1.28f,0),new Vector3(4.44f,.1f,.29f),shell);
                Box("Door jamb",new Vector3(cx+side*.82f,1.1f,-.02f),new Vector3(.15f,2.2f,.2f),wood);
            }
            Box("Door lintel",new Vector3(cx,2.22f,-.02f),new Vector3(1.8f,.13f,.25f),wood);
            Box("Door threshold",new Vector3(cx,.055f,0),new Vector3(1.53f,.09f,.4f),wood);
            var pivot=new GameObject("Hinged door "+room).transform;pivot.SetParent(roomRoot,false);pivot.localPosition=new Vector3(cx-.76f,0,0);
            Box("Door leaf",new Vector3(.76f,1.05f,0),new Vector3(1.5f,2.1f,.1f),wood,pivot);
            for(int panelIndex=0;panelIndex<2;panelIndex++)
            {
                float y=.55f+panelIndex*.95f;
                Box("Inset door panel",new Vector3(.76f,y,-.065f),new Vector3(1.2f,.7f,.035f),trim,pivot);
                Box("Panel inner oak",new Vector3(.76f,y,-.09f),new Vector3(1.09f,.59f,.035f),wood,pivot);
            }
            Ball("Door handle",new Vector3(1.29f,1.02f,-.14f),Vector3.one*.1f,Surface("Exterior brass","#a49161",3),pivot);
            doorPivots.Add(room,pivot);pivot.localRotation=Quaternion.Euler(0,modelDoor(room)?-95:0,0);
            Box("Number plaque",new Vector3(cx-1.62f,.91f,-.17f),new Vector3(.95f,.47f,.07f),red);
            Sign(room.ToString(),new Vector3(cx-1.62f,.91f,-.213f),.095f);
            MakeWallLamp(new Vector3(cx+1.9f,1.22f,-.22f),0);

            // Bed: four legs, mattress, folded pillow, quilt seams and tubular iron frame.
            float bx=cx-3.2f,bz=8.6f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                Rod("Bed post",new Vector3(bx+sx*.82f,.09f,bz+sz*1.4f),new Vector3(bx+sx*.82f,sz>0?1.22f:.92f,bz+sz*1.4f),.065f,blackIron);
            Box("Bed frame",new Vector3(bx,.39f,bz),new Vector3(1.75f,.16f,2.95f),blackIron);
            Box("Mattress",new Vector3(bx,.56f,bz),new Vector3(1.68f,.22f,2.85f),linen);
            Ball("Pillow",new Vector3(bx,.73f,bz+.98f),new Vector3(1.15f,.23f,.56f),linen);
            Box("Wool cover",new Vector3(bx,.69f,bz-.48f),new Vector3(1.7f,.065f,1.8f),blanket);
            Box("Folded quilt edge",new Vector3(bx,.73f,bz+.37f),new Vector3(1.7f,.09f,.15f),blanket);
            for(int lineIndex=0;lineIndex<5;lineIndex++)Box("Quilt stitch",new Vector3(bx-.65f+lineIndex*.325f,.728f,bz-.48f),new Vector3(.014f,.012f,1.6f),oak);
            for(int end=-1;end<=1;end+=2)
            {
                float top=end>0?1.16f:.87f,z=bz+end*1.4f;
                Rod("Bed horizontal rail",new Vector3(bx-.82f,top,z),new Vector3(bx+.82f,top,z),.06f,blackIron);
                for(int bar=0;bar<7;bar++)Rod("Bed vertical rail",new Vector3(bx-.68f+bar*.226f,.61f,z),new Vector3(bx-.68f+bar*.226f,top,z),.027f,blackIron);
            }
            floorObstacles.Add(new Rect(bx-.99f,bz-1.65f,1.98f,3.3f));
            // Desk and chair leave the centre aisle walkable.
            float dx=cx+3.25f,dz=6.3f;
            Box("Writing desk top",new Vector3(dx,1.04f,dz),new Vector3(2.1f,.15f,1.15f),oak);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box("Desk leg",new Vector3(dx+sx*.88f,.5f,dz+sz*.42f),new Vector3(.1f,1,.1f),oak);
            Box("Desk drawer",new Vector3(dx,.84f,dz-.47f),new Vector3(1.7f,.26f,.17f),oak);
            Rod("Drawer pull",new Vector3(dx-.16f,.85f,dz-.59f),new Vector3(dx+.16f,.85f,dz-.59f),.045f,brass);
            for(int sheet=0;sheet<3;sheet++)
            {
                var page=Box("Loose paper",new Vector3(dx-.15f+sheet*.09f,1.126f+sheet*.007f,dz-.15f),new Vector3(.44f,.008f,.56f),ivory);
                page.transform.localRotation=Quaternion.Euler(0,sheet*11-8,0);
            }
            for(int book=0;book<3;book++)Box("Ledger",new Vector3(dx+.66f,1.16f+book*.065f,dz+.23f),new Vector3(.46f,.06f,.54f),book==1?blanket:oak);
            MakeDeskLamp(new Vector3(dx-.65f,1.14f,dz+.24f),blackIron,brass,room);
            Box("Chair seat",new Vector3(dx,.54f,dz-1.13f),new Vector3(.64f,.12f,.64f),oak);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box("Chair leg",new Vector3(dx+sx*.25f,.27f,dz-1.13f+sz*.25f),new Vector3(.07f,.54f,.07f),oak);
            for(int bar=0;bar<4;bar++)Rod("Chair spindle",new Vector3(dx-.25f+bar*.167f,.56f,dz-1.43f),new Vector3(dx-.25f+bar*.167f,1.13f,dz-1.43f),.037f,oak);
            Box("Chair back",new Vector3(dx,1.15f,dz-1.43f),new Vector3(.68f,.15f,.09f),oak);
            floorObstacles.Add(new Rect(dx-1.2f,dz-1.65f,2.4f,2.45f));
            Box("Faded room rug",new Vector3(cx-.1f,.061f,3.05f),new Vector3(1.5f,.018f,2.1f),Surface("Faded room rug","#754b3e",4,room));
            // Wardrobe exists as geometry, but deliberately never participates in vision tests.
            float wx=cx+4.35f;
            Box("Wardrobe body",new Vector3(wx,1.02f,1.22f),new Vector3(.95f,2.04f,1.35f),oak);
            Box("Wardrobe cornice",new Vector3(wx,2.08f,1.22f),new Vector3(1.05f,.12f,1.46f),oak);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Wardrobe panel",new Vector3(wx-.49f,1.04f,1.22f+side*.34f),new Vector3(.035f,1.79f,.57f),darkWall);
                Ball("Wardrobe knob",new Vector3(wx-.54f,1.02f,1.22f+side*.12f),Vector3.one*.06f,brass);
            }
            floorObstacles.Add(new Rect(wx-.65f,.4f,1.3f,1.65f));
            Box("Suitcase",new Vector3(cx+.8f,.24f,1.36f),new Vector3(.82f,.42f,.58f),oak);
            for(int side=-1;side<=1;side+=2)Box("Case leather strap",new Vector3(cx+.8f+side*.25f,.458f,1.36f),new Vector3(.07f,.018f,.6f),blackIron);
            Rod("Suitcase handle",new Vector3(cx+.65f,.5f,1.36f),new Vector3(cx+.95f,.5f,1.36f),.06f,blackIron);
            floorObstacles.Add(new Rect(cx+.2f,.9f,1.2f,.95f));
            MakeWindow(room,cx-2.15f,wall,oak,ivory);
            MakeSuitePartitions(room,cx,wall,oak,ivory,blackIron,linen);
            // Camera mount and lens swivel remain visible as installed equipment on the shell.
            var mount=new GameObject("Camera mount "+room).transform;mount.SetParent(roomRoot,false);
            var swivel=new GameObject("Camera swivel "+room).transform;swivel.SetParent(mount,false);
            Box("Security camera housing",new Vector3(0,0,.12f),new Vector3(.3f,.22f,.52f),trim,swivel);
            Box("Camera lens face",new Vector3(0,-.015f,.393f),new Vector3(.23f,.17f,.025f),metal,swivel);
            Ball("Camera glass",new Vector3(0,-.015f,.415f),new Vector3(.11f,.11f,.025f),Surface("Camera lens","#0e2423",3),swivel);
            Ball("Camera status lamp",new Vector3(.08f,-.065f,.41f),Vector3.one*.035f,Surface("Camera LED","#c76441",0),swivel);
            Rod("Camera bracket",new Vector3(0,-.12f,-.15f),new Vector3(0,-.38f,-.15f),.05f,metal,mount);
            cameraPivots.Add(room,mount);cameraBodies.Add(room,swivel);
        }
        bool modelDoor(int room){return floorGame.Observation(room).doorOpen;}
        void MakeWindow(int room,float cx,Material wall,Material oak,Material ivory)
        {
            var glass=Surface("Frosted window","#80938e",0,room);
            Box("Window recess",new Vector3(cx-.65f,2.03f,10.47f),new Vector3(1.65f,1.65f,.04f),oak);
            Box("Window glass",new Vector3(cx-.65f,2.03f,10.43f),new Vector3(1.45f,1.45f,.04f),glass);
            for(int i=-1;i<=1;i++)
                Box("Window muntin",new Vector3(cx-.65f+i*.7f,2.03f,10.37f),new Vector3(.065f,1.55f,.09f),oak);
            for(int i=-1;i<=1;i++)Box("Window crossbar",new Vector3(cx-.65f,2.03f+i*.7f,10.37f),new Vector3(1.55f,.065f,.09f),oak);
            Box("Window sill",new Vector3(cx-.65f,1.24f,10.28f),new Vector3(1.85f,.1f,.35f),ivory);
            var curtains=Surface("Dusty curtain","#704a3f",2,room);
            for(int side=-1;side<=1;side+=2)for(int fold=0;fold<4;fold++)
                Box("Curtain fold",new Vector3(cx-.65f+side*(.89f+fold*.08f),2.02f,10.17f+(fold%2)*.055f),new Vector3(.11f,1.72f,.12f),curtains);
            var radiator=Surface("Old radiator","#969b8c",3,room);
            for(int fin=0;fin<12;fin++)
                Rod("Radiator rib",new Vector3(cx-1.37f+fin*.13f,.24f,10.14f),new Vector3(cx-1.37f+fin*.13f,1.02f,10.14f),.1f,radiator);
            Rod("Radiator feed",new Vector3(cx-1.6f,.35f,10.25f),new Vector3(cx+.2f,.35f,10.25f),.065f,radiator);
            Rod("Back wall conduit",new Vector3(cx+2.75f,.3f,10.34f),new Vector3(cx+2.75f,2.8f,10.34f),.045f,oak);
            Box("Wall notice",new Vector3(cx+1.8f,2.05f,10.46f),new Vector3(.63f,.8f,.04f),ivory);
            for(int i=0;i<6;i++)Box("Notice print",new Vector3(cx+1.8f,2.3f-i*.09f,10.43f),new Vector3(.43f,.018f,.008f),oak);
        }
        void MakeDeskLamp(Vector3 p,Material ironMat,Material brass,int room)
        {
            Ball("Lamp base",p,new Vector3(.37f,.07f,.37f),ironMat);
            Rod("Lamp stem",p,p+new Vector3(0,.55f,0),.045f,brass);
            var shade=Primitive("Desk lamp shade",PrimitiveType.Cylinder,p+new Vector3(0,.57f,0),new Vector3(.36f,.1f,.36f),ironMat,roomRoot);
            Ball("Lamp bulb",p+new Vector3(0,.49f,0),new Vector3(.15f,.06f,.15f),Surface("Warm bulb","#f1d69b",0,room));
        }
        void MakeWallLamp(Vector3 p,int room)
        {
            Ball("Wall lamp globe",p,new Vector3(.26f,.29f,.26f),Surface("Corridor lamp glass","#e0bd79",0));
            Ball("Lamp crown",p+new Vector3(0,.17f,0),new Vector3(.32f,.08f,.32f),metal);
            Rod("Wall lamp bracket",p+new Vector3(0,-.1f,.15f),p+new Vector3(0,-.1f,0),.05f,metal);
        }
        void BatchArchitecture()
        {
            var groups=new Dictionary<Material,List<MeshFilter>>();
            foreach(var mf in roomRoot.GetComponentsInChildren<MeshFilter>())
            {
                // Moving doors, camera parts and guests keep their own meshes.
                if(mf.transform.parent!=roomRoot)continue;
                var renderer=mf.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var material=renderer.sharedMaterial;
                if(!groups.ContainsKey(material))groups[material]=new List<MeshFilter>();groups[material].Add(mf);
            }
            foreach(var pair in groups)
            {
                var instances=new CombineInstance[pair.Value.Count];
                for(int i=0;i<instances.Length;i++)
                {
                    var mf=pair.Value[i];instances[i]=new CombineInstance{mesh=mf.sharedMesh,transform=roomRoot.worldToLocalMatrix*mf.transform.localToWorldMatrix};
                    mf.gameObject.SetActive(false);Destroy(mf.gameObject);
                }
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=pair.Key.name};mesh.CombineMeshes(instances,true,true);floorMeshes.Add(mesh);
                var o=new GameObject("Batched "+pair.Key.name);o.transform.SetParent(roomRoot,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=o.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
        public void SelectRoom(int room)
        {
            if(!HotelLayout.IsRoom(room)||HotelLayout.RoomFloor(room)!=CurrentFloor||IsTravelling)return;
            SelectedRoom=room;selectedRooms[CurrentFloor]=room;Area=room.ToString();RebuildFloorSpots();
        }
        void RebuildFloorSpots()
        {
            spots.Clear();
            if(CurrentFloor==1)
            {
                spots.Add(new Hotspot("前台 · 审查来客","review",new Vector3(-3,1.6f,4.25f),ReceptionApproach));
                spots.Add(new Hotspot("办公室 · 举报电脑","dossier",new Vector3(6.2f,2.15f,4.9f),ComputerApproach));
            }
            else
            {
                float h=HotelLayout.Height(CurrentFloor);
                foreach(int n in HotelLayout.RoomsOn(CurrentFloor))
                {
                    float cx=RoomSight.Center(n);
                    spots.Add(new Hotspot(n+" · 房门","room"+n,new Vector3(cx,h+2.46f,0),DoorApproach(n),n));
                    spots.Add(new Hotspot(n+" · 监控","camera",new Vector3(cx+.8f,h+1.8f,0),DoorApproach(n)+Vector3.right*.5f,n));
                    spots.Add(new Hotspot(n+" · 书桌","search0",new Vector3(cx+3.25f,h+1.2f,6.3f),new Vector3(cx+1.65f,h,5.5f),n));
                    spots.Add(new Hotspot(n+" · 行李","search1",new Vector3(cx+.8f,h+.6f,1.36f),new Vector3(cx-.3f,h,1.3f),n));
                }
            }
        }
        public bool SpotVisible(Hotspot spot)
        {
            if(!IsHotelFloor||!spot.action.StartsWith("search"))return true;
            return floorGame.State.phase==Shift.Search&&floorGame.InRoom(spot.room)!=null&&VisibilityAt(spot.room,spot.point)>.62f;
        }
        Vector3 InteractionApproach(Hotspot spot)
        {
            if(spot.action=="review")return ReceptionInteractionPoint;
            // A door and its monitor control can also be used from inside that room.
            if(spot.room!=0&&(spot.action.StartsWith("room")||spot.action=="camera")&&PlayerPosition.z>.32f&&Mathf.Abs(PlayerPosition.x-RoomSight.Center(spot.room))<RoomSight.RoomWidth*.5f-.2f)
                return new Vector3(RoomSight.Center(spot.room),HotelLayout.Height(CurrentFloor),.7f);
            return spot.approach;
        }
        public float InteractionDistance(Hotspot spot){return Vector3.Distance(PlayerPosition,InteractionApproach(spot));}
        public bool CanInteract(Hotspot spot)
        {
            if(IsTravelling||Climbing||spot==null||!spots.Contains(spot)||!SpotVisible(spot)||InteractionDistance(spot)>InteractionRange)return false;
            // Reject targets across partitions, closed doors and furniture, even when close in projection.
            Vector3 target=InteractionApproach(spot);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(PlayerPosition,target)/.08f));
            for(int i=1;i<=steps;i++)if(!FloorClear(Vector3.Lerp(PlayerPosition,target,i/(float)steps)))return false;
            return true;
        }
        public float VisibilityAt(int room,Vector3 point)
        {
            if(!HotelLayout.IsRoom(room))return 1;
            float cx=RoomSight.Center(room);var pos=PlayerPosition;
            return RoomSight.Visibility(floorGame.Observation(room),point.x-cx,point.z,pos.x-cx,pos.z,Climbing?-1:CurrentFloor);
        }
        public bool CheckHotspotReachability()
        {
            ReachableHotspots=0;if(IsTravelling)return true;bool ok=true;
            foreach(var spot in spots)
            {
                if(!SpotVisible(spot))continue;
                // A camera may reveal a search target behind a closed door; it is intentionally not yet reachable.
                if(IsHotelFloor&&spot.action.StartsWith("search")&&!floorGame.Observation(spot.room).doorOpen&&!(PlayerPosition.z>.32f&&Mathf.Abs(PlayerPosition.x-RoomSight.Center(spot.room))<RoomSight.RoomWidth*.5f-.2f))continue;
                bool reachable=FindPath(PlayerPosition,spot.approach)!=null;if(reachable)ReachableHotspots++;else ok=false;
            }
            return ok;
        }
        public void ChangeCamera(int change)
        {
            var s=SelectedObservation;if(!IsHotelFloor||IsTravelling||s==null)return;
            if(change==0)s.cameraInstalled=!s.cameraInstalled;
            else if(change==1){s.mount=(s.mount+1)%3;s.yaw=s.mount==1?225:s.mount==2?45:135;}
            else if(change==2)s.fieldOfView=s.fieldOfView<50?60:s.fieldOfView<75?90:40;
            else s.yaw+=change;
            s.Normalize();TickFloor();
        }
        public void RefreshDoors(){TickFloor();}
        public Vector3 DoorApproach(int room){return new Vector3(RoomSight.Center(room),HotelLayout.Height(HotelLayout.RoomFloor(room)),-1.25f);}
        void TickFloor()
        {
            if(floorGame==null)return;
            if(signMaterial!=null)signMaterial.mainTexture=floorFont.material.mainTexture;
            var observer=PlayerPosition;
            foreach(int n in HotelLayout.Rooms)
            {
                var s=floorGame.Observation(n);float cx=RoomSight.Center(n);
                RoomSight.Mount(s.mount,out float x,out float z);
                Vector4 cam=new Vector4(cx+x,z,s.yaw*Mathf.Deg2Rad,s.cameraInstalled?1:0);
                Vector4 sight=new Vector4(Mathf.Cos(s.fieldOfView*.5f*Mathf.Deg2Rad),Mathf.Cos((s.fieldOfView*.5f-7)*Mathf.Deg2Rad),RoomSight.CameraRange,s.doorOpen?1:0);
                foreach(var material in observedMaterials[n])
                {
                    material.SetVector("_Camera",cam);material.SetVector("_Sight",sight);material.SetVector("_Observer",new Vector4(observer.x,observer.z,Climbing?-1:CurrentFloor,0));
                }
                var mount=cameraPivots[n];mount.gameObject.SetActive(s.cameraInstalled);mount.localPosition=new Vector3(cx+x,2.45f,z);
                cameraBodies[n].localRotation=Quaternion.Euler(18,s.yaw,0);
                var door=doorPivots[n];door.localRotation=Quaternion.Slerp(door.localRotation,Quaternion.Euler(0,s.doorOpen?-95:0,0),1-Mathf.Exp(-Time.deltaTime*10));
            }
        }
        bool FloorClear(Vector3 point){return FloorClear(point,CurrentFloor);}
        bool FloorClear(Vector3 point,int floor)
        {
            float x=point.x,z=point.z;
            if(Mathf.Abs(point.y-HotelLayout.Height(floor))>.02f)return false;
            if(x>=HotelLayout.West&&x< -HotelLayout.HalfWidth+.18f&&z>=HotelLayout.Front+.18f&&z<=-.7f)return true;
            if(x< -HotelLayout.HalfWidth+.18f||x>HotelLayout.HalfWidth-.18f||z<HotelLayout.Front+.18f||z>RoomSight.Depth-.32f)return false;
            if(floor==1)return GroundClear(point);
            if(z<-.28f)return true;
            int room=floor*100+Mathf.Clamp(Mathf.RoundToInt(x/RoomSight.RoomWidth)+2,1,3);
            float local=x-RoomSight.Center(room);
            if(Mathf.Abs(local)>RoomSight.RoomWidth*.5f-.3f||!SuiteLayout.Clear(local,z))return false;
            if(z<.32f&&(!floorGame.Observation(room).doorOpen||Mathf.Abs(local)>.58f))return false;
            foreach(var obstacle in storeyObstacles[floor])if(obstacle.Contains(new Vector2(x,z)))return false;
            return true;
        }
        Vector3 FloorCell(int index,int floor){return new Vector3(HotelLayout.West+(index%FloorColumns)*FloorStep,HotelLayout.Height(floor),HotelLayout.Front+.18f+(index/FloorColumns)*FloorStep);}
        int FloorNearest(Vector3 point,int floor)
        {
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<FloorColumns*FloorRows;i++)
            {
                var p=FloorCell(i,floor);if(!FloorClear(p,floor))continue;
                float d=(p-point).sqrMagnitude;if(d<distance){best=i;distance=d;}
            }
            return best;
        }
        List<Vector3> FloorPath(Vector3 from,Vector3 to,int floor)
        {
            // Never snap an unreachable target to the other side of a locked wall or to a different room.
            if(!FloorClear(to,floor))return null;
            int start=FloorNearest(from,floor),goal=FloorNearest(to,floor);if(start<0||goal<0)return null;
            var previous=new int[FloorColumns*FloorRows];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&previous[goal]<0)
            {
                int v=queue.Dequeue();foreach(int delta in new[]{-FloorColumns,FloorColumns,-1,1})
                {
                    int next=v+delta;
                    if(next<0||next>=previous.Length||Mathf.Abs(next%FloorColumns-v%FloorColumns)>1||previous[next]>=0||!FloorClear(FloorCell(next,floor),floor))continue;
                    previous[next]=v;queue.Enqueue(next);
                }
            }
            if(previous[goal]<0)return null;
            var route=new List<Vector3>();
            for(int cell=goal;cell!=start;cell=previous[cell])route.Add(FloorCell(cell,floor));route.Reverse();route.Add(to);return route;
        }
    }
}
