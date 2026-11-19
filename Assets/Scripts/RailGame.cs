using System;
using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailGame : MonoBehaviour
    {
        public enum Mode { Menu, Loading, Playing, Paused, Won, Lost }
        const float FixedStep = 1f/120;
        /// <summary>Motor constants, shared with RailMotor and the level reachability tests.</summary>
        public const float RunSpeed = 9f, JumpSpeed = 16f, AirJumpSpeed = 14.15f, Gravity = 40f;
        public const float FallGravityMultiplier = 1.5f, MaxFallSpeed = 20f, FastFallGravityMultiplier = 2.2f, FastFallSpeed = 27f;
        public const float ApexGravityMultiplier = .5f, ApexThreshold = 2f, JumpCutSpeed = 6f;
        public const float GroundAccel = 95f, GroundDecel = 120f, GroundTurn = 170f, AirAccel = 65f, AirDecel = 30f, AirTurn = 110f;
        public const float CoyoteTime = .1f, JumpBufferTime = .14f, LedgeAssist = .3f;
        public const float DashSpeed = 17f, DashTime = .17f, DashCooldown = 1f;
        public const float SpringSpeed = 21.5f, StompBounce = 13.7f, StompJump = 17f;
        public const float KnockbackX = 5.5f, KnockbackY = 8.4f, KnockbackTime = .2f, KillPlane = -.95f;
        /// <summary>Camera: orthographic half-height while playing, look-ahead and distance from the play plane.</summary>
        public const float PlaySize = 5.2f, LookAhead = 2.4f, CameraDistance = 14f;
        public Mode mode = Mode.Menu;
        [NonSerialized] public RailWorld world;
        [NonSerialized] public RailChaos chaos;
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
        /// <summary>Fixed horizontal input for benchmarks and scripted checks; NaN reads the player's input.</summary>
        public float InputOverride = float.NaN;
        /// <summary>One full (held) jump for scripted checks.</summary>
        public bool JumpOverride;
        /// <summary>When set, drives the input instead of the player (tests, benchmark tour).</summary>
        [NonSerialized] public RailAutopilot autopilot;
        /// <summary>Counters the character animation reads to fire jump, air-jump and landing reactions.</summary>
        public int JumpCount { get; private set; }
        public int AirJumpCount { get; private set; }
        public int LandCount { get; private set; }
        public bool FastFalling => fastFall && !grounded && velocity.y<ApexThreshold;
        public bool HeroVisible => playerRig==null || playerRig.Visible;
        public float AxisInput => axis;
        float jumpBuffer, coyote, axis, knockback, shake, accumulator, hurtBlink, jumpOverrideHold;
        bool jumpHeld, fastFall, jumpCut;
        int pendingLevel, loadingFrames, freezeFrames;
        Vector2 previousPosition;
        RailWorld.Platform standing;
        float cameraX, cameraXVelocity, cameraLook, cameraLookVelocity, cameraAnchor, cameraAnchorVelocity, lastGroundY;
        CjRig playerRig;
        RailPost post;
        float[] attackTimers;
        bool[] hasThrown;
        readonly Transform[] projectiles = new Transform[12];
        readonly Vector2[] projectileVelocity = new Vector2[12];
        readonly float[] projectileLife = new float[12];

        void Awake()
        {
            // The simulation runs at a fixed 120 Hz and is interpolated, so rendering follows the display refresh.
            Application.targetFrameRate=-1;Application.runInBackground=true;QualitySettings.vSyncCount=1;
            LoadLevel(0);
            view=new GameObject("Game Camera").AddComponent<Camera>(); view.tag="MainCamera";
            view.orthographic=true;view.orthographicSize=7.5f;view.nearClipPlane=.3f;view.farClipPlane=160;
            view.backgroundColor=new Color(.64f,.74f,.79f);view.clearFlags=CameraClearFlags.Skybox;
            view.transform.rotation=Quaternion.Euler(8,-7,0);
            PlaceCamera(new Vector3(9.9f,4.3f,0));
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
        /// <summary>Upward launch (spring, stomp). A cuttable launch is shortened when jump is not held, like a jump.</summary>
        public void Launch(float power,bool cuttable=false){velocity.y=power;grounded=false;standing=null;coyote=0;jumpBuffer=0;jumpCut=cuttable;chaos.airJumpReady=true;}
        /// <summary>Bounce off a stomped enemy; holding or buffering jump turns it into a higher stomp jump.</summary>
        public void StompLaunch()
        {
            bool jumping=jumpHeld || jumpBuffer>0;
            // Only a held jump is cut on release; a buffered tap still gets the full stomp jump.
            Launch(jumping?StompJump:StompBounce,jumpHeld);
            invulnerability=Mathf.Max(invulnerability,.3f);
        }
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
            if(next!=Mode.Playing)playerRig?.SetVisible(true);
        }
        public void StartRun()
        {
            SetMode(Mode.Playing);lives=Level.Lives;score=0;checkpoint=0;elapsed=0;simTime=0;position=Level.Checkpoint(0);previousPosition=position;velocity=Vector2.zero;standing=null;
            grounded=false;invulnerability=0;hurtBlink=0;jumpBuffer=0;coyote=0;knockback=0;accumulator=0;shake=0;wind=0;windWarning=false;jumpHeld=fastFall=jumpCut=false;newRecord=false;
            bestTime=PlayerPrefs.GetFloat(Level.TimeKey,0);bestScore=PlayerPrefs.GetInt(Level.ScoreKey,0);
            audioMix.ResetRun();chaos.Reset();playerRig.Reset();hero.localScale=Vector3.one;playerRig.SetVisible(true);
            world.ResetPickups();
            world.ResetSignals();
            ClearProjectiles();
            for(int i=0;i<attackTimers.Length;i++){attackTimers[i]=i*.6f;hasThrown[i]=false;}
            Notice(Level.Intro);
            freezeFrames=1;
            lastGroundY=position.y;SnapCamera();
        }
        void ClearProjectiles(){for(int i=0;i<projectiles.Length;i++){projectileLife[i]=0;projectiles[i].gameObject.SetActive(false);}}
        public void Notice(string text){notice=text;noticeTime=4;}
        public void TogglePause(){if(mode==Mode.Playing)SetMode(Mode.Paused);else if(mode==Mode.Paused)SetMode(Mode.Playing);}
        public void Menu()
        {
            SetMode(Mode.Menu);InputOverride=float.NaN;shake=0;
            ClearProjectiles();chaos.ClearTransient();
            foreach(var signal in world.signals)signal.gameObject.SetActive(false);
        }
        void OnApplicationFocus(bool focus){if(!focus && mode==Mode.Playing && float.IsNaN(InputOverride) && autopilot==null)SetMode(Mode.Paused);}
        /// <summary>The single input entry point: movement axis, jump press this frame, jump held, fast fall.</summary>
        public void Feed(float moveAxis,bool pressed,bool held,bool fall)
        {
            axis=Mathf.Clamp(moveAxis,-1,1);
            if(pressed)jumpBuffer=JumpBufferTime;
            jumpHeld=held;fastFall=fall;
        }
        void Update()
        {
            // At most one menu-level command per frame, so a key never acts twice (e.g. R restarting and respawning).
            bool consumed=false;
            if(!uiModal)
            {
                if(RailInput.Pause){consumed=true;if(mode==Mode.Won||mode==Mode.Lost)Menu();else TogglePause();}
                else if(RailInput.Mute){muted=!muted;SaveSettings();}
                else if(mode==Mode.Menu && RailInput.Confirm){consumed=true;StartLevel(selectedLevel,true);}
                else if((mode==Mode.Won||mode==Mode.Lost) && RailInput.Confirm){consumed=true;FinishAction(true);}
                else if((mode==Mode.Won||mode==Mode.Lost) && RailInput.Restart){consumed=true;StartRun();}
                else if(mode==Mode.Menu && RailInput.MenuDown)selectedLevel=(selectedLevel+1)%RailLevel.All.Length;
                else if(mode==Mode.Menu && RailInput.MenuUp)selectedLevel=(selectedLevel+RailLevel.All.Length-1)%RailLevel.All.Length;
                else if(mode==Mode.Playing && RailInput.Restart){consumed=true;Respawn(false);}
            }
            if(mode==Mode.Loading && --loadingFrames<=0){LoadLevel(pendingLevel);StartRun();}
            if(mode!=Mode.Paused)noticeTime-=Time.deltaTime;
            if(mode==Mode.Playing)
            {
                float dt=Mathf.Min(Time.deltaTime,.08f);
                // The frame after a (re)start carries the loading time; skip it instead of fast-forwarding.
                if(freezeFrames>0){freezeFrames--;dt=0;}
                if(autopilot!=null)autopilot.Drive(this);
                else if(!consumed && !uiModal)
                {
                    if(JumpOverride)jumpOverrideHold=.35f;
                    jumpOverrideHold-=Time.deltaTime;
                    Feed(float.IsNaN(InputOverride)?RailInput.Move:InputOverride,RailInput.JumpPressed||JumpOverride,RailInput.JumpHeld||jumpOverrideHold>0,RailInput.FastFall);
                    if(RailInput.Dash)chaos.Dash();
                }
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
            simTime+=dt;elapsed+=dt;invulnerability-=dt;hurtBlink-=dt;knockback-=dt;
            chaos.BeforeStep(dt);
            world.Tick(simTime,dt);
            if(standing!=null)position.x+=standing.delta;
            coyote=grounded?CoyoteTime:coyote-dt;jumpBuffer-=dt;
            if(chaos.dash>0)velocity.x=(Mathf.Abs(axis)>.1f?Mathf.Sign(axis):playerRig.Direction)*DashSpeed;
            else if(knockback<=0)velocity.x=RailMotor.Horizontal(velocity.x,axis,RunSpeed*chaos.SpeedMultiplier,grounded,dt);
            if(jumpBuffer>0 && coyote>0)
            {velocity.y=JumpSpeed;grounded=false;standing=null;coyote=0;jumpBuffer=0;jumpCut=true;JumpCount++;audioMix.Jump();}
            // The double jump never lowers an ongoing rise; an early press stays buffered until it would help.
            else if(jumpBuffer>0 && !grounded && velocity.y<AirJumpSpeed && !NearLanding() && chaos.AirJump())
            {velocity.y=AirJumpSpeed;jumpBuffer=0;jumpCut=true;AirJumpCount++;audioMix.Spring();}
            velocity.y=RailMotor.Vertical(velocity.y,ref jumpCut,jumpHeld,fastFall,dt);
            UpdateWind();
            var old=position;var next=position+velocity*dt;
            next.x+=wind*dt*(grounded?.45f:1);
            bool wasGrounded=grounded;
            grounded=false;standing=null;
            const float radius=.25f;
            foreach(var p in world.platforms)
            {
                // Swept one-way roof landing; feet are authoritative.
                if(velocity.y<=0 && old.y>=p.top-.06f && next.y<=p.top && next.x+radius>p.Left && next.x-radius<p.Right)
                {next.y=p.top;velocity.y=0;grounded=true;standing=p;continue;}
                // Below the roof every platform is solid down to the ground.
                if(old.y<p.top-.06f && next.y<p.top-.06f)
                {
                    // Edges are compared where they were last step, so a wagon swaying into the player counts as a hit too.
                    bool fromLeft=old.x+radius<=p.Left-p.delta+.03f && next.x+radius>p.Left;
                    bool fromRight=old.x-radius>=p.Right-p.delta-.03f && next.x-radius<p.Right;
                    if(!fromLeft && !fromRight)continue;
                    // Ledge assist: just missing the edge by a few centimetres still lands on the roof.
                    if(next.y>=p.top-LedgeAssist && velocity.y<=3){next.y=p.top;velocity.y=0;grounded=true;standing=p;continue;}
                    if(fromLeft){next.x=p.Left-radius;velocity.x=Mathf.Min(0,velocity.x);}
                    else{next.x=p.Right+radius;velocity.x=Mathf.Max(0,velocity.x);}
                }
            }
            position=next;position.x=Mathf.Max(world.platforms[0].Left+radius,position.x);
            if(grounded){lastGroundY=position.y;if(!wasGrounded)LandCount++;}
            if(position.y<KillPlane){Respawn(true);return;}
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
                if(cycle>=2.9f && !hasThrown[i]){Throw(man.position);hasThrown[i]=true;}
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
            float reach=-velocity.y*JumpBufferTime+.05f;
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
        /// <summary>The inspector lobs his bag so that it arrives at the player's chest height (gravity 5 m/s²).</summary>
        void Throw(Vector3 man)
        {
            for(int i=0;i<projectiles.Length;i++)if(projectileLife[i]<=0)
            {
                float direction=position.x<man.x?-1:1;
                var origin=man+new Vector3(direction*.35f,1.55f,0);
                float time=Mathf.Max(.25f,Mathf.Abs(position.x-origin.x)/7);
                float rise=position.y+.85f-origin.y;
                projectiles[i].position=origin;
                projectileVelocity[i]=new Vector2(direction*7,Mathf.Clamp((rise+2.5f*time*time)/time,-3,7));
                projectileLife[i]=3;projectiles[i].gameObject.SetActive(true);break;
            }
        }
        public void Damage(float sourceX)
        {
            if(invulnerability>0 || chaos.dash>0 || mode!=Mode.Playing)return;
            chaos.BreakCombo();chaos.PopText("ÁÁÁ! REKLAMACE!",position+Vector2.up*2, new Color(1,.46f,.4f));
            lives--;audioMix.Hit();shake=.34f;invulnerability=2;hurtBlink=2;
            velocity=new Vector2(position.x<sourceX?-KnockbackX:KnockbackX,KnockbackY);knockback=KnockbackTime;grounded=false;standing=null;coyote=0;jumpCut=false;
            if(lives<=0)SetMode(Mode.Lost);
        }
        public void Respawn(bool loseLife)
        {
            if(loseLife){lives--;audioMix.Hit();if(lives<=0){SetMode(Mode.Lost);return;}}
            position=Level.Checkpoint(checkpoint);previousPosition=position;
            velocity=Vector2.zero;grounded=false;standing=null;invulnerability=hurtBlink=loseLife?2:.6f;jumpBuffer=0;coyote=0;knockback=0;jumpCut=false;
            // A manual reset (R) must not cure infection for free.
            chaos.ClearTransient(!loseLife);chaos.BreakCombo();
            ClearProjectiles();audioMix.Quiet();
            Notice(loseLife?"ZPÁTKY NA NÁSTUPIŠTI.  Zkus další skok.":"NÁVRAT NA CHECKPOINT");
            lastGroundY=position.y;SnapCamera();
        }
        void AnimateHero(float dt)
        {
            playerRig.Pose(RenderPosition,velocity,grounded,chaos.dash>0,knockback>0,dt);
            // Only real hits and respawns blink; a stomp's short invulnerability does not.
            playerRig.SetVisible(reducedMotion || hurtBlink<=0 || (int)(simTime*12)%2==0);
        }
        /// <summary>Puts the camera CameraDistance metres back along its view axis from a point on the play plane.</summary>
        void PlaceCamera(Vector3 framing)=>view.transform.position=framing-view.transform.forward*CameraDistance;
        float CameraMinX => world.platforms[0].Left+6;
        float CameraMaxX => Level.Finish-2;
        void SnapCamera()
        {
            if(!view)return;
            cameraLook=playerRig!=null?playerRig.Direction*LookAhead:LookAhead;cameraLookVelocity=0;
            cameraX=Mathf.Clamp(position.x+cameraLook,CameraMinX,CameraMaxX);cameraXVelocity=0;
            cameraAnchor=lastGroundY;cameraAnchorVelocity=0;
        }
        void UpdateCamera(float dt)
        {
            shake=Mathf.Max(0,shake-dt);
            float aspect=view.aspect;
            if(!(aspect>.4f))aspect=.4f; // also catches NaN from a zero-height window
            // Keep at least ~9 m visible ahead and behind on narrow windows.
            float size=mode==Mode.Menu?Mathf.Max(7.5f,13.3f/aspect):Mathf.Max(PlaySize,9.2f/aspect);
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,size,1-Mathf.Exp(-dt*8));
            if(mode==Mode.Menu){PlaceCamera(Vector3.Lerp(view.transform.position+view.transform.forward*CameraDistance,new Vector3(9.9f,4.3f,0),1-Mathf.Exp(-dt*4)));return;}
            var focus=RenderPosition;
            float step=Mathf.Max(dt,1e-4f);
            cameraLook=Mathf.SmoothDamp(cameraLook,playerRig.Direction*LookAhead,ref cameraLookVelocity,.4f,Mathf.Infinity,step);
            cameraX=Mathf.SmoothDamp(cameraX,Mathf.Clamp(focus.x+cameraLook,CameraMinX,CameraMaxX),ref cameraXVelocity,.09f,Mathf.Infinity,step);
            // Vertical framing anchors to the last roof; it follows high jumps late and falls quickly.
            float desired=lastGroundY;
            if(focus.y>desired+3.2f)desired=focus.y-3.2f;
            bool falling=focus.y<desired-1f;
            if(falling)desired=focus.y+1f;
            cameraAnchor=Mathf.SmoothDamp(cameraAnchor,desired,ref cameraAnchorVelocity,falling?.07f:.25f,Mathf.Infinity,step);
            PlaceCamera(new Vector3(cameraX,cameraAnchor+1.75f,0));
            if(shake>0&&!reducedMotion&&mode==Mode.Playing)view.transform.position+=new Vector3(Mathf.Sin(Time.time*90),Mathf.Cos(Time.time*80),0)*shake*.12f;
        }
        public string Snapshot() => JsonUtility.ToJson(new SnapshotData{mode=mode.ToString(),level=LevelIndex,x=position.x,y=position.y,grounded=grounded,lives=lives,score=score,checkpoint=checkpoint,elapsed=elapsed,infection=chaos.infection,combo=chaos.combo,tickets=chaos.ticketsCollected});
        [Serializable] class SnapshotData {public string mode;public float x,y,elapsed,infection;public int level,lives,score,checkpoint,combo,tickets;public bool grounded;}
    }
}
