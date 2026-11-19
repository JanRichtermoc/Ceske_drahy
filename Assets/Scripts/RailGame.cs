using System;
using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailGame : MonoBehaviour
    {
        public enum Mode { Menu, Loading, Playing, Paused, Won, Lost }
        const float FixedStep = 1f/120;
        public Mode mode = Mode.Menu;
        public RailWorld world;
        public RailChaos chaos;
        public RailAudio audioMix;
        public float hitShoutVolume;
        public Vector2 position = new Vector2(2,.12f), velocity;
        public bool grounded, muted, lowQuality, reducedMotion;
        public int lives = 3, score, checkpoint, bestScore;
        public float elapsed, invulnerability, simTime, bestTime, wind;
        public bool newRecord, windWarning;
        /// <summary>Set by the HUD while a modal panel owns keyboard and mouse input.</summary>
        public bool uiModal;
        public int selectedLevel;
        public int ModeVersion { get; private set; }
        public int LevelIndex { get; private set; }
        public RailLevel Level => RailLevel.All[LevelIndex];
        public bool HasNextLevel => LevelIndex+1<RailLevel.All.Length;
        public float Progress => Mathf.Clamp01(position.x/Level.Finish);
        public string Zone => Level.ZoneAt(position.x);
        public Vector2 RenderPosition => mode==Mode.Playing?Vector2.Lerp(previousPosition,position,Mathf.Clamp01(accumulator/FixedStep)):position;
        public Transform hero;
        public Camera view;
        public string notice = "";
        public float noticeTime;
        public float InputOverride = float.NaN;
        public bool JumpOverride;
        float jumpBuffer, coyote, axis, knockback, shake, accumulator;
        bool jumpReleased, jumpCut;
        int pendingLevel, loadingFrames;
        Vector2 previousPosition;
        RailWorld.Platform standing;
        Vector3 cameraVelocity;
        CjRig playerRig;
        RailPost post;
        float[] attackTimers;
        bool[] hasThrown;
        readonly Transform[] projectiles = new Transform[12];
        readonly Vector2[] projectileVelocity = new Vector2[12];
        readonly float[] projectileLife = new float[12];

        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;QualitySettings.vSyncCount=1;
            LoadLevel(0);
            view=new GameObject("Game Camera").AddComponent<Camera>(); view.tag="MainCamera";
            view.orthographic=true;view.orthographicSize=6.9f;view.farClipPlane=150;
            view.backgroundColor=new Color(.64f,.74f,.79f);view.clearFlags=CameraClearFlags.Skybox;
            view.transform.rotation=Quaternion.Euler(9,-10,0);view.transform.position=new Vector3(14,8,-23);
            view.gameObject.AddComponent<AudioListener>();
            post=view.gameObject.AddComponent<RailPost>();post.Apply(world.Grading);
            muted=PlayerPrefs.GetInt("muted",0)==1;lowQuality=PlayerPrefs.GetInt("lowQuality",0)==1;reducedMotion=PlayerPrefs.GetInt("reducedMotion",0)==1;ApplyQuality();
            hitShoutVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("hitShoutVolume",.94f));
            audioMix=gameObject.AddComponent<RailAudio>();
            audioMix.Initialize(this);
            gameObject.AddComponent<RailHUD>().game=this;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rail-benchmark")>=0)gameObject.AddComponent<RailBenchmark>();
        }
        void LoadLevel(int index)
        {
            world?.Dispose();
            LevelIndex=index;
            world=new RailWorld();
            world.Build(index);
            if(post)post.Apply(world.Grading);
            playerRig=new CjRig(world);
            hero=playerRig.Root;
            attackTimers=new float[world.inspectors.Count];
            hasThrown=new bool[world.inspectors.Count];
            chaos=new RailChaos(this);
            for(int i=0;i<projectiles.Length;i++)
            {
                projectiles[i]=world.Box("Revizorova brašna",Vector3.zero,new Vector3(.42f,.3f,.2f),world.rust);
                projectiles[i].gameObject.SetActive(false);
            }
        }
        /// <summary>Starts a route. With showLoading the HUD gets one frame to draw the loading card first.</summary>
        public void StartLevel(int index,bool showLoading=false)
        {
            if(index<0 || index>=RailLevel.All.Length)return;
            selectedLevel=index;
            if(index==LevelIndex){StartRun();return;}
            if(showLoading){pendingLevel=index;loadingFrames=2;SetMode(Mode.Loading);return;}
            LoadLevel(index);
            StartRun();
        }
        public void FinishAction(bool showLoading=false)
        {
            if(mode==Mode.Won && HasNextLevel)StartLevel(LevelIndex+1,showLoading);
            else StartRun();
        }
        public void Boing(){audioMix.Spring();}
        public void Celebrate(){audioMix.Celebrate();}
        public void Launch(float power){velocity.y=power;grounded=false;standing=null;coyote=0;jumpBuffer=0;jumpCut=false;chaos.airJumpReady=true;}
        public void ApplyQuality(){QualitySettings.shadows=lowQuality?ShadowQuality.Disable:ShadowQuality.All;QualitySettings.antiAliasing=lowQuality?0:4;if(post)post.enabled=!lowQuality;}
        public void SaveSettings(){PlayerPrefs.SetInt("muted",muted?1:0);PlayerPrefs.SetInt("lowQuality",lowQuality?1:0);PlayerPrefs.SetInt("reducedMotion",reducedMotion?1:0);PlayerPrefs.SetFloat("hitShoutVolume",hitShoutVolume);PlayerPrefs.Save();ApplyQuality();}
        public void ToggleFullscreen(){Screen.fullScreenMode=Screen.fullScreen?FullScreenMode.Windowed:FullScreenMode.FullScreenWindow;}
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void SetMode(Mode next)
        {
            if(mode==next)return;
            mode=next;ModeVersion++;
            // The hit blink may have hidden the hero on the frame the run ended.
            if(next!=Mode.Playing && hero)hero.gameObject.SetActive(true);
        }
        public void StartRun()
        {
            SetMode(Mode.Playing);lives=Level.Lives;score=0;checkpoint=0;elapsed=0;simTime=0;position=Level.Checkpoint(0);previousPosition=position;velocity=Vector2.zero;standing=null;
            grounded=false;invulnerability=0;jumpBuffer=0;coyote=0;knockback=0;accumulator=0;shake=0;wind=0;windWarning=false;jumpReleased=jumpCut=false;newRecord=false;
            bestTime=PlayerPrefs.GetFloat(Level.TimeKey,0);bestScore=PlayerPrefs.GetInt(Level.ScoreKey,0);
            audioMix.ResetRun();chaos.Reset();playerRig.Reset();hero.localScale=Vector3.one;hero.gameObject.SetActive(true);
            for(int i=0;i<world.tickets.Count;i++)world.tickets[i].gameObject.SetActive(true);
            world.ResetSignals();
            ClearProjectiles();
            for(int i=0;i<attackTimers.Length;i++){attackTimers[i]=i*.6f;hasThrown[i]=false;}
            Notice(Level.Intro);
            view.transform.position=new Vector3(8,6,-23);cameraVelocity=Vector3.zero;
        }
        void ClearProjectiles(){for(int i=0;i<projectiles.Length;i++){projectileLife[i]=0;projectiles[i].gameObject.SetActive(false);}}
        public void Notice(string text){notice=text;noticeTime=4;}
        public void TogglePause(){if(mode==Mode.Playing)SetMode(Mode.Paused);else if(mode==Mode.Paused)SetMode(Mode.Playing);}
        public void Menu()
        {
            SetMode(Mode.Menu);InputOverride=float.NaN;shake=0;
            ClearProjectiles();chaos.ClearTransient();
        }
        void OnApplicationFocus(bool focus){if(!focus && mode==Mode.Playing && float.IsNaN(InputOverride))SetMode(Mode.Paused);}
        void Update()
        {
            bool confirm=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
            if(!uiModal)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){if(mode==Mode.Won||mode==Mode.Lost)Menu();else TogglePause();}
                if(Input.GetKeyDown(KeyCode.M)){muted=!muted;SaveSettings();}
                if(mode==Mode.Menu && confirm)StartLevel(selectedLevel,true);
                else if((mode==Mode.Won||mode==Mode.Lost) && confirm)FinishAction(true);
            }
            if(mode==Mode.Loading && --loadingFrames<=0){LoadLevel(pendingLevel);StartRun();}
            if(mode==Mode.Playing && Input.GetKeyDown(KeyCode.R))Respawn(false);
            if(mode==Mode.Playing)
            {
                float dt=Mathf.Min(Time.deltaTime,.08f);
                axis=float.IsNaN(InputOverride)?((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0)):InputOverride;
                bool pressed=Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow)||JumpOverride;
                // Latched until a fixed step consumes it: frames shorter than one step run no Step at all.
                if(Input.GetKeyUp(KeyCode.Space)||Input.GetKeyUp(KeyCode.W)||Input.GetKeyUp(KeyCode.UpArrow))jumpReleased=true;
                if(pressed)jumpBuffer=.14f;
                if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))chaos.Dash();
                accumulator+=dt;
                while(mode==Mode.Playing && accumulator>=FixedStep){Step(FixedStep);accumulator-=FixedStep;}
                world.Interpolate(Mathf.Clamp01(accumulator/FixedStep));
                world.Weather(wind,windWarning,simTime,position.x);
                AnimateHero(dt);
            }
            else if(mode==Mode.Menu)
            {
                world.Tick(Time.time*.35f,Time.deltaTime);
                var showcase=world.platforms[1];
                hero.localScale=Vector3.one*1.42f;
                playerRig.Pose(new Vector2(showcase.x+showcase.offset-1,showcase.top),Vector2.zero,true,false,false,Time.deltaTime,true);
            }
            JumpOverride=false;
            if(mode!=Mode.Paused)UpdateCamera(Time.deltaTime);
        }
        public void Step(float dt)
        {
            previousPosition=position;
            simTime+=dt;elapsed+=dt;noticeTime-=dt;invulnerability-=dt;knockback-=dt;
            chaos.BeforeStep(dt);
            world.Tick(simTime,dt);
            if(standing!=null)position.x+=standing.delta;
            coyote=grounded?.11f:coyote-dt;jumpBuffer-=dt;
            if(chaos.dash>0)velocity.x=(Mathf.Abs(axis)>.1f?Mathf.Sign(axis):playerRig.Direction)*16;
            else if(knockback<=0)velocity.x=Mathf.MoveTowards(velocity.x,axis*7.8f*chaos.SpeedMultiplier,(grounded?55:32)*dt);
            if(jumpBuffer>0 && coyote>0){velocity.y=13.6f;grounded=false;standing=null;coyote=0;jumpBuffer=0;jumpCut=true;audioMix.Jump();}
            else if(jumpBuffer>0 && !grounded && !NearLanding() && chaos.AirJump()){velocity.y=12.3f;jumpBuffer=0;jumpCut=true;audioMix.Spring();}
            // Only the player's own jump is shortened; springs, stomps and knockback keep their launch.
            if(jumpReleased){if(jumpCut && velocity.y>5)velocity.y=5;jumpReleased=false;}
            if(velocity.y<=0)jumpCut=false;
            velocity.y-=28*dt;
            UpdateWind();
            var old=position;var next=position+velocity*dt;
            next.x+=wind*dt*(grounded?.45f:1);
            grounded=false;standing=null;
            foreach(var p in world.platforms)
            {
                const float radius=.25f;
                // Solid side faces, then swept one-way roof landing. Feet are authoritative.
                if(old.y<p.top-.06f && next.y+1.75f>p.top-2 && next.y<p.top-.06f)
                {
                    if(old.x+radius<=p.Left+.03f && next.x+radius>p.Left){next.x=p.Left-radius;velocity.x=0;}
                    if(old.x-radius>=p.Right-.03f && next.x-radius<p.Right){next.x=p.Right+radius;velocity.x=0;}
                }
                if(velocity.y<=0 && old.y>=p.top-.06f && next.y<=p.top && next.x+radius>p.Left && next.x-radius<p.Right)
                {next.y=p.top;velocity.y=0;grounded=true;standing=p;}
            }
            position=next;position.x=Mathf.Max(-7.5f,position.x);
            if(position.y< -4){Respawn(true);return;}
            for(int i=0;i<world.tickets.Count;i++)if(world.tickets[i].gameObject.activeSelf && Vector2.Distance(position+Vector2.up*.9f,world.tickets[i].position)<.85f)
            {world.tickets[i].gameObject.SetActive(false);chaos.Ticket();audioMix.Ticket();}
            for(int i=0;i<world.birds.Count;i++)if(!chaos.Bird(i,old)&&Vector2.Distance(position+Vector2.up,world.birds[i].position)<.67f)Damage(world.birds[i].position.x);
            if(mode!=Mode.Playing)return;
            for(int i=0;i<world.inspectors.Count;i++)
            {
                var man=world.inspectors[i];float distance=Mathf.Abs(position.x-man.position.x);
                if(chaos.Inspector(i,old)){world.signals[i].gameObject.SetActive(false);continue;}
                if(distance>12){world.signals[i].gameObject.SetActive(false);continue;}
                attackTimers[i]+=dt;float cycle=attackTimers[i]%4.6f;
                bool warning=cycle>2.1f && cycle<2.9f;world.signals[i].gameObject.SetActive(warning);
                if(warning)world.signals[i].localScale=Vector3.one*(.18f+Mathf.PingPong(simTime*1.3f,.2f));
                if(cycle>=2.9f && !hasThrown[i]){Throw(man.position+Vector3.up*1.3f);hasThrown[i]=true;}
                if(cycle<1)hasThrown[i]=false;
                if(distance<.6f && Mathf.Abs(position.y-man.position.y)<1.7f)Damage(man.position.x);
            }
            if(mode!=Mode.Playing)return;
            for(int i=0;i<projectiles.Length;i++)if(projectileLife[i]>0)
            {
                projectileLife[i]-=dt;projectileVelocity[i].y-=5*dt;projectiles[i].position+=(Vector3)(projectileVelocity[i]*dt);projectiles[i].Rotate(0,0,dt*220);
                if(Vector2.Distance(position+Vector2.up*.85f,projectiles[i].position)<.62f){Damage(projectiles[i].position.x);projectileLife[i]=0;}
                if(projectileLife[i]<=0)projectiles[i].gameObject.SetActive(false);
            }
            if(mode!=Mode.Playing)return;
            if(grounded && checkpoint<Level.CheckpointCount && standing==world.platforms[Level.Stations[checkpoint+1]])
            {
                checkpoint++;
                world.SetSignal(checkpoint-1,true);
                lives=Mathf.Max(lives,2);
                Notice("CHECKPOINT  "+checkpoint+" / "+Level.CheckpointCount);
                Celebrate();
            }
            if(world.SteamHit(position))Damage(position.x+.2f);
            if(mode!=Mode.Playing)return;
            chaos.AfterMove(dt,old);
            if(mode==Mode.Playing && grounded && position.x>=Level.Finish)Win();
        }
        /// <summary>A press just before touching a roof is kept for the stronger ground jump.</summary>
        bool NearLanding()
        {
            if(velocity.y>0)return false;
            float reach=-velocity.y*.14f+.05f;
            foreach(var p in world.platforms)
                if(position.x+.25f>p.Left && position.x-.25f<p.Right && position.y>=p.top-.06f && position.y-p.top<reach)return true;
            return false;
        }
        void UpdateWind()
        {
            wind=0;windWarning=false;
            if(Level.Wind<=0)return;
            float cycle=(simTime+2)%7;
            windWarning=cycle>4.1f && cycle<5.2f;
            if(cycle>=5.2f)wind=-Level.Wind*Mathf.Sin((cycle-5.2f)/1.8f*Mathf.PI);
        }
        void Win()
        {
            SetMode(Mode.Won);Celebrate();
            newRecord=bestTime<=0 || elapsed<bestTime;
            if(newRecord){bestTime=elapsed;PlayerPrefs.SetFloat(Level.TimeKey,elapsed);}
            if(score>bestScore){bestScore=score;PlayerPrefs.SetInt(Level.ScoreKey,score);}
            PlayerPrefs.Save();
        }
        void Throw(Vector3 origin)
        {
            for(int i=0;i<projectiles.Length;i++)if(projectileLife[i]<=0)
            {projectiles[i].position=origin;float direction=position.x<origin.x?-1:1;projectileVelocity[i]=new Vector2(direction*7,2);projectileLife[i]=3;projectiles[i].gameObject.SetActive(true);break;}
        }
        public void Damage(float sourceX)
        {
            if(invulnerability>0 || chaos.dash>0 || mode!=Mode.Playing)return;
            chaos.BreakCombo();chaos.PopText("ÁÁÁ! REKLAMACE!",position+Vector2.up*2, new Color(1,.46f,.4f));
            lives--;audioMix.Hit();shake=.34f;invulnerability=2;velocity=new Vector2(position.x<sourceX?-4.4f:4.4f,7);knockback=.23f;grounded=false;standing=null;coyote=0;jumpCut=false;
            if(lives<=0)SetMode(Mode.Lost);
        }
        public void Respawn(bool loseLife)
        {
            if(loseLife){lives--;audioMix.Hit();if(lives<=0){SetMode(Mode.Lost);return;}}
            position=Level.Checkpoint(checkpoint);previousPosition=position;
            velocity=Vector2.zero;grounded=false;standing=null;invulnerability=loseLife?2:.6f;jumpBuffer=0;coyote=0;knockback=0;jumpCut=false;
            // A manual reset (R) must not cure infection for free.
            chaos.ClearTransient(!loseLife);chaos.BreakCombo();
            ClearProjectiles();audioMix.Quiet();
            Notice(loseLife?"ZPÁTKY NA NÁSTUPIŠTI.  Zkus další skok.":"NÁVRAT NA CHECKPOINT");
        }
        void AnimateHero(float dt)
        {
            playerRig.Pose(RenderPosition,velocity,grounded,chaos.dash>0,knockback>0,dt);
            hero.gameObject.SetActive(reducedMotion || invulnerability<=0 || (int)(simTime*12)%2==0);
        }
        void UpdateCamera(float dt)
        {
            shake=Mathf.Max(0,shake-dt);
            var focus=RenderPosition;
            Vector3 target=mode==Mode.Menu?new Vector3(14,8,-23):new Vector3(Mathf.Clamp(focus.x+4,8,Level.Finish-3),6.3f+Mathf.Max(0,focus.y-3)*.65f,-23);
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,mode==Mode.Menu?8f:6.9f,Mathf.Clamp01(dt*8));
            view.transform.position=Vector3.SmoothDamp(view.transform.position,target,ref cameraVelocity,.22f,Mathf.Infinity,Mathf.Min(dt,.08f));
            if(shake>0&&!reducedMotion&&mode==Mode.Playing)view.transform.position+=new Vector3(Mathf.Sin(Time.time*90),Mathf.Cos(Time.time*80),0)*shake*.12f;
        }
        public string Snapshot() => JsonUtility.ToJson(new SnapshotData{mode=mode.ToString(),level=LevelIndex,x=position.x,y=position.y,grounded=grounded,lives=lives,score=score,checkpoint=checkpoint,elapsed=elapsed,infection=chaos.infection,combo=chaos.combo,tickets=chaos.ticketsCollected});
        [Serializable] class SnapshotData {public string mode;public float x,y,elapsed,infection;public int level,lives,score,checkpoint,combo,tickets;public bool grounded;}
    }
}
