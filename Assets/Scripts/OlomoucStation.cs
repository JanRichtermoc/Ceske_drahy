using UnityEngine;

namespace CeskeDrahy
{
    public static class OlomoucStation
    {
        public static void Build(RailWorld w)
        {
            var root=w.Group("Olomouc hlavní nádraží / stylizovaná výpravní budova",new Vector3(13,0,14));
            var brick=w.Mat("Station brick","#995747");var stone=w.Mat("Station limestone","#D6C4A3");
            var frame=w.Mat("Station bronze","#38515A",.5f,.4f);
            w.Box("Entrance hall",new Vector3(0,5.2f,0),new Vector3(24,12,5),brick,root);
            w.Box("West wing",new Vector3(-20,2.5f,1),new Vector3(16,6.5f,5),stone,root);
            w.Box("East wing",new Vector3(20,2.5f,1),new Vector3(16,6.5f,5),stone,root);
            w.Box("Flat cornice",new Vector3(0,11.25f,0),new Vector3(24.4f,.35f,5.4f),stone,root);
            for(int i=-10;i<=10;i+=4)
            {
                w.Box("Tall hall glazing",new Vector3(i,7,-2.54f),new Vector3(2.25f,6.4f,.12f),w.glass,root);
                w.Box("Limestone pilaster",new Vector3(i+1.5f,7,-2.68f),new Vector3(.42f,7.9f,.25f),stone,root);
                for(float y=4;y<10;y+=1.1f)w.Box("Glazing transom",new Vector3(i,y,-2.65f),new Vector3(2.2f,.07f,.05f),frame,root);
                w.Box("Glazing mullion",new Vector3(i,7,-2.65f),new Vector3(.06f,6.4f,.05f),frame,root);
                w.Box("Entrance doors",new Vector3(i,1.25f,-2.6f),new Vector3(2.3f,3.3f,.1f),w.glass,root);
                w.Box("Door handles",new Vector3(i,1.3f,-2.7f),new Vector3(.1f,.7f,.1f),w.steel,root);
            }
            for(float y=4;y<11;y+=.42f)w.Box("Brick mortar",new Vector3(0,y,-2.51f),new Vector3(24,.025f,.025f),w.rust,root);
            w.Box("Entrance canopy",new Vector3(0,3.2f,-4),new Vector3(29,.32f,5.4f),stone,root);
            w.Box("Name panel",new Vector3(0,3.8f,-5.9f),new Vector3(17,1,.15f),w.navy,root);
            w.Sign("OLOMOUC  hl. n.",new Vector3(0,3.8f,-6.01f),.13f,root);
            foreach(float x in new[]{-11.3f,11.3f})Clock(w,root,x,9.7f,-2.8f);
            // Winged railway emblem, interpreted as simple original geometry.
            w.Box("Emblem plinth",new Vector3(0,11.8f,0),new Vector3(1.1f,1.2f,1),stone,root);
            w.Box("Winged wheel",new Vector3(0,12.6f,0),Vector3.one*.8f,frame,root,PrimitiveType.Sphere);
            for(int i=0;i<5;i++)foreach(int direction in new[]{-1,1})
            {var wing=w.Box("Emblem feather",new Vector3(direction*(.6f+i*.3f),12.75f+i*.12f,0),new Vector3(.65f,.18f,.45f),frame,root);wing.localRotation=Quaternion.Euler(0,0,direction*16);}
            // The tall RCO silhouette and a tram suggest the forecourt without literal map accuracy.
            w.Box("RCO silhouette",new Vector3(43,11.5f,37),new Vector3(7,25,8),w.Mat("RCO facade","#9AACA6"));
            for(int floor=0;floor<18;floor++)w.Box("RCO glazing",new Vector3(43,floor*1.2f+1,32.95f),new Vector3(6,.7f,.08f),w.glass);
            var tram=w.Group("Olomoucká tramvaj",new Vector3(-8,.9f,8));
            w.Box("Tram cream",Vector3.zero,new Vector3(10,2.3f,2),w.cream,tram);
            w.Box("Tram red stripe",new Vector3(0,-.6f,-1.02f),new Vector3(10,.7f,.05f),w.rust,tram);
            for(int i=-4;i<5;i+=2)w.Box("Tram window",new Vector3(i,.4f,-1.03f),new Vector3(1.5f,.8f,.06f),w.glass,tram);
            w.Sign("4  NOVÁ ULICE",new Vector3(3,1.35f,-1.04f),.04f,tram);
            for(int x=48;x<173;x+=24)
            {
                var shelter=w.Group("Nástupištní přístřešek",new Vector3(x,0,7));
                w.Box("Canopy",new Vector3(0,6.4f,0),new Vector3(18,.18f,5),w.steel,shelter);
                foreach(float i in new[]{-7f,7f})w.Box("Canopy pillar",new Vector3(i,2.5f,0),new Vector3(.2f,7,.2f),frame,shelter);
            }
            Board(w,56,"PODCHOD","TUDY JE TO URČITĚ KRATŠÍ.");
            Board(w,112,"NÁSTUPIŠTĚ 5","ZMĚNA NÁSTUPIŠTĚ JE NÁŠ SPORT.");
            Board(w,168,"SMĚR DOMŮ","ZPOŽDĚNÍ: TAK AKORÁT.");
            var kiosk=w.Group("Tvarůžkový bufet",new Vector3(57,0,5));
            w.Box("Kiosk",new Vector3(0,1.4f,0),new Vector3(5,3.5f,2),w.green,kiosk);
            w.Box("Awning",new Vector3(0,3.1f,-.5f),new Vector3(5.6f,.25f,3),w.gold,kiosk);
            w.Sign("TVARŮŽEK = TURBO",new Vector3(0,2.5f,-1.1f),.055f,kiosk);
            w.Box("Counter",new Vector3(0,1,-1.2f),new Vector3(4,.22f,1),w.cream,kiosk);
        }
        static void Clock(RailWorld w,Transform parent,float x,float y,float z)
        {
            var disc=w.Box("Station clock",new Vector3(x,y,z),new Vector3(1.5f,.08f,1.5f),w.cream,parent,PrimitiveType.Cylinder);disc.localRotation=Quaternion.Euler(90,0,0);
            var h=w.Box("Hour hand",new Vector3(x+.12f,y+.1f,z-.1f),new Vector3(.11f,.5f,.03f),w.navy,parent);h.localRotation=Quaternion.Euler(0,0,-42);
            w.Box("Minute hand",new Vector3(x,y+.3f,z-.11f),new Vector3(.065f,.65f,.03f),w.navy,parent);
            for(int n=0;n<12;n++){float a=n*Mathf.PI/6;w.Box("Clock tick",new Vector3(x+Mathf.Sin(a)*.62f,y+Mathf.Cos(a)*.62f,z-.1f),new Vector3(.045f,.09f,.02f),w.navy,parent);}
        }
        static void Board(RailWorld w,float x,string title,string subtitle)
        {
            w.Box("Departure board",new Vector3(x,4.6f,5),new Vector3(8,1.8f,.2f),w.navy);
            w.Sign(title,new Vector3(x,4.95f,4.85f),.075f);
            w.Sign(subtitle,new Vector3(x,4.3f,4.85f),.028f,null,new Color(1,.74f,.3f));
        }
    }
}
