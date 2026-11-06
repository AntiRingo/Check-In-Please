using System.Collections;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        bool transitionPassed=true;
        void CheckTransition(bool condition,string label)
        {
            transitionPassed&=condition;
            if(condition)Debug.Log("STAIR_TRANSITION PASS "+label);else Debug.LogError("STAIR_TRANSITION FAIL "+label);
        }
        float TransitionFloorBrightness()
        {
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            float brightness=FrameBrightness(frame,new Vector3(1,0,-2));Destroy(frame);return brightness;
        }
        IEnumerator StairwellTransitionSmokeTest()
        {
            float x=-11+HotelLayout.StairOffset;
            yield return WalkDuringTest(new Vector3(x,.88f,.9f));yield return new WaitForEndOfFrame();
            float baseline=TransitionFloorBrightness();
            yield return WalkDuringTest(new Vector3(x,1.43f,1.9f));
            CheckTransition(world.VisibleFloorCount==1&&Mathf.Abs(world.FloorOpacity-.5f)<.01f,"old storey fades before the platform");
            yield return Capture("transition-01-outgoing-fade");yield return new WaitForEndOfFrame();
            float faded=TransitionFloorBrightness();
            CheckTransition(faded<baseline-.02f&&faded>.06f,"rendered storey is partially faded, not abruptly removed");
            Debug.Log("TRANSITION_BRIGHTNESS full="+baseline+" faded="+faded);
            yield return WalkDuringTest(new Vector3(x,.88f,.9f));yield return new WaitForEndOfFrame();
            CheckTransition(Mathf.Abs(TransitionFloorBrightness()-baseline)<.02f&&world.FloorOpacity==1,"turning back restores the outgoing storey");
            yield return WalkDuringTest(new Vector3(x,1.98f,2.9f));
            CheckTransition(world.InStairwell&&world.VisibleFloorCount==0&&world.VisibleStairHalfCount==2,"independent stairwell hides all storeys");
            yield return Capture("transition-02-stairwell-only");yield return new WaitForEndOfFrame();
            CheckTransition(TransitionFloorBrightness()<.09f,"hidden floor renders as background");
            var stopped=world.PlayerPosition;var camera=world.ViewPosition;
            world.GroundClick(world.Project(new Vector3(1,0,-2)));yield return null;
            CheckTransition(!world.Walking&&Vector3.Distance(stopped,world.PlayerPosition)<.001f,"hidden storey cannot be clicked through the stairwell");
            modal="journal";yield return HoldMovement(Vector2.up,.2f);modal="";
            CheckTransition(Vector3.Distance(stopped,world.PlayerPosition)<.001f&&Vector3.Distance(camera,world.ViewPosition)<.001f,"pause preserves the intermediate scene");
            world.StoreLocation(game.State);game=new GameModel(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(game.State)));world.Build("saved",game);yield return null;
            CheckTransition(world.InStairwell&&world.VisibleFloorCount==0&&Vector3.Distance(stopped,world.PlayerPosition)<.001f&&Vector3.Distance(camera,world.ViewPosition)<.001f,"save reload restores the hidden-floor transition");
        }
        IEnumerator CrossPlatformDuringTest()
        {
            bool continuous=true,changedFloor=false;int previousFloor=world.CurrentFloor;
            var previous=world.ViewPosition;
            world.WalkTo(new Vector3(-13+HotelLayout.StairOffset,2.2f,3.8f));
            float deadline=Time.realtimeSinceStartup+12;
            while(world.Walking&&Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                continuous&=Vector3.Distance(previous,world.ViewPosition)<=6.0f*Time.deltaTime+.015f;
                changedFloor|=world.CurrentFloor!=previousFloor;
                continuous&=world.VisibleFloorCount==0&&world.VisibleStairHalfCount==2;
                previous=world.ViewPosition;previousFloor=world.CurrentFloor;
            }
            CheckTransition(!world.Walking&&changedFloor&&continuous,"keeper and stairs remain continuous across the floor boundary");
            yield return Capture("transition-03-across-platform");
        }
    }
}
