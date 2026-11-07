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
Check(RoomSight.Visibility(vision,-3.2f,8.4f,0,-1.5f)>.99f,"Bedroom camera reveals the bed within its footprint");
Check(RoomSight.Visibility(vision,1.2f,6.3f,0,-1.5f)==0,"Partition blocks a study point inside the camera cone and range");
vision.yaw=315;
Check(RoomSight.Visibility(vision,-3.2f,8.4f,0,-1.5f)==0,"Rotating camera conceals the old view immediately");
vision.yaw=225;vision.mount=1;
Check(RoomSight.Visibility(vision,3.25f,6.3f,0,-1.5f)>.99f&&RoomSight.Visibility(vision,-3.2f,8.4f,0,-1.5f)==0,"Study installation changes coverage without seeing through bedroom wall");
vision.cameraInstalled=false;
Check(RoomSight.Visibility(vision,0,3.3f,0,-1.5f)==0,"Closed unmonitored room has no retained visibility");
vision.doorOpen=true;
Check(RoomSight.Visibility(vision,0,3,0,-1.5f)>.99f&&RoomSight.Visibility(vision,2.7f,.8f,0,-1.5f)==0,"Entrance reveals only lines through its aperture");
Check(RoomSight.Visibility(vision,0,3,5,-1.5f)==0,"Door view follows the keeper rather than revealing a whole room");
Check(RoomSight.Visibility(vision,6,3,0,-1.5f)==0,"Observation never crosses suite boundaries");
vision.doorOpen=false;
Check(RoomSight.Visibility(vision,1,3,0,2)>.99f,"Keeper can directly inspect the sitting room after entering");
Check(RoomSight.Visibility(vision,0,6,0,2)==0,"Interior walls also block the keeper's direct sight");
Check(RoomSight.Visibility(vision,1.2f,6.2f,1.2f,3.8f)>.99f,"Internal doorway passes direct sight");
Check(RoomSight.Visibility(vision,3.2f,9.3f,1.2f,6)==0,"Bathroom wall blocks sight from the study");
Check(SuiteLayout.Clear(-2.7f,4.4f)&&SuiteLayout.Clear(1.2f,7.7f)&&!SuiteLayout.Clear(0,4.4f)&&!SuiteLayout.Clear(-.6f,6),"Doorway walking openings match solid partitions");
float maximumCoverage=0;
for(int mount=0;mount<3;mount++)for(int angle=0;angle<360;angle+=15)
{
    var camera=new RoomObservation{room=201,cameraInstalled=true,mount=mount,yaw=angle,fieldOfView=95};int samples=0,visible=0;
    for(float x=-4.8f;x<5;x+=.4f)for(float z=.2f;z<10.5f;z+=.4f)
    {samples++;if(RoomSight.Camera(camera,x,z)>.5f)visible++;}
    maximumCoverage=Math.Max(maximumCoverage,visible/(float)samples);
}
Check(maximumCoverage<.5f,"Every mount and yaw at widest lens covers less than half the suite");
Check(SuiteLayout.Width*SuiteLayout.Depth>6.4f*6.6f*2.4f,"Each suite has more than 2.4 times the old floor area");
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
var stairSave=new GameModel(new GameState{keeperFloor=2,keeperX=-13,keeperZ=1.5f,keeperY=3.19f,keeperOnStairs=true});
var stairReload=new GameModel(JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(stairSave.State,new JsonSerializerOptions{IncludeFields=true}),new JsonSerializerOptions{IncludeFields=true}));
Check(stairReload.State.keeperOnStairs&&Math.Abs(stairReload.State.keeperY-3.19f)<.001f,"Stair save preserves intermediate walking height");
var invalidStair=new GameModel(new GameState{keeperFloor=3,keeperY=float.NaN,keeperOnStairs=true});
Check(!invalidStair.State.keeperOnStairs&&invalidStair.State.keeperY==HotelLayout.Height(3),"Invalid stair height falls back to saved floor");
Check(!new GameModel(new GameState{keeperFloor=3}).State.keeperOnStairs,"Legacy floor-only saves do not resume at ground height");
Console.WriteLine($"{count} total checks passed.");

var access=new GameModel();
foreach(var phase in new[]{Shift.Morning,Shift.Reception,Shift.Search,Shift.Audit,Shift.Finished})
{
    access.State.phase=phase;access.Observation(303).doorLocked=true;
    Check(!access.OpenDoor(303,false,2.99f)&&!access.Observation(303).doorOpen,"Locked door waits in "+phase);
    Check(access.OpenDoor(303,false,3)&&!access.Observation(303).doorLocked,"Locked door opens after delay in "+phase);
    access.CloseDoor(303);
    Check(access.OpenDoor(303,false),"Unlocked door opens immediately in "+phase);access.CloseDoor(303);
}
access=new GameModel();access.StartDay();access.Decide(true,202);
Check(access.Observation(202).doorLocked&&!access.OpenDoor(202,false)&&!access.OpenDoor(202,false,float.NaN),"Occupied doors lock and cannot bypass timing");
Check(access.OpenDoor(202,true),"Inside handle always permits immediate exit");access.CloseDoor(202);
Check(access.Observation(202).doorLocked,"Occupied door relocks when closed");
Check(!access.OwnerCanSee(202,2,0,-1.25f,.15f,3.6f,0,-1),"Owner cannot see through closed entrance even with camera");
access.Observation(202).cameraInstalled=true;
Check(!access.OwnerCanSee(202,2,0,-1.25f,.15f,3.6f,0,-1),"Installed camera does not give owner sight through door");
access.OpenDoor(202,false,3);
Check(access.OwnerCanSee(202,2,0,-1.25f,.15f,3.6f,0,-1),"Owner sees player opening doorway");
Check(!access.OwnerCanSee(202,3,0,2,.15f,3.6f,0,-1),"Owner sight cannot cross floors");
Check(!access.OwnerCanSee(202,2,0,6,.15f,3.6f,0,1),"Interior wall blocks owner sight even facing player");
Check(!access.OwnerCanSee(202,2,0,2,.15f,3.6f,0,1),"Owner does not see behind their back");
Check(!access.OwnerCanSee(202,2,3,-1.25f,.15f,3.6f,0,-1),"Open doorway does not reveal corridor through front wall");
Check(!access.OwnerCanSee(202,2,10.2f,2,.15f,3.6f,0,-1),"Owner cannot see into adjacent room");
var tenant=access.InRoom(202);
Check(access.NoticeIntrusion(202,false)!=""&&tenant.resentment==15&&tenant.vigilance==20,"Witnessed opening increases personal resentment and vigilance");
Check(access.NoticeIntrusion(202,false)==""&&tenant.resentment==15,"Repeated sight of same opening does not repeatedly penalize");
Check(access.NoticeIntrusion(202,true)!=""&&tenant.resentment==40&&tenant.vigilance==50,"Discovery inside escalates intrusion once");
Check(access.NoticeIntrusion(202,true)==""&&!access.ResetIntrusion(202,2,1.2f,6),"Hiding inside does not reset the same encounter");
var accessReload=new GameModel(JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(access.State,new JsonSerializerOptions{IncludeFields=true}),new JsonSerializerOptions{IncludeFields=true}));
Check(accessReload.InRoom(202).vigilance==50&&accessReload.InRoom(202).resentment==40&&accessReload.NoticeIntrusion(202,true)==""&&accessReload.Observation(202).doorOpen,"Save roundtrip keeps owner reactions, encounter and door state");
Check(access.ResetIntrusion(202,2,3,-1.25f)&&access.NoticeIntrusion(202,false)!="","A later intrusion after leaving can be noticed again");
tenant.vigilance=0;bool calmSees=access.OwnerCanSee(202,2,3,2,0,3.6f,0,-1);
tenant.vigilance=100;Check(!calmSees&&access.OwnerCanSee(202,2,3,2,0,3.6f,0,-1),"Vigilance widens the owner's actual field of view");
for(int i=0;i<8;i++){access.ResetIntrusion(202,1,0,0);access.NoticeIntrusion(202,true);}
Check(tenant.resentment==100&&tenant.vigilance==100,"Owner reaction values are capped");
while(access.Current!=null)access.Decide(false,0);access.BeginNight();
Check(!access.OwnerCanSee(202,2,0,2,.15f,3.6f,0,-1)&&access.NoticeIntrusion(202,true)=="","Absent owner cannot witness intrusion");
access.BeginAudit();Check(access.OwnerPresent(202),"Owner returns when roll call ends for audit");
access.EndDay();Check(!access.Observation(202).doorOpen&&!access.Observation(202).doorLocked&&access.InRoom(202)==null,"Next day vacates and unlocks rooms");
var legacyAccess=new GameState();legacyAccess.guests.Add(new Guest{day=1,room=301,decision=Decision.Admitted});
var migratedAccess=new GameModel(legacyAccess);Check(migratedAccess.Observation(301).doorLocked&&!migratedAccess.Observation(302).doorLocked,"Old saves gain locks only on occupied closed rooms");
Console.WriteLine($"{count} total checks including room access and owner detection passed.");

var oldSuite=new GameModel(new GameState{keeperFloor=3,keeperX=6.4f,keeperZ=-1.25f});
Check(Math.Abs(oldSuite.State.keeperX-RoomSight.Center(303))<.001f&&oldSuite.State.keeperLayout==2,"Legacy suite position migrates to the same room on wider floor");
var oldStair=new GameModel(new GameState{keeperFloor=2,keeperX=-13,keeperZ=1.5f,keeperY=3.19f,keeperOnStairs=true});
Check(Math.Abs(oldStair.State.keeperX-(-13+HotelLayout.StairOffset))<.001f,"Legacy staircase save moves with the expanded west wing");
Console.WriteLine($"{count} total expanded-hotel checks passed. Maximum camera coverage: {maximumCoverage:P0}");
Check(StairTransition.FloorOpacity(.9f)==1&&StairTransition.FloorOpacity(3.8f)==0&&Math.Abs(StairTransition.FloorOpacity(1.9f)-.5f)<.001f,"Transition has full, partial and stairwell-only stages");
bool fadeMonotone=true;float previousOpacity=1;
for(float z=-.7f;z<4.1f;z+=.01f){float opacity=StairTransition.FloorOpacity(z);fadeMonotone&=opacity<=previousOpacity+.00001f&&opacity>=0&&opacity<=1;previousOpacity=opacity;}
Check(fadeMonotone,"Approaching a platform fades monotonically without overshoot");
Check(StairTransition.ViewHeight(1,-11,1.9f)==0&&StairTransition.ViewHeight(1,-13,1.9f)==4.4f,"Visible storeys retain their fixed composition during fades");
Check(Math.Abs(StairTransition.ViewHeight(1,-12.001f,3.8f)-StairTransition.ViewHeight(1,-11.999f,3.8f))<.01f,"Logical floor boundary does not cause a projection jump");
bool hiddenRebase=true;
for(float z=-.7f;z<3.8f;z+=.01f)if(StairTransition.FloorOpacity(z)>.001f)
    hiddenRebase&=StairTransition.ViewHeight(1,-11,z)==0&&StairTransition.ViewHeight(1,-13,z)==4.4f;
Check(hiddenRebase,"Projection height changes only while all storeys are hidden");
Check(Math.Abs(StairTransition.ViewHeight(2,-12,3.8f)-6.6f)<.001f,"Second connection uses the same stairwell transition at its own height");
Check(Math.Abs(StairTransition.ViewHeight(1,-11.001f,3.6f)-StairTransition.ViewHeight(1,-10.999f,3.6f))<.01f,"Walking sideways around the right corner does not snap the view");
Check(Math.Abs(StairTransition.ViewHeight(1,-11.3f,3.501f)-StairTransition.ViewHeight(1,-11.3f,3.499f))<.01f,"Hugging a rail into the platform keeps height continuous");
Console.WriteLine($"{count} total checks passed.");
