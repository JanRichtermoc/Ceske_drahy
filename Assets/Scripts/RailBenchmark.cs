using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Opt-in standalone validation: -rail-benchmark /absolute/output/folder.</summary>
    public sealed class RailBenchmark : MonoBehaviour
    {
        readonly List<float> timings=new List<float>();
        int frames;string output;RailGame game;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rail-benchmark");
            if(index<0 || index+1>=args.Length){Destroy(this);return;}
            output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
            game=GetComponent<RailGame>();
            int level=0;
            int levelArgument=Array.IndexOf(args,"-rail-level");
            if(levelArgument>=0 && levelArgument+1<args.Length && int.TryParse(args[levelArgument+1],out int requested))
                level=Mathf.Clamp(requested-1,0,RailLevel.All.Length-1);
            game.StartLevel(level);
            game.InputOverride=0;
            var platform=game.world.platforms[1];
            game.position=new Vector2(platform.x,platform.top);
            game.muted=false;
        }
        void Update()
        {
            frames++;
            if(frames>120)timings.Add(Time.unscaledDeltaTime*1000);
            if(frames==240)ScreenCapture.CaptureScreenshot(Path.Combine(output,"standalone.png"));
            if(frames<480)return;
            timings.Sort();float total=0;foreach(var time in timings)total+=time;
            var result=new Result{device=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,
                samples=timings.Count,meanMs=total/timings.Count,medianMs=timings[timings.Count/2],p95Ms=timings[(int)(timings.Count*.95f)],
                maximumMs=timings[timings.Count-1],state=game.Snapshot()};
            File.WriteAllText(Path.Combine(output,"benchmark.json"),JsonUtility.ToJson(result,true));
            Debug.Log("RAILWAY_STANDALONE_VALIDATED "+JsonUtility.ToJson(result));Application.Quit();enabled=false;
        }
        [Serializable] class Result {public string device,gpu,state;public int width,height,samples;public float meanMs,medianMs,p95Ms,maximumMs;}
    }
}
