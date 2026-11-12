using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CeskeDrahy;

/// <summary>Deterministic integration checks run against the real motor in Play Mode.</summary>
public static class RailSmokeTests
{
    static FieldInfo Field(string name)=>typeof(RailGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);
    static void Check(bool ok,string message){if(!ok)throw new Exception("RAIL TEST FAILED: "+message);}
    static void Tick(RailGame g,int count){for(int i=0;i<count;i++)g.Step(1f/120);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<RailGame>();
        Check(g!=null,"Run tests in Play Mode");
        int savedBest=PlayerPrefs.GetInt("best",0);bool oldMuted=g.muted;var passed=new List<string>();
        g.enabled=false;g.muted=true;
        try
        {
            g.StartRun();Field("axis").SetValue(g,0f);Tick(g,4);
            Check(g.grounded && Mathf.Abs(g.position.y-.12f)<.001f,"Initial platform landing");passed.Add("initial landing");
            g.Damage(3);int health=g.lives;g.Damage(3);
            Check(health==2 && g.lives==health,"Invulnerability must prevent repeated damage");passed.Add("damage + invulnerability");
            g.invulnerability=0;g.Damage(3);g.invulnerability=0;g.Damage(3);
            Check(g.mode==RailGame.Mode.Lost && g.lives==0,"Zero health must lose");passed.Add("game over");
            g.StartRun();g.checkpoint=1;g.position=new Vector2(65,-5);Tick(g,1);
            Check(g.lives==2 && Mathf.Abs(g.position.x-54)<.01f,"Fall must cost one life and restore checkpoint");passed.Add("fall + checkpoint respawn");
            g.StartRun();Tick(g,4);Field("jumpBuffer").SetValue(g,.14f);
            float fullPeak=0;
            for(int i=0;i<120;i++){g.Step(1f/120);fullPeak=Mathf.Max(fullPeak,g.position.y);}
            g.StartRun();Tick(g,4);Field("jumpBuffer").SetValue(g,.14f);Tick(g,8);Field("jumpReleased").SetValue(g,true);Tick(g,1);Field("jumpReleased").SetValue(g,false);
            float shortPeak=0;for(int i=0;i<120;i++){g.Step(1f/120);shortPeak=Mathf.Max(shortPeak,g.position.y);}
            Check(fullPeak>3.1f && shortPeak<fullPeak-1,"Variable-height jump");passed.Add("variable jump height");
            g.StartRun();g.position=new Vector2(8.3f,.12f);g.grounded=false;Field("coyote").SetValue(g,.09f);Field("jumpBuffer").SetValue(g,.14f);Tick(g,1);
            Check(g.velocity.y>10,"Coyote jump outside platform");passed.Add("coyote time");
            g.StartRun();g.position=new Vector2(2,.14f);g.velocity=new Vector2(0,-3);Field("jumpBuffer").SetValue(g,.14f);Tick(g,3);
            Check(g.velocity.y>10,"A buffered jump must fire immediately after landing");passed.Add("jump buffer");
            g.StartRun();g.position=new Vector2(25,4.2f);Tick(g,1);
            Check(g.lives==2,"Pigeon overlap must damage the player");passed.Add("pigeon collision");
            g.StartRun();g.position=new Vector2(27,2.6f);Tick(g,470);
            Check(g.lives<3,"An inspector must throw a bag that can hit a stationary player");passed.Add("inspector projectile collision");
            g.StartRun();g.position=new Vector2(84,3.1f);Tick(g,2);float x=g.position.x;Tick(g,45);
            Check(g.grounded && Mathf.Abs(g.position.x-x)>.01f,"Moving train must carry the player");passed.Add("moving train carry");
            g.StartRun();Field("axis").SetValue(g,1f);var visited=new HashSet<int>();
            for(int i=0;i<120*60 && g.mode==RailGame.Mode.Playing;i++)
            {
                g.invulnerability=999; // This test isolates track geometry; damage was tested above.
                if(g.grounded)
                {
                    int p=g.world.platforms.FindIndex(v=>g.position.x>=v.Left-.25f&&g.position.x<=v.Right+.25f&&Mathf.Abs(g.position.y-v.top)<.1f);
                    if(p>=0){visited.Add(p);if(p<g.world.platforms.Count-1 && g.position.x>g.world.platforms[p].Right-1.65f)Field("jumpBuffer").SetValue(g,.14f);}
                }
                g.Step(1f/120);
            }
            Check(g.mode==RailGame.Mode.Won && visited.Count==13 && g.checkpoint==2,"Entire track must be reachable");
            Check(g.score>0,"Tickets must be collected");passed.Add("13 platforms + both checkpoints + finish + tickets");
            string result="PASS ("+passed.Count+"): "+string.Join("; ",passed);Debug.Log(result);return result;
        }
        finally
        {
            PlayerPrefs.SetInt("best",savedBest);PlayerPrefs.Save();g.muted=oldMuted;
            g.StartRun();g.Menu();g.enabled=true;g.hero.gameObject.SetActive(true);Field("axis").SetValue(g,0f);
        }
    }
}
