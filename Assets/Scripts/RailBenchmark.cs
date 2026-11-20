using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// Opt-in standalone validation: -rail-benchmark /absolute/output/folder [-rail-level n] [-rail-uncapped] [-rail-tour].
    /// The tour visits every route (start, middle and finish), captures a screenshot of each spot and times each route.
    /// </summary>
    public sealed class RailBenchmark : MonoBehaviour
    {
        readonly List<float> timings=new List<float>();
        int frames;string output;RailGame game;bool tour,configured;
        // Render counters (available in release players): sampled with every timed frame, reported as medians.
        static readonly string[] counterNames={"SetPass Calls Count","Draw Calls Count","Triangles Count","Shadow Casters Count"};
        readonly ProfilerRecorder[] counters=new ProfilerRecorder[counterNames.Length];
        readonly List<long>[] counterSamples={new List<long>(),new List<long>(),new List<long>(),new List<long>()};
        void OnEnable(){for(int i=0;i<counters.Length;i++)counters[i]=ProfilerRecorder.StartNew(ProfilerCategory.Render,counterNames[i]);}
        void OnDisable(){foreach(var counter in counters)counter.Dispose();}
        void Sample(List<float> into)
        {
            into.Add(Time.unscaledDeltaTime*1000);
            for(int i=0;i<counters.Length;i++)if(counters[i].Valid)counterSamples[i].Add(counters[i].LastValue);
        }
        /// <summary>Starts a tour from code (Play Mode in the Editor), without command-line arguments.</summary>
        public static RailBenchmark StartTour(RailGame game,string folder)
        {
            var benchmark=game.gameObject.AddComponent<RailBenchmark>();
            benchmark.output=Path.GetFullPath(folder);benchmark.tour=true;benchmark.configured=true;
            return benchmark;
        }
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(!configured)
            {
                int index=Array.IndexOf(args,"-rail-benchmark");
                if(index<0 || index+1>=args.Length){Destroy(this);return;}
                output=Path.GetFullPath(args[index+1]);
                tour=Array.IndexOf(args,"-rail-tour")>=0;
            }
            Directory.CreateDirectory(output);
            game=GetComponent<RailGame>();
            // Uncapped timings measure the real frame cost; the game itself caps at the display refresh.
            if(Array.IndexOf(args,"-rail-uncapped")>=0){QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;}
            game.muted=true;game.reducedMotion=true;
            if(tour){StartCoroutine(Tour());return;}
            int level=0;
            int levelArgument=Array.IndexOf(args,"-rail-level");
            if(levelArgument>=0 && levelArgument+1<args.Length && int.TryParse(args[levelArgument+1],out int requested))
                level=Mathf.Clamp(requested-1,0,RailLevel.All.Length-1);
            game.StartLevel(level);
            game.InputOverride=0;
            var platform=game.world.platforms[1];
            game.position=new Vector2(platform.x,platform.top);
        }
        void Update()
        {
            if(tour)return;
            frames++;
            if(frames>120)Sample(timings);
            if(frames==240)ScreenCapture.CaptureScreenshot(Path.Combine(output,"standalone.png"));
            if(frames<480)return;
            var result=Measure(timings);result.state=game.Snapshot();
            File.WriteAllText(Path.Combine(output,"benchmark.json"),JsonUtility.ToJson(result,true));
            Debug.Log("RAILWAY_STANDALONE_VALIDATED "+JsonUtility.ToJson(result));Application.Quit();enabled=false;
        }
        IEnumerator Tour()
        {
            var report=new TourReport();
            for(int level=0;level<RailLevel.All.Length;level++)
            {
                game.StartLevel(level);
                yield return null;yield return null;
                var platforms=game.world.platforms;
                int[] spots={0,platforms.Count/2,platforms.Count-1};
                var routeTimings=new List<float>();
                foreach(var list in counterSamples)list.Clear();
                for(int s=0;s<spots.Length;s++)
                {
                    var platform=platforms[spots[s]];
                    for(int f=0;f<70;f++)
                    {
                        game.InputOverride=0;game.invulnerability=999;
                        game.position=new Vector2(platform.x+platform.offset,platform.top);game.velocity=Vector2.zero;
                        if(f>20)Sample(routeTimings);
                        yield return null;
                    }
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,$"L{level+1:00}_{(char)('a'+s)}.png"));
                    yield return null;yield return null;
                }
                var result=Measure(routeTimings);result.state=game.Snapshot();
                report.routes.Add(result);
            }
            File.WriteAllText(Path.Combine(output,"tour.json"),JsonUtility.ToJson(report,true));
            Debug.Log("RAILWAY_TOUR_DONE "+output);
            if(!configured)Application.Quit();
            Destroy(this);
        }
        Result Measure(List<float> samples)
        {
            samples.Sort();float total=0;foreach(var time in samples)total+=time;
            long Median(int counter){var list=counterSamples[counter];if(list.Count==0)return -1;list.Sort();return list[list.Count/2];}
            return new Result{device=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,
                vSync=QualitySettings.vSyncCount,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                samples=samples.Count,meanMs=total/Mathf.Max(1,samples.Count),medianMs=samples.Count>0?samples[samples.Count/2]:0,
                p95Ms=samples.Count>0?samples[(int)(samples.Count*.95f)]:0,maximumMs=samples.Count>0?samples[samples.Count-1]:0,
                setPassCalls=Median(0),drawCalls=Median(1),triangles=Median(2),shadowCasters=Median(3)};
        }
        [Serializable] class Result {public string device,gpu,state,quality;public int width,height,samples,vSync;public float meanMs,medianMs,p95Ms,maximumMs;public long setPassCalls,drawCalls,triangles,shadowCasters;}
        [Serializable] class TourReport {public List<Result> routes=new List<Result>();}
    }
}
