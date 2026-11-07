using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame : MonoBehaviour
    {
        GameModel game;
        WorldView world;
        Font font;
        string modal="menu",toast="",subject="",comparison="";
        float toastUntil;
        int roomChoice=201;
        Clue inspecting;
        bool flipped, muted, confirmNew;
        Vector2 journalScroll;
        readonly HashSet<string> selected=new HashSet<string>();
        AudioSource ambience;
        bool smoke, floorSmoke;
        bool navigationPassed = true;
        bool visualsPassed = true, saveRoundtripPassed = true;
        bool artPassed = true;
        readonly List<string> runtimeErrors = new List<string>();
        string SavePath { get { return Path.Combine(Application.persistentDataPath,"hotel-save-v1.json"); } }
        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            floorSmoke=Environment.GetCommandLineArgs().Contains("-floorSmokeTest")||Environment.GetCommandLineArgs().Contains("-hotelSmokeTest");
            smoke=floorSmoke||Environment.GetCommandLineArgs().Contains("-demoSmokeTest");
            if(smoke)Application.logMessageReceived+=RecordError;
            font=Resources.Load<Font>("HotelFont");
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},24);
            game=new GameModel();world=gameObject.AddComponent<WorldView>();world.Setup(font);world.Build("lobby",game);
            MakeAudio();
            if(floorSmoke)StartCoroutine(FloorSmokeTest());else if(smoke)StartCoroutine(SmokeTest());
        }
        void RecordError(string message,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeErrors.Add(message);if(smoke&&type==LogType.Exception)Application.Quit(1);}
        void OnDestroy(){if(smoke)Application.logMessageReceived-=RecordError;}
        void MakeAudio()
        {
            const int hz=22050;var data=new float[hz*4];var rng=new System.Random(2);
            for(int i=0;i<data.Length;i++)
            { float t=i/(float)hz;data[i]=Mathf.Sin(t*50*2*Mathf.PI)*.08f+Mathf.Sin(t*100*2*Mathf.PI)*.025f+((float)rng.NextDouble()-.5f)*.015f; }
            var clip=AudioClip.Create("Heating pipes and electrical hum",data.Length,1,hz,false);clip.SetData(data,0);
            ambience=gameObject.AddComponent<AudioSource>();ambience.clip=clip;ambience.loop=true;ambience.volume=.18f;ambience.Play();
        }
        void Update()
        {
            if(!smoke&&Input.GetKeyDown(KeyCode.Escape))
            {
                if(modal=="")modal="pause";
                else if(modal=="pause"||modal=="review"||modal=="item"||modal=="journal"||modal=="help"||modal=="dossier"||modal=="rooms"||modal=="camera")LeaveModal();
            }
            if(!smoke&&Input.GetKeyDown(KeyCode.J) && modal=="")modal="journal";
            Vector2 movement=smoke?validationMovement:Application.isFocused?new Vector2(
                (Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)):Vector2.zero;
            bool wasManual=world.ManualMoving;
            world.Tick(modal!="",movement);
            TickDoorOpening(movement);
            if(modal=="")CheckRoomIntrusions(openingRoom);
            RefreshInteractions();
            if(!smoke&&Application.isFocused&&modal==""&&!world.IsTravelling)
            {
                if(Input.GetKeyDown(KeyCode.R))CycleInteraction(1);
                else if(Mathf.Abs(Input.mouseScrollDelta.y)>.01f)CycleInteraction(Input.mouseScrollDelta.y<0?1:-1);
                if(Input.GetKeyDown(KeyCode.E))PerformInteraction();
            }
            if(smoke){CheckFixedView();singleFloorPassed&=world.VisibleFloorCount==(world.InStairwell?0:1);stairHalvesPassed&=world.VisibleStairHalfCount==(world.Climbing?(world.FloorOpacity<.999f?2:1):(world.CurrentFloor==2?2:1));}
            else if(wasManual&&!world.ManualMoving)Save();
        }
        void Say(string message) {toast=message;toastUntil=Time.unscaledTime+6;}
        void Save()
        {
            world.StoreLocation(game.State);
            if(smoke)return;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath+".tmp",JsonUtility.ToJson(game.State,true));
                if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");
                else File.Move(SavePath+".tmp",SavePath);
            }
            catch(Exception e){Debug.LogError(e);Say("存档失败："+e.Message);}
        }
        void Continue()
        {
            try
            {
                var s=JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                if(s==null||s.version!=1||s.guests==null||s.guests.Count!=9||s.day<1||s.day>3)
                    throw new InvalidDataException("存档格式不匹配");
                CancelDoorOpening(false);game=new GameModel(s);world.Build("saved",game);RestoreModal();
            }
            catch(Exception e){Say("无法读取存档："+e.Message);}
        }
        void RestoreModal()
        {
            modal=game.State.phase==Shift.Morning?"brief":game.State.phase==Shift.Finished?"ending":"";
        }
        void LeaveModal() {modal=game.State.phase==Shift.Finished?"ending":game.State.phase==Shift.Morning?"menu":"";}
        void NewGame()
        {CancelDoorOpening(false);game=new GameModel();world.Build("lobby",game);modal="brief";confirmNew=false;selected.Clear();Save();}
        void StartDay()
        {Say(game.StartDay());world.Build("lobby",game);modal=game.State.phase==Shift.Finished?"ending":game.State.news.Count>0?"dispatch":"";Save();}
        void Visit(string area)
        {
            CancelDoorOpening(false);
            if(world.IsTravelling){Say("正在沿楼梯前往目标楼层。");return;}
            int room=int.TryParse(area,out int parsed)&&HotelLayout.IsRoom(parsed)?parsed:0;
            int floor=room==0?1:HotelLayout.RoomFloor(room);modal="";
            world.NavigateToFloor(floor,()=>
            {
                if(room!=0)world.SelectRoom(room);
                Vector3 destination=room!=0?world.DoorApproach(room):area=="office"?WorldView.ComputerApproach:WorldView.ReceptionApproach;
                if(!world.WalkTo(destination,Save))Say("通路受阻，请先打开房门。");
            });
        }
        void Act(string action)
        {
            if(action=="review")
            {
                if(!world.AtReception){Say("请走到一楼前台审查来客。");return;}
                if(game.State.phase!=Shift.Reception){Say("当前不在接待时段。");return;}
                if(game.Current==null){Say("今天的来客已经处理完毕，可以开始夜班。");return;}
                toast="";comparison="";roomChoice=HotelLayout.Rooms.FirstOrDefault(n=>game.InRoom(n)==null);modal="review";
            }
            else if(action=="dossier")
            {
                if(!world.AtComputer){Say("请到一楼办公室的电脑前办理举报。");return;}
                if(game.State.phase==Shift.Search){Say(game.BeginAudit());world.RefreshResidents();Save();}
                if(game.State.phase!=Shift.Audit){Say("档案处置在夜间审计时进行。");return;}
                toast="";selected.Clear();subject=game.State.clues.FirstOrDefault(c=>CanUse(c))?.guestId??"";modal="dossier";
            }
            else if(action.StartsWith("room")&&int.TryParse(action.Substring(4),out int selectedRoom))world.SelectRoom(selectedRoom);
            else if(action.StartsWith("search"))
            {
                if(!int.TryParse(world.Area,out int n))return;
                if(world.IsHotelFloor)
                {
                    var spot=world.spots.FirstOrDefault(s=>s.action==action&&s.room==world.SelectedRoom);
                    if(spot==null||!world.SpotVisible(spot)){Say("当前看不到这件物品。");return;}
                    if(!world.CanInteract(spot)){Say("请先打开房门，走近物品再搜查。");return;}
                }
                inspecting=game.FindClue(n,action=="search0"?0:1,out var error);
                if(inspecting==null){Say(error);return;}
                toast="";flipped=false;modal="item";Save();
            }
            else if(action=="rooms")Visit("201");
            else Visit(action);
        }
        static bool CanUse(Clue c) {return c.disposition==Disposition.Unresolved||c.disposition==Disposition.Withheld;}
        void Decision(bool admit)
        {Say(game.Decide(admit,roomChoice));comparison="";modal="";world.RefreshResidents();Save();}
        void Night()
        {Say(game.BeginNight());modal="";world.RefreshResidents();Save();}
        void FloorCamera(int change)
        {
            if((change==0||change==1)&&game.State.phase!=Shift.Search){Say("宵禁点名后才能安装、拆除或移动摄像头。");return;}
            world.ChangeCamera(change);Save();
        }
        void Audit()
        {
            if(world.IsTravelling)return;
            if(game.State.phase!=Shift.Search&&game.State.phase!=Shift.Audit){Say("夜间搜查后，可在办公室电脑上举报住户。");return;}
            modal="";world.NavigateToFloor(1,()=>world.WalkTo(WorldView.ComputerApproach,()=>{Act("dossier");Save();}));
        }
        void EndDay()
        {Say(game.EndDay());world.Build("lobby",game);Save();RestoreModal();}
        void Resolve(Disposition d)
        {
            if(!world.AtComputer){Say("请在办公室电脑前提交。");return;}
            Say(game.Resolve(subject,d,selected));selected.RemoveWhere(id=>!game.State.clues.Any(c=>c.id==id&&CanUse(c)));Save();
        }
        string validationFrameFolder;
        IEnumerator Capture(string name,float settle=.8f)
        {
            artPassed &= world.ArtReady && world.CheckHotspotReachability();
            artPassed &= world.CharacterHeightPixels>35&&world.CharacterHeightPixels<90;
            Debug.Log("ART_SCALE "+world.Area+" keeperHeight="+world.CharacterHeightPixels.ToString("F1")+"px reachable="+world.ReachableHotspots+"/"+world.spots.Count);
            yield return new WaitForSeconds(settle);yield return new WaitForEndOfFrame();
            // A preview can keep the prior PNG mapped on Windows. Give each validation run
            // its own directory so reruns never overwrite an image currently being reviewed.
            if(validationFrameFolder==null)
            {
                validationFrameFolder=Path.Combine(Application.dataPath,"..","Validation",Application.version,"run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                Directory.CreateDirectory(validationFrameFolder);Debug.Log("VALIDATION_FRAMES "+validationFrameFolder);
            }
            var folder=validationFrameFolder;
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null){visualsPassed=false;yield break;}
            var pixels=frame.GetPixels32();int darkest=255,brightest=0;
            for(int i=0;i<pixels.Length;i+=101)
            {int value=(pixels[i].r+pixels[i].g+pixels[i].b)/3;darkest=Math.Min(darkest,value);brightest=Math.Max(brightest,value);}
            visualsPassed &= brightest>40 && brightest-darkest>30;
            File.WriteAllBytes(Path.Combine(folder,name+".png"),ImageConversion.EncodeToPNG(frame));
            Destroy(frame);yield return new WaitForSeconds(.4f);
        }
        IEnumerator SmokeTest()
        {
            yield return new WaitForSeconds(1);yield return Capture("00-title");NewGame();yield return Capture("01-briefing");
            for(int d=1;d<=3;d++)
            {
                StartDay();modal="";
                yield return WalkDuringTest(WorldView.ReceptionApproach);
                if(d==1)yield return Capture("02-lobby");
                while(game.Current!=null)
                {
                    Act("review");if(d==1&&game.Current.id=="A-017")yield return Capture("03-review");
                    bool admit=game.Violation(game.Current)=="";
                    roomChoice=game.Today.Any(g=>g.decision==CheckInPlease.Decision.Admitted)?301:201;Decision(admit);
                }
                Night();var occupied=game.Today.Where(g=>g.decision==CheckInPlease.Decision.Admitted).ToArray();
                foreach(var g in occupied.Take(2))
                {
                    yield return TravelDuringTest(g.room.ToString());
                    if(d==1&&g.room==301)
                    {
                        yield return UseInteractionDuringTest("camera",301);
                        CheckInteraction(modal=="camera"&&game.Observation(301).cameraInstalled,"nearby camera opens control panel");
                        yield return Capture("interaction-05-camera-controls");modal="";
                    }
                    if(!world.SelectedObservation.doorOpen)yield return UseInteractionDuringTest("room"+g.room,g.room);
                    yield return WalkDuringTest(world.spots.First(s=>s.action=="search0"&&s.room==world.SelectedRoom).approach);
                    if(d==1)yield return Capture("04-room-"+g.room);
                    Act("search0");if(d==1)yield return Capture("05-inspection-"+g.room);modal="";
                    yield return WalkDuringTest(world.spots.First(s=>s.action=="search1"&&s.room==world.SelectedRoom).approach);
                    Act("search1");modal="";
                }
                Audit();yield return WaitForMovement();
                navigationPassed &= world.AtComputer && modal=="dossier";
                if(d==1)
                {
                    modal="";yield return Capture("06-office");modal="dossier";
                    subject=occupied[0].id;selected.Clear();
                    foreach(var c in game.State.clues.Where(c=>c.guestId==subject&&CanUse(c)))selected.Add(c.id);
                    yield return Capture("06-dossier");
                }
                foreach(var g in occupied.Take(2))
                {subject=g.id;selected.Clear();foreach(var c in game.State.clues.Where(c=>c.guestId==g.id&&CanUse(c)))selected.Add(c.id);Resolve(d==1?Disposition.Withheld:Disposition.Reported);}
                EndDay();
                if(d<3)
                {
                    var saved=game.State;
                    var path=Path.Combine(Application.dataPath,"..","Validation","save-roundtrip-day"+d+".json");
                    File.WriteAllText(path,JsonUtility.ToJson(saved,true));
                    var restored=JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                    saveRoundtripPassed &= restored!=null && restored.day==saved.day && restored.phase==saved.phase &&
                        restored.money==saved.money && restored.guests.Count==9 && restored.clues.Count==saved.clues.Count && restored.pending.Count==saved.pending.Count;
                    if(restored!=null)game=new GameModel(restored);
                }
            }
            yield return Capture("07-ending");
            bool ok=game.State.phase==Shift.Finished && game.State.day==3 && game.State.money>=0 && navigationPassed && visualsPassed && saveRoundtripPassed && artPassed && interactionPassed && singleFloorPassed && stairHalvesPassed && fixedViewPassed && runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(Application.dataPath,"..","Validation","smoke-result.json"),
                "{\"passed\":"+ok.ToString().ToLowerInvariant()+",\"navigationPassed\":"+navigationPassed.ToString().ToLowerInvariant()+",\"visualsPassed\":"+visualsPassed.ToString().ToLowerInvariant()+",\"artAndHotspotsPassed\":"+artPassed.ToString().ToLowerInvariant()+",\"saveRoundtripPassed\":"+saveRoundtripPassed.ToString().ToLowerInvariant()+",\"errorCount\":"+runtimeErrors.Count+",\"day\":"+game.State.day+",\"clues\":"+game.State.clues.Count+",\"cash\":"+game.State.money+"}");
            Debug.Log("DEMO_SMOKE_TEST "+(ok?"PASS":"FAIL"));Application.Quit(ok?0:1);
        }
    }
}
