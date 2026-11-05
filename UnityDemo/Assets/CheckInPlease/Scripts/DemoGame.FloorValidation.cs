using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        bool sawStairHeight;
        bool keyboardPassed=true,singleFloorPassed=true;
        Vector2 validationMovement;
        float testedMoveSpeed;
        void CheckKeyboard(bool condition,string label)
        {
            keyboardPassed&=condition;
            if(condition)Debug.Log("KEYBOARD_CHECK PASS "+label);else Debug.LogError("KEYBOARD_CHECK FAIL "+label);
        }
        IEnumerator HoldMovement(Vector2 input,float duration)
        {
            Vector3 start=world.PlayerPosition;float began=Time.time;validationMovement=input;
            while(Time.time-began<duration)yield return null;
            validationMovement=Vector2.zero;
            testedMoveSpeed=Vector3.Distance(start,world.PlayerPosition)/Mathf.Max(.001f,Time.time-began);
            yield return null;
        }
        IEnumerator KeyboardSmokeTest()
        {
            var origin=new Vector3(-1.2f,0,-1.8f);float straightSpeed=0;
            foreach(var input in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
            {
                yield return WalkDuringTest(origin);var start=world.Project(world.PlayerPosition);
                yield return HoldMovement(input,.2f);var delta=world.Project(world.PlayerPosition)-start;
                CheckKeyboard(input.x==0?delta.y*input.y< -5&&Mathf.Abs(delta.x)<1:delta.x*input.x>5&&Mathf.Abs(delta.y)<1,"screen direction "+input);
                straightSpeed=testedMoveSpeed;
            }
            yield return WalkDuringTest(origin);yield return HoldMovement(Vector2.one,.2f);
            CheckKeyboard(Mathf.Abs(testedMoveSpeed-straightSpeed)<.15f&&straightSpeed>3,"diagonal speed equals straight speed");
            var stopped=world.PlayerPosition;modal="journal";yield return HoldMovement(Vector2.up,.25f);modal="";
            CheckKeyboard(Vector3.Distance(stopped,world.PlayerPosition)<.001f,"modal blocks movement");
            bool oldArrival=false;world.WalkTo(WorldView.ComputerApproach,()=>oldArrival=true);
            yield return HoldMovement(Vector2.left,.2f);
            CheckKeyboard(!oldArrival&&!world.Walking,"keyboard cancels mouse path and pending interaction");
            yield return WalkDuringTest(origin);world.NavigateToFloor(3,()=>oldArrival=true);
            yield return HoldMovement(Vector2.right,.2f);
            CheckKeyboard(!oldArrival&&!world.IsTravelling&&world.CurrentFloor==1,"keyboard cancels approach to stairs");
            yield return WalkDuringTest(new Vector3(0,0,-2.7f));yield return HoldMovement(Vector2.down,.8f);
            CheckKeyboard(world.PlayerPosition.z>=-3.121f,"cannot leave floor edge");
            yield return WalkDuringTest(new Vector3(-2,0,3.1f));yield return HoldMovement(Vector2.up,.8f);
            CheckKeyboard(world.PlayerPosition.z<3.45f,"reception furniture blocks movement");
            yield return WalkDuringTest(new Vector3(2.7f,0,2));yield return HoldMovement(Vector2.right,.7f);
            CheckKeyboard(world.PlayerPosition.x<2.91f&&world.PlayerPosition.z>.1f,"office wall blocks movement");
            yield return WalkDuringTest(origin);
        }
        IEnumerator WaitForMovement()
        {
            yield return null;float deadline=Time.realtimeSinceStartup+65;
            while(world.Walking&&Time.realtimeSinceStartup<deadline)
            {
                if(world.Climbing&&Mathf.Abs(world.PlayerPosition.y/HotelLayout.StoreyHeight-Mathf.Round(world.PlayerPosition.y/HotelLayout.StoreyHeight))>.08f)sawStairHeight=true;
                yield return null;
            }
            bool stopped=!world.Walking;navigationPassed&=stopped;
            if(!stopped)Debug.LogError("Hotel navigation timed out on floor "+world.CurrentFloor);
        }
        IEnumerator WalkDuringTest(Vector3 point)
        {
            bool reached=false;bool route=world.WalkTo(point,()=>reached=true);
            if(route)yield return WaitForMovement();navigationPassed&=reached;
            if(!reached)Debug.LogError("Validation path failed: "+point);
        }
        IEnumerator TravelDuringTest(string area)
        {
            Visit(area);yield return WaitForMovement();
            int floor=int.TryParse(area,out int room)?HotelLayout.RoomFloor(room):1;
            bool reached=world.CurrentFloor==floor&&(room==0||world.SelectedRoom==room);
            navigationPassed&=reached;if(!reached)Debug.LogError("Failed hotel destination "+area);
        }
        float FrameBrightness(Texture2D frame,Vector3 point)
        {
            var p=world.Project(point);float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            int x=Mathf.RoundToInt((Screen.width-1600*scale)*.5f+p.x*scale);
            int y=Mathf.RoundToInt((Screen.height-900*scale)*.5f+(900-p.y)*scale);float sum=0;
            for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)
            {
                var color=frame.GetPixel(Mathf.Clamp(x+dx,0,frame.width-1),Mathf.Clamp(y+dy,0,frame.height-1));
                sum+=Mathf.Max(color.r,Mathf.Max(color.g,color.b));
            }
            return sum/25;
        }
        IEnumerator FloorSmokeTest()
        {
            yield return new WaitForSeconds(1);game=new GameModel();game.StartDay();modal="";toast="";world.Build("lobby",game);
            yield return KeyboardSmokeTest();
            yield return Capture("hotel-01-ground-floor");
            Act("review");bool locationGates=modal!="review";
            yield return WalkDuringTest(WorldView.ReceptionApproach);Act("review");locationGates&=modal=="review";
            yield return Capture("hotel-02-reception-six-rooms");roomChoice=201;Decision(true);
            Act("review");roomChoice=301;Decision(true);Act("review");roomChoice=302;Decision(true);Night();toast="";
            Visit("301");float deadline=Time.realtimeSinceStartup+15;
            while(!world.Climbing&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return HoldMovement(Vector2.right,.3f);CheckKeyboard(world.Climbing&&world.IsTravelling,"stair flight remains automatic");
            yield return new WaitForSeconds(.5f);yield return Capture("hotel-03-stair-ascent",.05f);
            yield return WaitForMovement();
            bool stairs=world.CurrentFloor==3&&world.StairTripsCompleted>=2&&sawStairHeight&&Mathf.Abs(world.PlayerPosition.y-8.8f)<.02f;
            yield return Capture("hotel-04-third-floor");
            Act("dossier");locationGates&=modal!="dossier"&&game.State.phase==Shift.Search;toast="";
            world.ChangeCamera(0);var observed=new Vector3(-6.4f,8.8f,3.3f);
            bool visibility=world.VisibilityAt(301,observed)>.9f&&!game.Observation(302).cameraInstalled;
            yield return Capture("hotel-05-third-floor-camera");
            var frame=ScreenCapture.CaptureScreenshotAsTexture();float blackPixel=FrameBrightness(frame,new Vector3(0,8.86f,5.3f));Destroy(frame);
            visibility&=blackPixel<.035f;Debug.Log("HOTEL_BLACK_ROOM "+blackPixel);
            world.ChangeCamera(180);yield return Capture("hotel-06-camera-turned-away");visibility&=world.VisibilityAt(301,observed)==0;
            world.ChangeCamera(180);world.ChangeCamera(1);visibility&=game.Observation(301).mount==1&&game.Observation(201).mount==0;
            world.ChangeCamera(2);world.ChangeCamera(0);visibility&=world.VisibilityAt(301,observed)==0;
            world.ChangeCamera(0);
            yield return TravelDuringTest("302");yield return HoldMovement(Vector2.up,.65f);
            CheckKeyboard(world.PlayerPosition.z<-.27f,"closed guest door blocks keyboard movement");
            yield return WalkDuringTest(world.DoorApproach(302));world.ToggleDoor();yield return Capture("hotel-07-third-floor-door");
            visibility&=world.VisibilityAt(302,new Vector3(0,8.8f,4))>.95f;
            yield return WalkDuringTest(world.spots.First(s=>s.action=="search0").approach);Act("search0");modal="";
            yield return WalkDuringTest(world.spots.First(s=>s.action=="search1").approach);Act("search1");modal="";
            visibility&=game.State.clues.Count==2;
            yield return WalkDuringTest(world.DoorApproach(302));world.ToggleDoor();
            visibility&=world.VisibilityAt(302,new Vector3(0,8.8f,4))==0&&!world.spots.Any(s=>s.action.StartsWith("search")&&world.SpotVisible(s));
            world.StoreLocation(game.State);var saved=JsonUtility.ToJson(game.State);game=new GameModel(JsonUtility.FromJson<GameState>(saved));world.Build("saved",game);
            bool persistence=world.CurrentFloor==3&&game.Observation(301).cameraInstalled&&!game.Observation(302).doorOpen&&game.InRoom(301)!=null;
            yield return TravelDuringTest("203");world.ToggleDoor();yield return Capture("hotel-08-second-floor");
            visibility&=world.VisibilityAt(203,new Vector3(6.4f,4.4f,4))>.95f&&world.VisibilityAt(303,new Vector3(6.4f,8.8f,4))==0;
            Audit();yield return WaitForMovement();locationGates&=world.AtComputer&&modal=="dossier"&&game.State.phase==Shift.Audit;
            subject=game.InRoom(302).id;selected.Clear();foreach(var clue in game.State.clues.Where(c=>c.guestId==subject))selected.Add(clue.id);
            yield return Capture("hotel-09-computer-report");int before=game.State.rations;Resolve(Disposition.Reported);
            bool reporting=game.State.rations==before+3&&game.State.clues.Count(c=>c.disposition==Disposition.Reported)==2;
            modal="";toast="";yield return Capture("hotel-10-office-computer");
            stairs&=world.CurrentFloor==1&&world.StairTripsCompleted>=4;
            bool passed=visibility&&persistence&&stairs&&reporting&&locationGates&&navigationPassed&&visualsPassed&&artPassed&&keyboardPassed&&singleFloorPassed&&runtimeErrors.Count==0;
            var result="{\"passed\":"+passed.ToString().ToLowerInvariant()+",\"visibilityPassed\":"+visibility.ToString().ToLowerInvariant()+",\"stairsPassed\":"+stairs.ToString().ToLowerInvariant()+",\"singleFloorPassed\":"+singleFloorPassed.ToString().ToLowerInvariant()+",\"keyboardPassed\":"+keyboardPassed.ToString().ToLowerInvariant()+",\"locationGatesPassed\":"+locationGates.ToString().ToLowerInvariant()+",\"computerReportingPassed\":"+reporting.ToString().ToLowerInvariant()+",\"navigationPassed\":"+navigationPassed.ToString().ToLowerInvariant()+",\"visualsPassed\":"+visualsPassed.ToString().ToLowerInvariant()+",\"artPassed\":"+artPassed.ToString().ToLowerInvariant()+",\"persistencePassed\":"+persistence.ToString().ToLowerInvariant()+",\"errorCount\":"+runtimeErrors.Count+"}";
            File.WriteAllText(Path.Combine(Application.dataPath,"..","Validation","hotel-result.json"),result);
            Debug.Log("HOTEL_SMOKE_TEST "+(passed?"PASS":"FAIL"));Application.Quit(passed?0:1);
        }
    }
}
