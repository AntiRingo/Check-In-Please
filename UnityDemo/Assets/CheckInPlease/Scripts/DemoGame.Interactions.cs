using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        readonly List<Hotspot> nearbyInteractions=new List<Hotspot>();
        string interactionId="";
        int interactionFrame=-1;
        Hotspot FocusedInteraction {get{return nearbyInteractions.FirstOrDefault(s=>s.Id==interactionId);}}
        void RefreshInteractions()
        {
            nearbyInteractions.Clear();
            if(modal!=""||world.IsTravelling){interactionId="";return;}
            nearbyInteractions.AddRange(world.spots.Where(world.CanInteract).OrderBy(world.InteractionDistance).ThenBy(s=>s.Id));
            if(!nearbyInteractions.Any(s=>s.Id==interactionId))interactionId=nearbyInteractions.FirstOrDefault()?.Id??"";
        }
        void CycleInteraction(int direction)
        {
            RefreshInteractions();if(nearbyInteractions.Count<2)return;
            int index=nearbyInteractions.FindIndex(s=>s.Id==interactionId);
            interactionId=nearbyInteractions[(index+(direction>=0?1:-1)+nearbyInteractions.Count)%nearbyInteractions.Count].Id;
        }
        string InteractionAction(Hotspot spot)
        {
            if(spot.action=="review")return game.State.phase==Shift.Reception&&game.Current==null?"开始夜班":"开始登记";
            if(spot.action=="dossier")return "使用举报电脑";
            if(spot.action.StartsWith("room"))return game.Observation(spot.room).doorOpen?"关门":"开门";
            if(spot.action=="camera")return game.Observation(spot.room).cameraInstalled?"调整摄像头":"安装摄像头";
            return spot.action=="search0"?"搜查书桌":"搜查行李";
        }
        void PerformInteraction()
        {
            RefreshInteractions();var spot=FocusedInteraction;
            if(spot==null||!world.CanInteract(spot)||interactionFrame==Time.frameCount)return;
            interactionFrame=Time.frameCount;world.StopWalking();
            if(spot.room!=0)world.SelectRoom(spot.room);
            if(spot.action.StartsWith("room"))
            {
                if(game.State.phase!=Shift.Search)Say("宵禁点名后才能打开客房门。");
                else{world.ToggleDoor();Save();}
            }
            else if(spot.action=="camera")
            {
                if(!game.Observation(spot.room).cameraInstalled&&game.State.phase==Shift.Search)FloorCamera(0);
                modal="camera";
            }
            else if(spot.action=="review"&&game.State.phase==Shift.Reception&&game.Current==null)Night();
            else Act(spot.action);
            RefreshInteractions();
        }
        void InteractionHUD()
        {
            if(FocusedInteraction==null)return;
            var placed=new List<Rect>();
            // Stable target order keeps buttons still when R or the wheel changes the highlight.
            foreach(var spot in nearbyInteractions.OrderBy(s=>s.Id).ToArray())
            {
                var screen=world.Project(spot.point);string action=InteractionAction(spot);
                float width=Mathf.Clamp(action.Length*15+24,66,134);
                float x=screen.x+10;if(x+width>1572)x=screen.x-width-10;
                x=Mathf.Clamp(x,28,1572-width);float y=Mathf.Clamp(screen.y-14,210,780);
                var rect=new Rect(x,y,width,28);
                for(int attempt=1;attempt<=8&&placed.Any(r=>r.Overlaps(new Rect(rect.x-3,rect.y-2,rect.width+6,rect.height+4)));attempt++)
                    rect.y=Mathf.Clamp(y+((attempt+1)/2)*(attempt%2==1?32:-32),210,780);
                placed.Add(rect);
                if(InteractionButton(rect,action,spot.Id==interactionId))
                {interactionId=spot.Id;PerformInteraction();break;}
                if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))Event.current.Use();
            }
        }
        bool InteractionButton(Rect rect,string action,bool focused)
        {
            Fill(new Rect(rect.x+1,rect.y+2,rect.width,rect.height),new Color(0,0,0,.4f));
            Fill(rect,focused?new Color(.37f,.42f,.35f,.98f):new Color(.07f,.09f,.09f,.93f));
            Edge(rect,focused?cream:new Color(.22f,.27f,.24f));
            int fontSize=button.fontSize;button.fontSize=15;
            var oldText=button.normal.textColor;var oldBackground=GUI.backgroundColor;
            button.normal.background=button.hover.background=button.active.background=Texture2D.blackTexture;
            button.normal.textColor=button.hover.textColor=button.active.textColor=focused?textColor:new Color(.50f,.55f,.51f);
            GUI.backgroundColor=Color.clear;
            bool clicked=GUI.Button(rect,action,button);
            GUI.backgroundColor=oldBackground;button.fontSize=fontSize;
            button.normal.textColor=button.hover.textColor=button.active.textColor=oldText;
            return clicked&&!smoke;
        }
        void CameraControls()
        {
            var s=game.Observation(world.SelectedRoom);Close();
            Title("房间监控",world.SelectedRoom+" · 摄像头",s.cameraInstalled?"调整安装位置、朝向和视角，观察对应的局部区域。":"宵禁点名后可在这里安装摄像头。");
            Fill(new Rect(315,300,970,340),panel);
            L(350,328,900,45,s.cameraInstalled?world.MountLabel+"  /  朝向 "+s.yaw.ToString("0")+"°  /  视角 "+s.fieldOfView.ToString("0")+"°":"尚未安装",25);
            bool night=game.State.phase==Shift.Search;
            if(B(350,403,270,52,s.cameraInstalled?"拆除摄像头":"安装摄像头",night))FloorCamera(0);
            if(B(650,403,270,52,"切换安装位置",night&&s.cameraInstalled))FloorCamera(1);
            if(B(350,492,270,52,"左转 15°",s.cameraInstalled))FloorCamera(-15);
            if(B(650,492,270,52,"右转 15°",s.cameraInstalled))FloorCamera(15);
            if(B(950,492,270,52,"视角 "+s.fieldOfView.ToString("0")+"°",s.cameraInstalled))FloorCamera(2);
            L(350,575,900,40,"家具不遮挡观察。监控无法代替走近物品搜查。",18,dim);
        }
    }
}
