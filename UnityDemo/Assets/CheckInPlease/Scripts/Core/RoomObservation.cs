using System;

namespace CheckInPlease
{
    [Serializable]
    public sealed class RoomObservation
    {
        public int room, mount, accessVersion;
        public bool cameraInstalled, doorOpen, doorLocked;
        public float yaw = 135, fieldOfView = 60;
        public void Normalize()
        {
            if(doorOpen)doorLocked=false;
            mount = Math.Max(0, Math.Min(2, mount));
            if (float.IsNaN(yaw) || float.IsInfinity(yaw)) yaw = 135;
            yaw = (yaw % 360 + 360) % 360;
            if (float.IsNaN(fieldOfView) || float.IsInfinity(fieldOfView) || fieldOfView <= 0) fieldOfView = 60;
            fieldOfView = Math.Max(35, Math.Min(95, fieldOfView));
        }
    }

    // Same XZ geometry and smooth edges as HotelRoom.shader. Furniture is intentionally
    // absent: it can block walking, but never blocks camera or doorway observation.
    public static class RoomSight
    {
        public const float RoomWidth = SuiteLayout.Width, Depth = SuiteLayout.Depth, DoorHalfWidth = .76f, CameraRange = 8.8f;
        public static float Center(int room) { return (room % 100 - 2) * RoomWidth; }
        public static void Mount(int mount, out float x, out float z)
        {
            x = mount == 1 ? 4.65f : -4.65f;
            z = mount == 0 ? 10.15f : mount == 1 ? 7.2f : .5f;
        }
        static float Smooth(float a, float b, float v)
        {
            float t = Math.Max(0, Math.Min(1, (v - a) / (b - a)));
            return t * t * (3 - 2 * t);
        }
        static float Distance(float x, float z) { return (float)Math.Sqrt(x * x + z * z); }
        public static float Camera(RoomObservation room, float localX, float z)
        {
            if (!room.cameraInstalled) return 0;
            Mount(room.mount, out float x, out float mz);
            float dx = localX - x, dz = z - mz, distance = Distance(dx, dz);
            if (distance < .001f) return 1;
            double angle = room.yaw * Math.PI / 180;
            float dot = (dx * (float)Math.Sin(angle) + dz * (float)Math.Cos(angle)) / distance;
            float outer = (float)Math.Cos(room.fieldOfView * .5 * Math.PI / 180);
            float inner = (float)Math.Cos((room.fieldOfView * .5 - 7) * Math.PI / 180);
            return Smooth(outer, inner, dot) * (1 - Smooth(CameraRange - .8f, CameraRange, distance)) * SuiteLayout.Sight(x,mz,localX,z);
        }
        public static float Direct(RoomObservation room, float localX, float z, float observerLocalX, float observerZ)
        {
            float distance = Distance(localX - observerLocalX, z - observerZ);
            if (Math.Abs(observerLocalX) < RoomWidth * .5f - .18f && observerZ > .18f && observerZ < Depth)
                return (1 - Smooth(7.2f, 8.2f, distance))*SuiteLayout.Sight(observerLocalX,observerZ,localX,z);
            if (!room.doorOpen || observerZ >= .18f || observerZ < -3.5f || z <= 0) return 0;
            float t = -observerZ / Math.Max(.001f, z - observerZ);
            float crossing = observerLocalX + (localX - observerLocalX) * t;
            return (1 - Smooth(DoorHalfWidth - .14f, DoorHalfWidth, Math.Abs(crossing))) *
                (1 - Smooth(7.2f, 8.2f, distance))*SuiteLayout.Sight(observerLocalX,observerZ,localX,z);
        }
        public static float Visibility(RoomObservation room, float localX, float z, float observerLocalX, float observerZ, int observerFloor = 0)
        {
            if (Math.Abs(localX) > RoomWidth * .5f || z < 0 || z > Depth) return 0;
            float direct=observerFloor!=0&&observerFloor!=HotelLayout.RoomFloor(room.room)?0:Direct(room,localX,z,observerLocalX,observerZ);
            return Math.Max(Camera(room, localX, z), direct);
        }
    }
}
