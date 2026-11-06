using UnityEngine;

namespace CheckInPlease
{
    public sealed partial class WorldView
    {
        void MakeSuitePartitions(int room,float cx,Material wall,Material oak,Material ivory,Material iron,Material linen)
        {
            var tiles=Surface("Bathroom tiles","#85928a",5,room);
            var rug=Surface("Living room rug","#694c3d",4,room);
            var upholstery=Surface("Living sofa fabric","#606d5d",2,room);
            Box("Living room carpet",new Vector3(cx-2.85f,.061f,2.1f),new Vector3(3.5f,.018f,2.7f),rug);
            Box("Study floor",new Vector3(cx+2.2f,.063f,6.05f),new Vector3(5.35f,.018f,3.15f),Surface("Study parquet","#695841",1,room));
            Box("Bathroom floor",new Vector3(cx+2.2f,.065f,9.15f),new Vector3(5.35f,.02f,2.75f),tiles);
            foreach(var w in SuiteLayout.Walls)
            {
                var center=new Vector3(cx+(w.x1+w.x2)*.5f,.48f,(w.z1+w.z2)*.5f);
                var size=new Vector3(Mathf.Abs(w.x2-w.x1)+.16f,.96f,Mathf.Abs(w.z2-w.z1)+.16f);
                Box("Suite partition",center,size,wall);
                Box("Suite partition cut edge",center+Vector3.up*.52f,new Vector3(size.x+.035f,.08f,size.z+.035f),shell);
            }
            // Open internal doorways connect distinct rooms without introducing extra interaction steps.
            foreach(var doorway in new[]{new Vector2(-2.7f,4.4f),new Vector2(1.2f,4.4f),new Vector2(1.2f,7.7f)})
            {
                foreach(int side in new[]{-1,1})Box("Suite doorway jamb",new Vector3(cx+doorway.x+side*.82f,.65f,doorway.y),new Vector3(.1f,1.3f,.22f),oak);
                Box("Suite doorway threshold",new Vector3(cx+doorway.x,.06f,doorway.y),new Vector3(1.52f,.035f,.3f),oak);
            }
            // Front sitting room and small kitchenette.
            Box("Suite sofa seat",new Vector3(cx-3.2f,.43f,2.3f),new Vector3(2.7f,.46f,.95f),upholstery);
            Box("Suite sofa back",new Vector3(cx-3.2f,.9f,2.76f),new Vector3(2.7f,.84f,.16f),upholstery);
            foreach(int side in new[]{-1,1})Box("Suite sofa arm",new Vector3(cx-3.2f+side*1.33f,.68f,2.3f),new Vector3(.17f,.55f,1.05f),oak);
            Box("Suite coffee table",new Vector3(cx-3.2f,.53f,.85f),new Vector3(1.6f,.12f,.7f),oak);
            foreach(int side in new[]{-1,1})Box("Coffee table support",new Vector3(cx-3.2f+side*.6f,.26f,.85f),new Vector3(.1f,.52f,.5f),iron);
            Box("Suite newspaper",new Vector3(cx-3.2f,.6f,.85f),new Vector3(.55f,.015f,.4f),ivory);
            floorObstacles.Add(new Rect(cx-4.7f,1.55f,3,1.65f));floorObstacles.Add(new Rect(cx-4.15f,.32f,1.9f,1.05f));
            Box("Kitchenette cabinet",new Vector3(cx+4.4f,.48f,3.25f),new Vector3(.9f,.96f,1.8f),oak);
            Box("Kitchenette counter",new Vector3(cx+4.4f,1.02f,3.25f),new Vector3(1.0f,.12f,1.9f),ivory);
            Ball("Kitchen sink",new Vector3(cx+4.4f,1.09f,3.65f),new Vector3(.65f,.035f,.55f),iron);
            Ball("Kettle body",new Vector3(cx+4.4f,1.25f,2.75f),new Vector3(.32f,.35f,.32f),iron);
            Ring(new Vector3(cx+4.4f,1.4f,2.75f),.16f,.025f,iron);
            floorObstacles.Add(new Rect(cx+3.72f,2.1f,1.2f,2.0f));
            // Bedroom storage beside the bed, separate from the study.
            Box("Bedside cabinet",new Vector3(cx-1.35f,.47f,9.65f),new Vector3(.9f,.94f,.9f),oak);
            MakeDeskLamp(new Vector3(cx-1.35f,.98f,9.65f),iron,ivory,room);
            floorObstacles.Add(new Rect(cx-1.92f,9.1f,1.14f,1.1f));
            // Dedicated bathroom: bathtub, washbasin and toilet.
            Box("Bathtub base",new Vector3(cx+3.35f,.28f,9.3f),new Vector3(2.3f,.56f,1.55f),ivory);
            Box("Bathtub water recess",new Vector3(cx+3.35f,.57f,9.3f),new Vector3(1.95f,.03f,1.18f),Surface("Bath interior","#536b68",3,room));
            foreach(int side in new[]{-1,1})Box("Bathtub rim",new Vector3(cx+3.35f+side*1.08f,.61f,9.3f),new Vector3(.15f,.12f,1.55f),ivory);
            Rod("Bath tap",new Vector3(cx+4.35f,.62f,9.3f),new Vector3(cx+4.35f,.94f,9.3f),.065f,iron);
            Box("Washbasin pedestal",new Vector3(cx+.25f,.42f,10.0f),new Vector3(.45f,.84f,.44f),ivory);
            Ball("Washbasin bowl",new Vector3(cx+.25f,.9f,10.0f),new Vector3(.9f,.2f,.62f),ivory);
            Box("Bathroom mirror",new Vector3(cx+.25f,1.65f,10.44f),new Vector3(.85f,.9f,.035f),Surface("Dull bathroom mirror","#8caaa2",3,room));
            Box("Toilet cistern",new Vector3(cx+.15f,.74f,8.3f),new Vector3(.65f,.62f,.28f),ivory);
            Ball("Toilet bowl",new Vector3(cx+.15f,.4f,8.7f),new Vector3(.62f,.45f,.88f),ivory);
            Box("Folded bathroom towel",new Vector3(cx+3.5f,.69f,10.0f),new Vector3(.65f,.13f,.35f),linen);
            floorObstacles.Add(new Rect(cx+2.02f,8.33f,2.7f,1.95f));
            floorObstacles.Add(new Rect(cx-.35f,9.5f,1.2f,.85f));floorObstacles.Add(new Rect(cx-.4f,8.0f,1.1f,1.35f));
        }
        void MakeGroundExpansion(Material oak,Material ivory)
        {
            var seat=Surface("Lobby lounge upholstery","#5e675c",2);
            // A west lounge and rear records area give the enlarged ground floor a purpose.
            for(int row=0;row<2;row++)
            {
                float z=3.0f+row*4.2f;
                Box("West lounge sofa",new Vector3(-12,.42f,z),new Vector3(3.6f,.48f,1.05f),seat);
                Box("West lounge back",new Vector3(-12,.93f,z+.48f),new Vector3(3.6f,.85f,.15f),wood);
                Box("Lounge coffee table",new Vector3(-12,.55f,z-1.4f),new Vector3(1.9f,.12f,.75f),oak);
                Box("Lounge newspaper",new Vector3(-12,.62f,z-1.4f),new Vector3(.65f,.02f,.45f),ivory);
                floorObstacles.Add(new Rect(-14,z-.8f,4,1.6f));floorObstacles.Add(new Rect(-13.1f,z-1.95f,2.2f,1.1f));
            }
            for(int cabinet=0;cabinet<3;cabinet++)
            {
                float x=9.4f+cabinet*2.3f;
                Box("Rear records cabinet",new Vector3(x,.9f,9.8f),new Vector3(1.7f,1.8f,.85f),trim);
                for(int drawer=0;drawer<4;drawer++)
                {
                    Box("Records drawer",new Vector3(x,.26f+drawer*.43f,9.35f),new Vector3(1.5f,.36f,.055f),plaster);
                    Box("Records label",new Vector3(x,.3f+drawer*.43f,9.31f),new Vector3(.4f,.09f,.02f),ivory);
                }
                floorObstacles.Add(new Rect(x-1.0f,9.1f,2.0f,1.3f));
            }
            Box("Office records table",new Vector3(12.3f,.93f,6.1f),new Vector3(2.6f,.14f,1.35f),oak);
            foreach(int side in new[]{-1,1})Box("Records table legs",new Vector3(12.3f+side*1.0f,.45f,6.1f),new Vector3(.15f,.9f,1.15f),wood);
            Box("Records ledgers",new Vector3(12.3f,1.05f,6.1f),new Vector3(.85f,.12f,.65f),ivory);
            floorObstacles.Add(new Rect(10.8f,5.2f,3,1.8f));
        }
    }
}
