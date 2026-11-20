using System;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Goofy arcade rules, reversible enemy stuns and pooled visual feedback.</summary>
    public sealed class RailChaos
    {
        public sealed class Pop { public Vector2 at;public string text;public float life;public Color color; }
        public readonly Pop[] pops=new Pop[18];
        public readonly Transform[] streetEnemies,medkits,cheeses,springs;
        readonly Transform[] needles=new Transform[12],warning,aura=new Transform[5],syringes;
        readonly Vector2[] needleVelocity=new Vector2[12];
        readonly float[] needleLife=new float[12],streetTime,streetStun,inspectorStun,birdStun,springCooldown;
        readonly bool[] streetThrown,streetCalled;
        readonly Vector3[] medkitPositions,cheesePositions;
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
            var level=world.Level;
            streetEnemies=new Transform[level.Street.Length];warning=new Transform[level.Street.Length];syringes=new Transform[level.Street.Length];
            streetTime=new float[level.Street.Length];streetStun=new float[level.Street.Length];
            streetThrown=new bool[level.Street.Length];streetCalled=new bool[level.Street.Length];
            medkits=new Transform[level.Medkits.Length];medkitPositions=new Vector3[level.Medkits.Length];
            cheeses=new Transform[level.Snacks.Length];cheesePositions=new Vector3[level.Snacks.Length];
            springs=new Transform[level.Springs.Length];springCooldown=new float[level.Springs.Length];
            inspectorStun=new float[world.inspectors.Count];birdStun=new float[world.birds.Count];
            for(int i=0;i<pops.Length;i++)pops[i]=new Pop();
            float[] xs=level.Street;
            for(int i=0;i<xs.Length;i++)
            {
                float y=Surface(xs[i]);
                var man=world.Person("Bezdomovec / fiktivní podchodový alchymista "+i,new Vector3(xs[i],y,0),false,true);
                var roof=world.PlatformAt(xs[i]);if(roof!=null)man.SetParent(roof.root,true);
                streetEnemies[i]=man;
                var syringe=Syringe("Obří injekce",man);syringe.localPosition=new Vector3(-.6f,1.1f,-.12f);syringe.localScale=Vector3.one*1.4f;
                syringes[i]=syringe;
                warning[i]=world.Box("Pozor na injekci",new Vector3(0,2.4f,0),Vector3.one*.3f,world.Mat("Lime serum","#B4EB43"),man,PrimitiveType.Sphere);
                warning[i].gameObject.SetActive(false);
                if(i==0||i==2)
                {
                    // The cart stands on the same roof as its owner and rides along with a moving wagon.
                    var cart=world.Group("Vozík s krabicemi",new Vector3(xs[i]+1.2f,y+.25f,.65f));
                    if(roof!=null)cart.SetParent(roof.root,true);
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
                var root=OnRoof("Lékárnička",heals[i],1.25f);
                world.Box("Case",Vector3.zero,new Vector3(.6f,.5f,.24f),world.Printed("Medkit","Medkit",.2f,.5f),root);
                world.Box("Handle",new Vector3(0,.29f,0),new Vector3(.22f,.06f,.06f),world.dark,root);
                medkits[i]=root;medkitPositions[i]=root.localPosition;
            }
            float[] snacks=world.Level.Snacks;
            for(int i=0;i<snacks.Length;i++)
            {
                var root=OnRoof("Tvarůžkové turbo",snacks[i],1.7f);
                world.Box("Tvarůžky",Vector3.zero,new Vector3(.66f,.44f,.22f),world.Printed("Tvaruzky","Tvaruzky",.25f,.4f),root);
                cheeses[i]=root;cheesePositions[i]=root.localPosition;
            }
            float[] bounce=world.Level.Springs;
            for(int i=0;i<bounce.Length;i++)
            {
                var root=world.Group("Pružící kufr",new Vector3(bounce[i],Surface(bounce[i]),0));
                world.Box("Suitcase",new Vector3(0,.2f,0),new Vector3(1,.34f,.65f),world.Printed("Suitcase","Suitcase",.1f,.55f),root);
                world.Box("Handle",new Vector3(0,.41f,0),new Vector3(.3f,.07f,.08f),world.dark,root);
                world.Sign("HOP!",new Vector3(0,.2f,-.35f),.038f,root);
                springs[i]=root;
            }
            for(int i=0;i<aura.Length;i++)
            {aura[i]=world.Box("Cartoon infection bubble",Vector3.zero,Vector3.one*.12f,world.Mat("Lime serum","#B4EB43"),null,PrimitiveType.Sphere);aura[i].gameObject.SetActive(false);}
        }
        float Surface(float x)=>world.Surface(x);
        /// <summary>Stunned: the animated character falls over, the primitive figure tips over.</summary>
        void KnockDown(Transform person,float tilt){var actor=world.ActorOf(person);if(actor!=null)actor.Set(RailActor.Mode.Knockdown);else person.rotation=Quaternion.Euler(0,0,tilt);}
        void Stand(Transform person){var actor=world.ActorOf(person);if(actor!=null)actor.Set(RailActor.Mode.Idle);else person.rotation=Quaternion.identity;}
        /// <summary>A pickup floating above the roof at x, parented to that roof so it rides a moving wagon.</summary>
        Transform OnRoof(string name,float x,float height)
        {
            var roof=world.PlatformAt(x);
            if(roof==null)return world.Group(name,new Vector3(x,Surface(x)+height,0));
            return world.Group(name,new Vector3(x-roof.x,roof.top+height,0),roof.root);
        }
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
            for(int i=0;i<world.inspectors.Count;i++){inspectorStun[i]=0;Stand(world.inspectors[i]);}
            for(int i=0;i<streetEnemies.Length;i++){streetTime[i]=i*.8f;streetStun[i]=0;streetThrown[i]=streetCalled[i]=false;Stand(streetEnemies[i]);warning[i].gameObject.SetActive(false);}
            for(int i=0;i<springs.Length;i++)springCooldown[i]=0;
            ClearTransient();
        }
        public void ClearTransient(bool keepInfection=false)
        {
            if(!keepInfection)infection=0;
            turbo=dash=infectionProtection=0;airJumpReady=true;
            for(int i=0;i<needles.Length;i++){needleLife[i]=0;needles[i].gameObject.SetActive(false);}
            foreach(var t in aura)t.gameObject.SetActive(false);
            if(!keepInfection)foreach(var p in pops)p.life=0;
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
            if(dashCooldown>0)return;dash=RailGame.DashTime;dashCooldown=RailGame.DashCooldown;game.audioMix.Dash();PopText("DOVOLENÍ!",game.position+Vector2.up*2.3f);
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
            infection=0;infectionProtection=1;game.lives=Mathf.Min(game.Level.Lives,game.lives+1);AddScore(150,"ZASE VE FORMĚ!");game.Celebrate();
        }
        public void BeforeStep(float dt)
        {
            dash=Mathf.Max(0,dash-dt);dashCooldown=Mathf.Max(0,dashCooldown-dt);turbo=Mathf.Max(0,turbo-dt);
            infection=Mathf.Max(0,infection-5*dt);infectionProtection-=dt;comboTime-=dt;
            if(comboTime<=0)combo=1;
            if(game.grounded)airJumpReady=true;
            foreach(var p in pops)if(p.life>0){p.life-=dt;p.at.y+=dt*.9f;}
            for(int i=0;i<birdStun.Length;i++)if(birdStun[i]>0){birdStun[i]-=dt;if(birdStun[i]<=0)world.birds[i].gameObject.SetActive(true);}
            for(int i=0;i<inspectorStun.Length;i++)if(inspectorStun[i]>0){inspectorStun[i]-=dt;if(inspectorStun[i]<=0)Stand(world.inspectors[i]);}
            announcementTimer-=dt;
            if(announcementTimer<=0)
            {
                int line=announcementIndex%RailAudio.Announcements.Length;
                if(game.noticeTime<=0 && game.audioMix.Announce(line))
                {
                    announcementIndex++;
                    game.Notice("HLÁŠENÍ: "+RailAudio.Announcements[line]);
                    announcementTimer=14;
                }
                else announcementTimer=2;
            }
        }
        bool Stomp(Vector2 old,float x,float head)
        {
            if(game.velocity.y>0||Mathf.Abs(game.position.x-x)>.75f||old.y<head-.38f||game.position.y>head+.12f)return false;
            game.StompLaunch();stomps++;AddScore(250,"BOING!");game.Boing();return true;
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
            inspectorStun[i]=6;KnockDown(world.inspectors[i],75);return true;
        }
        public void AfterMove(float dt,Vector2 old)
        {
            for(int i=0;i<streetEnemies.Length;i++)
            {
                var man=streetEnemies[i];
                var actor=world.ActorOf(man);
                if(actor!=null)
                {
                    actor.Face(game.position.x,dt);
                    // The syringe sits in the right hand of the animated alchemist.
                    if(actor.Hand)syringes[i].position=actor.Hand.position+Vector3.up*.05f;
                }
                if(streetStun[i]>0){streetStun[i]-=dt;warning[i].gameObject.SetActive(false);if(streetStun[i]<=0)Stand(man);continue;}
                if(Stomp(old,man.position.x,man.position.y+2)){streetStun[i]=6;KnockDown(man,-75);continue;}
                float distance=Mathf.Abs(game.position.x-man.position.x);
                if(distance>17){warning[i].gameObject.SetActive(false);continue;}
                streetTime[i]+=dt;float phase=streetTime[i]%4.5f;bool aiming=phase>2.6f&&phase<3.5f;
                actor?.Set(phase>3.2f&&phase<3.75f?RailActor.Mode.Throw:aiming?RailActor.Mode.Wait:RailActor.Mode.Idle);
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
                if(game.mode!=RailGame.Mode.Playing)return;
            }
            for(int i=0;i<needles.Length;i++)if(needleLife[i]>0)
            {
                needleLife[i]-=dt;needles[i].position+=(Vector3)(needleVelocity[i]*dt);
                if(Vector2.Distance(game.position+Vector2.up*.9f,needles[i].position)<.65f){Infect();needleLife[i]=0;}
                if(needleLife[i]<=0)needles[i].gameObject.SetActive(false);
            }
            // A lethal infection ends the run; nothing may be collected afterwards (no revival by a medkit).
            if(game.mode!=RailGame.Mode.Playing)return;
            for(int i=0;i<medkits.Length;i++)if(medkits[i].gameObject.activeSelf)
            {
                medkits[i].localPosition=medkitPositions[i]+Vector3.up*Mathf.Sin(game.simTime*3+i)*.12f;
                if(Vector2.Distance(game.position+Vector2.up*.9f,medkits[i].position)<.85f){medkits[i].gameObject.SetActive(false);Cure();}
            }
            for(int i=0;i<cheeses.Length;i++)if(cheeses[i].gameObject.activeSelf)
            {
                cheeses[i].localPosition=cheesePositions[i]+Vector3.up*Mathf.Sin(game.simTime*3+i)*.17f;cheeses[i].localRotation=Quaternion.Euler(0,Mathf.Sin(game.simTime*2.2f+i)*32,0);
                if(Vector2.Distance(game.position+Vector2.up,cheeses[i].position)<.8f){cheeses[i].gameObject.SetActive(false);turbo=7;AddScore(200,"TVARŮŽKOVÉ TURBO!");game.Celebrate();}
            }
            for(int i=0;i<springs.Length;i++)
            {
                springCooldown[i]-=dt;springs[i].localScale=new Vector3(1,springCooldown[i]>.55f?.5f:1,1);
                if(game.grounded&&springCooldown[i]<=0&&Mathf.Abs(game.position.x-springs[i].position.x)<.58f&&Mathf.Abs(game.position.y-springs[i].position.y)<.1f)
                {springCooldown[i]=.85f;game.Launch(RailGame.SpringSpeed);game.Boing();PopText("KUFROVÝ KATAPULT!",game.position+Vector2.up*2);}
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
