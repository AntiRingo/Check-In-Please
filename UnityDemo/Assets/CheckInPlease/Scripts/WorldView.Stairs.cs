using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class WorldView
    {
        // A continuous walking surface shared by keyboard movement, click paths and picking.
        // Each connection is a U: right flight, half-height landing, left return flight.
        const float StairBottom=-.7f,StairTop=3.3f,StairTurn=3.8f,StairHalf=2.2f;
        const float StairLeg=StairTurn-StairBottom,StairLength=StairLeg*2+2;
        int activeStair,stairEntryFloor;
        public bool OnStairLanding {get;private set;}
        public int ActiveStairLowerFloor {get{return activeStair;}}
        public bool StairHalfVisible(int lower,bool lowerHalf)
        {return StairBranchVisible(lowerHalf?upstairs[lower]:downstairs[lower+1]);}

        bool StairSurface(int lower,Vector3 point,out Vector3 surface)
        {
            surface=point;float x=point.x-HotelLayout.StairOffset,z=point.z;
            if(z<=StairBottom||z>4.05f)return false;
            float h=HotelLayout.Height(lower);
            if(z>=3.5f&&x>=-13.48f&&x<=-10.52f)surface.y=h+StairHalf;
            else if(z<=3.5f&&Mathf.Abs(x+11)<=.48f)surface.y=h+StairHalf*Mathf.Clamp01((z-StairBottom)/4);
            else if(z<=3.5f&&Mathf.Abs(x+13)<=.48f)surface.y=h+4.4f-StairHalf*Mathf.Clamp01((z-StairBottom)/4);
            else return false;
            return true;
        }
        bool LocateWalkPoint(Vector3 point,out int floor,out int stair)
        {
            floor=0;stair=0;
            for(int f=1;f<=3;f++)if(FloorClear(point,f)){floor=f;return true;}
            for(int lower=1;lower<3;lower++)
                if(StairSurface(lower,point,out var surface)&&Mathf.Abs(surface.y-point.y)<.08f){stair=lower;return true;}
            return false;
        }
        bool StepOnWalkSurface(Vector3 candidate,out Vector3 next)
        {
            next=candidate;float distance=.09f;bool found=false;
            for(int floor=1;floor<=3;floor++)
            {
                var p=new Vector3(candidate.x,HotelLayout.Height(floor),candidate.z);
                float d=Mathf.Abs(p.y-candidate.y);
                if(d<=distance&&FloorClear(p,floor)){distance=d;next=p;found=true;}
            }
            for(int lower=1;lower<3;lower++)
            {
                if(!StairSurface(lower,candidate,out var p))continue;
                float d=Mathf.Abs(p.y-candidate.y);
                if(d<=distance){distance=d;next=p;found=true;}
            }
            return found;
        }
        void UpdateStairPosition()
        {
            if(!LocateWalkPoint(actor.position,out int floor,out int stair))return;
            bool wasClimbing=Climbing,wasLanding=OnStairLanding;int previousStair=activeStair;
            Climbing=stair!=0;activeStair=stair;
            OnStairLanding=Climbing&&actor.position.z>=StairTop;
            if(Climbing)
            {
                if(!wasClimbing)stairEntryFloor=CurrentFloor;
                floor=actor.position.x-HotelLayout.StairOffset< -12?stair+1:stair;
                // A small dead band avoids flickering between storeys on the middle of the landing.
                if(OnStairLanding&&Mathf.Abs(actor.position.x-HotelLayout.StairOffset+12)<.12f&&(CurrentFloor==stair||CurrentFloor==stair+1))floor=CurrentFloor;
            }
            else if(wasClimbing)
            {
                if(stairEntryFloor!=0&&floor!=stairEntryFloor)StairTripsCompleted++;
                stairEntryFloor=0;
            }
            if(floor!=CurrentFloor)SetCurrentFloor(floor);
            else if(wasClimbing!=Climbing||wasLanding!=OnStairLanding||previousStair!=activeStair)DisplayFloors();
        }
        float StairStation(Vector3 p)
        {
            return StairTransition.Station(p.x-HotelLayout.StairOffset,p.z);
        }
        Vector3 StairPoint(int lower,float station)
        {
            float x,z,h=HotelLayout.Height(lower);
            if(station<=StairLeg){x=-11;z=StairBottom+station;h+=StairHalf*Mathf.Clamp01((z-StairBottom)/4);}
            else if(station<StairLeg+2){x=-11-(station-StairLeg);z=StairTurn;h+=StairHalf;}
            else{x=-13;z=StairBottom+StairLength-station;h+=4.4f-StairHalf*Mathf.Clamp01((z-StairBottom)/4);}
            return new Vector3(x+HotelLayout.StairOffset,h,z);
        }
        void AppendStairPath(List<Vector3> route,int lower,Vector3 from,Vector3 to)
        {
            float start=StairStation(from),end=StairStation(to),direction=Mathf.Sign(end-start);
            route.Add(StairPoint(lower,start));
            // Keep the bends and slope/landing joins explicit; never cut across the central void.
            var joins=new[]{4f,StairLeg,StairLeg+2,StairLength-4};
            if(direction<0)Array.Reverse(joins);
            foreach(float join in joins)if((join-start)*direction>0&&(end-join)*direction>0)route.Add(StairPoint(lower,join));
            route.Add(StairPoint(lower,end));route.Add(to);
        }
        bool AppendFloorPath(List<Vector3> route,Vector3 from,Vector3 to,int floor)
        {
            var part=FloorPath(from,to,floor);if(part==null)return false;
            route.AddRange(part);return true;
        }
        List<Vector3> HotelPath(Vector3 from,Vector3 to)
        {
            if(!LocateWalkPoint(from,out int fromFloor,out int fromStair)||!LocateWalkPoint(to,out int toFloor,out int toStair))return null;
            var route=new List<Vector3>();
            if(fromStair!=0&&fromStair==toStair){AppendStairPath(route,fromStair,from,to);return route;}
            var cursor=from;
            if(fromStair!=0)
            {
                fromFloor=to.y>HotelLayout.Height(fromStair)+StairHalf?fromStair+1:fromStair;
                var exit=StairPoint(fromStair,fromFloor==fromStair?0:StairLength);
                AppendStairPath(route,fromStair,cursor,exit);
                cursor=StairApproach(fromFloor,fromFloor==fromStair?fromFloor+1:fromFloor-1);route.Add(cursor);
            }
            int entryFloor=toStair==0?toFloor:(fromFloor<=toStair?toStair:toStair+1);
            while(fromFloor!=entryFloor)
            {
                int next=HotelLayout.NextFloor(fromFloor,entryFloor),lower=Math.Min(fromFloor,next);
                var approach=StairApproach(fromFloor,next);
                if(!AppendFloorPath(route,cursor,approach,fromFloor))return null;
                var enter=StairPoint(lower,next>fromFloor?0:StairLength);
                var exit=StairPoint(lower,next>fromFloor?StairLength:0);
                route.Add(enter);AppendStairPath(route,lower,enter,exit);
                cursor=StairApproach(next,fromFloor);route.Add(cursor);fromFloor=next;
            }
            if(toStair!=0)
            {
                var approach=StairApproach(entryFloor,entryFloor==toStair?entryFloor+1:entryFloor-1);
                if(!AppendFloorPath(route,cursor,approach,entryFloor))return null;
                var enter=StairPoint(toStair,entryFloor==toStair?0:StairLength);route.Add(enter);
                AppendStairPath(route,toStair,enter,to);
            }
            else if(!AppendFloorPath(route,cursor,to,toFloor))return null;
            return route;
        }
        bool PickWalkSurface(Vector2 canvas,out Vector3 point)
        {
            var ray=view.ViewportPointToRay(new Vector3(canvas.x/1600,1-canvas.y/900,0));
            point=Vector3.zero;float nearest=float.MaxValue;bool found=false;
            var floorPlane=new Plane(Vector3.up,Vector3.up*HotelLayout.Height(CurrentFloor));
            float distance;
            if(FloorOpacity>.15f&&floorPlane.Raycast(ray,out distance))
            {
                var p=ray.GetPoint(distance);
                if(FloorClear(p)){point=p;nearest=distance;found=true;}
            }
            for(int lower=1;lower<3;lower++)foreach(bool lowerHalf in new[]{true,false})
            {
                if(!StairHalfVisible(lower,lowerHalf))continue;
                float h=HotelLayout.Height(lower);
                var slope=new Plane(new Vector3(0,1,lowerHalf?-.55f:.55f),new Vector3(0,h+(lowerHalf?0:4.4f),StairBottom));
                if(slope.Raycast(ray,out distance)&&distance<nearest)
                {
                    var p=ray.GetPoint(distance);float x=(lowerHalf?-11:-13)+HotelLayout.StairOffset;
                    if(p.z>StairBottom&&p.z<StairTop&&Mathf.Abs(p.x-x)<=.48f)
                    {point=p;nearest=distance;found=true;}
                }
                var landing=new Plane(Vector3.up,Vector3.up*(h+StairHalf));
                if(landing.Raycast(ray,out distance)&&distance<nearest)
                {
                    var p=ray.GetPoint(distance);
                    if(p.z>=StairTop&&p.z<=4.05f&&p.x-HotelLayout.StairOffset>=(lowerHalf?-12.15f:-13.48f)&&p.x-HotelLayout.StairOffset<=(lowerHalf?-10.52f:-11.85f)&&StairSurface(lower,p,out var surface))
                    {point=surface;nearest=distance;found=true;}
                }
            }
            return found;
        }
    }
}
