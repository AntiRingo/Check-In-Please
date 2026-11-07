using System;

namespace CheckInPlease
{
    public sealed partial class GameModel
    {
        public const float UnlockSeconds=3;
        public bool OwnerPresent(int room)
        {return InRoom(room)!=null&&(State.phase==Shift.Reception||State.phase==Shift.Audit);}

        public bool NeedsUnlock(int room,bool fromInside)
        {var door=Observation(room);return door!=null&&!door.doorOpen&&door.doorLocked&&!fromInside;}

        public bool OpenDoor(int room,bool fromInside,float seconds=0)
        {
            var door=Observation(room);if(door==null)return false;
            if(NeedsUnlock(room,fromInside)&&(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<UnlockSeconds))return false;
            door.doorLocked=false;door.doorOpen=true;return true;
        }
        public void CloseDoor(int room)
        {
            var door=Observation(room);if(door==null)return;
            door.doorOpen=false;door.doorLocked=InRoom(room)!=null;
        }

        // This is the owner's sight, never the keeper's camera coverage. Coordinates are room-local.
        public bool OwnerCanSee(int room,int keeperFloor,float keeperX,float keeperZ,
            float ownerX,float ownerZ,float facingX,float facingZ)
        {
            if(!OwnerPresent(room)||keeperFloor!=HotelLayout.RoomFloor(room))return false;
            if(Math.Abs(keeperX)>=SuiteLayout.Width*.5f-.18f||keeperZ>SuiteLayout.Depth||keeperZ< -3.5f)return false;
            float dx=keeperX-ownerX,dz=keeperZ-ownerZ,distance=(float)Math.Sqrt(dx*dx+dz*dz);
            float alert=InRoom(room).vigilance/100f;
            if(distance>7+3*alert)return false;
            float facingLength=(float)Math.Sqrt(facingX*facingX+facingZ*facingZ);
            if(distance>.001f&&(facingLength<.001f||(dx*facingX+dz*facingZ)/(distance*facingLength)<Math.Cos((55+30*alert)*Math.PI/180)))return false;
            if(keeperZ<=.18f)
            {
                if(!Observation(room).doorOpen)return false;
                float t=ownerZ/Math.Max(.001f,ownerZ-keeperZ);
                if(Math.Abs(ownerX+dx*t)>RoomSight.DoorHalfWidth-.08f)return false;
            }
            return SuiteLayout.Sight(ownerX,ownerZ,keeperX,keeperZ)>.6f;
        }

        public bool ResetIntrusion(int room,int keeperFloor,float keeperX,float keeperZ)
        {
            var guest=InRoom(room);if(guest==null||guest.intrusionLevel==0)return false;
            bool inRoom=keeperZ>.18f&&keeperZ<SuiteLayout.Depth&&Math.Abs(keeperX)<SuiteLayout.Width*.5f;
            bool atDoor=keeperZ>=-2.8f&&keeperZ<=.18f&&Math.Abs(keeperX)<2.2f;
            if(keeperFloor==HotelLayout.RoomFloor(room)&&(inRoom||atDoor))return false;
            guest.intrusionLevel=0;return true;
        }

        public string NoticeIntrusion(int room,bool inside)
        {
            var guest=InRoom(room);int level=inside?2:1;
            if(!OwnerPresent(room)||guest.intrusionLevel>=level)return "";
            guest.intrusionLevel=level;
            int resentment=guest.resentment,vigilance=guest.vigilance;
            guest.resentment=Math.Min(100,resentment+(inside?25:15));
            guest.vigilance=Math.Min(100,vigilance+(inside?30:20));
            string message=guest.name+(inside?"发现你擅自在房内":"撞见你打开房门")+"：反感 +"+(guest.resentment-resentment)+"，警惕 +"+(guest.vigilance-vigilance)+"。";
            Log(room+" 房 · "+message);return message;
        }
    }
}
