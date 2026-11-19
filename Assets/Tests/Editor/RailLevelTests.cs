using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CeskeDrahy.Tests
{
    /// <summary>Static checks of the ten routes: data consistency, placement and jump reachability.</summary>
    public sealed class RailLevelTests
    {
        static IEnumerable<RailLevel> Levels => RailLevel.All;

        [Test]
        public void TenRoutesWithUniqueIdsAndKeys()
        {
            Assert.AreEqual(10, RailLevel.All.Length);
            Assert.AreEqual(10, RailLevel.All.Select(l => l.Id).Distinct().Count(), "Ids");
            Assert.AreEqual(10, RailLevel.All.Select(l => l.ScoreKey).Distinct().Count(), "Score keys");
            Assert.AreEqual(10, RailLevel.All.Select(l => l.TimeKey).Distinct().Count(), "Time keys");
        }

        [Test]
        public void OriginalRoutesKeepTheirSavedScoreKeys()
        {
            Assert.AreEqual("best", RailLevel.All.Single(l => l.Id == "olomouc-hl-n").ScoreKey);
            Assert.AreEqual("bestLevel1", RailLevel.All.Single(l => l.Id == "olomouc-hodolany").ScoreKey);
            Assert.AreEqual("bestLevel2", RailLevel.All.Single(l => l.Id == "olomouc-prednadrazi").ScoreKey);
        }

        [Test]
        public void DifficultyNeverDropsByMoreThanOneAndEndsAtFive()
        {
            for (int i = 1; i < RailLevel.All.Length; i++)
                Assert.GreaterOrEqual(RailLevel.All[i].Difficulty, RailLevel.All[i - 1].Difficulty - 1, RailLevel.All[i].Name);
            Assert.AreEqual(5, RailLevel.All[RailLevel.All.Length - 1].Difficulty);
        }

        [TestCaseSource(nameof(Levels))]
        public void KindsZonesAndStationsAgree(RailLevel level)
        {
            Assert.AreEqual(level.Platforms.Length, level.Kinds.Length, "One kind per platform");
            Assert.AreEqual('S', level.Kinds[0], "Starts on a station platform");
            Assert.AreEqual('S', level.Kinds[level.Kinds.Length - 1], "Finishes on a station platform");
            Assert.GreaterOrEqual(level.CheckpointCount, 1, "At least one checkpoint");
            Assert.AreEqual(level.Stations.Length, level.Zones.Length, "One zone name per station");
            Assert.IsTrue(level.Kinds.All(k => "STCH".IndexOf(k) >= 0), "Known platform kinds");
            Assert.IsTrue(level.Lives >= 2 && level.Lives <= 3);
        }

        [TestCaseSource(nameof(Levels))]
        public void PlatformsRunLeftToRightWithoutOverlap(RailLevel level)
        {
            for (int i = 1; i < level.Platforms.Length; i++)
            {
                Vector4 a = level.Platforms[i - 1], b = level.Platforms[i];
                float gap = (b.x - b.y * .5f) - (a.x + a.y * .5f) - a.w - b.w;
                Assert.Greater(gap, .5f, level.Name + " platforms " + (i - 1) + " and " + i + " can touch while moving");
            }
        }

        [TestCaseSource(nameof(Levels))]
        public void EveryGapIsReachableWithADoubleJump(RailLevel level)
        {
            for (int i = 1; i < level.Platforms.Length; i++)
            {
                Vector4 a = level.Platforms[i - 1], b = level.Platforms[i];
                float gap = (b.x - b.y * .5f) - (a.x + a.y * .5f);
                float rise = b.z - a.z;
                Assert.Less(rise, RailMotor.MaxHeight - .8f, level.Name + ": platform " + i + " is too high");
                // Nominal phase; headwind on windy routes takes a third of the run speed.
                float reach = RailMotor.Reach(rise) * (level.Wind > 0 ? .75f : 1);
                Assert.Less(gap, reach * .8f, level.Name + ": gap before platform " + i + " (" + gap.ToString("0.0") + " m) is too wide");
            }
        }

        [Test]
        public void RouteOneNeedsNoDoubleJump()
        {
            var level = RailLevel.All[0];
            for (int i = 1; i < level.Platforms.Length; i++)
            {
                Vector4 a = level.Platforms[i - 1], b = level.Platforms[i];
                float gap = (b.x - b.y * .5f) - (a.x + a.y * .5f);
                Assert.Less(gap, RailMotor.SingleReach(b.z - a.z) * .8f, "Tutorial gap before platform " + i);
            }
        }

        /// <summary>Steps the real motor: full-speed run, ground jump, double jump at the apex, until the feet fall back to rise.</summary>
        static float SimulatedReach(float rise, bool held)
        {
            const float dt = 1f / 120;
            float x = 0, y = 0, vy = RailGame.JumpSpeed;
            bool cut = false, airJumped = false;
            for (int i = 0; i < 2000; i++)
            {
                if (!airJumped && vy <= 0) { vy = RailGame.AirJumpSpeed; airJumped = true; }
                vy = RailMotor.Vertical(vy, ref cut, held, false, dt);
                x += RailGame.RunSpeed * dt; y += vy * dt;
                if (airJumped && vy < 0 && y <= rise) return x;
            }
            return x;
        }

        [TestCase(-2f)]
        [TestCase(0f)]
        [TestCase(2f)]
        [TestCase(3.3f)]
        public void MotorMatchesAnalyticReach(float rise)
        {
            float ratio = SimulatedReach(rise, false) / RailMotor.Reach(rise);
            Assert.That(ratio, Is.InRange(.96f, 1.02f), "Simulated / analytic reach at rise " + rise);
            Assert.Greater(SimulatedReach(rise, true), SimulatedReach(rise, false), "Holding jump hangs longer at the apex");
        }

        [Test]
        public void TapJumpIsAShortHop()
        {
            const float dt = 1f / 120;
            float y = 0, vy = RailGame.JumpSpeed, peak = 0;
            bool cut = true;
            for (int i = 0; i < 240 && (vy > 0 || y > 0); i++) { vy = RailMotor.Vertical(vy, ref cut, false, false, dt); y += vy * dt; peak = Mathf.Max(peak, y); }
            Assert.Less(peak, .8f, "A tap is a hop");
            Assert.Greater(peak, .25f, "A tap still leaves the ground");
        }

        [Test]
        public void MotorAcceleratesStopsAndTurnsQuickly()
        {
            const float dt = 1f / 120;
            float vx = 0; int steps = 0;
            while (vx < RailGame.RunSpeed && steps < 120) { vx = RailMotor.Horizontal(vx, 1, RailGame.RunSpeed, true, dt); steps++; }
            Assert.LessOrEqual(steps * dt, .11f, "Ground run-up");
            steps = 0;
            while (vx > 0 && steps < 120) { vx = RailMotor.Horizontal(vx, 0, RailGame.RunSpeed, true, dt); steps++; }
            Assert.LessOrEqual(steps * dt, .09f, "Ground stop");
            vx = RailGame.RunSpeed; steps = 0;
            while (vx > -RailGame.RunSpeed && steps < 240) { vx = RailMotor.Horizontal(vx, -1, RailGame.RunSpeed, false, dt); steps++; }
            Assert.LessOrEqual(steps * dt, .25f, "Air reversal");
        }

        [Test]
        public void FallTimeMatchesCappedFall()
        {
            Assert.AreEqual(0, RailMotor.FallTime(0), 1e-5f);
            float g = RailGame.Gravity * RailGame.FallGravityMultiplier;
            Assert.AreEqual(Mathf.Sqrt(2 * 1f / g), RailMotor.FallTime(1), 1e-4f);
            float capHeight = RailGame.MaxFallSpeed * RailGame.MaxFallSpeed / (2 * g);
            Assert.AreEqual(RailGame.MaxFallSpeed / g + 2 / RailGame.MaxFallSpeed, RailMotor.FallTime(capHeight + 2), 1e-4f);
        }

        [TestCaseSource(nameof(Levels))]
        public void EnemiesAndPickupsStandOnPlatforms(RailLevel level)
        {
            foreach (var x in level.Inspectors.Concat(level.Street).Concat(level.Medkits).Concat(level.Snacks).Concat(level.Springs))
                Assert.IsTrue(level.Platforms.Any(p => x >= p.x - p.y * .5f && x <= p.x + p.y * .5f), level.Name + ": nothing to stand on at x=" + x);
            Assert.IsFalse(level.Inspectors.Concat(level.Street).Any(x => x < 9), level.Name + ": enemy next to the spawn point");
            foreach (int vent in level.Vents)
                Assert.AreNotEqual('S', level.Kinds[vent], level.Name + ": steam vents belong on wagon roofs");
        }

        [TestCaseSource(nameof(Levels))]
        public void CheckpointsStandOnTheirStationPlatforms(RailLevel level)
        {
            for (int c = 0; c <= level.CheckpointCount; c++)
            {
                var spawn = level.Checkpoint(c);
                var platform = level.Platforms[level.Stations[c]];
                Assert.That(spawn.x, Is.InRange(platform.x - platform.y * .5f, platform.x + platform.y * .5f), level.Name + " checkpoint " + c);
                Assert.AreEqual(platform.z, spawn.y, 1e-4f);
            }
            Assert.Greater(level.Finish, level.Platforms[level.Platforms.Length - 1].x);
        }

        [Test]
        public void ZoneFollowsTheStations()
        {
            var level = RailLevel.All[0];
            Assert.AreEqual(level.Zones[0], level.ZoneAt(2));
            Assert.AreEqual(level.Zones[level.Zones.Length - 1], level.ZoneAt(level.Finish));
        }
    }
}
