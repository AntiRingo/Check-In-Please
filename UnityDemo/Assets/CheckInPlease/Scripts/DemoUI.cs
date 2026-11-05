using System;
using System.Linq;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        readonly Color ink=new Color(.09f,.12f,.12f),cream=new Color(.77f,.76f,.68f),dim=new Color(.74f,.78f,.74f),
            textColor=new Color(.94f,.93f,.87f),
            panel=new Color(.075f,.10f,.105f,.97f),accent=new Color(.48f,.26f,.22f),line=new Color(.27f,.33f,.32f);
        Texture2D disk, paperArt, counterArt;
        Font headingFont;
        GUIStyle label,button;
        Vector2 subjectScroll,dispatchScroll;
        void InitUI()
        {
            paperArt=Resources.Load<Texture2D>("Art/Paper");counterArt=Resources.Load<Texture2D>("Art/Counter");
            headingFont=Font.CreateDynamicFontFromOSFont(new[]{"SimSun","Noto Serif CJK SC","Microsoft YaHei"},32);
            label=new GUIStyle(GUI.skin.label){font=font,wordWrap=true,richText=false};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,alignment=TextAnchor.MiddleCenter,wordWrap=true,
                border=new RectOffset(0,0,0,0),padding=new RectOffset(8,8,4,4)};
            disk=new Texture2D(128,128,TextureFormat.RGBA32,false);var p=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)p[y*128+x]=new Color(1,1,1,Mathf.Clamp01(63.5f-new Vector2(x-63.5f,y-63.5f).magnitude));
            disk.SetPixels(p);disk.Apply();
        }
        void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Circle(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,disk);GUI.color=old;}
        void L(float x,float y,float w,float h,string text,int size=20,Color? color=null,TextAnchor align=TextAnchor.UpperLeft)
        {label.font=size>=26?headingFont:font;label.fontSize=size;label.normal.textColor=color??textColor;label.alignment=align;GUI.Label(new Rect(x,y,w,h),text,label);}
        void Edge(Rect r,Color c){Fill(new Rect(r.x,r.y,r.width,1),c);Fill(new Rect(r.x,r.yMax-1,r.width,1),c);Fill(new Rect(r.x,r.y,1,r.height),c);Fill(new Rect(r.xMax-1,r.y,1,r.height),c);}
        bool B(float x,float y,float w,float h,string text,bool enabled=true,bool red=false)
        {
            var r=new Rect(x,y,w,h);bool hover=r.Contains(Event.current.mousePosition);
            Fill(new Rect(x+2,y+3,w,h),new Color(0,0,0,.35f));
            Fill(r,enabled?(red?accent:hover?line:new Color(.14f,.18f,.18f,.95f)):new Color(.10f,.13f,.13f));
            Edge(r,enabled?new Color(.48f,.48f,.41f):line);Edge(new Rect(x+4,y+4,w-8,h-8),new Color(.54f,.53f,.46f,.3f));
            button.normal.background=button.hover.background=button.active.background=Texture2D.blackTexture;
            button.normal.textColor=button.hover.textColor=button.active.textColor=enabled?textColor:new Color(.55f,.60f,.57f);
            var old=GUI.backgroundColor;GUI.backgroundColor=Color.clear;GUI.enabled=enabled;
            bool clicked=GUI.Button(r,text,button);GUI.enabled=true;GUI.backgroundColor=old;return clicked;
        }
        void Sheet(Rect r,string code)
        {
            PaperBase(r);Edge(new Rect(r.x+15,r.y+12,r.width-30,r.height-24),new Color(.32f,.34f,.29f,.45f));
            L(r.x+24,r.y+15,r.width-48,30,code,14,ink);
            for(float y=r.y+53;y<r.yMax-18;y+=28)Fill(new Rect(r.x+22,y,r.width-44,1),new Color(.18f,.22f,.19f,.12f));
        }
        void PaperBase(Rect r)
        {
            Fill(new Rect(r.x+5,r.y+6,r.width,r.height),new Color(0,0,0,.35f));
            if(paperArt!=null)GUI.DrawTextureWithTexCoords(r,paperArt,new Rect(.135f,.02f,.71f,.95f));else Fill(r,cream);
        }
        void Stamp(Rect r,string text,float rotation=-8)
        {
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(rotation,r.center);
            var c=new Color(.37f,.17f,.13f,.8f);Edge(r,c);Edge(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),c);
            L(r.x+8,r.y+5,r.width-16,r.height-10,text,26,c,TextAnchor.MiddleCenter);GUI.matrix=matrix;
        }
        void Emblem(float x,float y,float size,Color c)
        {
            Circle(new Rect(x,y,size,size),c);Circle(new Rect(x+3,y+3,size-6,size-6),panel);
            for(int i=0;i<3;i++)Fill(new Rect(x+size*.25f+i*size*.19f,y+size*.25f,size*.09f,size*.5f),c);
        }
        void Portrait(Rect r,int variant,bool light=false)
        {
            Fill(r,light?new Color(.40f,.43f,.39f):new Color(.12f,.15f,.15f));
            GUI.DrawTexture(r,world.PortraitTexture(variant),ScaleMode.ScaleToFit,true);Edge(r,new Color(.28f,.30f,.27f));
        }
        void OnGUI()
        {
            if(game==null)return;if(label==null)InitUI();
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(modal=="review"||modal=="dossier")GUI.DrawTexture(new Rect(0,0,1600,900),counterArt);
            Header();
            if(modal=="")WorldHUD();
            else
            {
                if(modal!="review"&&modal!="dossier")Fill(new Rect(0,88,1600,812),new Color(.015f,.025f,.03f,modal=="menu"?.22f:.70f));
                switch(modal)
                {
                    case "menu":Menu();break;case "brief":Brief();break;case "review":Review();break;case "dispatch":Dispatch();break;
                    case "item":Inspect();break;case "dossier":Dossier();break;case "rooms":Rooms();break;
                    case "journal":Journal();break;case "help":Help();break;case "ending":Ending();break;case "pause":Pause();break;
                }
            }
            if(Time.unscaledTime<toastUntil && toast!="" && modal!="" && modal!="ending" && modal!="brief")
            {float y=modal=="review"?300:modal=="dossier"?174:modal=="item"?216:76;
             Fill(new Rect(355,y,990,54),panel);Fill(new Rect(355,y,4,54),accent);L(375,y+6,950,44,toast,17);}
            GUI.matrix=Matrix4x4.identity;
        }
        void Header()
        {
            Fill(new Rect(0,0,1600,70),new Color(.04f,.055f,.055f,.78f));Fill(new Rect(28,69,1544,1),new Color(.62f,.60f,.51f,.5f));
            L(30,8,480,40,"check in, please",31);L(32,46,500,20,"第 17 区   /   北境共同体   /   经营许可证 044",12,dim);
            Emblem(560,17,35,cream);
            string[] phases={"晨间简报","日间接待","宵禁搜查","夜间审计","试营业结束"};
            L(615,18,330,35,"第 0"+game.State.day+" 日  ·  "+phases[(int)game.State.phase],22);
            L(990,20,570,34,"现金 "+game.State.money+"   配给 "+game.State.rations+"   信任 "+game.State.trust+"   暴露 "+game.State.heat,19,cream);
        }
        void Title(string small,string title,string subtitle)
        {L(80,118,1000,26,small,15,dim);L(80,150,1300,52,title,36);L(82,208,1250,50,subtitle,19,textColor);}
        void Close(){if(B(1400,114,120,42,"返回"))LeaveModal();}
        void Dispatch()
        {
            Title("INCOMING MAIL  /  CONSEQUENCES","昨夜的回音","你的选择已经离开这间旅店。");
            Sheet(new Rect(170,290,1260,440),"第 17 区   /   送达回执");
            dispatchScroll=GUI.BeginScrollView(new Rect(205,350,1190,335),dispatchScroll,new Rect(0,0,1140,Mathf.Max(330,game.State.news.Count*95)));
            for(int i=0;i<game.State.news.Count;i++)L(5,i*95,1125,90,game.State.news[i],23,ink);
            GUI.EndScrollView();
            if(B(1080,750,350,55,"收好回执，继续营业",true,true))modal="";
        }
        void Menu()
        {
            Fill(new Rect(80,150,640,610),panel);Emblem(120,192,62,cream);
            L(120,285,550,95,"check in, please",53);L(122,378,520,70,"每个人都需要一间房。\n每间房都有一个秘密。",25);
            L(122,493,510,85,"三日可玩原型\n固定俯视场景 · 证件审查 · 客房搜查 · 情报处置",18,dim);
            if(B(120,605,245,58,confirmNew?"确认覆盖旧存档":"开始新游戏",true,true))
            {if(System.IO.File.Exists(SavePath)&&!confirmNew)confirmNew=true;else NewGame();}
            if(B(385,605,245,58,"继续游戏",System.IO.File.Exists(SavePath)))Continue();
            if(B(120,684,245,42,"操作说明"))modal="help";
            if(B(385,684,245,42,"退出"))Application.Quit();
            L(1030,615,420,90,"秩 序 即 安 宁\n\n请将登记表置于视线之内。",23,dim);
        }
        void Brief()
        {
            Title("MORNING BULLETIN  /  06:30","第 "+game.State.day+" 日 · 营业之前","旅店尚未开门。新的规定已经生效。");
            Sheet(new Rect(80,288,690,440),"管理局文件   /   通告 0"+game.State.day);
            L(115,350,610,50,"今日入住规定",29,ink);L(115,420,610,220,game.Rules,25,ink);
            Fill(new Rect(810,288,710,440),panel);
            L(845,320,640,40,"值班提示",26);L(845,378,640,150,"每天营业支出：20 现金 + 2 配给\n合规入住收入：每人 14 现金\n每夜可搜查 4 处，同一物品不会重复消耗行动。\n举报需要同一住客的 2 个独立来源。",21);
            L(845,560,640,140,game.State.day==1?"许可证将在三日后复核。你可以严格执行，也可以承担放行的代价。":"昨夜的决定可能在开门后收到回执。\n新规从今日开始追溯检查新登记。",21,dim);
            if(B(1080,750,440,56,"缴纳营业支出，开始接待",true,true))StartDay();
        }
        void WorldHUD()
        {
            string area=world.Area=="lobby"?"旅店大厅":world.Area=="office"?"值班办公室":"客房 "+world.Area;
            L(38,91,300,40,area,28);L(40,134,300,20,"点击地面移动 · 点击标记互动",13,dim);
            if(B(1360,90,205,38,"客房分配 / 地图"))modal="rooms";
            if(B(1360,138,98,34,"日志 [J]"))modal="journal";
            if(B(1468,138,97,34,"暂停"))modal="pause";
            foreach(var spot in world.spots.ToArray())
            {
                var p=world.Project(spot.point);
                float width=spot.label.Length>7?205:160;
                if(B(Mathf.Clamp(p.x-width/2,25,1575-width),p.y-18,width,36,"◇  "+spot.label)) {var target=spot;world.WalkTo(target.approach,()=>Act(target.action));}
            }
            string objective=game.State.phase==Shift.Reception?(game.Current!=null?"前往前台审查  "+game.Current.name:"今日来客已处理完毕。开始夜班。"):
                game.State.phase==Shift.Search?"住客正在统一点名。剩余搜查行动  "+game.State.actions+" / 4":"前往办公室，决定每份档案的去向。";
            Fill(new Rect(28,817,1020,65),new Color(.035f,.045f,.045f,.86f));
            L(44,823,990,27,objective,20);
            L(44,853,990,24,Time.unscaledTime<toastUntil&&toast!=""?toast:world.Walking?"正在前往目标……":"J 值班日志   /   Esc 暂停",14,dim);
            if(game.State.phase==Shift.Reception && game.Current==null){if(B(1140,830,425,48,"关闭前台 · 开始夜班",true,true))Night();}
            else if(game.State.phase==Shift.Search){if(B(1140,830,425,48,"结束搜查 · 前往办公室",true,true))Audit();}
            else if(game.State.phase==Shift.Audit){if(B(1140,830,425,48,"打开档案",true,true)){Visit("office");Act("dossier");}}
            if(Event.current.type==EventType.MouseDown && Event.current.button==0)
            {
                var p=Event.current.mousePosition;
                if(p.y>175&&p.y<815){world.GroundClick(p);Event.current.Use();}
            }
        }
        void Review()
        {
            var g=game.Current;if(g==null){modal="";return;}
            L(35,91,350,45,"前台审查",32);Close();
            GUI.BeginGroup(new Rect(610,88,360,295));
            GUI.DrawTexture(new Rect(10,0,340,400),world.PortraitTexture(g.portrait),ScaleMode.ScaleToFit,true);GUI.EndGroup();
            Fill(new Rect(1015,296,390,70),new Color(.03f,.045f,.045f,.8f));
            L(1030,303,365,28,g.name+"  /  "+g.occupation,23);L(1030,336,365,26,"来访目的："+g.purpose,16,cream);
            Sheet(new Rect(130,425,365,285),"北境共同体   /   身份证明");
            Portrait(new Rect(155,482,100,123),g.portrait,true);
            L(277,485,190,40,g.name,27,ink);L(277,534,185,66,"公民编号\n"+g.id,21,ink);
            L(158,627,305,65,"职业："+g.occupation+"\n签发机关：第 17 区",18,ink);
            Sheet(new Rect(515,425,365,285),"人员流动管理局  /  通行许可");
            L(544,486,300,168,"持有人："+g.name+"\n登记号："+g.permitId+"\n有效至：第 "+g.expires+" 日\n劳动许可："+(g.workPermit?"持有":"未持有"),22,ink);
            Stamp(new Rect(724,651,117,38),"待核验",-7);
            Sheet(new Rect(900,425,280,285),"旅店 044   /   住宿登记");
            L(927,486,230,120,"姓名："+g.name+"\n访客计划："+(g.visitor?"有":"无")+"\n预分配房间",20,ink);
            L(930,618,220,60,roomChoice==0?"客满":roomChoice.ToString(),38,ink,TextAnchor.MiddleCenter);
            Sheet(new Rect(1205,425,310,285),"当日生效   /   第 "+game.State.day+" 日");
            L(1232,477,255,40,"核对规则",27,ink);L(1232,526,255,163,game.Rules.Replace("。",""),16,ink);
            if(B(130,730,280,44,"对照证件与条例"))comparison=game.Violation(g)==""?"核对结果：未发现证件违规。":"核对结果："+game.Violation(g);
            L(432,735,720,42,comparison,20,comparison.Contains("未发现")?cream:new Color(.83f,.64f,.54f));
            if(B(130,801,280,58,"询 问 住 客"))Say(g.statement);
            L(430,811,80,34,"房间",18);
            for(int n=201;n<=203;n++)
            {bool free=game.InRoom(n)==null;if(B(505+(n-201)*110,801,100,58,(roomChoice==n?"● ":"")+n,free))roomChoice=n;}
            if(B(875,801,300,58,"准 予 入 住  /  +14"))Decision(true);
            if(B(1205,801,310,58,"拒 绝 入 住",true,true))Decision(false);
        }
        void Inspect()
        {
            if(inspecting==null){modal="";return;}
            var g=game.State.guests.First(x=>x.id==inspecting.guestId);
            Fill(new Rect(62,105,1476,705),panel);Edge(new Rect(68,111,1464,693),cream);
            L(100,132,1100,50,"物品查看   /   "+inspecting.title,33);Close();
            L(102,187,1100,30,g.room+" 房   ·   "+g.name+"   ·   "+inspecting.source,18,dim);
            PaperBase(new Rect(108,365,430,337));
            Fill(new Rect(125,474,394,1),new Color(.25f,.27f,.23f,.5f));
            L(167,510,320,50,"来源物品封套",27,ink,TextAnchor.MiddleCenter);
            L(167,572,320,70,g.room+"  /  "+g.id,30,ink,TextAnchor.MiddleCenter);
            Stamp(new Rect(220,408,180,50),"内 部 留 存",-12);
            Sheet(new Rect(570,246,552,510),flipped?"背面  /  来源核验":"北境共同体   /   内部材料   /   禁止转录");
            L(612,316,465,80,flipped?"物品标记":inspecting.title,32,ink);
            L(612,415,465,225,flipped?"封口有磨损。物品已原位放回。\n\n记录编号："+inspecting.id+"\n独立来源："+inspecting.source:inspecting.text,25,ink);
            Stamp(new Rect(878,665,180,50),"已 记 录",-9);
            L(1170,270,330,45,"观察记录",29);Fill(new Rect(1170,325,325,1),line);
            L(1170,357,325,200,"已记录到情报档案\n\n关联住客："+g.name+"\n独立来源："+inspecting.source+"\n剩余行动："+game.State.actions,21);
            L(1170,590,325,115,"发现物品并不等于证明有罪。\n举报后的代价会落到真实的人身上。",19,dim);
            if(B(570,824,255,51,flipped?"查 看 正 面":"翻 到 背 面"))flipped=!flipped;
            if(B(845,824,320,51,"放 回 物 品",true,true))modal="";
        }
        void Dossier()
        {
            Fill(new Rect(38,101,1524,70),new Color(.03f,.045f,.045f,.8f));
            L(70,113,900,50,"住客报告   /   夜间档案",34);Close();
            Fill(new Rect(70,202,330,572),panel);Edge(new Rect(77,209,316,558),line);
            L(99,226,265,40,"线 索 索 引",27);
            var ids=game.State.clues.Where(CanUse).Select(c=>c.guestId).Distinct().ToArray();
            if(ids.Length==0)L(104,310,250,140,"暂无可处置材料。\n可直接结束夜班。",20,dim);
            subjectScroll=GUI.BeginScrollView(new Rect(94,290,280,437),subjectScroll,new Rect(0,0,255,Mathf.Max(432,ids.Length*65)));
            for(int i=0;i<ids.Length;i++)
            {var g=game.State.guests.First(x=>x.id==ids[i]);if(B(0,i*65,250,55,(subject==g.id?"● ":"")+g.name+" / "+g.room)){subject=g.id;selected.Clear();}}
            GUI.EndScrollView();
            PaperBase(new Rect(432,214,635,567));PaperBase(new Rect(425,202,635,567));
            L(466,230,545,50,"举 报 档 案",32,ink);Fill(new Rect(466,285,552,1),ink);
            var guest=game.State.guests.FirstOrDefault(g=>g.id==subject);
            if(guest!=null)
            {Portrait(new Rect(466,309,105,125),guest.portrait,true);L(601,319,395,95,guest.name+"  /  "+guest.id+"\n房间  "+guest.room,25,ink);}
            L(466,455,540,35,"关联材料  /  勾选独立来源",23,ink);
            var list=game.State.clues.Where(c=>c.guestId==subject&&CanUse(c)).ToArray();
            for(int i=0;i<list.Length;i++)
            {
                var c=list[i];float y=505+i*113;bool check=selected.Contains(c.id);
                Edge(new Rect(466,y,550,98),new Color(.3f,.31f,.27f,.6f));
                L(481,y+9,445,27,"来源："+c.source+" / 第 "+c.day+" 日",15,ink);L(481,y+42,430,44,c.title,23,ink);
                if(B(938,y+28,59,47,check?"✓":"＋")){if(check)selected.Remove(c.id);else selected.Add(c.id);}
            }
            Sheet(new Rect(1090,230,440,540),"第 17 区   /   档案流转单");L(1125,290,370,50,"提 交 检 查",31,ink);
            int count=list.Count(c=>selected.Contains(c.id));L(1125,355,370,66,"已选 "+count+" 份材料\n举报至少需要 2 个独立来源",20,ink);
            if(B(1120,443,380,53,"提交管理局 / 配给 +3",count>=2,true))Resolve(Disposition.Reported);
            if(B(1120,511,380,53,"交给地下联络人 / 现金 +18",count>0))Resolve(Disposition.Leaked);
            if(B(1120,579,180,47,"封存材料",count>0))Resolve(Disposition.Withheld);
            if(B(1320,579,180,47,"销毁材料",count>0))Resolve(Disposition.Destroyed);
            L(1125,656,370,90,"举报：信任 +1，次日出现回执。\n外泄：暴露 +2，次日额外 +1。\n每份材料只能兑现一次。",17,ink);
            if(B(1100,818,425,57,"完成审计 · 结束本日",true,true))EndDay();
        }
        void Rooms()
        {
            Title("HOTEL PLAN  /  ROOM REGISTER","旅店区域","点击区域切换固定摄像头。夜间可进入已入住的客房。");Close();
            if(B(80,300,690,80,"一层大厅  /  前台接待"))Visit("lobby");
            if(B(810,300,710,80,"值班办公室  /  情报档案"))Visit("office");
            for(int n=201;n<=203;n++)
            {
                float x=80+(n-201)*490;var g=game.InRoom(n);Fill(new Rect(x,430,460,300),panel);
                L(x+28,458,400,55,n.ToString(),40);L(x+28,533,400,90,g==null?"空房\n尚未分配":g.name+"\n"+g.occupation,23,dim);
                if(B(x+28,650,404,50,"进入客房",g!=null&&game.State.phase==Shift.Search))Visit(n.ToString());
            }
        }
        void Journal()
        {
            Title("LEDGER  /  CONSEQUENCES","值班日志","登记、线索与已经到达的后果。最新记录在前。");Close();
            Fill(new Rect(80,280,1440,490),panel);
            var logs=game.State.journal.AsEnumerable().Reverse().ToArray();
            journalScroll=GUI.BeginScrollView(new Rect(105,305,1390,440),journalScroll,new Rect(0,0,1330,Mathf.Max(435,logs.Length*60)));
            for(int i=0;i<logs.Length;i++){L(8,i*60,1300,53,logs[i],20);Fill(new Rect(8,i*60+55,1300,1),line);}
            GUI.EndScrollView();
        }
        void Help()
        {
            Title("KEEPER'S HANDBOOK","值班操作","这是一个可以完整走过三个营业日的玩法原型。");
            L(100,300,1350,390,"01  点击地面移动；点击场景标签，主角会走近并互动。\n\n02  前台：查看身份证、通行证与今日条例。询问住客，再批准或拒绝。\n\n03  接待完毕后进入夜班。地图进入客房，检查书桌与行李。每夜 4 次搜查。\n\n04  办公室：选择同一人的两份材料，可举报；也可以封存、销毁或外泄。\n\n05  完成审计进入次日。三日后根据现金、信任与暴露获得结局。\n\nJ 打开日志 · Esc 返回或暂停 · 每次重要操作自动保存。",23);
            if(B(1090,730,430,60,"返回",true,true))modal=game.State.phase==Shift.Morning?"menu":"";
        }
        void Pause()
        {
            Fill(new Rect(540,240,520,480),panel);L(580,275,440,70,"值班暂停",35);
            if(B(580,365,440,55,"继续值班",true,true))modal="";
            if(B(580,435,440,55,muted?"打开环境音":"关闭环境音")){muted=!muted;ambience.mute=muted;}
            if(B(580,505,440,55,"操作说明"))modal="help";
            if(B(580,575,440,55,"保存并返回标题")){Save();modal="menu";}
            if(B(580,645,440,45,"保存并退出")){Save();Application.Quit();}
        }
        void Ending()
        {
            Title("LICENSE REVIEW  /  END OF DEMO","三日值班结束","灯还亮着，广播还在继续。");
            Fill(new Rect(80,280,1440,440),panel);Emblem(125,325,70,cream);
            L(240,325,1180,160,game.State.ending,32);
            L(240,525,1180,80,"最终现金  "+game.State.money+"      配给  "+game.State.rations+"      管理局信任  "+game.State.trust+"      暴露  "+game.State.heat,24,dim);
            L(240,610,1150,60,"记录线索 "+game.State.clues.Count+" 份  ·  举报 "+game.State.clues.Count(c=>c.disposition==Disposition.Reported)+" 份  ·  外泄 "+game.State.clues.Count(c=>c.disposition==Disposition.Leaked)+" 份",21);
            if(B(820,750,330,55,"查看值班日志"))modal="journal";
            if(B(1180,750,340,55,"返回标题",true,true))modal="menu";
        }
    }
}
