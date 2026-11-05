using System;
using System.Collections.Generic;
using System.Linq;

namespace CheckInPlease
{
    public enum Shift { Morning, Reception, Search, Audit, Finished }
    public enum Decision { Waiting, Admitted, Rejected }
    public enum Disposition { Unresolved, Withheld, Reported, Leaked, Destroyed }

    [Serializable] public class Guest
    {
        public string id, name, occupation, purpose, permitId, statement;
        public int day, expires, room, portrait;
        public bool workPermit, visitor;
        public Decision decision;
        public string[] items;
    }
    [Serializable] public class Clue
    {
        public string id, guestId, source, title, text;
        public int day;
        public Disposition disposition;
    }
    [Serializable] public class Consequence
    {
        public int dueDay, money, trust, heat;
        public string text;
    }
    [Serializable] public class GameState
    {
        public int version = 1, day = 1, money = 72, rations = 8, trust = 5, heat, actions = 4;
        public Shift phase = Shift.Morning;
        public string ending = "";
        public List<Guest> guests = new List<Guest>();
        public List<Clue> clues = new List<Clue>();
        public List<Consequence> pending = new List<Consequence>();
        public List<string> journal = new List<string>();
        public List<string> news = new List<string>();
        public List<RoomObservation> rooms = new List<RoomObservation>();
        public int keeperFloor = 1;
        public float keeperX = -1.2f, keeperZ = -1.8f;
    }

    // Pure C# rules: UI, saves and automated tests all use this same model.
    public sealed class GameModel
    {
        public GameState State { get; private set; }
        public GameModel(GameState state = null)
        {
            State = state ?? NewState();
            // Older v1 saves did not contain observation settings.
            if (State.rooms == null) State.rooms = new List<RoomObservation>();
            foreach (int room in HotelLayout.Rooms)
            {
                var observation = State.rooms.Find(r => r != null && r.room == room);
                if (observation == null)
                {
                    observation = new RoomObservation { room = room, cameraInstalled = room == 201 };
                    State.rooms.Add(observation);
                }
                observation.Normalize();
            }
            if(State.keeperFloor<1||State.keeperFloor>3)State.keeperFloor=1;
            if(float.IsNaN(State.keeperX)||float.IsInfinity(State.keeperX)||Math.Abs(State.keeperX)>14)State.keeperX=-1.2f;
            if(float.IsNaN(State.keeperZ)||float.IsInfinity(State.keeperZ)||State.keeperZ< -3.2f||State.keeperZ>6.5f)State.keeperZ=-1.8f;
        }
        public RoomObservation Observation(int room) { return State.rooms.Find(r => r != null && r.room == room); }
        static Guest G(int day, string id, string name, string job, string purpose, string permit,
            int expires, bool work, bool visitor, int portrait, string statement, params string[] items)
        {
            return new Guest { day = day, id = id, name = name, occupation = job, purpose = purpose,
                permitId = permit, expires = expires, workPermit = work, visitor = visitor,
                portrait = portrait, statement = statement, items = items };
        }
        static GameState NewState()
        {
            var s = new GameState();
            s.guests.AddRange(new[] {
                G(1,"A-017","伊莲娜","纺织工","探望妹妹","A-017",4,true,false,1,
                    "她住在北区医院。我只待一夜。信封里的东西……是家事。",
                    "配给申请回执|申请：儿童退烧药。审批结果：暂缓。申请人 A-017。",
                    "未寄出的信|‘如果委员会不批准，我会从旧车站想办法。不要告诉旅店的人。’"),
                G(1,"B-042","维克托","锅炉工","设备检修","B-024",5,true,false,2,
                    "登记员把数字写反了。锅炉停了，整条街都会挨冻。",
                    "维修清单|北区供热管道裂损。采购被以‘指标正常’为由驳回。",
                    "手绘管道图|图中标出未上报的地下通道。落款 B-042。"),
                G(1,"C-109","帕维尔","档案员","公务","C-109",3,true,false,0,
                    "不要为我安排临街的房间。广播总是吵得人无法入睡。",
                    "工作便笺|销毁目录第 41 至 53 页。不得保留副本。",
                    "夹层照片|一张旧合影。背面有几个被反复划掉的名字。"),
                G(2,"D-031","米拉","教师","寻找工作","D-031",5,false,false,1,
                    "新的许可要在工作单位开，可他们又让我先找到工作。",
                    "退回的申请|任职许可：缺少单位推荐信。",
                    "儿童习作|‘我希望广播有一天会停下来。’红笔批注：请家长到校。"),
                G(2,"E-066","列夫","电工","设备检修","E-066",5,true,false,2,
                    "大厅的电幕线路不稳定。我只是来检查线路。",
                    "检修工单|电幕录音装置换新；旧元件须统一回收。",
                    "藏起的接线图|一个可以切断麦克风、却保留指示灯的接线方案。"),
                G(2,"F-204","安东","文书","公务","F-204",1,true,false,0,
                    "我来时证件还有效。铁路停运不该算在我头上。",
                    "铁路停运通知|因安全审查，北线暂停三日。",
                    "申诉材料|请求撤销对家人的劳动调配。附件：三份被退回的申诉。"),
                G(3,"G-008","索菲娅","护士","探亲","G-008",5,true,true,1,
                    "弟弟会来拿一件外套。他不会过夜。这也算访客吗？",
                    "探视登记|访客：弟弟。时间：宵禁后。物品：冬衣一件。",
                    "外套内袋|几张前往边界的车票。姓名处已被刮去。"),
                G(3,"H-071","尼古拉","测绘员","公务","H-071",6,true,false,2,
                    "请按程序办。我想我们都不需要多余的麻烦。",
                    "测绘记录|旅店客房、后门与通风口的精确尺寸。",
                    "密封公函|区域重整名单中包括这家旅店。签发日期：明日。"),
                G(3,"J-115","奥列格","退休工人","探亲","J-115",7,true,false,0,
                    "以前我们在这条街跳舞。你大概不记得了。",
                    "旧节目单|一场早已被禁止纪念的街头演出。",
                    "袖珍收音机|旋钮停在未登记的频段。机壳里藏着一张家人的照片。")
            });
            s.news.Add("北境共同体 · 第 17 区\n旅店经营许可证将在三日后复核。请维持配给、现金与管理局信任。");
            return s;
        }
        public IEnumerable<Guest> Today { get { return State.guests.Where(g => g.day == State.day); } }
        public Guest Current { get { return Today.FirstOrDefault(g => g.decision == Decision.Waiting); } }
        public Guest InRoom(int room) { return Today.FirstOrDefault(g => g.room == room && g.decision == Decision.Admitted); }
        public string Rules
        {
            get { return "01  身份号必须与通行证一致。\n02  通行证到期日不得早于今日。" +
                (State.day >= 2 ? "\n03  今日起，所有住客须持劳动许可。" : "") +
                (State.day >= 3 ? "\n04  宵禁期间禁止接待有访客计划的住客。" : ""); }
        }
        public string Violation(Guest g)
        {
            if (g.id != g.permitId) return "身份号与通行证不一致";
            if (g.expires < State.day) return "通行证已经过期";
            if (State.day >= 2 && !g.workPermit) return "缺少劳动许可";
            if (State.day >= 3 && g.visitor) return "违反访客禁令";
            return "";
        }
        void Log(string text) { State.journal.Add("第 " + State.day + " 日 · " + text); }
        void ApplyConsequences(int throughDay)
        {
            foreach (var e in State.pending.Where(e => e.dueDay <= throughDay).ToArray())
            {
                State.money += e.money; State.trust += e.trust; State.heat += e.heat;
                State.news.Add(e.text); Log(e.text); State.pending.Remove(e);
            }
        }
        public string StartDay()
        {
            if (State.phase != Shift.Morning) return "当前不能开始营业。";
            State.news.Clear();
            ApplyConsequences(State.day);
            State.money -= 20; State.rations -= 2; State.actions = 4;
            Log("缴纳营业支出 20；消耗配给 2。");
            if (CheckFailure()) return State.ending;
            State.phase = Shift.Reception; return "营业开始。今日的新规已放在前台。";
        }
        public string Decide(bool admit, int room)
        {
            var g = Current;
            if (State.phase != Shift.Reception || g == null) return "当前没有待审查住客。";
            if (admit && (!HotelLayout.IsRoom(room) || InRoom(room) != null)) return "请选择二楼或三楼的空房间。";
            var violation = Violation(g);
            g.decision = admit ? Decision.Admitted : Decision.Rejected;
            if (admit)
            {
                g.room = room; State.money += 14;
                Log(g.name + " 入住 " + room + "，房费 +14。");
                if (violation != "") State.pending.Add(new Consequence { dueDay = State.day + 1, money = -10, trust = -2,
                    text = "复核通知：" + g.name + " 的登记违反规定（" + violation + "）。罚款 10，信任 -2。" });
            }
            else { Log("拒绝 " + g.name + " 入住。"); }
            return admit ? "已分配 " + room + " 房。房费 +14。" : "住客离开了旅店。";
        }
        public string BeginNight()
        {
            if (State.phase != Shift.Reception || Current != null) return "先处理完今天的来客。";
            State.phase = Shift.Search; Log("宵禁开始。住客前往统一点名，搜查机会为 4 次。");
            return "住客暂时离开。每搜查一处消耗一次行动。";
        }
        public Clue FindClue(int room, int source, out string error)
        {
            error = ""; var g = InRoom(room);
            if (State.phase != Shift.Search || g == null) { error = "只能在夜间搜查已入住的客房。"; return null; }
            if (source < 0 || source > 1) { error = "没有可搜查的物品。"; return null; }
            string id = g.id + "/" + source;
            var old = State.clues.FirstOrDefault(c => c.id == id);
            if (old != null) return old;
            if (State.actions <= 0) { error = "今晚的搜查时间已用完。请返回办公室处理情报。"; return null; }
            State.actions--;
            string[] parts = g.items[source].Split('|');
            var c = new Clue { id = id, guestId = g.id, source = source == 0 ? "书桌抽屉" : "行李夹层",
                title = parts[0], text = parts[1], day = State.day };
            State.clues.Add(c); Log("在 " + room + " 的" + c.source + "记录了“" + c.title + "”。");
            return c;
        }
        public string BeginAudit()
        {
            if (State.phase != Shift.Search) return "还未进入夜间。";
            State.phase = Shift.Audit; return "请逐人整理证据。同一人的两个独立来源，才能向管理局提交。";
        }
        public string Resolve(string guestId, Disposition choice, IEnumerable<string> selected)
        {
            if (State.phase != Shift.Audit) return "请先结束搜查。";
            var ids = new HashSet<string>(selected ?? new string[0]);
            var clues = State.clues.Where(c => c.guestId == guestId && ids.Contains(c.id) &&
                (c.disposition == Disposition.Unresolved || c.disposition == Disposition.Withheld)).ToList();
            if (clues.Count == 0) return "请选择尚未处置的线索。";
            if (choice == Disposition.Unresolved) return "请选择处置方式。";
            if (choice == Disposition.Reported && clues.Select(c => c.source).Distinct().Count() < 2)
                return "举报需要同一住客的两个独立来源。";
            foreach (var c in clues) c.disposition = choice;
            string result;
            switch (choice)
            {
                case Disposition.Reported:
                    State.rations += 3; State.trust += 1;
                    State.pending.Add(new Consequence { dueDay = State.day + 1,
                        text = "管理局回执：" + State.guests.First(g => g.id == guestId).name + " 已被带走协助调查。房间空了下来。" });
                    result = "档案已提交。配给 +3，信任 +1。后续将在次日送达。"; break;
                case Disposition.Leaked:
                    State.money += 18; State.heat += 2;
                    State.pending.Add(new Consequence { dueDay = State.day + 1, heat = 1,
                        text = "匿名纸条：消息已经传出去。街对面的陌生人仍在观察旅店。暴露 +1。" });
                    result = "线索交给了地下联络人。现金 +18，暴露 +2。"; break;
                case Disposition.Destroyed: result = "线索已销毁。不会再出现在可提交档案中。"; break;
                default: result = "线索已封存，可以在之后的夜晚重新处理。"; break;
            }
            Log(result); return result;
        }
        public string EndDay()
        {
            if (State.phase != Shift.Audit) return "请先在办公室完成夜间审计。";
            foreach (var c in State.clues.Where(c => c.disposition == Disposition.Unresolved)) c.disposition = Disposition.Withheld;
            if (State.day >= 3) ApplyConsequences(State.day + 1);
            if (CheckFailure()) return State.ending;
            if (State.day >= 3)
            {
                State.phase = Shift.Finished;
                State.ending = State.heat >= 6 ? "被注视的人\n旅店仍然营业，但你的名字已出现在另一份档案上。" :
                    State.trust >= 7 ? "模范经营者\n许可证顺利续期。几位住客再也没有回来。" :
                    "尚未熄灭的灯\n你保住了旅店，也保留了一些没有上交的秘密。";
                Log("三日试营业结束。"); return State.ending;
            }
            State.day++; State.phase = Shift.Morning; return "夜班结束。未处置的线索已自动封存。";
        }
        bool CheckFailure()
        {
            if (State.money < 0 || State.rations < 0 || State.trust <= 0)
            {
                State.phase = Shift.Finished;
                State.ending = State.trust <= 0 ? "许可证被吊销\n管理局不再信任你的登记。" : "停业通知\n现金或配给耗尽，旅店被暂时封存。";
                return true;
            }
            return false;
        }
    }
}
