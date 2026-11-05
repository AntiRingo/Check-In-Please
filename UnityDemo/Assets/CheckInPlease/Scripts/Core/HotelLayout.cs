using System;
using System.Collections.Generic;

namespace CheckInPlease
{
    public static class HotelLayout
    {
        public const int FloorCount = 3;
        public const float StoreyHeight = 4.4f;
        public static readonly int[] Rooms = {201,202,203,301,302,303};
        public static bool IsRoom(int room) { return (room / 100 == 2 || room / 100 == 3) && room % 100 >= 1 && room % 100 <= 3; }
        public static int RoomFloor(int room) { return IsRoom(room) ? room / 100 : 1; }
        public static float Height(int floor) { return (floor - 1) * StoreyHeight; }
        public static IEnumerable<int> RoomsOn(int floor)
        { foreach(int room in Rooms) if(RoomFloor(room)==floor) yield return room; }
        public static int NextFloor(int current,int destination)
        {
            if(current<1||current>FloorCount||destination<1||destination>FloorCount)throw new ArgumentOutOfRangeException();
            return current+Math.Sign(destination-current);
        }
    }
}
