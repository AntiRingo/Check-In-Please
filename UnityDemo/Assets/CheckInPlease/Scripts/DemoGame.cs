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
        bool smoke;
        bool navigationPassed = true;
        bool visualsPassed = true, saveRoundtripPassed = true;
        bool artPassed = true;
        readonly List<string> runtimeErrors = new List<string>();
        string SavePath { get { return Path.Combine(Application.persistentDataPath,"hotel-save-v1.json"); } }
        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            smoke=Environment.GetCommandLineArgs().Contains("-demoSmokeTest");
            if(smoke)Application.logMessageReceived+=RecordError;
            font=Resources.Load<Font>("HotelFont");
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},24);
            game=new GameModel();world=gameObject.AddComponent<WorldView>();world.Setup(font);world.Build("lobby",game);
            MakeAudio();
            if(smoke)StartCoroutine(SmokeTest());
        }
        void RecordError(string message,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeErrors.Add(message);}
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
            world.Tick(modal!="");
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(modal=="")modal="pause";
                else if(modal=="pause"||modal=="review"||modal=="item"||modal=="journal"||modal=="help"||modal=="dossier"||modal=="rooms")LeaveModal();
            }
            if(Input.GetKeyDown(KeyCode.J) && modal=="")modal="journal";
        }
        void Say(string message) {toast=message;toastUntil=Time.unscaledTime+6;}
        void Save()
        {
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
                game=new GameModel(s);world.Build("lobby",game);RestoreModal();
            }
            catch(Exception e){Say("无法读取存档："+e.Message);}
        }
        void RestoreModal()
        {
            modal=game.State.phase==Shift.Morning?"brief":game.State.phase==Shift.Finished?"ending":"";
            if(game.State.phase==Shift.Audit)world.Build("office",game);
        }
        void LeaveModal() {modal=game.State.phase==Shift.Finished?"ending":game.State.phase==Shift.Morning?"menu":"";}
        void NewGame()
        {game=new GameModel();world.Build("lobby",game);modal="brief";confirmNew=false;selected.Clear();Save();}
        void StartDay()
        {Say(game.StartDay());world.Build("lobby",game);modal=game.State.phase==Shift.Finished?"ending":game.State.news.Count>0?"dispatch":"";Save();}
        void Visit(string area)
        {
            if(area!="lobby" && area!="office" && game.State.phase!=Shift.Search)
            {Say("住客仍在房间里。宵禁点名后才能搜查。");return;}
            if(int.TryParse(area,out int n)&&game.InRoom(n)==null){Say(n+" 房目前无人入住。");return;}
            modal="";world.Build(area,game);
        }
        void Act(string action)
        {
            if(action=="review")
            {
                if(game.State.phase!=Shift.Reception){Say("当前不在接待时段。");return;}
                if(game.Current==null){Say("今天的来客已经处理完毕，可以开始夜班。");return;}
                toast="";comparison="";roomChoice=Enumerable.Range(201,3).FirstOrDefault(n=>game.InRoom(n)==null);modal="review";
            }
            else if(action=="dossier")
            {
                if(game.State.phase==Shift.Search){Say(game.BeginAudit());Save();}
                if(game.State.phase!=Shift.Audit){Say("档案处置在夜间审计时进行。");return;}
                toast="";selected.Clear();subject=game.State.clues.FirstOrDefault(c=>CanUse(c))?.guestId??"";modal="dossier";
            }
            else if(action.StartsWith("search"))
            {
                if(!int.TryParse(world.Area,out int n))return;
                inspecting=game.FindClue(n,action=="search0"?0:1,out var error);
                if(inspecting==null){Say(error);return;}
                toast="";flipped=false;modal="item";Save();
            }
            else if(action=="rooms")modal="rooms";
            else Visit(action);
        }
        static bool CanUse(Clue c) {return c.disposition==Disposition.Unresolved||c.disposition==Disposition.Withheld;}
        void Decision(bool admit)
        {Say(game.Decide(admit,roomChoice));comparison="";modal="";Save();world.Build("lobby",game);}
        void Night()
        {Say(game.BeginNight());modal="";world.Build("lobby",game);Save();}
        void Audit()
        {Say(game.BeginAudit());Visit("office");Act("dossier");Save();}
        void EndDay()
        {Say(game.EndDay());Save();world.Build("lobby",game);RestoreModal();}
        void Resolve(Disposition d)
        {Say(game.Resolve(subject,d,selected));selected.RemoveWhere(id=>!game.State.clues.Any(c=>c.id==id&&CanUse(c)));Save();}
        IEnumerator Capture(string name)
        {
            artPassed &= world.ArtReady && world.ReachableHotspots==world.spots.Count;
            float expectedHeight=world.Area=="lobby"?160:world.Area=="office"?245:295;
            artPassed &= Mathf.Abs(world.CharacterHeightPixels-expectedHeight)<1;
            Debug.Log("ART_SCALE "+world.Area+" keeperHeight="+world.CharacterHeightPixels.ToString("F1")+"px reachable="+world.ReachableHotspots+"/"+world.spots.Count);
            yield return new WaitForSeconds(.8f);yield return new WaitForEndOfFrame();
            var folder=Path.Combine(Application.dataPath,"..","Validation");Directory.CreateDirectory(folder);
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
                if(d==1)
                {
                    bool reached=false;world.WalkTo(world.spots.First(s=>s.action=="review").approach,()=>reached=true);
                    float deadline=Time.realtimeSinceStartup+12;
                    while(!reached&&Time.realtimeSinceStartup<deadline)yield return null;
                    navigationPassed &= reached;
                    yield return Capture("02-lobby");
                }
                while(game.Current!=null)
                {
                    Act("review");if(d==1&&game.Current.id=="A-017")yield return Capture("03-review");
                    bool admit=game.Violation(game.Current)=="";Decision(admit);
                }
                Night();var occupied=game.Today.Where(g=>g.decision==CheckInPlease.Decision.Admitted).ToArray();
                foreach(var g in occupied.Take(2))
                {
                    Visit(g.room.ToString());
                    if(d==1)
                    {
                        bool reached=false;world.WalkTo(world.spots.First(s=>s.action=="search0").approach,()=>reached=true);
                        float deadline=Time.realtimeSinceStartup+12;
                        while(!reached&&Time.realtimeSinceStartup<deadline)yield return null;
                        navigationPassed &= reached;
                        yield return Capture("04-room-"+g.room);
                    }
                    Act("search0");if(d==1)yield return Capture("05-inspection-"+g.room);modal="";
                    Act("search1");modal="";
                }
                Audit();
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
            bool ok=game.State.phase==Shift.Finished && game.State.day==3 && game.State.money>=0 && navigationPassed && visualsPassed && saveRoundtripPassed && artPassed && runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(Application.dataPath,"..","Validation","smoke-result.json"),
                "{\"passed\":"+ok.ToString().ToLowerInvariant()+",\"navigationPassed\":"+navigationPassed.ToString().ToLowerInvariant()+",\"visualsPassed\":"+visualsPassed.ToString().ToLowerInvariant()+",\"artAndHotspotsPassed\":"+artPassed.ToString().ToLowerInvariant()+",\"saveRoundtripPassed\":"+saveRoundtripPassed.ToString().ToLowerInvariant()+",\"errorCount\":"+runtimeErrors.Count+",\"day\":"+game.State.day+",\"clues\":"+game.State.clues.Count+",\"cash\":"+game.State.money+"}");
            Debug.Log("DEMO_SMOKE_TEST "+(ok?"PASS":"FAIL"));Application.Quit(ok?0:1);
        }
    }
}
