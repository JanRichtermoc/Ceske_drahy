using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailHUD : MonoBehaviour
    {
        public RailGame game;
        GUIStyle title,display,body,small,number,button,eyebrow;
        readonly Color ink=new Color(.045f,.095f,.13f,.96f), muted=new Color(.67f,.77f,.8f), paper=new Color(.95f,.95f,.89f), amber=new Color(1,.72f,.31f);
        bool settings;
        float fps;
        void Init()
        {
            if(title!=null)return;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title=Style(font,88,FontStyle.Bold,paper);display=Style(font,48,FontStyle.Bold,paper);
            body=Style(font,22,FontStyle.Normal,paper);small=Style(font,16,FontStyle.Normal,muted);
            number=Style(font,30,FontStyle.Bold,paper);eyebrow=Style(font,16,FontStyle.Bold,amber);
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=21,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(25,20,0,0)};
            button.normal.background=Texture2D.whiteTexture;button.hover.background=Texture2D.whiteTexture;button.active.background=Texture2D.whiteTexture;button.focused.background=Texture2D.whiteTexture;
            button.normal.textColor=ink;button.hover.textColor=ink;button.active.textColor=ink;button.focused.textColor=ink;
        }
        GUIStyle Style(Font font,int size,FontStyle weight,Color color)=>new GUIStyle{font=font,fontSize=size,fontStyle=weight,normal={textColor=color},wordWrap=true};
        void Rect(float x,float y,float w,float h,Color c){GUI.color=QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
        void Label(float x,float y,float w,float h,string value,GUIStyle style){GUI.Label(new Rect(x,y,w,h),value,style);}
        bool Button(float x,float y,float w,string value,bool primary=true)
        {
            GUI.backgroundColor=(primary?amber:paper).linear;bool clicked=GUI.Button(new Rect(x,y,w,58),value,button);GUI.backgroundColor=Color.white;return clicked;
        }
        void Update(){fps=Mathf.Lerp(fps,1/Mathf.Max(Time.unscaledDeltaTime,.001f),.05f);}
        void OnGUI()
        {
            Init();float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(game.mode==RailGame.Mode.Menu){MainMenu();return;}
            Hud();
            if(game.mode==RailGame.Mode.Paused)Overlay("CHVÍLI POSTŮJ.","Vlak počká. Dej si pauzu.",true);
            if(game.mode==RailGame.Mode.Won)Overlay("STIHL JSI TO.","Poslední spoj je tvůj. Příští zastávka: domov.",false);
            if(game.mode==RailGame.Mode.Lost)Overlay("UJEL TI VLAK.","Každá cesta má další pokus.",false);
        }
        void MainMenu()
        {
            Rect(0,0,1600,66,ink);Label(48,23,900,30,"ČD  /  NEZÁVISLÁ PLOŠINOVKA",eyebrow);Label(1240,23,320,30,"PRAHA     18:42     +12 °C",small);
            Rect(0,66,660,768,new Color(.035f,.085f,.12f,.985f));Rect(660,66,2,768,new Color(1,1,1,.1f));
            Label(54,126,550,30,"01  —  DOBRODRUŽSTVÍ NA KOLEJÍCH",eyebrow);
            Label(48,183,610,220,"ČESKÉ\nDRÁHY",title);
            Rect(54,409,72,5,amber);Label(54,441,555,40,"POSLEDNÍ SPOJ",number);
            Label(54,501,530,94,"David má jízdenku. Jen je na špatném nástupišti. Přeskoč vagony, vyhni se holubům a stihni cestu domů.",body);
            if(Button(54,626,540,"NA NÁSTUPIŠTĚ                         →")){settings=false;game.StartRun();}
            if(Button(54,698,260,"NASTAVENÍ",false))settings=!settings;
            Label(338,716,270,30,"ENTER  /  rychlý start",small);
            Rect(1050,645,492,143,ink);Label(1080,667,430,28,"TRAŤ 01   /   PRAŽSKÝ VEČER",eyebrow);
            Label(1080,708,430,58,"3 úseky. Jeden poslední vlak.",body);
            Rect(0,834,1600,66,ink);Label(48,856,1050,35,"A D / ← →  POHYB      MEZERNÍK  SKOK      ESC  PAUZA      M  ZVUK",small);
            Label(1230,856,350,30,"OSOBNÍ FANOUŠKOVSKÁ HRA",small);
            if(settings)Settings(720,135);
        }
        void Hud()
        {
            Rect(28,24,360,88,ink);Label(52,40,310,25,"ČESKÉ DRÁHY  /  POSLEDNÍ SPOJ",eyebrow);Label(52,72,310,36,game.Zone,number);
            Rect(660,24,280,72,ink);Label(683,41,230,30,"JÍZDENKY    "+(game.score/100).ToString("00"),body);
            Rect(1200,24,372,88,ink);Label(1224,40,250,25,"VÝDRŽ",small);
            for(int i=0;i<3;i++)Rect(1224+i*46,76,32,9,i<game.lives?amber:new Color(.28f,.34f,.37f));
            Label(1400,50,145,35,((int)game.elapsed/60).ToString("00")+":"+((int)game.elapsed%60).ToString("00"),number);
            Rect(0,843,1600,57,ink);
            Rect(28,858,1544,4,new Color(.1f,.17f,.2f,.8f));Rect(28,858,1544*game.Progress,4,amber);
            Label(28,871,650,25,"A D  POHYB    /    MEZERNÍK  SKOK    /    R  CHECKPOINT",small);
            Label(1240,871,340,25,"ESC  PAUZA   ·   "+Mathf.RoundToInt(fps)+" FPS",small);
            if(game.noticeTime>0){Rect(460,130,680,54,ink);Label(480,145,640,30,game.notice,eyebrow);}
            if(game.position.x<9 && game.mode==RailGame.Mode.Playing)
            {Rect(50,655,470,130,ink);Label(74,678,430,30,"ROZBĚHNI SE A SKOČ",eyebrow);Label(74,720,420,55,"Delší podržení mezerníku = vyšší skok.",body);}
        }
        void Overlay(string heading,string description,bool paused)
        {
            Rect(0,0,1600,900,new Color(.025f,.06f,.09f,.72f));Rect(420,190,760,530,ink);
            Label(474,231,660,28,paused?"PAUZA  /  NÁSTUPIŠTĚ JE TVOJE":"KONEC JÍZDY  /  "+game.Zone,eyebrow);
            Label(472,292,670,72,heading,display);Label(474,379,650,60,description,body);
            Label(474,456,650,45,"JÍZDENKY  "+game.score/100+"     ČAS  "+game.elapsed.ToString("0.0")+" s     REKORD  "+PlayerPrefs.GetInt("best",0)/100,body);
            if(Button(474,542,652,paused?"POKRAČOVAT                              →":"ZNOVU NA TRAŤ                        →")){if(paused)game.TogglePause();else game.StartRun();}
            if(Button(474,614,316,"HLAVNÍ MENU",false)){settings=false;game.Menu();}
            if(Button(810,614,316,"NASTAVENÍ",false))settings=!settings;
            if(settings)Settings(430,205);
        }
        void Settings(float x,float y)
        {
            Rect(x,y,650,550,ink);Label(x+34,y+30,590,55,"NASTAVENÍ",display);
            if(Button(x+34,y+110,582,"ZVUK: "+(game.muted?"VYPNUTÝ":"ZAPNUTÝ"),false)){game.muted=!game.muted;game.SaveSettings();}
            if(Button(x+34,y+186,582,"GRAFIKA: "+(game.lowQuality?"ÚSPORNÁ":"VYSOKÁ"),false)){game.lowQuality=!game.lowQuality;game.SaveSettings();}
            if(Button(x+34,y+262,582,"OTŘESY KAMERY: "+(game.reducedMotion?"VYPNUTÉ":"ZAPNUTÉ"),false)){game.reducedMotion=!game.reducedMotion;game.SaveSettings();}
            Label(x+34,y+344,580,65,"Úsporná grafika vypne stíny a vyhlazování. Nastavení se automaticky ukládá.",body);
            if(Button(x+34,y+446,582,"HOTOVO"))settings=false;
        }
    }
}
