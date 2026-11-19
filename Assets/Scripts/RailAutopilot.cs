using System.Collections.Generic;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// Simple route-running bot used by the integration tests and the standalone tour. It runs right, jumps from
    /// near the roof edge, double-jumps at the apex when the next roof is still ahead, waits for a moving wagon that
    /// has drifted away and steers onto the landing roof.
    /// </summary>
    public sealed class RailAutopilot
    {
        public readonly HashSet<int> visited=new HashSet<int>();
        int last;
        bool waiting;
        public int Current => last;

        public void Reset(){visited.Clear();last=0;waiting=false;}

        /// <summary>Index of the roof the feet stand on, or -1.</summary>
        public static int RoofUnder(RailGame game)
        {
            var platforms=game.world.platforms;
            for(int i=0;i<platforms.Count;i++)
            {
                var v=platforms[i];
                if(game.position.x>=v.Left-.25f && game.position.x<=v.Right+.25f && Mathf.Abs(game.position.y-v.top)<.1f)return i;
            }
            return -1;
        }

        public void Drive(RailGame game)
        {
            var platforms=game.world.platforms;
            if(game.grounded){int p=RoofUnder(game);if(p>=0){last=p;visited.Add(p);}}
            float input=1;bool press=false;
            if(last<platforms.Count-1)
            {
                var start=platforms[last];
                var target=platforms[last+1];
                var a=game.Level.Platforms[last];var b=game.Level.Platforms[last+1];
                float nominal=(b.x-b.y*.5f)-(a.x+a.y*.5f);
                float gap=target.Left-start.Right;
                if(game.grounded)
                {
                    // A moving wagon that has drifted away is waited for at the edge.
                    waiting=gap>nominal+.3f && game.position.x>start.Right-2.2f;
                    if(waiting)input=game.position.x>start.Right-1.6f?-.4f:0;
                    else if(game.position.x>start.Right-1)press=true;
                }
                if(!game.grounded && game.chaos.airJumpReady && game.velocity.y<.7f &&
                    game.position.x<target.Left+1.3f && game.position.x>start.Right-1.35f)press=true;
                if(!game.grounded && game.position.x>target.x && game.position.y>target.top+.2f)input=0;
            }
            game.Feed(input,press,true,false);
        }
    }
}
