using System.Collections;
using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        bool freeStairsPassed=true;
        void CheckStair(bool condition,string label)
        {
            freeStairsPassed&=condition;
            if(condition)Debug.Log("FREE_STAIRS PASS "+label);else Debug.LogError("FREE_STAIRS FAIL "+label+" at "+world.PlayerPosition);
        }
        Vector2 WorldMovement(Vector3 direction)
        {
            var camera=Camera.main.transform;
            return new Vector2(Vector3.Dot(direction,Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized),Vector3.Dot(direction,Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized));
        }
        IEnumerator KeyboardWalkTo(Vector3 target)
        {
            float deadline=Time.realtimeSinceStartup+12;
            while(Vector3.ProjectOnPlane(target-world.PlayerPosition,Vector3.up).magnitude>.1f&&Time.realtimeSinceStartup<deadline)
            {
                var delta=Vector3.ProjectOnPlane(target-world.PlayerPosition,Vector3.up);
                validationMovement=WorldMovement(delta.normalized)*Mathf.Min(1,delta.magnitude/.18f);yield return null;
            }
            validationMovement=Vector2.zero;yield return null;
            CheckStair(Vector3.Distance(target,world.PlayerPosition)<.15f,"keyboard reaches "+target);
        }
        IEnumerator ClickWalkTo(Vector3 target)
        {
            world.GroundClick(world.Project(target));yield return WaitForMovement();
            CheckStair(Vector3.Distance(target,world.PlayerPosition)<.08f,"mouse picks actual walking height "+target);
        }
        IEnumerator FreeStairSmokeTest()
        {
            yield return WalkDuringTest(new Vector3(-11+HotelLayout.StairOffset,0,-1));RefreshInteractions();
            CheckStair(!world.spots.Any(s=>s.action.StartsWith("stairs"))&&nearbyInteractions.Count==0,"stairs need no interaction button");
            yield return HoldMovement(WorldMovement(Vector3.forward),.6f);
            CheckStair(world.Climbing&&!world.IsTravelling&&world.PlayerPosition.y>.5f&&world.CurrentFloor==1&&world.StairHalfVisible(1,true)&&!world.InStairwell,"walk directly onto lower flight and show lower floor");
            var stopped=world.PlayerPosition;yield return new WaitForSeconds(.3f);
            CheckStair(Vector3.Distance(stopped,world.PlayerPosition)<.001f,"release movement stops on a tread");
            yield return Capture("stairs-01-lower-flight");
            yield return HoldMovement(WorldMovement(Vector3.back),.2f);
            CheckStair(world.PlayerPosition.y<stopped.y-.2f&&world.Climbing,"reverse midway without completing the flight");
            yield return HoldMovement(WorldMovement(Vector3.right),.6f);
            CheckStair(world.PlayerPosition.x<=-10.51f+HotelLayout.StairOffset&&world.Climbing,"stair side rail prevents leaving the flight");
            yield return ClickWalkTo(new Vector3(-11+HotelLayout.StairOffset,1.65f,2.3f));
            yield return StairwellTransitionSmokeTest();
            yield return KeyboardWalkTo(new Vector3(-11+HotelLayout.StairOffset,2.2f,3.8f));
            CheckStair(world.OnStairLanding&&world.VisibleFloorCount==0&&world.StairHalfVisible(1,true)&&world.StairHalfVisible(1,false),"platform displays both connected halves without a storey");
            yield return Capture("stairs-02-fixed-platform");
            yield return CrossPlatformDuringTest();
            yield return KeyboardWalkTo(new Vector3(-13+HotelLayout.StairOffset,3.19f,1.5f));
            CheckStair(world.Climbing&&!world.OnStairLanding&&world.CurrentFloor==2&&world.VisibleFloorCount==1&&world.FloorOpacity>.5f&&world.StairHalfVisible(1,false)&&!world.StairHalfVisible(2,true),"upper storey fades in with the upper flight");
            yield return Capture("stairs-03-fixed-upper");
            var savedPosition=world.PlayerPosition;world.StoreLocation(game.State);
            game=new GameModel(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(game.State)));world.Build("saved",game);yield return null;
            CheckStair(world.Climbing&&world.CurrentFloor==2&&Vector3.Distance(savedPosition,world.PlayerPosition)<.001f,"reload preserves height and stair viewpoint");
            yield return HoldMovement(WorldMovement(Vector3.forward),.2f);
            CheckStair(world.PlayerPosition.y<savedPosition.y-.2f,"can reverse on upper half after reload");
            yield return KeyboardWalkTo(new Vector3(-13+HotelLayout.StairOffset,4.4f,-1.2f));
            CheckStair(!world.Climbing&&world.CurrentFloor==2,"walk off stairs into second storey");
            yield return WalkDuringTest(new Vector3(-11+HotelLayout.StairOffset,4.4f,-1));
            // Mouse paths can be interrupted on the slope, not just on the approach.
            bool obsoleteArrival=false;world.NavigateToFloor(3,()=>obsoleteArrival=true);
            float deadline=Time.realtimeSinceStartup+12;
            while(world.PlayerPosition.y<4.9f&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return HoldMovement(WorldMovement(Vector3.back),.15f);
            CheckStair(world.Climbing&&!world.IsTravelling&&!world.Walking&&!obsoleteArrival,"WASD takes over a map route on a slope");
            yield return KeyboardWalkTo(new Vector3(-11+HotelLayout.StairOffset,6.6f,3.8f));
            yield return KeyboardWalkTo(new Vector3(-13+HotelLayout.StairOffset,6.6f,3.8f));
            yield return KeyboardWalkTo(new Vector3(-13+HotelLayout.StairOffset,8.8f,-1.2f));
            CheckStair(!world.Climbing&&world.CurrentFloor==3&&!obsoleteArrival,"manual movement reaches third storey without old callback");
            world.SelectRoom(301);yield return WalkDuringTest(world.DoorApproach(301));
        }
        IEnumerator DescendStairsByClick()
        {
            yield return WalkDuringTest(new Vector3(-13+HotelLayout.StairOffset,4.4f,-1.25f));
            yield return ClickWalkTo(new Vector3(-13+HotelLayout.StairOffset,3.465f,1));
            CheckStair(world.CurrentFloor==2&&world.Climbing&&world.StairHalfVisible(1,false),"descending upper half retains upper floor");
            yield return ClickWalkTo(new Vector3(-12+HotelLayout.StairOffset,2.2f,3.8f));
            CheckStair(world.OnStairLanding,"mouse reaches turning platform");
            // The whole connected stairwell stays visible while both storeys are hidden.
            yield return ClickWalkTo(new Vector3(-11+HotelLayout.StairOffset,2.2f,3.8f));
            CheckStair(world.OnStairLanding&&world.CurrentFloor==1&&world.InStairwell,"crossing platform changes floor logically while the stairwell stays visible");
            yield return ClickWalkTo(new Vector3(-11+HotelLayout.StairOffset,1.43f,1.9f));
            CheckTransition(world.VisibleFloorCount==1&&Mathf.Abs(world.FloorOpacity-.5f)<.01f,"descending arrival fades the lower storey in");
            yield return ClickWalkTo(new Vector3(-11+HotelLayout.StairOffset,.935f,1));
            CheckStair(world.CurrentFloor==1&&world.Climbing&&world.StairHalfVisible(1,true)&&!world.StairHalfVisible(1,false),"descending lower half shows lower floor");
            yield return ClickWalkTo(new Vector3(-11+HotelLayout.StairOffset,0,-1.4f));
            CheckStair(!world.Climbing&&world.CurrentFloor==1,"mouse exits stairs onto first storey");
        }
    }
}
