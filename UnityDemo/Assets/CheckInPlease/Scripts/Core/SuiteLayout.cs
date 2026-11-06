using System;

namespace CheckInPlease
{
    public struct SuiteWall
    {
        public float x1,z1,x2,z2;
        public SuiteWall(float ax,float az,float bx,float bz){x1=ax;z1=az;x2=bx;z2=bz;}
    }
    // Shared by room construction, walking collision and the CPU/GPU observation masks.
    public static class SuiteLayout
    {
        public const float Width=10.2f,Depth=10.6f;
        public static readonly SuiteWall[] Walls={
            new SuiteWall(-5.1f,4.4f,-3.5f,4.4f),
            new SuiteWall(-1.9f,4.4f,.4f,4.4f),
            new SuiteWall(2,4.4f,5.1f,4.4f),
            new SuiteWall(-.6f,4.4f,-.6f,10.6f),
            new SuiteWall(-.6f,7.7f,.4f,7.7f),
            new SuiteWall(2,7.7f,5.1f,7.7f)
        };
        public static bool Clear(float x,float z)
        {
            foreach(var wall in Walls)
            {
                float dx=wall.x2-wall.x1,dz=wall.z2-wall.z1;
                float t=Math.Max(0,Math.Min(1,((x-wall.x1)*dx+(z-wall.z1)*dz)/(dx*dx+dz*dz)));
                float px=x-wall.x1-dx*t,pz=z-wall.z1-dz*t;
                if(px*px+pz*pz<.24f*.24f)return false;
            }
            return true;
        }
        static float Cross(float ax,float az,float bx,float bz){return ax*bz-az*bx;}
        public static float Sight(float fromX,float fromZ,float toX,float toZ)
        {
            float dx=toX-fromX,dz=toZ-fromZ,visibility=1;
            foreach(var wall in Walls)
            {
                float wx=wall.x2-wall.x1,wz=wall.z2-wall.z1,den=Cross(dx,dz,wx,wz);
                if(Math.Abs(den)<.00001f)continue;
                float ax=wall.x1-fromX,az=wall.z1-fromZ;
                float t=Cross(ax,az,wx,wz)/den,u=Cross(ax,az,dx,dz)/den;
                // Surfaces on the visible side of a wall remain visible themselves.
                if(t<=.002f||t>=.995f)continue;
                float length=(float)Math.Sqrt(wx*wx+wz*wz);
                float edge=Math.Max(-u,u-1)*length;
                float fade=Math.Max(0,Math.Min(1,(edge+.06f)/.28f));
                visibility=Math.Min(visibility,fade*fade*(3-2*fade));
            }
            return visibility;
        }
    }
}
