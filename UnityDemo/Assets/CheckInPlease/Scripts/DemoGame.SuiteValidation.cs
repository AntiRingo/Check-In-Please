using System.Collections;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        bool fixedViewPassed=true,suitesPassed=true;
        float fixedViewSize;
        Quaternion fixedViewRotation;
        Vector3 fixedViewOffset;
        void CheckFixedView()
        {
            if(fixedViewSize==0){fixedViewSize=world.ViewSize;fixedViewRotation=world.ViewRotation;fixedViewOffset=world.ViewFloorOffset;}
            var offset=world.ViewFloorOffset-fixedViewOffset;
            fixedViewPassed&=Quaternion.Angle(fixedViewRotation,world.ViewRotation)<.01f&&Mathf.Abs(fixedViewSize-world.ViewSize)<.001f&&Mathf.Abs(offset.x)<.001f&&Mathf.Abs(offset.z)<.001f&&(world.InStairwell||Mathf.Abs(offset.y)<.001f);
        }
        void CheckSuite(bool condition,string label)
        {
            suitesPassed&=condition;
            if(condition)Debug.Log("SUITE_CHECK PASS "+label);else Debug.LogError("SUITE_CHECK FAIL "+label);
        }
        IEnumerator SuiteSmokeTest(int room)
        {
            float cx=RoomSight.Center(room),h=HotelLayout.Height(HotelLayout.RoomFloor(room));
            yield return WalkDuringTest(new Vector3(cx, h,3.7f));
            var start=world.PlayerPosition;yield return HoldMovement(WorldMovement(Vector3.forward),.5f);
            CheckSuite(world.PlayerPosition.z<4.17f,"solid partition blocks walking out of living room");
            CheckSuite(world.VisibilityAt(room,new Vector3(cx,h,6))==0,"direct sight cannot pass through solid partition");
            yield return WalkDuringTest(new Vector3(cx-1.25f,h,6));
            CheckSuite(world.VisibilityAt(room,new Vector3(cx-3.2f,h,8.4f))>.95f,"bedroom reached through its doorway");
            yield return Capture("suite-01-bedroom");
            yield return WalkDuringTest(new Vector3(cx+1.2f,h,6.8f));
            CheckSuite(world.VisibilityAt(room,new Vector3(cx+3.25f,h,6.3f))>.95f,"study reached through a separate doorway");
            CheckSuite(world.VisibilityAt(room,new Vector3(cx+3.2f,h,8.6f))==0,"bathroom stays hidden behind partition");
            yield return Capture("suite-02-study");
            yield return WalkDuringTest(new Vector3(cx+1.3f,h,9.2f));
            CheckSuite(world.VisibilityAt(room,new Vector3(cx+3.2f,h,9.3f))>.95f,"bathroom accessible through internal passage");
            yield return Capture("suite-03-bathroom");
            yield return WalkDuringTest(world.DoorApproach(room));
        }
    }
}
