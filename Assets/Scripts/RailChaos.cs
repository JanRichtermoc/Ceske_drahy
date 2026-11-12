using System;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Goofy arcade rules, reversible enemy stuns and pooled visual feedback.</summary>
    public sealed class RailChaos
    {
        public sealed class Pop { public Vector2 at;public string text;public float life;public Color color; }
        public readonly Pop[] pops=new Pop[18];
        public readonly Transform[] streetEnemies=new Transform[5];
        public readonly Transform[] medkits=new Transform[4];
        public readonly Transform[] cheeses=new Transform[3];
        public readonly Transform[] springs=new Transform[3];
        readonly Transform[] needles=new Transform[12],warning=new Transform[5],aura=new Transform[5];
        readonly Vector2[] needleVelocity=new Vector2[12];
        readonly float[] needleLife=new float[12],streetTime=new float[5],streetStun=new float[5],inspectorStun=new float[6],birdStun=new float[7],springCooldown=new float[3];
        readonly bool[] streetThrown=new bool[5],streetCalled=new bool[5];
        readonly Vector3[] medkitPositions=new Vector3[4],cheesePositions=new Vector3[3];
        readonly RailGame game;readonly RailWorld world;
        public float infection,turbo,dash,dashCooldown,comboTime;
        public int combo=1,bestCombo=1,ticketsCollected,stomps;
        public bool airJumpReady=true;
        float infectionProtection,announcementTimer=12;
        int popIndex,announcementIndex;
        public float SpeedMultiplier => (turbo>0?1.24f:1)*(infection>0?.92f:1);
        public RailChaos(RailGame owner)
        {
            game=owner;world=owner.world;
            for(int i=0;i<pops.Length;i++)pops[i]=new Pop();
            float[] xs=world.Level.Street;
            for(int i=0;i<xs.Length;i++)
            {
                float y=Surface(xs[i]);
                var man=world.Person("Bezdomovec / fiktivní podchodový alchymista "+i,new Vector3(xs[i],y,0),false,true);
                var roof=world.PlatformAt(xs[i]);if(roof!=null)man.SetParent(roof.root,true);
                streetEnemies[i]=man;
                var syringe=Syringe("Obří injekce",man);syringe.localPosition=new Vector3(-.6f,1.1f,-.12f);syringe.localScale=Vector3.one*1.4f;
                warning[i]=world.Box("Pozor na injekci",new Vector3(0,2.4f,0),Vector3.one*.3f,world.Mat("Lime serum","#B4EB43"),man,PrimitiveType.Sphere);
                warning[i].gameObject.SetActive(false);
                if(i==0||i==2)
                {
                    var cart=world.Group("Vozík s krabicemi",new Vector3(xs[i]+1.2f,y+.25f,.65f));
                    world.Box("Cart",new Vector3(0,.35f,0),new Vector3(.9f,.12f,.6f),world.steel,cart);
                    world.Box("Cardboard",new Vector3(0,.7f,0),new Vector3(.8f,.6f,.55f),world.Mat("Cardboard","#AE8D61"),cart);
                    world.Sign("NEŠŤOUCHAT",new Vector3(0,.75f,-.3f),.012f,cart);
                    foreach(float side in new[]{-.35f,.35f})world.Box("Cart wheel",new Vector3(side,0,-.25f),Vector3.one*.23f,world.dark,cart,PrimitiveType.Sphere);
                }
            }
            for(int i=0;i<needles.Length;i++){needles[i]=Syringe("Létající injekce");needles[i].gameObject.SetActive(false);}
            float[] heals=world.Level.Medkits;
            for(int i=0;i<heals.Length;i++)
            {
                var root=world.Group("Lékárnička",new Vector3(heals[i],Surface(heals[i])+1.25f,0));
                world.Box("Case",Vector3.zero,new Vector3(.6f,.5f,.24f),world.cream,root);
                world.Box("Cross upright",new Vector3(0,0,-.14f),new Vector3(.12f,.32f,.025f),world.green,root);
                world.Box("Cross crossbar",new Vector3(0,0,-.15f),new Vector3(.32f,.12f,.025f),world.green,root);
                medkits[i]=root;medkitPositions[i]=root.position;
            }
            float[] snacks=world.Level.Snacks;
            for(int i=0;i<snacks.Length;i++)
            {
                var root=world.Group("Tvarůžkové turbo",new Vector3(snacks[i],Surface(snacks[i])+1.7f,0));
                var disc=world.Box("Tvarůžek",Vector3.zero,new Vector3(.65f,.14f,.65f),world.gold,root,PrimitiveType.Cylinder);disc.localRotation=Quaternion.Euler(70,0,0);
                world.Box("Cheese hole",new Vector3(.12f,.12f,-.19f),Vector3.one*.13f,world.rust,root,PrimitiveType.Sphere);
                world.Box("Cheese hole",new Vector3(-.13f,-.1f,-.18f),Vector3.one*.09f,world.rust,root,PrimitiveType.Sphere);
                cheeses[i]=root;cheesePositions[i]=root.position;
            }
            float[] bounce=world.Level.Springs;
            for(int i=0;i<bounce.Length;i++)
            {
                var root=world.Group("Pružící kufr",new Vector3(bounce[i],Surface(bounce[i]),0));
                world.Box("Suitcase",new Vector3(0,.18f,0),new Vector3(1,.3f,.65f),world.Mat("Suitcase pink","#D9587B"),root);
                world.Box("Lid",new Vector3(0,.38f,0),new Vector3(1.1f,.1f,.7f),world.gold,root);
                world.Sign("HOP!",new Vector3(0,.2f,-.35f),.038f,root);
                springs[i]=root;
            }
            for(int i=0;i<aura.Length;i++)
            {aura[i]=world.Box("Cartoon infection bubble",Vector3.zero,Vector3.one*.12f,world.Mat("Lime serum","#B4EB43"),null,PrimitiveType.Sphere);aura[i].gameObject.SetActive(false);}
        }
        float Surface(float x)=>world.Surface(x);
        Transform Syringe(string name,Transform parent=null)
        {
            var root=world.Group(name,Vector3.zero,parent);
            var barrel=world.Box("Barrel",Vector3.zero,new Vector3(.18f,.3f,.18f),world.cream,root,PrimitiveType.Cylinder);barrel.localRotation=Quaternion.Euler(0,0,90);
            world.Box("Neon serum",new Vector3(0,0,-.1f),new Vector3(.38f,.1f,.04f),world.Mat("Lime serum","#B4EB43"),root);
            world.Box("Plunger",new Vector3(.4f,0,0),new Vector3(.24f,.06f,.07f),world.steel,root);
            world.Box("Thumb pad",new Vector3(.54f,0,0),new Vector3(.06f,.3f,.2f),world.green,root);
            world.Box("Needle",new Vector3(-.48f,0,0),new Vector3(.36f,.025f,.025f),world.steel,root);
            return root;
        }
        public void Reset()
        {
            infection=turbo=dash=dashCooldown=comboTime=infectionProtection=0;combo=bestCombo=1;ticketsCollected=stomps=0;airJumpReady=true;announcementTimer=12;announcementIndex=0;
            foreach(var p in pops)p.life=0;
            foreach(var t in medkits)t.gameObject.SetActive(true);foreach(var t in cheeses)t.gameObject.SetActive(true);
            for(int i=0;i<world.birds.Count;i++){birdStun[i]=0;world.birds[i].gameObject.SetActive(true);}
            for(int i=0;i<world.inspectors.Count;i++){inspectorStun[i]=0;world.inspectors[i].rotation=Quaternion.identity;}
            for(int i=0;i<streetEnemies.Length;i++){streetTime[i]=i*.8f;streetStun[i]=0;streetThrown[i]=streetCalled[i]=false;streetEnemies[i].rotation=Quaternion.identity;warning[i].gameObject.SetActive(false);}
            for(int i=0;i<springs.Length;i++)springCooldown[i]=0;
            ClearTransient();
        }
        public void ClearTransient()
        {
            infection=turbo=dash=infectionProtection=0;airJumpReady=true;
            for(int i=0;i<needles.Length;i++){needleLife[i]=0;needles[i].gameObject.SetActive(false);}
            foreach(var t in aura)t.gameObject.SetActive(false);
        }
        public void PopText(string text,Vector2 position,Color? color=null,float duration=1.15f)
        {
            var p=pops[popIndex++%pops.Length];p.text=text;p.at=position;p.life=duration;p.color=color??new Color(1,.78f,.28f);
        }
        public void AddScore(int value,string label)
        {
            combo=comboTime>0?Mathf.Min(5,combo+1):1;comboTime=3;bestCombo=Mathf.Max(bestCombo,combo);
            game.score+=value*combo;PopText(label+" +"+value*combo,game.position+Vector2.up*2.2f);
        }
        public void Ticket(){ticketsCollected++;AddScore(100,combo>=2?"KOMBO!":"CVAK!");}
        public void BreakCombo(){combo=1;comboTime=0;}
        public bool AirJump()
        {
            if(!airJumpReady)return false;airJumpReady=false;PopText("JEŠTĚ JEDEN HOP!",game.position+Vector2.up*2);return true;
        }
        public void Dash()
        {
            if(dashCooldown>0)return;dash=.19f;dashCooldown=1.25f;game.audioMix.Dash();PopText("DOVOLENÍ!",game.position+Vector2.up*2.3f);
        }
        public void Infect()
        {
            if(infectionProtection>0||game.invulnerability>0||dash>0||game.mode!=RailGame.Mode.Playing)return;
            infectionProtection=1.3f;infection=Mathf.Min(100,infection+52);BreakCombo();
            if(infection<100)game.audioMix.Hit();
            PopText("FUJTAJBL!",game.position+Vector2.up*2,new Color(.7f,1,.25f));
            game.Notice("NÁKAZA! Lékárnička tě vyčistí. Shift uhne injekci.");
            if(infection>=100){game.Damage(game.position.x+1);infection=45;}
        }
        public void Cure()
        {
            infection=0;infectionProtection=1;game.lives=Mathf.Min(3,game.lives+1);AddScore(150,"ZASE VE FORMĚ!");game.Celebrate();
        }
        public void BeforeStep(float dt)
        {
            dash=Mathf.Max(0,dash-dt);dashCooldown=Mathf.Max(0,dashCooldown-dt);turbo=Mathf.Max(0,turbo-dt);
            infection=Mathf.Max(0,infection-5*dt);infectionProtection-=dt;comboTime-=dt;
            if(comboTime<=0)combo=1;
            if(game.grounded)airJumpReady=true;
            foreach(var p in pops)if(p.life>0){p.life-=dt;p.at.y+=dt*.9f;}
            for(int i=0;i<birdStun.Length;i++)if(birdStun[i]>0){birdStun[i]-=dt;if(birdStun[i]<=0 && i<world.birds.Count)world.birds[i].gameObject.SetActive(true);}
            for(int i=0;i<inspectorStun.Length;i++)if(inspectorStun[i]>0){inspectorStun[i]-=dt;if(inspectorStun[i]<=0&&i<world.inspectors.Count)world.inspectors[i].rotation=Quaternion.identity;}
            announcementTimer-=dt;
            if(announcementTimer<=0)
            {
                if(game.noticeTime<=0)
                {
                    int line=announcementIndex++%RailAudio.Announcements.Length;
                    game.Notice("HLÁŠENÍ: "+RailAudio.Announcements[line]);
                    game.audioMix.Announce(line);
                }
                announcementTimer=14;
            }
        }
        bool Stomp(Vector2 old,float x,float head)
        {
            if(game.velocity.y>0||Mathf.Abs(game.position.x-x)>.75f||old.y<head-.38f||game.position.y>head+.12f)return false;
            game.Launch(11.5f);game.invulnerability=Mathf.Max(game.invulnerability,.3f);stomps++;AddScore(250,"BOING!");game.Boing();return true;
        }
        public bool Bird(int i,Vector2 old)
        {
            if(birdStun[i]>0)return true;var p=world.birds[i].position;
            if(!Stomp(old,p.x,p.y+.18f))return false;
            birdStun[i]=4;world.birds[i].gameObject.SetActive(false);return true;
        }
        public bool Inspector(int i,Vector2 old)
        {
            if(inspectorStun[i]>0)return true;var p=world.inspectors[i].position;
            if(!Stomp(old,p.x,p.y+1.9f))return false;
            inspectorStun[i]=6;world.inspectors[i].rotation=Quaternion.Euler(0,0,75);return true;
        }
        public void AfterMove(float dt,Vector2 old)
        {
            for(int i=0;i<streetEnemies.Length;i++)
            {
                var man=streetEnemies[i];
                if(streetStun[i]>0){streetStun[i]-=dt;warning[i].gameObject.SetActive(false);if(streetStun[i]<=0)man.rotation=Quaternion.identity;continue;}
                if(Stomp(old,man.position.x,man.position.y+2)){streetStun[i]=6;man.rotation=Quaternion.Euler(0,0,-75);continue;}
                float distance=Mathf.Abs(game.position.x-man.position.x);
                if(distance>17){warning[i].gameObject.SetActive(false);continue;}
                streetTime[i]+=dt;float phase=streetTime[i]%4.5f;bool aiming=phase>2.6f&&phase<3.5f;
                warning[i].gameObject.SetActive(aiming);if(aiming)warning[i].localScale=Vector3.one*(.23f+Mathf.PingPong(game.simTime,.2f));
                if(aiming && !streetCalled[i] && distance<12)
                {
                    string line=game.audioMix.Bark(man.position.x);
                    if(line!=null)
                    {
                        streetCalled[i]=true;
                        PopText(line,man.position+Vector3.up*2.7f,new Color(.86f,1,.61f),2.7f);
                    }
                }
                if(phase>=3.5f&&!streetThrown[i]){Shoot(man.position+Vector3.up*1.25f);streetThrown[i]=true;}
                if(phase<1)streetThrown[i]=streetCalled[i]=false;
                if(distance<.7f&&Mathf.Abs(game.position.y-man.position.y)<1.7f)Infect();
            }
            for(int i=0;i<needles.Length;i++)if(needleLife[i]>0)
            {
                needleLife[i]-=dt;needles[i].position+=(Vector3)(needleVelocity[i]*dt);
                if(Vector2.Distance(game.position+Vector2.up*.9f,needles[i].position)<.65f){Infect();needleLife[i]=0;}
                if(needleLife[i]<=0)needles[i].gameObject.SetActive(false);
            }
            for(int i=0;i<medkits.Length;i++)if(medkits[i].gameObject.activeSelf)
            {
                medkits[i].position=medkitPositions[i]+Vector3.up*Mathf.Sin(game.simTime*3+i)*.12f;
                if(Vector2.Distance(game.position+Vector2.up*.9f,medkits[i].position)<.85f){medkits[i].gameObject.SetActive(false);Cure();}
            }
            for(int i=0;i<cheeses.Length;i++)if(cheeses[i].gameObject.activeSelf)
            {
                cheeses[i].position=cheesePositions[i]+Vector3.up*Mathf.Sin(game.simTime*3+i)*.17f;cheeses[i].Rotate(0,dt*100,0);
                if(Vector2.Distance(game.position+Vector2.up,cheeses[i].position)<.8f){cheeses[i].gameObject.SetActive(false);turbo=7;AddScore(200,"TVARŮŽKOVÉ TURBO!");game.Celebrate();}
            }
            for(int i=0;i<springs.Length;i++)
            {
                springCooldown[i]-=dt;springs[i].localScale=new Vector3(1,springCooldown[i]>.55f?.5f:1,1);
                if(game.grounded&&springCooldown[i]<=0&&Mathf.Abs(game.position.x-springs[i].position.x)<.58f&&Mathf.Abs(game.position.y-springs[i].position.y)<.1f)
                {springCooldown[i]=.85f;game.Launch(18);game.Boing();PopText("KUFROVÝ KATAPULT!",game.position+Vector2.up*2);}
            }
            for(int i=0;i<aura.Length;i++)
            {
                aura[i].gameObject.SetActive(infection>0);
                if(infection>0)aura[i].position=new Vector3(game.position.x+Mathf.Sin(game.simTime*3+i*1.3f)*.6f,game.position.y+.6f+i*.27f,-.4f);
            }
        }
        void Shoot(Vector3 origin)
        {
            for(int i=0;i<needles.Length;i++)if(needleLife[i]<=0)
            {
                float direction=game.position.x<origin.x?-1:1;needles[i].position=origin;needles[i].rotation=Quaternion.Euler(0,direction>0?180:0,0);
                needleVelocity[i]=new Vector2(direction*6.3f,Mathf.Clamp((game.position.y+.9f-origin.y)*.5f,-1.1f,1.1f));needleLife[i]=2.7f;needles[i].gameObject.SetActive(true);break;
            }
        }
    }
}
