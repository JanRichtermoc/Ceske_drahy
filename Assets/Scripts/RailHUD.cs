using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailHUD : MonoBehaviour
    {
        public RailGame game;
        GUIStyle title,display,body,small,number,button,eyebrow,popup;
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
            popup=Style(font,23,FontStyle.Bold,paper);popup.alignment=TextAnchor.MiddleCenter;
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
            if(game.mode==RailGame.Mode.Playing)
            {
                if(!game.reducedMotion && game.audioMix.ImpactPulse>0)
                {
                    var hit=new Color(1,.21f,.27f,.32f*game.audioMix.ImpactPulse);
                    Rect(0,0,1600,12,hit);Rect(0,0,12,900,hit);Rect(1588,0,12,900,hit);Rect(0,888,1600,12,hit);
                }
                DrawPops(scale);
            }
            Hud();
            if(game.mode==RailGame.Mode.Paused)Overlay("CHVÍLI POSTŮJ.","Vlak počká. Dej si pauzu.",true);
            if(game.mode==RailGame.Mode.Won)Overlay("TY JSI TO STIHL!","Vlak má zpoždění. Ty už ne.",false);
            if(game.mode==RailGame.Mode.Lost)Overlay("UJEL TI VLAK.","Holubi si tě napsali do jízdního řádu.",false);
        }
        void MainMenu()
        {
            Rect(0,0,1600,66,ink);Label(48,23,900,30,"ČD  /  NEZÁVISLÁ PLOŠINOVKA",eyebrow);Label(1240,23,320,30,"OLOMOUC HL. N.",small);
            Rect(0,66,660,768,new Color(.035f,.085f,.12f,.985f));Rect(660,66,2,768,new Color(1,1,1,.1f));
            Label(54,126,550,30,"POSLEDNÍ SPOJ",eyebrow);
            Label(48,183,610,220,"ČESKÉ\nDRÁHY",title);
            Rect(54,409,72,5,amber);Label(54,441,555,40,"OLOMOUCKÝ CHAOS",number);
            Label(54,501,530,94,"David má jízdenku. Nádraží má jiné plány. Odraz se od kufru, proskoč mezi vagony a nenech se píchnout.",body);
            if(Button(54,626,540,"NA NÁSTUPIŠTĚ                         →")){settings=false;game.StartRun();}
            if(Button(54,698,260,"NASTAVENÍ",false))settings=!settings;
            Label(338,716,270,30,"ENTER  /  rychlý start",small);
            Rect(1050,645,492,143,ink);Label(1080,667,430,28,"HLÁŠENÍ PRO CESTUJÍCÍ",eyebrow);
            Label(1080,708,430,58,"Vlak má zpoždění. Holubi bohužel ne.",body);
            Rect(0,834,1600,66,ink);Label(48,856,1050,35,"A D / ← →  POHYB     MEZERNÍK  DVOJSKOK     SHIFT  ÚHYB     ESC  PAUZA",small);
            Label(1230,856,350,30,"OSOBNÍ FANOUŠKOVSKÁ HRA",small);
            if(settings)Settings(720,135);
        }
        void Hud()
        {
            Rect(28,24,400,88,ink);Label(52,40,355,25,"ČESKÉ DRÁHY  /  OLOMOUCKÝ CHAOS",eyebrow);Label(52,72,355,36,game.Zone,number);
            Rect(644,24,312,88,ink);Label(666,38,260,32,"BODY  "+game.score.ToString("00000"),number);
            Label(668,78,266,26,"JÍZDENKY  "+game.chaos.ticketsCollected+"     KOMBO ×"+game.chaos.combo,eyebrow);
            DrawStatus();
            Rect(1200,24,372,88,ink);Label(1224,40,250,25,"VÝDRŽ",small);
            for(int i=0;i<3;i++)Rect(1224+i*46,76,32,9,i<game.lives?amber:new Color(.28f,.34f,.37f));
            Label(1400,50,145,35,((int)game.elapsed/60).ToString("00")+":"+((int)game.elapsed%60).ToString("00"),number);
            Rect(0,843,1600,57,ink);
            Rect(28,858,1544,4,new Color(.1f,.17f,.2f,.8f));Rect(28,858,1544*game.Progress,4,amber);
            Label(28,871,1010,25,"A D  POHYB    /    MEZERNÍK  DVOJSKOK    /    SHIFT  ÚHYB    /    R  CHECKPOINT",small);
            Label(1240,871,340,25,"ESC  PAUZA   ·   "+Mathf.RoundToInt(fps)+" FPS",small);
            if(game.noticeTime>0){Rect(460,130,680,54,ink);Label(480,145,640,30,game.notice,eyebrow);}
            if(game.position.x<9 && game.mode==RailGame.Mode.Playing)
            {Rect(50,655,470,130,ink);Label(74,678,430,30,"KUFROVÝ KATAPULT",eyebrow);Label(74,720,420,55,"Šlápni na růžový kufr. Další skok můžeš udělat ve vzduchu.",body);}
        }
        void DrawStatus()
        {
            var chaos=game.chaos;
            if(chaos.infection>0)
            {
                Rect(28,127,360,65,ink);
                Label(50,139,310,24,"NÁKAZA  "+Mathf.CeilToInt(chaos.infection)+" %",eyebrow);
                Rect(50,172,316,5,new Color(.25f,.31f,.34f));
                Rect(50,172,316*chaos.infection/100,5,new Color(.7f,.93f,.27f));
            }
            if(chaos.turbo>0)
            {
                Rect(28,207,360,45,ink);
                Label(50,218,310,25,"TVARŮŽKOVÉ TURBO  "+Mathf.CeilToInt(chaos.turbo)+" s",eyebrow);
            }
            Rect(1200,127,372,54,ink);
            Label(1224,139,315,24,chaos.dashCooldown>0?"ÚHYB SE NABÍJÍ":"SHIFT  /  ÚHYB PŘIPRAVEN",small);
            Rect(1224,168,320*(1-chaos.dashCooldown/1.25f),4,amber);
        }
        void DrawPops(float scale)
        {
            if(scale<=0)return;
            foreach(var pop in game.chaos.pops)
            {
                if(pop.life<=0)continue;
                Vector3 screen=game.view.WorldToScreenPoint(new Vector3(pop.at.x,pop.at.y,-.4f));
                if(screen.z<=0)continue;
                float x=(screen.x-(Screen.width-1600*scale)/2)/scale;
                float y=(Screen.height-screen.y-(Screen.height-900*scale)/2)/scale;
                float alpha=Mathf.Clamp01(pop.life*3);
                popup.normal.textColor=new Color(.04f,.06f,.08f,alpha);
                Label(x-188,y+2,380,56,pop.text,popup);
                popup.normal.textColor=new Color(pop.color.r,pop.color.g,pop.color.b,alpha);
                Label(x-190,y,380,56,pop.text,popup);
            }
        }
        void Overlay(string heading,string description,bool paused)
        {
            Rect(0,0,1600,900,new Color(.025f,.06f,.09f,.72f));Rect(420,190,760,530,ink);
            Label(474,231,660,28,paused?"PAUZA  /  NÁSTUPIŠTĚ JE TVOJE":"KONEC JÍZDY  /  "+game.Zone,eyebrow);
            Label(472,292,670,72,heading,display);Label(474,379,650,60,description,body);
            Label(474,456,650,45,"BODY  "+game.score+"     KOMBO ×"+game.chaos.bestCombo+"     ČAS  "+game.elapsed.ToString("0.0")+" s",body);
            Label(474,498,650,30,"REKORD  "+PlayerPrefs.GetInt("best",0)+"     ODRAZY  "+game.chaos.stomps,small);
            if(Button(474,542,652,paused?"POKRAČOVAT                              →":"ZNOVU NA TRAŤ                        →")){if(paused)game.TogglePause();else game.StartRun();}
            if(Button(474,614,316,"HLAVNÍ MENU",false)){settings=false;game.Menu();}
            if(Button(810,614,316,"NASTAVENÍ",false))settings=!settings;
            if(settings)Settings(430,205);
        }
        void Settings(float x,float y)
        {
            Rect(x,y,650,585,ink);Label(x+34,y+30,590,55,"NASTAVENÍ",display);
            if(Button(x+34,y+110,582,"ZVUK: "+(game.muted?"VYPNUTÝ":"ZAPNUTÝ"),false)){game.muted=!game.muted;game.SaveSettings();}
            if(Button(x+34,y+186,582,"GRAFIKA: "+(game.lowQuality?"ÚSPORNÁ":"VYSOKÁ"),false)){game.lowQuality=!game.lowQuality;game.SaveSettings();}
            if(Button(x+34,y+262,582,"OTŘESY KAMERY: "+(game.reducedMotion?"VYPNUTÉ":"ZAPNUTÉ"),false)){game.reducedMotion=!game.reducedMotion;game.SaveSettings();}
            Label(x+34,y+344,580,31,"ŘEV PŘI ZÁSAHU  "+Mathf.RoundToInt(game.hitShoutVolume*100)+" %",body);
            float volume=GUI.HorizontalSlider(new Rect(x+38,y+386,570,25),game.hitShoutVolume,0,1);
            if(!Mathf.Approximately(volume,game.hitShoutVolume))
            {
                game.hitShoutVolume=volume;
                game.SaveSettings();
            }
            Label(x+34,y+420,580,32,"Ovlivní jen řev. Náraz a hlášky zůstanou slyšet.",small);
            if(Button(x+34,y+485,582,"HOTOVO"))settings=false;
        }
    }
}
