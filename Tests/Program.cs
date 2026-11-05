using CheckInPlease;
using System.Text.Json;

int count=0;
void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
GameModel NightModel()
{
    var m=new GameModel();m.StartDay();while(m.Current!=null)m.Decide(true,201+m.Today.Count(g=>g.decision==Decision.Admitted));m.BeginNight();return m;
}
var m=new GameModel();m.StartDay();m.BeginNight();Check(m.State.phase==Shift.Reception,"Cannot skip unprocessed reception");
m.Decide(true,201);m.Decide(true,201);Check(m.Current.id=="B-042","Cannot double book room");
m.Decide(true,202);m.Decide(false,0);m.BeginNight();
var a=m.FindClue(201,0,out _);m.FindClue(201,0,out _);Check(m.State.actions==3&&m.State.clues.Count==1,"Repeated search has no extra cost or duplicate clue");
var b=m.FindClue(201,1,out _);m.BeginAudit();
int r=m.State.rations;m.Resolve("A-017",Disposition.Reported,new[]{a.id});Check(m.State.rations==r,"Report requires two independent sources");
m.Resolve("A-017",Disposition.Reported,new[]{a.id,b.id});Check(m.State.rations==r+3,"Valid report grants reward");
m.Resolve("A-017",Disposition.Reported,new[]{a.id,b.id});m.Resolve("A-017",Disposition.Leaked,new[]{a.id,b.id});
Check(m.State.rations==r+3&&m.State.heat==0,"Resolved evidence cannot reward twice or change destination");
m.EndDay();int cash=m.State.money;m.StartDay();Check(m.State.money==cash-30&&m.State.trust==4,"Incorrect admission produces next-day fine and trust loss");
Check(m.Violation(m.Current).Contains("劳动许可"),"Day two introduces labour permit rule");
var n=NightModel();n.FindClue(201,0,out _);n.FindClue(201,1,out _);n.FindClue(202,0,out _);n.FindClue(202,1,out _);
Check(n.FindClue(203,0,out _) == null && n.State.actions==0,"Four action night budget enforced");
n.BeginAudit();n.EndDay();Check(n.State.clues.All(c=>c.disposition==Disposition.Withheld),"Unresolved clues safely archived at day end");
var json=JsonSerializer.Serialize(n.State,new JsonSerializerOptions{IncludeFields=true});
var restored=JsonSerializer.Deserialize<GameState>(json,new JsonSerializerOptions{IncludeFields=true});
Check(restored.day==2&&restored.clues.Count==4&&restored.pending.Count==1,"State save roundtrip preserves clues and delayed consequences");
var poor=new GameModel();poor.State.money=19;poor.StartDay();Check(poor.State.phase==Shift.Finished,"Bankruptcy gives a defined ending");
var allReject=new GameModel();for(int d=1;d<=3;d++){allReject.StartDay();while(allReject.Current!=null)allReject.Decide(false,0);allReject.BeginNight();allReject.BeginAudit();allReject.EndDay();}
Check(allReject.State.phase==Shift.Finished&&allReject.State.day==3,"Reject-all path completes without evidence softlock");
var full=new GameModel();for(int d=1;d<=3;d++)
{
    full.StartDay();while(full.Current!=null)full.Decide(full.Violation(full.Current)=="",201+full.Today.Count(g=>g.decision==Decision.Admitted));
    full.BeginNight();var g=full.Today.First(x=>x.decision==Decision.Admitted);var c1=full.FindClue(g.room,0,out _);var c2=full.FindClue(g.room,1,out _);
    full.BeginAudit();full.Resolve(g.id,Disposition.Reported,new[]{c1.id,c2.id});full.EndDay();
}
Check(full.State.phase==Shift.Finished&&full.State.day==3&&full.State.trust==8,"Three-day gameplay loop reaches compliant ending");
var finalAudit=new GameModel();finalAudit.State.day=3;finalAudit.StartDay();
while(finalAudit.Current!=null)finalAudit.Decide(true,201+finalAudit.Today.Count(g=>g.decision==Decision.Admitted));
finalAudit.BeginNight();finalAudit.BeginAudit();int before=finalAudit.State.money;finalAudit.EndDay();
Check(finalAudit.State.money==before-10&&finalAudit.State.trust==3,"Final license review settles last-day violations");
Console.WriteLine($"{count} model checks passed.");
