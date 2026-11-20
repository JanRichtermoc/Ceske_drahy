using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CeskeDrahy;

/// <summary>Deterministic integration checks run against the real motor in Play Mode.</summary>
public static class RailSmokeTests
{
    const string BackupFile = "Library/RailTestPrefsBackup.json";
    static FieldInfo Field(string name)=>typeof(RailGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);
    static void Check(bool ok,string message){if(!ok)throw new Exception("RAIL TEST FAILED: "+message);}
    static void Tick(RailGame g,int count){for(int i=0;i<count;i++)g.Step(1f/120);}
    /// <summary>Input for the following steps: move axis, jump pressed now, jump held, fast fall.</summary>
    static void Feed(RailGame g,float axis,bool pressed=false,bool held=false,bool fall=false)=>g.Feed(axis,pressed,held,fall);
    static float Peak(RailGame g,int steps){float peak=g.position.y;for(int i=0;i<steps;i++){g.Step(1f/120);peak=Mathf.Max(peak,g.position.y);}return peak;}

    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<RailGame>();
        Check(g!=null,"Run tests in Play Mode");
        RestoreCrashedBackup();
        var saved=SaveRecords();bool oldMuted=g.muted;var passed=new List<string>();
        g.enabled=false;g.muted=true;
        try
        {
            g.StartLevel(0);Feed(g,0);Tick(g,4);
            Check(g.grounded && Mathf.Abs(g.position.y-.12f)<.001f,"Initial platform landing");passed.Add("initial landing");
            g.Damage(3);int health=g.lives;g.Damage(3);
            Check(health==2 && g.lives==health,"Invulnerability must prevent repeated damage");passed.Add("damage + invulnerability");
            g.invulnerability=0;g.Damage(3);g.invulnerability=0;g.Damage(3);
            Check(g.mode==RailGame.Mode.Lost && g.lives==0,"Zero health must lose");
            Check(g.HeroVisible,"The hero must stay visible on the loss screen");
            g.position=new Vector2(52,.8f);g.velocity=Vector2.zero;Tick(g,2);
            Check(g.lives==0 && g.chaos.medkits[0].gameObject.activeSelf,"The simulation must not collect pickups after a loss");passed.Add("game over");
            g.StartRun();g.checkpoint=1;g.position=new Vector2(65,-5);Tick(g,1);
            Check(g.lives==2 && Mathf.Abs(g.position.x-54)<.01f,"Fall must cost one life and restore checkpoint");passed.Add("fall + checkpoint respawn");
            g.StartRun();g.position=new Vector2(65,-.9f);g.velocity=new Vector2(0,-6);Tick(g,1);
            Check(g.lives==2,"Falling below the kill plane must respawn before the player sinks into the ground");passed.Add("kill plane");

            // Jump shape: full held jump, early release, and a one-frame tap.
            g.StartRun();Tick(g,4);Feed(g,0,true,true);float fullPeak=Peak(g,120);
            g.StartRun();Tick(g,4);Feed(g,0,true,true);Tick(g,8);Feed(g,0);float shortPeak=Peak(g,120);
            Check(fullPeak>3.1f && shortPeak<fullPeak-1,"Variable-height jump ("+fullPeak.ToString("0.00")+" / "+shortPeak.ToString("0.00")+")");passed.Add("variable jump height");
            g.StartRun();Tick(g,4);float ground=g.position.y;Feed(g,0,true,false);float tapPeak=Peak(g,120)-ground;
            Check(tapPeak>.2f && tapPeak<.8f,"A tap gives a short hop ("+tapPeak.ToString("0.00")+" m)");passed.Add("tap hop");
            g.StartRun();g.position=new Vector2(2,.3f);g.velocity=new Vector2(0,-2);Feed(g,0,true,false);Tick(g,4);
            float bufferedTap=Peak(g,120)-.12f;
            Check(bufferedTap<.8f,"A buffered tap released before landing is still a short hop ("+bufferedTap.ToString("0.00")+" m)");passed.Add("buffered tap is short");

            g.StartRun();g.position=new Vector2(8.3f,.12f);g.grounded=false;Field("coyote").SetValue(g,.09f);Feed(g,0,true,true);Tick(g,1);
            Check(g.velocity.y>10,"Coyote jump outside platform");passed.Add("coyote time");
            g.StartRun();g.position=new Vector2(2,.14f);g.chaos.airJumpReady=false;g.velocity=new Vector2(0,-3);Feed(g,0,true,true);Tick(g,3);
            Check(g.velocity.y>10,"A buffered jump must fire immediately after landing");passed.Add("jump buffer");

            // Fall speed: normal cap and fast fall.
            g.StartRun();g.position=new Vector2(2,14);Feed(g,0);float slowest=0;for(int i=0;i<90 && !g.grounded;i++){g.Step(1f/120);slowest=Mathf.Min(slowest,g.velocity.y);}
            Check(slowest>=-RailGame.MaxFallSpeed-.01f && slowest<-RailGame.MaxFallSpeed+.5f,"Fall speed is capped ("+slowest.ToString("0.0")+")");
            g.StartRun();g.position=new Vector2(2,14);Feed(g,0,false,false,true);float fastest=0;for(int i=0;i<90 && !g.grounded;i++){g.Step(1f/120);fastest=Mathf.Min(fastest,g.velocity.y);}
            Check(fastest<-RailGame.MaxFallSpeed-3 && fastest>=-RailGame.FastFallSpeed-.01f,"Fast fall is faster but capped ("+fastest.ToString("0.0")+")");passed.Add("fall cap + fast fall");

            // Ledge assist: missing the roof edge by a few centimetres still lands.
            g.StartRun();var roof=g.world.platforms[1];
            g.position=new Vector2(roof.Left-.4f,roof.top-.2f);g.velocity=new Vector2(RailGame.RunSpeed,-1);g.grounded=false;Feed(g,1);Tick(g,6);
            Check(g.grounded && Mathf.Abs(g.position.y-roof.top)<.001f,"Ledge assist lands a near miss");passed.Add("ledge assist");
            g.StartRun();g.position=new Vector2(roof.Left-.4f,roof.top-.9f);g.velocity=new Vector2(RailGame.RunSpeed,-1);g.grounded=false;Feed(g,1);Tick(g,6);
            Check(!g.grounded && g.position.x<=roof.Left-.24f,"A clear miss hits the wall");passed.Add("solid wall below the edge");

            // Left end of the start platform is a wall (route 4 starts on a 14 m platform).
            g.StartLevel(3);var start=g.world.platforms[0];g.position=new Vector2(start.Left+1,start.top);Feed(g,-1);Tick(g,120);
            Check(g.grounded && g.lives==g.Level.Lives && g.position.x>=start.Left+.24f,"The player cannot walk off the start platform");passed.Add("left world edge");
            g.StartLevel(0);Feed(g,0);

            g.StartRun();g.position=new Vector2(25,4.2f);Tick(g,1);
            Check(g.lives==2,"Pigeon overlap must damage the player");passed.Add("pigeon collision");
            g.StartRun();StunBirds(g);g.position=new Vector2(27,2.6f);Tick(g,470);
            Check(g.lives<3,"An inspector must throw a bag that hits a standing player");passed.Add("aimed inspector bag");
            g.StartRun();g.position=new Vector2(84,3.1f);Tick(g,2);float x=g.position.x;Tick(g,45);
            Check(g.grounded && Mathf.Abs(g.position.x-x)>.01f,"Moving train must carry the player");passed.Add("moving train carry");
            Check(TicketsRideTheirRoof(g),"Tickets must ride a moving wagon");passed.Add("tickets ride wagons");

            g.StartRun();g.position=new Vector2(5.5f,.12f);Tick(g,2);
            Check(g.velocity.y>16,"Suitcase must launch the player");
            Feed(g,0);Tick(g,1);
            Check(g.velocity.y>15,"Releasing jump must not cut a suitcase launch");passed.Add("suitcase launch");
            Feed(g,0,true,true);Tick(g,1);
            Check(g.chaos.airJumpReady && g.velocity.y>RailGame.AirJumpSpeed,"An early double jump must not lower a stronger rise");passed.Add("double jump never lowers");
            for(int i=0;i<120 && g.velocity.y>=RailGame.AirJumpSpeed;i++)Tick(g,1);
            Feed(g,0,true,true);Tick(g,1);
            Check(!g.chaos.airJumpReady && g.velocity.y>12,"One extra jump is available in the air");passed.Add("double jump");

            g.StartRun();Feed(g,0);g.StompLaunch();float bounce=g.velocity.y;
            Check(Mathf.Approximately(bounce,RailGame.StompBounce) && (float)Field("hurtBlink").GetValue(g)<=0,"A stomp bounces without the hit blink");
            g.StartRun();Feed(g,0,false,true);g.StompLaunch();
            Check(g.velocity.y>bounce+2,"Holding jump on a stomp jumps higher");passed.Add("stomp bounce + stomp jump");

            g.StartRun();g.chaos.Dash();g.Damage(3);Tick(g,1);
            Check(g.lives==3 && Mathf.Abs(g.velocity.x)>14,"Dash must move and avoid damage");passed.Add("dash");
            g.StartRun();g.position=new Vector2(41.4f,2.3f);Tick(g,490);
            Check(g.chaos.infection>0 && g.chaos.SpeedMultiplier<1,"Syringe projectile must infect and slow the player");passed.Add("syringe infection");
            g.position=new Vector2(52,.8f);g.velocity=Vector2.zero;Tick(g,2);
            Check(g.chaos.infection==0 && !g.chaos.medkits[0].gameObject.activeSelf,"Collecting a medkit clears infection");passed.Add("medkit cure");

            // Route 1, fully: every roof, checkpoints, tickets and a stored best time.
            g.StartRun();var pilot=new RailAutopilot();
            for(int i=0;i<120*60 && g.mode==RailGame.Mode.Playing;i++)
            {
                g.invulnerability=999; // This test isolates track geometry; damage was tested above.
                pilot.Drive(g);g.Step(1f/120);
            }
            Check(g.mode==RailGame.Mode.Won && pilot.visited.Count==g.world.platforms.Count && g.checkpoint==g.Level.CheckpointCount,"Entire track must be reachable");
            Check(g.score>0,"Tickets must be collected");
            Check(g.newRecord && Mathf.Abs(PlayerPrefs.GetFloat(g.Level.TimeKey)-g.elapsed)<.001f,"Finishing must store the best time");
            passed.Add(pilot.visited.Count+" platforms + checkpoints + finish + tickets + best time");
            g.FinishAction();
            Check(g.LevelIndex==1 && g.mode==RailGame.Mode.Playing && g.lives==g.Level.Lives,"Finishing Olomouc advances to the next route");
            passed.Add("campaign advancement");

            Feed(g,0);
            g.StartLevel(Array.FindIndex(RailLevel.All,l=>l.Id=="olomouc-hodolany"));
            g.position=new Vector2(42,3.5f);
            g.simTime=3.9f;
            Tick(g,1);
            Check(g.lives==2,"An active steam vent must damage the player");
            passed.Add("steam hazard");

            g.StartLevel(Array.FindIndex(RailLevel.All,l=>l.Id=="olomouc-prednadrazi"));
            Check(g.lives==2,"The last route starts with two lives");
            g.chaos.Cure();
            Check(g.lives==g.Level.Lives,"A medkit never raises lives above the route's maximum");passed.Add("medkit respects the life limit");

            for(int level=1;level<RailLevel.All.Length;level++)
            {
                g.StartLevel(level);
                Check(g.lives==g.Level.Lives,"Advanced route starting health");
                Check(Traverse(g),"Every roof and checkpoint must be reachable on route "+level);
                Check(RollingStockFits(g,out string misfit),misfit);
                passed.Add("advanced route "+level);
            }
            g.StartLevel(0);Check(RollingStockFits(g,out string firstMisfit),firstMisfit);passed.Add("rolling stock fits its platforms");
            g.Damage(3);g.Menu();
            Check(g.mode==RailGame.Mode.Menu && (float)Field("shake").GetValue(g)==0 && g.chaos.infection==0,"Returning to the menu clears the run");
            Check(g.world.signals.TrueForAll(s=>!s.gameObject.activeSelf),"No inspector warning stays lit in the menu");passed.Add("menu cleanup");
            string result="PASS ("+passed.Count+"): "+string.Join("; ",passed);Debug.Log(result);return result;
        }
        finally
        {
            RestoreRecords(saved);g.muted=oldMuted;
            g.autopilot=null;g.StartLevel(0);g.Menu();g.enabled=true;Feed(g,0);
        }
    }
    static void StunBirds(RailGame g)
    {
        var stun=(float[])typeof(RailChaos).GetField("birdStun",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g.chaos);
        for(int i=0;i<stun.Length;i++)stun[i]=999;
    }
    /// <summary>Passenger stock is built to its platform: walkable roof at the collision top, body as long as the platform.</summary>
    static bool RollingStockFits(RailGame g,out string misfit)
    {
        misfit="";
        foreach(var p in g.world.platforms)
        {
            if(!p.root || !(p.root.name.StartsWith("Motorový vůz") || p.root.name.StartsWith("Vůz")))continue;
            var roof=p.root.Find("Arched roof").GetComponent<Renderer>().bounds;
            var body=p.root.Find("Lower body").GetComponent<Renderer>().bounds;
            float roofTop=roof.max.y-p.root.position.y,length=body.size.x;
            if(roofTop<p.top-.02f || roofTop>p.top+.08f){misfit=p.root.name+" roof at "+roofTop.ToString("0.000")+" for a collision top "+p.top;return false;}
            if(Mathf.Abs(length-p.width)>p.width*.01f){misfit=p.root.name+" is "+length.ToString("0.00")+" m long on a "+p.width+" m platform";return false;}
            // The running gear hangs under the body (a roof too low for the ground rails would push it inside).
            var frame=p.root.Find("Underframe").GetComponent<Renderer>().bounds;
            if(frame.max.y>body.min.y+.03f){misfit=p.root.name+" underframe reaches into the body (roof "+p.top+" m too low for the rails)";return false;}
        }
        return true;
    }
    static bool TicketsRideTheirRoof(RailGame g)
    {
        var moving=g.world.platforms.Find(p=>p.amplitude!=0);
        if(moving==null)return false;
        int checkedTickets=0;
        for(int i=0;i<g.world.tickets.Count;i++)
        {
            var t=g.world.tickets[i];
            if(t.parent!=moving.root)continue;
            checkedTickets++;
            if(Mathf.Abs(t.position.x-(moving.root.position.x+g.world.ticketOrigins[i].x))>.001f)return false;
        }
        return checkedTickets>0;
    }
    /// <summary>Tests finish routes for real; the player's own records are backed up, cleared and restored afterwards.</summary>
    static Dictionary<string,object> SaveRecords()
    {
        var saved=new Dictionary<string,object>();
        var lines=new List<string>();
        foreach(var level in RailLevel.All)
        {
            saved[level.ScoreKey]=PlayerPrefs.HasKey(level.ScoreKey)?(object)PlayerPrefs.GetInt(level.ScoreKey):null;
            saved[level.TimeKey]=PlayerPrefs.HasKey(level.TimeKey)?(object)PlayerPrefs.GetFloat(level.TimeKey):null;
            lines.Add(level.ScoreKey+"\ti\t"+(saved[level.ScoreKey]??""));
            lines.Add(level.TimeKey+"\tf\t"+(saved[level.TimeKey] is float f?f.ToString("R",System.Globalization.CultureInfo.InvariantCulture):""));
            // A faster stored record must not make "finishing stores the best time" fail.
            PlayerPrefs.DeleteKey(level.ScoreKey);PlayerPrefs.DeleteKey(level.TimeKey);
        }
        // Survives an Editor crash mid-run; restored by the next run.
        File.WriteAllLines(BackupFile,lines);
        return saved;
    }
    static void RestoreRecords(Dictionary<string,object> saved)
    {
        foreach(var entry in saved)
        {
            if(entry.Value==null)PlayerPrefs.DeleteKey(entry.Key);
            else if(entry.Value is int score)PlayerPrefs.SetInt(entry.Key,score);
            else PlayerPrefs.SetFloat(entry.Key,(float)entry.Value);
        }
        PlayerPrefs.Save();
        if(File.Exists(BackupFile))File.Delete(BackupFile);
    }
    static void RestoreCrashedBackup()
    {
        if(!File.Exists(BackupFile))return;
        foreach(var line in File.ReadAllLines(BackupFile))
        {
            var parts=line.Split('\t');if(parts.Length<3)continue;
            if(parts[2]=="")PlayerPrefs.DeleteKey(parts[0]);
            else if(parts[1]=="i")PlayerPrefs.SetInt(parts[0],int.Parse(parts[2]));
            else PlayerPrefs.SetFloat(parts[0],float.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture));
        }
        PlayerPrefs.Save();File.Delete(BackupFile);
    }
    static bool Traverse(RailGame game)
    {
        var pilot=new RailAutopilot();
        for(int frame=0;frame<120*75 && game.mode==RailGame.Mode.Playing;frame++)
        {
            game.invulnerability=999; // Check traversal independently of enemy attack timing.
            pilot.Drive(game);
            int lives=game.lives;
            Tick(game,1);
            if(game.lives<lives)return false;
        }
        return game.mode==RailGame.Mode.Won && game.checkpoint==game.Level.CheckpointCount && pilot.visited.Count==game.world.platforms.Count;
    }
}
