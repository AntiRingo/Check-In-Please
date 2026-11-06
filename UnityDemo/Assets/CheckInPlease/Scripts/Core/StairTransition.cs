using System;

namespace CheckInPlease
{
    // Position-driven transition: stopping, reversing and loading a save require no timer state.
    public static class StairTransition
    {
        public const float FadeStartZ=1.1f,FadeEndZ=2.7f;
        public const float LegLength=4.5f,Length=11;
        static float Smooth(float value)
        {float t=Math.Max(0,Math.Min(1,value));return t*t*(3-2*t);}
        public static float FloorOpacity(float z)
        {return 1-Smooth((z-FadeStartZ)/(FadeEndZ-FadeStartZ));}
        public static float Station(float localX,float z)
        {
            if(z>=3.5f&&localX>=-13&&localX<=-11)return LegLength-11-localX;
            float leg=Math.Max(0,Math.Min(LegLength,z+.7f));
            return localX> -12?leg:Length-leg;
        }
        public static float ViewHeight(int lower,float localX,float z)
        {
            // Rebase the fixed projection only while every storey is hidden. This prevents
            // the persistent keeper/stair image from jumping at the logical floor boundary.
            float start=FadeEndZ+.7f;
            float across=Math.Max(0,Math.Min(1,(-11-localX)/2));
            float landing=Smooth((LegLength+2*across-start)/(Length-2*start));
            float approach=Smooth((z-FadeEndZ)/(3.5f-FadeEndZ));
            float side=localX> -12?0:1;
            // Unlike the navigation centreline, this 2D blend is continuous when walking
            // sideways near a corner or hugging either rail. The side split is only reachable
            // after approach reaches one, so it cannot cause a visible discontinuity.
            float height=side+(landing-side)*approach;
            return HotelLayout.Height(lower)+HotelLayout.StoreyHeight*height;
        }
    }
}
