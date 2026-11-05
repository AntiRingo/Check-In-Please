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
var vision=new RoomObservation{room=201,cameraInstalled=true};
Check(RoomSight.Visibility(vision,0,3.3f,0,-1.5f)>.99f,"Camera reveals objects in its directional footprint");
Check(RoomSight.Visibility(vision,-2.7f,6.4f,0,-1.5f)==0,"Camera does not reveal the entire room");
vision.yaw=315;
Check(RoomSight.Visibility(vision,0,3.3f,0,-1.5f)==0,"Rotating camera conceals the old view immediately");
vision.yaw=135;vision.mount=1;
Check(RoomSight.Visibility(vision,-1,4.3f,0,-1.5f)==0,"Changing installation point changes coverage");
vision.cameraInstalled=false;
Check(RoomSight.Visibility(vision,0,3.3f,0,-1.5f)==0,"Closed unmonitored room has no retained visibility");
vision.doorOpen=true;
Check(RoomSight.Visibility(vision,0,5,0,-1.5f)>.99f&&RoomSight.Visibility(vision,2.7f,.8f,0,-1.5f)==0,"Door aperture reveals only sight lines passing through the opening");
Check(RoomSight.Visibility(vision,0,5,5,-1.5f)==0,"Door view follows the keeper rather than revealing a whole room");
Check(RoomSight.Visibility(vision,4,3,0,-1.5f)==0,"Observation never crosses room boundaries");
vision.doorOpen=false;
Check(RoomSight.Visibility(vision,1,3,0,2)>.99f,"Keeper can directly inspect a room after entering");
var legacy=new GameState{rooms=null};var migrated=new GameModel(legacy);
Check(migrated.State.rooms.Count==6&&migrated.Observation(201).cameraInstalled&&!migrated.Observation(301).cameraInstalled,"Legacy saves receive all six rooms with safe observation defaults");
migrated.Observation(202).cameraInstalled=true;migrated.Observation(202).mount=2;migrated.Observation(202).yaw=45;migrated.Observation(203).doorOpen=true;
var settingsJson=JsonSerializer.Serialize(migrated.State,new JsonSerializerOptions{IncludeFields=true});
var settingsReloaded=new GameModel(JsonSerializer.Deserialize<GameState>(settingsJson,new JsonSerializerOptions{IncludeFields=true}));
Check(settingsReloaded.Observation(202).mount==2&&settingsReloaded.Observation(202).yaw==45&&settingsReloaded.Observation(203).doorOpen,"Door state, camera position and direction survive save roundtrip");
Console.WriteLine($"{count} total gameplay and observation checks passed.");
var hotel=new GameModel();hotel.StartDay();hotel.Decide(true,301);
Check(hotel.InRoom(301)?.id=="A-017","Front desk can assign a third-floor room");
hotel.Decide(true,301);Check(hotel.Current.id=="B-042","Third-floor rooms cannot be double-booked");
hotel.Decide(true,204);hotel.Decide(true,101);Check(hotel.Current.id=="B-042","Only configured rooms on floors two and three accept guests");
hotel.Decide(true,302);hotel.Decide(true,303);hotel.BeginNight();
Check(hotel.FindClue(301,0,out _)!=null&&hotel.FindClue(302,1,out _)!=null,"Third-floor guests participate in the existing clue system");
var thirdSight=new RoomObservation{room=301,doorOpen=true};
Check(RoomSight.Visibility(thirdSight,0,4,0,-1.25f,2)==0&&RoomSight.Visibility(thirdSight,0,4,0,-1.25f,3)>.99f,"Doorway observation cannot leak between vertically aligned floors");
Check(HotelLayout.NextFloor(1,3)==2&&HotelLayout.NextFloor(3,1)==2&&HotelLayout.NextFloor(2,3)==3,"Travel between first and third floor passes through second floor");
hotel.State.keeperFloor=3;hotel.State.keeperX=-6.4f;hotel.State.keeperZ=-1.25f;
var hotelReload=new GameModel(JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(hotel.State,new JsonSerializerOptions{IncludeFields=true}),new JsonSerializerOptions{IncludeFields=true}));
Check(hotelReload.State.keeperFloor==3&&hotelReload.InRoom(301)!=null&&hotelReload.State.rooms.Count==6,"Save roundtrip preserves floor location and third-floor bookings");
Console.WriteLine($"{count} total hotel checks passed.");
