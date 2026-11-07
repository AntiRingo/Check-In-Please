using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        int openingRoom;
        float openingElapsed;
        Vector3 openingPosition;
        void CancelDoorOpening(bool notify=true)
        {
            if(openingRoom==0)return;
            openingRoom=0;openingElapsed=0;
            if(notify)Say("开锁已中断，房门仍然锁着。");
        }
        void UseRoomDoor(int room)
        {
            if(openingRoom==room){CancelDoorOpening();return;}
            if(game.Observation(room).doorOpen)
            {game.CloseDoor(room);world.RefreshDoors();Save();return;}
            bool inside=world.PlayerInsideRoom(room);
            if(game.NeedsUnlock(room,inside))
            {
                openingRoom=room;openingElapsed=0;openingPosition=world.PlayerPosition;
                Say("房门已锁，正在开锁；移动、再次互动或打开界面可中断。");
                CheckRoomIntrusions(room);
            }
            else FinishDoorOpening(room,inside,0);
        }
        void FinishDoorOpening(int room,bool inside,float elapsed)
        {
            openingRoom=0;openingElapsed=0;
            if(!game.OpenDoor(room,inside,elapsed))return;
            world.RefreshDoors();Say(room+" 房门已打开。");
            CheckRoomIntrusions(room);Save();
        }
        void TickDoorOpening(Vector2 movement)
        {
            if(openingRoom==0)return;
            var door=world.spots.FirstOrDefault(s=>s.action=="room"+openingRoom);
            if(modal!=""||movement.sqrMagnitude>.001f||world.Walking||world.IsTravelling||
                Vector3.Distance(world.PlayerPosition,openingPosition)>.03f||door==null||!world.CanInteract(door))
            {CancelDoorOpening();return;}
            openingElapsed+=Mathf.Min(Time.deltaTime,.1f);
            if(openingElapsed>=GameModel.UnlockSeconds)FinishDoorOpening(openingRoom,world.PlayerInsideRoom(openingRoom),openingElapsed);
        }
        void CheckRoomIntrusions(int opening=0)
        {
            bool changed=false;
            foreach(int room in HotelLayout.Rooms)
            {
                changed|=game.ResetIntrusion(room,world.Climbing?-1:world.CurrentFloor,world.PlayerPosition.x-RoomSight.Center(room),world.PlayerPosition.z);
                bool inside=world.PlayerInsideRoom(room);
                if((!inside&&opening!=room)||!world.OwnerSeesPlayer(room))continue;
                string reaction=game.NoticeIntrusion(room,inside);
                if(reaction=="")continue;
                world.FaceIntruder(room);Say(reaction);changed=true;
            }
            if(changed)Save();
        }
        void RoomAccessHUD()
        {
            if(openingRoom!=0)
            {
                Fill(new Rect(535,718,530,80),panel);
                L(555,727,490,30,"正在打开 "+openingRoom+" 的锁  ·  "+Mathf.Max(0,GameModel.UnlockSeconds-openingElapsed).ToString("0.0")+" 秒",21);
                Fill(new Rect(555,770,490,8),line);
                Fill(new Rect(555,770,490*Mathf.Clamp01(openingElapsed/GameModel.UnlockSeconds),8),cream);
            }
            if(!world.IsHotelFloor||world.Climbing)return;
            int room=world.SelectedRoom;
            foreach(int candidate in HotelLayout.RoomsOn(world.CurrentFloor))if(world.PlayerInsideRoom(candidate)){room=candidate;break;}
            var guest=game.InRoom(room);var door=game.Observation(room);
            string state=door.doorOpen?"门已打开":door.doorLocked?"已锁 · 开锁需 3 秒":"未上锁";
            Fill(new Rect(38,170,375,guest==null?42:74),panel);
            L(50,178,350,26,room+"  ·  "+state,17,cream);
            if(guest!=null)L(50,210,350,26,guest.name+"  反感 "+guest.resentment+"  /  警惕 "+guest.vigilance,16,guest.vigilance>0?new Color(.93f,.65f,.49f):dim);
        }
    }
}
