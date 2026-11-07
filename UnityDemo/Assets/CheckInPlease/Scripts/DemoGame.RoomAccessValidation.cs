using System.Collections;
using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class DemoGame
    {
        IEnumerator RoomAccessSmokeTest()
        {
            game.Decide(true,202);world.Build("202",game);
            var owner=game.InRoom(202);var door=game.Observation(202);
            yield return UseInteractionDuringTest("room202",202,false);
            CheckInteraction(openingRoom==202&&!door.doorOpen&&door.doorLocked,"daytime locked door starts timed opening");
            yield return new WaitForSeconds(.45f);
            CheckInteraction(!door.doorOpen&&owner.resentment==0,"incomplete unlock stays closed and owner cannot see through it");
            yield return Capture("door-01-unlocking",.1f);
            yield return HoldMovement(Vector2.left,.12f);
            CheckInteraction(openingRoom==0&&!door.doorOpen&&door.doorLocked,"keyboard cancels unlocking without opening door");
            yield return WalkDuringTest(world.DoorApproach(202));
            yield return UseInteractionDuringTest("room202",202,false);
            modal="pause";yield return null;
            CheckInteraction(openingRoom==0&&!door.doorOpen,"pause cancels incomplete unlock");modal="";
            yield return UseInteractionDuringTest("room202",202,false);
            yield return UseInteractionDuringTest("room202",202,false);
            CheckInteraction(openingRoom==0&&!door.doorOpen,"repeat interaction cancels unlocking");
            yield return UseInteractionDuringTest("room202",202,false);
            world.WalkTo(world.DoorApproach(202)+Vector3.left);
            yield return WaitForMovement();
            CheckInteraction(openingRoom==0&&!door.doorOpen,"mouse route cancels unlocking");
            yield return WalkDuringTest(world.DoorApproach(202));
            yield return UseInteractionDuringTest("room202",202);
            CheckInteraction(door.doorOpen&&!door.doorLocked&&owner.resentment==15&&owner.vigilance==20,"owner witnesses completed daytime opening");
            CheckInteraction(game.InRoom(201)==null&&game.State.actions==4,"opening costs time rather than search actions");
            yield return Capture("door-02-owner-notices-opening");
            yield return WalkDuringTest(new Vector3(0,HotelLayout.Height(2),1.3f));
            CheckInteraction(world.PlayerInsideRoom(202)&&owner.resentment==40&&owner.vigilance==50,"daytime entry is walkable and owner notices intrusion");
            int journalCount=game.State.journal.Count;
            yield return new WaitForSeconds(.25f);
            CheckInteraction(owner.resentment==40&&game.State.journal.Count==journalCount,"continuous sight does not repeat penalties");
            yield return Capture("door-03-owner-notices-inside");
            yield return UseInteractionDuringTest("room202",202);
            CheckInteraction(!door.doorOpen&&door.doorLocked,"occupied room relocks when closed");
            yield return UseInteractionDuringTest("room202",202,false);
            CheckInteraction(door.doorOpen&&openingRoom==0,"inside handle opens locked room immediately");
            world.StoreLocation(game.State);
            game=new GameModel(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(game.State)));world.Build("saved",game);
            yield return null;
            CheckInteraction(world.PlayerInsideRoom(202)&&game.InRoom(202).resentment==40&&game.InRoom(202).vigilance==50&&game.InRoom(202).intrusionLevel==2,"Unity save keeps daytime interior and owner reaction without repeating it");
            yield return WalkDuringTest(world.DoorApproach(202)+Vector3.left*3);
            CheckInteraction(game.InRoom(202).intrusionLevel==0,"leaving room and doorway ends encounter");
            yield return WalkDuringTest(world.DoorApproach(203));
            yield return UseInteractionDuringTest("room203",203,false);
            CheckInteraction(game.Observation(203).doorOpen&&openingRoom==0,"empty room opens immediately during reception");
            while(game.Current!=null)game.Decide(false,0);
            game.BeginNight();world.RefreshResidents();
            yield return WalkDuringTest(new Vector3(0,HotelLayout.Height(2),1.3f));
            int before=game.InRoom(202).resentment;
            CheckInteraction(!world.OwnerSeesPlayer(202)&&before==40,"absent owner does not detect night entry");
            game.BeginAudit();world.RefreshResidents();yield return null;
            CheckInteraction(game.InRoom(202).resentment==before+25,"returning owner detects player already inside during audit");
            yield return Capture("door-04-returning-owner");
            yield return WalkDuringTest(world.DoorApproach(202));
            yield return UseInteractionDuringTest("room202",202);
            yield return UseInteractionDuringTest("room202",202);
            CheckInteraction(game.Observation(202).doorOpen,"locked door also opens during audit");
            CancelDoorOpening(false);
        }
    }
}
