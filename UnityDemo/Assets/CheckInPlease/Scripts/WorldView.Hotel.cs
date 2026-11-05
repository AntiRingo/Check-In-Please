using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class WorldView
    {
        Transform buildingRoot;
        readonly Dictionary<int,Transform> storeys=new Dictionary<int,Transform>();
        readonly Dictionary<int,Transform> staircases=new Dictionary<int,Transform>();
        readonly Dictionary<int,List<Rect>> storeyObstacles=new Dictionary<int,List<Rect>>();
        readonly List<Transform> residents=new List<Transform>();
        readonly int[] selectedRooms={0,0,201,301};
        public int CurrentFloor {get;private set;}=1;
        public bool Climbing {get;private set;}
        public bool IsTravelling {get{return travelDestination!=0;}}
        public int TravelDestination {get{return travelDestination;}}
        public int StairTripsCompleted {get;private set;}
        public int VisibleFloorCount
        {
            get{int count=0;foreach(var floor in storeys.Values)if(floor.gameObject.activeInHierarchy)count++;return count;}
        }
        int travelDestination,stairDestination;
        Action travelArrived;
        Vector3 hotelCameraBase;
        public static Vector3 ReceptionApproach {get{return new Vector3(-1.2f,0,3.1f);}}
        public static Vector3 ComputerApproach {get{return new Vector3(6.2f,0,3.2f);}}
        public bool AtComputer {get{return CurrentFloor==1&&!IsTravelling&&Vector3.Distance(PlayerPosition,ComputerApproach)<.85f;}}
        public bool AtReception {get{return CurrentFloor==1&&!IsTravelling&&Vector3.Distance(PlayerPosition,ReceptionApproach)<.85f;}}
        void ReleaseHotel()
        {
            storeys.Clear();staircases.Clear();storeyObstacles.Clear();residents.Clear();
            travelDestination=0;stairDestination=0;travelArrived=null;Climbing=false;
        }
        void BuildHotel(string area,GameModel model)
        {
            floorGame=model;buildingRoot=new GameObject("Three storey hotel").transform;buildingRoot.SetParent(transform,false);
            if(floorFont==null)floorFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimSun","Arial"},80);
            shell=Surface("Cut stone caps","#77766b",0);trim=Surface("Oxidised mouldings","#3e4844",0);
            wood=Surface("Corridor oak","#62503e",1);metal=Surface("Blackened iron","#303b38",3);
            red=Surface("Oxblood paint","#4d302c",0);plaster=Surface("Corridor plaster","#6b7166",0);
            for(int floor=1;floor<=3;floor++)
            {
                roomRoot=new GameObject("Floor "+floor).transform;roomRoot.SetParent(buildingRoot,false);
                roomRoot.localPosition=Vector3.up*HotelLayout.Height(floor);storeys.Add(floor,roomRoot);
                floorObstacles=new List<Rect>();storeyObstacles.Add(floor,floorObstacles);
                if(floor==1)BuildGroundLevel();else BuildGuestLevel(floor);
                BuildLanding(floor);BatchArchitecture();
            }
            for(int lower=1;lower<3;lower++)
            {
                roomRoot=new GameObject("Stairs "+lower+" to "+(lower+1)).transform;roomRoot.SetParent(buildingRoot,false);
                roomRoot.localPosition=Vector3.up*HotelLayout.Height(lower);staircases.Add(lower,roomRoot);
                BuildStaircase();BatchArchitecture();
            }
            int initial=area=="saved"?model.State.keeperFloor:HotelLayout.IsRoom(ParseRoom(area))?HotelLayout.RoomFloor(ParseRoom(area)):1;
            CurrentFloor=initial;SelectedRoom=initial==1?201:(HotelLayout.IsRoom(ParseRoom(area))?ParseRoom(area):selectedRooms[initial]);
            if(initial>1)selectedRooms[initial]=SelectedRoom;
            roomRoot=storeys[initial];floorObstacles=storeyObstacles[initial];
            Vector3 start=area=="saved"?new Vector3(model.State.keeperX,HotelLayout.Height(initial),model.State.keeperZ):
                initial==1?(area=="office"?ComputerApproach:new Vector3(-1.2f,0,-1.8f)):DoorApproach(SelectedRoom);
            if(!FloorClear(start))start=initial==1?new Vector3(-1.2f,0,-1.8f):StairLanding(initial);
            actor=Person(start,0,true,buildingRoot,.85f);actor.rotation=Quaternion.Euler(0,-20,0);
            view.orthographicSize=10.35f;view.transform.position=new Vector3(-14,22,-28);
            view.transform.LookAt(new Vector3(-2,.3f,1.6f));view.transform.position+=view.transform.up*2;
            hotelCameraBase=view.transform.position;view.transform.position+=Vector3.up*HotelLayout.Height(initial);
            CharacterHeightPixels=Mathf.Abs(Project(actor.position+Vector3.up*1.89f*actor.localScale.y).y-Project(actor.position).y);
            RefreshResidents();SetCurrentFloor(initial);TickFloor();ArtReady=true;
        }
        static int ParseRoom(string area){return int.TryParse(area,out int n)?n:0;}
        void BuildGroundLevel()
        {
            var tile=Surface("Lobby stone tiles","#60665d",5);
            var oak=Surface("Reception dark oak","#66513b",1);
            var ivory=Surface("Office papers","#bfb89f",2);
            Box("Ground foundation",new Vector3(0,-.2f,1.65f),new Vector3(19.65f,.4f,10.05f),trim);
            Box("Lobby floor",new Vector3(0,0,1.65f),new Vector3(19.2f,.1f,9.9f),tile);
            Box("Office wood floor",new Vector3(6.4f,.055f,3.3f),new Vector3(6.1f,.02f,6.5f),wood);
            Box("Reception runner",new Vector3(-2,.065f,-1.65f),new Vector3(14.2f,.025f,1.4f),Surface("Reception carpet","#633a32",4));
            Box("Entrance mat",new Vector3(0,.07f,-2.8f),new Vector3(2.3f,.04f,.8f),red);
            Box("Back wall",new Vector3(0,1.5f,6.62f),new Vector3(19.45f,3,.2f),plaster);
            Box("Back lower paint",new Vector3(0,.55f,6.48f),new Vector3(19.3f,1.1f,.07f),trim);
            Box("Back dado rail",new Vector3(0,1.12f,6.42f),new Vector3(19.3f,.07f,.09f),wood);
            Box("Roof cutaway rim",new Vector3(0,3.05f,6.62f),new Vector3(19.6f,.13f,.32f),shell);
            foreach(int side in new[]{-1,1})
            {
                Box("Side cutaway wall",new Vector3(side*9.65f,.66f,3.3f),new Vector3(.2f,1.32f,6.65f),plaster);
                Box("Side coping",new Vector3(side*9.65f,1.37f,3.3f),new Vector3(.28f,.1f,6.78f),shell);
                Box("Rear corner pillar",new Vector3(side*9.65f,1.5f,6.6f),new Vector3(.35f,3.1f,.35f),shell);
            }
            foreach(int side in new[]{-1,1})
                Box("Front cutaway parapet",new Vector3(side*5.25f,.18f,-3.35f),new Vector3(8.3f,.36f,.18f),trim);
            for(int i=-1;i<=1;i+=2)
                Box("Entrance upright",new Vector3(i*1.15f,1.2f,-3.35f),new Vector3(.12f,2.4f,.16f),metal);
            Box("Entrance sign",new Vector3(0,2.4f,-3.35f),new Vector3(2.45f,.35f,.2f),red);
            Sign("旅馆入口",new Vector3(0,2.41f,-3.47f),.075f);
            // The reception counter has a clear approach on the lobby side.
            Box("Reception counter",new Vector3(-3.65f,.57f,4.25f),new Vector3(6.9f,1.14f,1.2f),oak);
            Box("Countertop",new Vector3(-3.65f,1.18f,4.25f),new Vector3(7.1f,.16f,1.35f),wood);
            for(int i=0;i<12;i++)Box("Counter panel",new Vector3(-6.78f+i*.57f,.57f,3.63f),new Vector3(.48f,.87f,.04f),oak);
            Box("Reception number plate",new Vector3(-3.65f,.75f,3.59f),new Vector3(2.15f,.43f,.03f),red);
            Sign("前台登记",new Vector3(-3.65f,.75f,3.56f),.087f);
            Box("Registration book",new Vector3(-1.7f,1.29f,4.06f),new Vector3(.74f,.04f,.65f),ivory);
            for(int i=0;i<4;i++)Box("Pending forms",new Vector3(-4.3f,1.3f+i*.025f,4.1f),new Vector3(.52f,.022f,.65f),ivory);
            MakeDeskLamp(new Vector3(-2.8f,1.28f,4.55f),metal,Surface("Brass desk lamp","#a08c5d",3),0);
            Ball("Reception bell base",new Vector3(-.75f,1.3f,3.95f),new Vector3(.29f,.09f,.29f),metal);
            Ball("Reception bell",new Vector3(-.75f,1.37f,3.95f),new Vector3(.2f,.12f,.2f),Surface("Bell brass","#a08c5d",3));
            Box("Key board",new Vector3(-3.2f,1.94f,6.43f),new Vector3(3.8f,1.35f,.13f),oak);
            foreach(int room in HotelLayout.Rooms)
            {
                int index=Array.IndexOf(HotelLayout.Rooms,room);float x=-4.6f+(index%3)*1.4f,y=2.3f-(index/3)*.61f;
                Box("Key tag",new Vector3(x,y,6.31f),new Vector3(.72f,.28f,.06f),red);
                Sign(room.ToString(),new Vector3(x,y,6.27f),.055f);
                Rod("Key hook",new Vector3(x,y-.19f,6.3f),new Vector3(x,y-.19f,6.13f),.035f,metal);
                Ball("Key ring",new Vector3(x,y-.24f,6.12f),new Vector3(.09f,.12f,.025f),ivory);
            }
            floorObstacles.Add(new Rect(-7.3f,3.45f,7.4f,1.65f));
            floorObstacles.Add(new Rect(-5.3f,6.0f,4.2f,.4f));
            // Waiting corner and small table, separate from the route to the stairs.
            var upholstery=Surface("Waiting seat upholstery","#593f34",2);
            Box("Waiting bench",new Vector3(-6.8f,.4f,.8f),new Vector3(2.6f,.45f,.95f),upholstery);
            Box("Bench back",new Vector3(-6.8f,.85f,1.23f),new Vector3(2.65f,.9f,.15f),wood);
            foreach(int side in new[]{-1,1})Box("Bench arm",new Vector3(-6.8f+side*1.32f,.7f,.8f),new Vector3(.15f,.6f,1.05f),wood);
            Box("Waiting table",new Vector3(-6.8f,.6f,-.5f),new Vector3(1.3f,.1f,.65f),oak);
            foreach(int side in new[]{-1,1})Box("Table leg",new Vector3(-6.8f+side*.5f,.3f,-.5f),new Vector3(.1f,.6f,.5f),oak);
            Box("Newspaper",new Vector3(-6.8f,.66f,-.5f),new Vector3(.6f,.015f,.4f),ivory);
            floorObstacles.Add(new Rect(-8.3f,.1f,3.0f,1.5f));floorObstacles.Add(new Rect(-7.65f,-.98f,1.7f,.95f));
            // Office with a permanent doorway: the computer is reached from the corridor.
            Box("Office dividing wall",new Vector3(3.15f,.66f,3.3f),new Vector3(.22f,1.32f,6.6f),plaster);
            Box("Office dividing coping",new Vector3(3.15f,1.37f,3.3f),new Vector3(.28f,.1f,6.7f),shell);
            Box("Office rear post",new Vector3(3.15f,1.5f,6.6f),new Vector3(.3f,3.1f,.3f),shell);
            foreach(var panelX in new[]{4.2f,8.4f})
            {
                Box("Office front wall",new Vector3(panelX,.65f,0),new Vector3(2.1f,1.3f,.2f),plaster);
                Box("Office dado",new Vector3(panelX,.35f,-.13f),new Vector3(2.1f,.63f,.04f),red);
                Box("Office front cap",new Vector3(panelX,1.35f,0),new Vector3(2.2f,.1f,.27f),shell);
            }
            for(int side=-1;side<=1;side+=2)Box("Office doorway jamb",new Vector3(6.3f+side*1.0f,1.1f,0),new Vector3(.13f,2.2f,.2f),wood);
            Box("Office door lintel",new Vector3(6.3f,2.25f,0),new Vector3(2.2f,.22f,.23f),wood);
            Box("Office name plate",new Vector3(4.2f,.9f,-.16f),new Vector3(1.6f,.4f,.06f),red);
            Sign("值班办公室",new Vector3(4.2f,.9f,-.21f),.066f);
            Box("Office desk",new Vector3(6.2f,1.05f,4.95f),new Vector3(2.6f,.16f,1.35f),oak);
            foreach(int side in new[]{-1,1})Box("Office desk pedestal",new Vector3(6.2f+side*.98f,.5f,4.95f),new Vector3(.46f,1,1.17f),wood);
            Box("Office chair",new Vector3(6.2f,.53f,3.95f),new Vector3(.72f,.15f,.72f),upholstery);
            Box("Office chair back",new Vector3(6.2f,.95f,3.64f),new Vector3(.75f,.8f,.13f),wood);
            Rod("Office chair base",new Vector3(6.2f,.05f,3.95f),new Vector3(6.2f,.5f,3.95f),.13f,metal);
            floorObstacles.Add(new Rect(4.65f,4.12f,3.1f,1.65f));floorObstacles.Add(new Rect(5.65f,3.52f,1.1f,.96f));
            MakeComputer(new Vector3(6.1f,1.15f,5.05f));
            Box("Computer filing tray",new Vector3(7.18f,1.2f,4.8f),new Vector3(.48f,.1f,.67f),metal);
            Box("Computer report sheets",new Vector3(7.18f,1.26f,4.8f),new Vector3(.42f,.015f,.61f),ivory);
            for(int cabinet=0;cabinet<3;cabinet++)
            {
                float x=4.4f+cabinet*1.6f;Box("Office file cabinet",new Vector3(x,.85f,6.15f),new Vector3(1.2f,1.7f,.56f),trim);
                for(int drawer=0;drawer<4;drawer++)
                {
                    float y=.23f+drawer*.4f;Box("File drawer",new Vector3(x,y,5.85f),new Vector3(1.05f,.34f,.05f),plaster);
                    Box("File drawer label",new Vector3(x,y+.05f,5.81f),new Vector3(.3f,.08f,.02f),ivory);
                }
            }
            floorObstacles.Add(new Rect(3.6f,5.72f,5.3f,.65f));
            Box("Office wall notice",new Vector3(6.2f,2.35f,6.47f),new Vector3(2.5f,.57f,.05f),red);
            Sign("住户信息管理",new Vector3(6.2f,2.35f,6.43f),.09f);
            // Tall frosted windows and plumbing give the ground floor the same material vocabulary.
            for(int w=0;w<2;w++)
            {
                float x=-8.3f+w*1.8f;Box("Lobby window",new Vector3(x,2.0f,6.45f),new Vector3(1.4f,1.8f,.08f),wood);
                Box("Frosted lobby glass",new Vector3(x,2.0f,6.39f),new Vector3(1.22f,1.62f,.04f),Surface("Frosted lobby glass","#7c9188",3));
                Box("Window vertical bar",new Vector3(x,2.0f,6.35f),new Vector3(.065f,1.7f,.07f),wood);
                Box("Window horizontal bar",new Vector3(x,2.0f,6.35f),new Vector3(1.3f,.065f,.07f),wood);
                for(int rib=0;rib<9;rib++)Rod("Lobby radiator",new Vector3(x-.52f+rib*.13f,.25f,6.2f),new Vector3(x-.52f+rib*.13f,.98f,6.2f),.1f,plaster);
            }
            MakeWallLamp(new Vector3(1.8f,2.2f,6.25f),0);MakeWallLamp(new Vector3(8.7f,1.2f,-.2f),0);
        }
        void MakeComputer(Vector3 p)
        {
            var casing=Surface("Terminal casing","#71776b",0);var screen=Surface("Terminal screen","#274d42",3);
            var phosphor=Surface("Terminal phosphor","#b5d8a6",3);
            Box("CRT stand",p+new Vector3(0,.09f,.1f),new Vector3(.62f,.17f,.45f),metal);
            Box("CRT computer",p+new Vector3(0,.55f,.1f),new Vector3(1.02f,.79f,.72f),casing);
            Box("CRT bezel",p+new Vector3(0,.55f,-.275f),new Vector3(.88f,.65f,.05f),metal);
            Box("Computer display",p+new Vector3(0,.56f,-.308f),new Vector3(.75f,.51f,.023f),screen);
            for(int row=0;row<6;row++)Box("Computer text line",p+new Vector3(-.07f,.73f-row*.06f,-.323f),new Vector3(row==0?.55f:.36f+row*.025f,.013f,.008f),phosphor);
            Box("Keyboard",p+new Vector3(0,.045f,-.62f),new Vector3(.95f,.09f,.35f),casing);
            for(int row=0;row<3;row++)for(int col=0;col<11;col++)
                Box("Keyboard key",p+new Vector3(-.4f+col*.08f,.099f,-.72f+row*.09f),new Vector3(.062f,.02f,.062f),metal);
            Ball("Computer mouse",p+new Vector3(.7f,.06f,-.59f),new Vector3(.16f,.11f,.24f),casing);
        }
        bool GroundClear(Vector3 point)
        {
            float x=point.x,z=point.z;
            if(Mathf.Abs(x-3.15f)<.25f&&z>-.15f)return false;
            if(x>3.15f&&Mathf.Abs(z)<.3f&&(x<5.48f||x>7.05f))return false;
            foreach(var obstacle in floorObstacles)if(obstacle.Contains(new Vector2(x,z)))return false;
            return true;
        }
        void BuildLanding(int floor)
        {
            Box("Stair lobby floor",new Vector3(-11.8f,-.08f,-1.7f),new Vector3(4.4f,.24f,3.3f),Surface("Stair stone","#656c61",5));
            Box("Stair west parapet",new Vector3(-14.05f,.42f,-1.7f),new Vector3(.17f,.84f,3.4f),plaster);
            Box("Stair front parapet",new Vector3(-11.8f,.3f,-3.35f),new Vector3(4.55f,.6f,.18f),trim);
            Box("Floor sign backing",new Vector3(-11.8f,.58f,-3.46f),new Vector3(1.8f,.5f,.05f),red);
            Sign(floor+" F",new Vector3(-11.8f,.58f,-3.5f),.095f);
        }
        void BuildStaircase()
        {
            const int steps=12;float rise=HotelLayout.StoreyHeight/2/steps;
            var tread=Surface("Stair oak treads","#6b5a44",1);
            for(int i=0;i<steps;i++)
            {
                float height=(i+1)*rise;
                Box("Ascending tread",new Vector3(-11,height*.5f,-.7f+(i+.5f)*4/steps),new Vector3(1.42f,height,4f/steps+.015f),tread);
                Box("Returning tread",new Vector3(-13,2.2f+height*.5f,3.3f-(i+.5f)*4/steps),new Vector3(1.42f,height,4f/steps+.015f),tread);
                if(i%3==0)foreach(int side in new[]{-1,1})
                {
                    float za=-.7f+(i+1)*4f/steps,zb=3.3f-(i+1)*4f/steps;
                    Rod("First flight baluster",new Vector3(-11+side*.72f,height,za),new Vector3(-11+side*.72f,height+.85f,za),.035f,metal);
                    Rod("Return flight baluster",new Vector3(-13+side*.72f,2.2f+height,zb),new Vector3(-13+side*.72f,3.05f+height,zb),.035f,metal);
                }
            }
            Box("Half landing",new Vector3(-12,2.08f,3.8f),new Vector3(3.5f,.24f,1.05f),tread);
            foreach(int side in new[]{-1,1})
            {
                Rod("First flight handrail",new Vector3(-11+side*.72f,.85f,-.7f),new Vector3(-11+side*.72f,3.05f,3.3f),.065f,wood);
                Rod("Return flight handrail",new Vector3(-13+side*.72f,3.05f,3.3f),new Vector3(-13+side*.72f,5.25f,-.7f),.065f,wood);
            }
            Rod("Landing handrail",new Vector3(-13.72f,3.05f,4.25f),new Vector3(-10.28f,3.05f,4.25f),.065f,wood);
            for(int i=0;i<5;i++)Rod("Landing baluster",new Vector3(-13.7f+i*.85f,2.2f,4.25f),new Vector3(-13.7f+i*.85f,3.05f,4.25f),.035f,metal);
        }
        Vector3 StairLanding(int floor){return new Vector3(-11.2f,HotelLayout.Height(floor),-1.8f);}
        void AddStairSpots()
        {
            float y=HotelLayout.Height(CurrentFloor);
            if(CurrentFloor<3)spots.Add(new Hotspot("楼梯 · 上"+(CurrentFloor+1)+"楼","stairs"+(CurrentFloor+1),new Vector3(-10.8f,y+1.6f,.45f),StairLanding(CurrentFloor)));
            if(CurrentFloor>1)spots.Add(new Hotspot("楼梯 · 下"+(CurrentFloor-1)+"楼","stairs"+(CurrentFloor-1),new Vector3(-13.1f,y+.55f,-1.7f),StairLanding(CurrentFloor)));
        }
        void SetCurrentFloor(int floor)
        {
            CurrentFloor=floor;roomRoot=storeys[floor];floorObstacles=storeyObstacles[floor];
            if(floor>1)SelectedRoom=selectedRooms[floor];Area=floor==1?"lobby":SelectedRoom.ToString();
            RebuildFloorSpots();DisplayFloors();UpdateHotelCamera();
        }
        void DisplayFloors()
        {
            foreach(var pair in storeys)pair.Value.gameObject.SetActive(pair.Key==CurrentFloor);
            foreach(var pair in staircases)pair.Value.gameObject.SetActive(pair.Key==CurrentFloor||pair.Key==CurrentFloor-1||(Climbing&&pair.Key==Math.Min(CurrentFloor,stairDestination)));
        }
        public bool NavigateToFloor(int destination,Action done=null)
        {
            if(destination<1||destination>3||IsTravelling)return false;
            Debug.Log("HOTEL_TRAVEL "+CurrentFloor+" -> "+destination);
            travelDestination=destination;travelArrived=done;ContinueFloorTravel();return true;
        }
        void CancelFloorTravel()
        {
            if(!IsTravelling||Climbing)return;
            travelDestination=0;travelArrived=null;DisplayFloors();
        }
        void ContinueFloorTravel()
        {
            if(CurrentFloor==travelDestination)
            {
                travelDestination=0;var callback=travelArrived;travelArrived=null;DisplayFloors();callback?.Invoke();return;
            }
            if(!WalkTo(StairLanding(CurrentFloor),BeginStairFlight))
            {travelDestination=0;travelArrived=null;Debug.LogError("No path to the stair landing on floor "+CurrentFloor);}
        }
        void BeginStairFlight()
        {
            stairDestination=HotelLayout.NextFloor(CurrentFloor,travelDestination);int lower=Math.Min(CurrentFloor,stairDestination);
            float y=HotelLayout.Height(lower);var waypoints=new List<Vector3>{new Vector3(-11.2f,y,-1.8f),new Vector3(-11,y,-.7f)};
            for(int i=1;i<=12;i++)waypoints.Add(new Vector3(-11,y+2.2f*i/12,-.7f+4f*i/12));
            waypoints.Add(new Vector3(-11,y+2.2f,3.8f));waypoints.Add(new Vector3(-13,y+2.2f,3.8f));waypoints.Add(new Vector3(-13,y+2.2f,3.3f));
            for(int i=1;i<=12;i++)waypoints.Add(new Vector3(-13,y+2.2f+2.2f*i/12,3.3f-4f*i/12));
            waypoints.Add(new Vector3(-13,y+4.4f,-1.8f));waypoints.Add(new Vector3(-11.2f,y+4.4f,-1.8f));
            if(stairDestination<CurrentFloor)waypoints.Reverse();
            Climbing=true;DisplayFloors();path.Clear();foreach(var p in waypoints)path.Enqueue(p);
            arrived=()=>{Climbing=false;StairTripsCompleted++;SetCurrentFloor(stairDestination);ContinueFloorTravel();};
        }
        void UpdateHotelCamera()
        {
            if(buildingRoot==null||actor==null)return;
            // Keep the outgoing floor framed until the landing, then cut to the new floor.
            view.transform.position=hotelCameraBase+Vector3.up*HotelLayout.Height(CurrentFloor);
            if(CurrentFloor==1)Area=actor.position.x>3.15f&&actor.position.z>0?"office":"lobby";
        }
        public void StoreLocation(GameState state)
        {
            if(actor==null||IsTravelling)return;
            state.keeperFloor=CurrentFloor;state.keeperX=actor.position.x;state.keeperZ=actor.position.z;
        }
        public void RefreshResidents()
        {
            foreach(var resident in residents)if(resident!=null){resident.gameObject.SetActive(false);Destroy(resident.gameObject);}residents.Clear();
            if(floorGame.State.phase!=Shift.Reception)return;
            if(floorGame.Current!=null)residents.Add(Person(new Vector3(-3.3f,0,2.45f),floorGame.Current.portrait,false,storeys[1],.85f));
            foreach(int room in HotelLayout.Rooms)
            {
                var guest=floorGame.InRoom(room);if(guest==null)continue;
                int floor=HotelLayout.RoomFloor(room);
                var resident=Person(new Vector3(RoomSight.Center(room)+.15f,HotelLayout.Height(floor)+.05f,3.6f),guest.portrait,false,storeys[floor],.85f);residents.Add(resident);
                foreach(var renderer in resident.GetComponentsInChildren<Renderer>())
                {
                    if(renderer.sharedMaterial==shadowMat){renderer.enabled=false;continue;}
                    var old=renderer.sharedMaterial;renderer.sharedMaterial=Surface("Guest "+old.name,"#"+ColorUtility.ToHtmlStringRGB(old.color),2,room);
                }
            }
            TickFloor();
        }
    }
}
