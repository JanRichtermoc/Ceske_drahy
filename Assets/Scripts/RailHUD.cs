using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// IMGUI interface styled after Czech railway signage: a departure board menu, an enamel station
    /// sign, a stopwatch, a route strip with checkpoints and a ticket-shaped results card.
    /// Layout is authored at 1600×900 and anchored to screen edges; text is rasterized at the real size.
    /// </summary>
    public sealed class RailHUD : MonoBehaviour
    {
        public RailGame game;
        enum X { Left, Center, Right }
        enum Y { Top, Middle, Bottom }

        static readonly Color Navy = Hex("#0D2236"), NavyDeep = Hex("#081624"), Blue = Hex("#1D5E9E"), Amber = Hex("#F5B335"),
            Enamel = Hex("#F3F1EA"), Paper = Hex("#F2EBD8"), Ink = Hex("#14273A"), Muted = Hex("#9FB2C2"), Red = Hex("#D9463B"), Green = Hex("#43C072"), Led = Hex("#FFB733");
        Font body, bodyBold, cond, condBold, condBlack;
        GUIStyle text;
        Texture2D dots, shadow;
        GUIStyle shadowStyle;
        float k, ox, oy, fps, appear;
        bool settings, volumeDirty, mouseMoved;
        Vector2 lastMouse = new Vector2(-1, -1);
        int seenMode = -1;
        readonly float[] bestTimes = new float[RailLevel.All.Length];
        string clock = "";
        float clockAge;

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Color A(Color c, float a) { c.a *= a; return c; }

        void Awake()
        {
            body = Resources.Load<Font>("Fonts/Barlow-Medium"); bodyBold = Resources.Load<Font>("Fonts/Barlow-SemiBold");
            cond = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold"); condBold = Resources.Load<Font>("Fonts/BarlowCondensed-Bold");
            condBlack = Resources.Load<Font>("Fonts/BarlowCondensed-ExtraBold");
            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body = body ? body : fallback; bodyBold = bodyBold ? bodyBold : fallback; cond = cond ? cond : fallback;
            condBold = condBold ? condBold : fallback; condBlack = condBlack ? condBlack : fallback;
            // Created here, not in a field initializer: GUIStyle needs the engine to be ready.
            text = new GUIStyle { wordWrap = false, richText = false };
            // Dot-matrix mask for the departure board.
            dots = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) dots.SetPixel(x, y, x == 3 || y == 3 ? new Color(0, 0, 0, .22f) : Color.clear);
            dots.Apply();
            // Drop shadow: a blurred rounded square, drawn 9-sliced so any panel size gets one smooth shadow.
            shadow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - 32) - 8, 0), dy = Mathf.Max(Mathf.Abs(y + .5f - 32) - 8, 0);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                shadow.SetPixel(x, y, new Color(0, 0, 0, Mathf.Exp(-d * d / 98f) * (1 - Mathf.SmoothStep(18, 24, d))));
            }
            shadow.Apply();
            shadowStyle = new GUIStyle { border = new RectOffset(24, 24, 24, 24) };
            shadowStyle.normal.background = shadow;
        }
        void OnDestroy() { if (dots) Destroy(dots); if (shadow) Destroy(shadow); }
        void Update()
        {
            fps = Mathf.Lerp(fps, 1 / Mathf.Max(Time.unscaledDeltaTime, .001f), .05f);
            appear = Mathf.MoveTowards(appear, 1, Time.unscaledDeltaTime * 4);
            clockAge -= Time.unscaledDeltaTime;
            if (clockAge <= 0) { clock = System.DateTime.Now.ToString("HH:mm"); clockAge = 1; }
        }

        // ---------- layout helpers ----------
        Rect R(float x, float y, float w, float h, X ax = X.Center, Y ay = Y.Middle)
        {
            float sx = ax == X.Left ? x * k : ax == X.Right ? Screen.width - (1600 - x) * k : ox + x * k;
            float sy = ay == Y.Top ? y * k : ay == Y.Bottom ? Screen.height - (900 - y) * k : oy + y * k;
            return new Rect(sx, sy, w * k, h * k);
        }
        static Color Gui(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        void Fill(Rect r, Color c, float radius = 0)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Gui(c), 0, radius * k);
        }
        void Outline(Rect r, Color c, float width, float radius = 0)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Gui(c), width * k, radius * k);
        }
        void Shadow(Rect r, float radius, float alpha = .35f)
        {
            if (Event.current.type != EventType.Repaint) return;
            // The texture's blur fades over its 24 px border, so the solid core lines up with the panel; nudged down a little.
            const float spread = 24;
            var area = new Rect(r.x - spread, r.y - spread + 4 * k, r.width + spread * 2, r.height + spread * 2);
            var previous = GUI.color;
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01(alpha * 1.2f));
            shadowStyle.Draw(area, false, false, false, false);
            GUI.color = previous;
        }
        void Panel(Rect r, Color c, float radius = 14) { Shadow(r, radius); Fill(r, c, radius); }
        void Label(Rect r, string value, Font font, float size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool shadow = false)
        {
            text.font = font; text.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * k)); text.alignment = anchor;
            if (shadow)
            {
                text.normal.textColor = Gui(new Color(0, 0, 0, .45f * color.a));
                GUI.Label(new Rect(r.x + 2 * k, r.y + 2 * k, r.width, r.height), value, text);
            }
            text.normal.textColor = Gui(color);
            GUI.Label(r, value, text);
        }
        float Width(string value, Font font, float size)
        {
            text.font = font; text.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * k));
            return text.CalcSize(new GUIContent(value)).x / k;
        }
        bool Button(Rect r, string value, bool primary, string hint = null)
        {
            bool hover = GUI.enabled && r.Contains(Event.current.mousePosition);
            Color fill = primary ? (hover ? Hex("#FFC85A") : Amber) : (hover ? Hex("#26405A") : Hex("#183048"));
            Shadow(r, 10, .25f);
            Fill(r, fill, 10);
            if (!primary) Outline(r, A(Enamel, hover ? .45f : .18f), 1.5f, 10);
            Color ink = primary ? Ink : Enamel;
            Label(new Rect(r.x + 22 * k, r.y, r.width - 44 * k, r.height), value, condBold, 26, ink);
            if (hint != null) Label(new Rect(r.x + 22 * k, r.y, r.width - 44 * k, r.height), hint, cond, 18, A(ink, .65f), TextAnchor.MiddleRight);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
        static string Format(float seconds)
        {
            if (seconds <= 0) return "--:--.--";
            int minutes = (int)(seconds / 60);
            return minutes.ToString("00") + ":" + (seconds - minutes * 60).ToString("00.00");
        }

        // ---------- frame ----------
        void OnGUI()
        {
            k = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            // Hover only changes the departure-board selection when the mouse actually moves, so arrow keys keep working.
            if (Event.current.type == EventType.Repaint)
            {
                mouseMoved = lastMouse.x >= 0 && (Event.current.mousePosition - lastMouse).sqrMagnitude > .25f;
                lastMouse = Event.current.mousePosition;
            }
            // A drag released outside the window never sends MouseUp to IMGUI.
            if (volumeDirty && !RailInput.MouseHeld && Event.current.type == EventType.Repaint) { volumeDirty = false; game.SaveSettings(); }
            ox = (Screen.width - 1600 * k) * .5f; oy = (Screen.height - 900 * k) * .5f;
            // Transient panels never survive a mode change (pause, finish, menu).
            if (seenMode != game.ModeVersion)
            {
                seenMode = game.ModeVersion; settings = false; appear = 0;
                if (game.mode == RailGame.Mode.Menu) for (int i = 0; i < bestTimes.Length; i++) bestTimes[i] = PlayerPrefs.GetFloat(RailLevel.All[i].TimeKey, 0);
            }
            if (settings && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { CloseSettings(); Event.current.Use(); }
            game.uiModal = settings;
            GUI.enabled = !settings;
            switch (game.mode)
            {
                case RailGame.Mode.Menu: Menu(); break;
                case RailGame.Mode.Loading: Loading(); break;
                default:
                    if (game.mode == RailGame.Mode.Playing) HitFlash();
                    Hud();
                    if (game.mode == RailGame.Mode.Playing) Pops();
                    if (game.mode == RailGame.Mode.Paused) Pause();
                    if (game.mode == RailGame.Mode.Won) Ticket();
                    if (game.mode == RailGame.Mode.Lost) Missed();
                    break;
            }
            GUI.enabled = true;
            if (settings) Settings();
        }

        // ---------- menu ----------
        void Menu()
        {
            if (!settings && Event.current.type == EventType.KeyDown)
            {
                int n = RailLevel.All.Length;
                if (Event.current.keyCode == KeyCode.DownArrow || Event.current.keyCode == KeyCode.S) { game.selectedLevel = (game.selectedLevel + 1) % n; Event.current.Use(); }
                if (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.W) { game.selectedLevel = (game.selectedLevel + n - 1) % n; Event.current.Use(); }
            }
            // Left scrim keeps the title readable over the live 3D scene.
            Fill(new Rect(0, 0, ox + 700 * k, Screen.height), A(NavyDeep, .86f));
            Fill(new Rect(ox + 700 * k, 0, 60 * k, Screen.height), A(NavyDeep, .35f));
            TopBar();
            var level = RailLevel.All[game.selectedLevel];
            Label(R(64, 118, 600, 30), "POSLEDNÍ SPOJ  ·  OLOMOUC", condBold, 22, Amber);
            Label(R(58, 146, 640, 128), "SUPERCIGOS", condBlack, 118, Enamel, TextAnchor.MiddleLeft, true);
            Fill(R(64, 282, 96, 7), Amber, 3);
            Label(R(64, 298, 600, 46), "PLOŠINOVKA PO STŘECHÁCH", condBold, 40, Enamel);
            text.wordWrap = true;
            Label(R(64, 350, 560, 70), "CJ má jízdenku. Nádraží má jiné plány. Proskákej deset olomouckých stanic po střechách vagonů a stihni poslední spoj.", body, 19, A(Enamel, .78f), TextAnchor.UpperLeft);
            text.wordWrap = false;
            // Selected route card.
            var card = R(64, 440, 560, 178);
            Panel(card, Hex("#132C44"), 16);
            Fill(new Rect(card.x, card.y, 8 * k, card.height), Amber, 4);
            Label(R(92, 456, 300, 26), "TRAŤ " + (game.selectedLevel + 1) + " / " + RailLevel.All.Length, condBold, 20, Amber);
            Pips(R(470, 462, 130, 16), level.Difficulty);
            Label(R(92, 482, 520, 50), level.Name, condBlack, 42, Enamel);
            text.wordWrap = true;
            Label(R(92, 532, 510, 52), level.Brief, body, 17, A(Enamel, .75f), TextAnchor.UpperLeft);
            text.wordWrap = false;
            float best = bestTimes[game.selectedLevel];
            Label(R(92, 584, 510, 26), best > 0 ? "OSOBNÍ REKORD  " + Format(best) : "ZATÍM NEDOHRÁNO", condBold, 20, best > 0 ? Green : Muted);
            if (Button(R(64, 640, 560, 64), "HRÁT", true, "ENTER")) game.StartLevel(game.selectedLevel, true);
            if (Button(R(64, 716, 272, 56), "NASTAVENÍ", false)) settings = true;
            if (Button(R(352, 716, 272, 56), "KONEC HRY", false)) game.Quit();
            Board();
            BottomHints("ŠIPKY  VÝBĚR TRATI", "ENTER  HRÁT", "A D  POHYB", "MEZERNÍK  SKOK A DVOJSKOK", "S  RYCHLÝ PÁD", "SHIFT  ÚHYB", "ESC  PAUZA");
        }
        void TopBar()
        {
            Fill(new Rect(0, 0, Screen.width, 72 * k), A(NavyDeep, .94f));
            Fill(new Rect(0, 72 * k, Screen.width, 3 * k), Amber);
            var badge = R(40, 16, 64, 40, X.Left, Y.Top);
            Fill(badge, Blue, 10);
            Label(badge, "SC", condBlack, 28, Enamel, TextAnchor.MiddleCenter);
            Label(R(120, 16, 700, 40, X.Left, Y.Top), "SUPERCIGOS  /  NEZÁVISLÁ PLOŠINOVKA", condBold, 22, Enamel);
            ClockIcon(R(1462, 22, 28, 28, X.Right, Y.Top));
            Label(R(1498, 16, 80, 40, X.Right, Y.Top), clock, condBold, 30, Enamel);
        }
        void ClockIcon(Rect r)
        {
            Fill(r, Enamel, r.width / k * .5f);
            Fill(new Rect(r.x + r.width * .14f, r.y + r.height * .14f, r.width * .72f, r.height * .72f), Navy, r.width / k * .36f);
            var now = System.DateTime.Now;
            Hand(r.center, r.width * .26f, now.Hour % 12 * 30 + now.Minute * .5f, 2.2f);
            Hand(r.center, r.width * .34f, now.Minute * 6, 1.6f);
        }
        void Hand(Vector2 pivot, float length, float angle, float width)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);
            Fill(new Rect(pivot.x - width * k * .5f, pivot.y - length, width * k, length), Amber);
            GUI.matrix = matrix;
        }
        void Pips(Rect r, int count)
        {
            float size = r.height, gap = size * .45f;
            for (int i = 0; i < 5; i++)
                Fill(new Rect(r.x + i * (size + gap), r.y, size, size), i < count ? Amber : A(Muted, .25f), 3);
        }
        void Board()
        {
            var board = R(770, 104, 790, 690);
            Panel(board, Hex("#0A1622"), 18);
            Outline(board, A(Hex("#2B4A66"), .9f), 2, 18);
            Label(R(806, 120, 400, 56), "ODJEZDY", condBlack, 50, Led);
            Label(R(1010, 132, 300, 40), "DEPARTURES", cond, 22, A(Led, .55f));
            Label(R(1300, 126, 230, 44), "NÁSTUPIŠTĚ OLOMOUC", cond, 17, A(Enamel, .5f), TextAnchor.MiddleRight);
            float[] col = { 806, 940, 1032, 1352, 1490 };
            string[] head = { "REKORD", "VLAK", "SMĚR", "OBTÍŽNOST", "TRAŤ" };
            for (int c = 0; c < head.Length; c++) Label(R(col[c], 182, 200, 24), head[c], condBold, 16, A(Enamel, .45f));
            Fill(R(796, 208, 738, 2), A(Hex("#2B4A66"), .9f));
            if (Event.current.type == EventType.Repaint)
            {
                // Whole-pixel cells (a multiple of the 4 px texture) so the LED raster never blurs or beats.
                var screen = R(796, 216, 738, 560);
                screen = new Rect(Mathf.Round(screen.x), Mathf.Round(screen.y), Mathf.Round(screen.width), Mathf.Round(screen.height));
                float cell = 4 * Mathf.Max(1, Mathf.Round(k));
                GUI.DrawTextureWithTexCoords(screen, dots, new Rect(0, 0, screen.width / cell, screen.height / cell));
            }
            for (int i = 0; i < RailLevel.All.Length; i++)
            {
                var level = RailLevel.All[i];
                var row = R(796, 216 + i * 56, 738, 52);
                if (GUI.enabled && row.Contains(Event.current.mousePosition) && (Event.current.type == EventType.MouseMove || (Event.current.type == EventType.Repaint && mouseMoved)))
                    game.selectedLevel = i;
                bool selected = i == game.selectedLevel;
                if (selected) Fill(row, Amber, 8);
                else if (i % 2 == 1) Fill(row, A(Color.white, .03f), 8);
                Color ink = selected ? Ink : Led;
                float y = 216 + i * 56;
                Label(R(col[0], y, 130, 52), bestTimes[i] > 0 ? Format(bestTimes[i]) : "--:--.--", condBold, 24, selected ? Ink : bestTimes[i] > 0 ? Green : A(Led, .5f));
                Label(R(col[1], y, 90, 52), "Os " + (4601 + i * 2), condBold, 22, A(ink, .8f));
                Label(R(col[2], y, 320, 52), level.Name, condBlack, 27, ink);
                var pips = R(col[3], y + 19, 110, 14);
                float size = pips.height, gap = size * .45f;
                for (int p = 0; p < 5; p++) Fill(new Rect(pips.x + p * (size + gap), pips.y, size, size), p < level.Difficulty ? (selected ? Ink : Led) : A(selected ? Ink : Led, .2f), 2);
                Label(R(col[4], y, 50, 52), (i + 1).ToString(), condBlack, 28, ink, TextAnchor.MiddleCenter);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) game.StartLevel(i, true);
            }
            Label(R(806, 762, 720, 26), "Všechny tratě jsou otevřené. Rekordy se ukládají na tomto počítači.", body, 15, A(Enamel, .45f));
        }
        void BottomHints(params string[] hints)
        {
            Fill(new Rect(0, Screen.height - 58 * k, Screen.width, 58 * k), A(NavyDeep, .94f));
            float x = 40;
            foreach (var hint in hints)
            {
                float w = Width(hint, condBold, 17) + 26;
                var chip = R(x, 856, w, 30, X.Left, Y.Bottom);
                Fill(chip, A(Enamel, .08f), 8);
                Label(chip, hint, condBold, 17, A(Enamel, .8f), TextAnchor.MiddleCenter);
                x += w + 10;
            }
        }

        // ---------- loading ----------
        void Loading()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), NavyDeep);
            var level = RailLevel.All[game.selectedLevel];
            Label(R(0, 340, 1600, 40), "PŘISTAVUJEME SOUPRAVU…", condBold, 28, Amber, TextAnchor.MiddleCenter);
            Label(R(0, 384, 1600, 90), level.Name, condBlack, 76, Enamel, TextAnchor.MiddleCenter);
            Label(R(0, 476, 1600, 40), level.Brief, body, 20, A(Enamel, .7f), TextAnchor.MiddleCenter);
        }

        // ---------- in-game HUD ----------
        void Hud()
        {
            var level = game.Level;
            // Enamel station sign.
            float signWidth = Mathf.Max(300, Width(level.Name, condBlack, 40) + 56);
            var sign = R(28, 22, signWidth, 66, X.Left, Y.Top);
            Shadow(sign, 10);
            Fill(sign, Enamel, 10);
            Outline(new Rect(sign.x + 5 * k, sign.y + 5 * k, sign.width - 10 * k, sign.height - 10 * k), Navy, 3, 7);
            Label(sign, level.Name, condBlack, 40, Navy, TextAnchor.MiddleCenter);
            string zone = (game.Zone == level.Name ? "START" : game.Zone) + (level.CheckpointCount > 0 ? "   ·   CHECKPOINT " + game.checkpoint + " / " + level.CheckpointCount : "");
            var zoneChip = R(28, 94, Width(zone, condBold, 18) + 28, 30, X.Left, Y.Top);
            Fill(zoneChip, A(Navy, .9f), 8);
            Label(zoneChip, zone, condBold, 18, Amber, TextAnchor.MiddleCenter);
            Status();
            // Stopwatch.
            var watch = R(660, 18, 280, 92, X.Center, Y.Top);
            Panel(watch, A(Navy, .94f), 14);
            Label(new Rect(watch.x, watch.y + 4 * k, watch.width, 56 * k), Format(Mathf.Max(.001f, game.elapsed)), condBlack, 54, Enamel, TextAnchor.MiddleCenter);
            Label(new Rect(watch.x, watch.y + 58 * k, watch.width, 26 * k), game.bestTime > 0 ? "REKORD  " + Format(game.bestTime) : "BEZ REKORDU", condBold, 17, game.bestTime > 0 ? Amber : Muted, TextAnchor.MiddleCenter);
            // Lives, score, tickets, combo.
            var stats = R(1232, 18, 340, 92, X.Right, Y.Top);
            Panel(stats, A(Navy, .94f), 14);
            for (int i = 0; i < game.Level.Lives; i++)
            {
                var lamp = R(1254 + i * 34, 34, 24, 24, X.Right, Y.Top);
                bool on = i < game.lives;
                if (on) Fill(new Rect(lamp.x - 4 * k, lamp.y - 4 * k, lamp.width + 8 * k, lamp.height + 8 * k), A(Red, .35f), 16);
                Fill(lamp, on ? Red : A(Muted, .25f), 12);
                if (on) Fill(new Rect(lamp.x + 5 * k, lamp.y + 4 * k, 7 * k, 6 * k), A(Color.white, .6f), 4);
            }
            Label(R(1380, 26, 172, 40, X.Right, Y.Top), game.score.ToString(), condBlack, 38, Enamel, TextAnchor.MiddleRight);
            TicketIcon(R(1256, 72, 26, 18, X.Right, Y.Top));
            Label(R(1290, 66, 120, 30, X.Right, Y.Top), game.chaos.ticketsCollected + " / " + game.world.tickets.Count, condBold, 20, A(Enamel, .85f));
            if (game.chaos.combo > 1)
            {
                var pill = R(1452, 68, 100, 26, X.Right, Y.Top);
                Fill(pill, Amber, 13);
                Label(pill, "KOMBO ×" + game.chaos.combo, condBlack, 17, Ink, TextAnchor.MiddleCenter);
            }
            // Dash readiness.
            var dash = R(1232, 118, 340, 34, X.Right, Y.Top);
            Fill(dash, A(Navy, .9f), 10);
            float ready = 1 - game.chaos.dashCooldown / 1.25f;
            Fill(new Rect(dash.x + 4 * k, dash.y + dash.height - 7 * k, (dash.width - 8 * k) * ready, 3 * k), ready >= 1 ? Green : Amber, 2);
            Label(new Rect(dash.x + 14 * k, dash.y, dash.width, dash.height - 3 * k), ready >= 1 ? "SHIFT  ·  ÚHYB PŘIPRAVEN" : "ÚHYB SE NABÍJÍ", condBold, 17, ready >= 1 ? Enamel : Muted);
            Notice();
            Wind();
            Route();
            if (game.LevelIndex == 0 && game.position.x < 24 && game.mode == RailGame.Mode.Playing)
            {
                bool first = game.position.x < 9;
                var tip = R(28, 690, 440, 112, X.Left, Y.Bottom);
                Panel(tip, A(Navy, .94f), 14);
                Label(R(50, 702, 400, 28, X.Left, Y.Bottom), first ? "KUFROVÝ KATAPULT" : "RYCHLÝ PÁD A ÚHYB", condBold, 20, Amber);
                text.wordWrap = true;
                Label(R(50, 732, 400, 60, X.Left, Y.Bottom), first ? "Šlápni na růžový kufr. Ve vzduchu můžeš skočit ještě jednou."
                    : "Ve vzduchu drž S nebo šipku dolů a dopadneš dřív. Shift tě protáhne kolem brašny.", body, 18, Enamel, TextAnchor.UpperLeft);
                text.wordWrap = false;
            }
        }
        void TicketIcon(Rect r)
        {
            Fill(r, Amber, 3);
            Fill(new Rect(r.x + r.width * .62f, r.y + 2 * k, 1.5f * k, r.height - 4 * k), A(Ink, .5f));
            Fill(new Rect(r.x - r.height * .2f, r.y + r.height * .3f, r.height * .4f, r.height * .4f), Navy, 6);
        }
        void Status()
        {
            var chaos = game.chaos; float y = 134;
            if (chaos.infection > 0)
            {
                var chip = R(28, y, 300, 46, X.Left, Y.Top);
                Fill(chip, A(Navy, .92f), 10);
                Label(new Rect(chip.x + 14 * k, chip.y + 2 * k, chip.width, 24 * k), "NÁKAZA  " + Mathf.CeilToInt(chaos.infection) + " %", condBold, 18, Hex("#B6EC4A"));
                Fill(new Rect(chip.x + 14 * k, chip.y + 32 * k, 272 * k, 5 * k), A(Muted, .25f), 3);
                Fill(new Rect(chip.x + 14 * k, chip.y + 32 * k, 272 * k * chaos.infection / 100, 5 * k), Hex("#B6EC4A"), 3);
                y += 54;
            }
            if (chaos.turbo > 0)
            {
                var chip = R(28, y, 300, 34, X.Left, Y.Top);
                Fill(chip, A(Navy, .92f), 10);
                Label(new Rect(chip.x + 14 * k, chip.y, chip.width, chip.height), "TVARŮŽKOVÉ TURBO  " + Mathf.CeilToInt(chaos.turbo) + " s", condBold, 18, Amber);
            }
        }
        void Notice()
        {
            if (game.noticeTime <= 0 || string.IsNullOrEmpty(game.notice)) return;
            float alpha = Mathf.Clamp01(game.noticeTime * 2.5f) * Mathf.Clamp01((4.2f - game.noticeTime) * 4);
            float w = Mathf.Min(1000, Width(game.notice, bodyBold, 19) + 150);
            var banner = R(800 - w * .5f, 122, w, 40, X.Center, Y.Top);
            Fill(banner, A(Navy, .92f * alpha), 20);
            var tag = new Rect(banner.x + 6 * k, banner.y + 6 * k, 96 * k, banner.height - 12 * k);
            Fill(tag, A(Amber, alpha), 14);
            Label(tag, "HLÁŠENÍ", condBlack, 15, A(Ink, alpha), TextAnchor.MiddleCenter);
            Label(new Rect(tag.xMax + 14 * k, banner.y, banner.width - 130 * k, banner.height), game.notice, bodyBold, 19, A(Enamel, alpha));
        }
        void Wind()
        {
            if (!game.windWarning && game.wind > -.2f) return;
            bool gust = game.wind <= -.2f;
            float blink = gust ? 1 : (Mathf.Sin(UnityEngine.Time.unscaledTime * 14) > 0 ? 1 : .35f);
            var chip = R(640, 170, 320, 40, X.Center, Y.Top);
            Fill(chip, A(gust ? Red : Amber, .95f * blink), 20);
            Label(chip, gust ? "POPRYV VĚTRU" : "POZOR  ·  VÍTR SE ZVEDÁ", condBlack, 22, gust ? Enamel : Ink, TextAnchor.MiddleCenter);
        }
        void Route()
        {
            var level = game.Level;
            var bar = new Rect(0, Screen.height - 46 * k, Screen.width, 46 * k);
            Fill(bar, A(NavyDeep, .9f));
            float left = 170 * k, right = Screen.width - 150 * k, y = Screen.height - 23 * k;
            Label(new Rect(20 * k, bar.y, 150 * k, bar.height), "R  ·  CHECKPOINT", condBold, 15, A(Enamel, .55f));
            Label(new Rect(Screen.width - 146 * k, bar.y, 130 * k, bar.height), "ESC  ·  " + Mathf.RoundToInt(fps) + " FPS", condBold, 15, A(Enamel, .55f), TextAnchor.MiddleRight);
            Fill(new Rect(left, y - 2 * k, right - left, 4 * k), A(Muted, .3f), 2);
            Fill(new Rect(left, y - 2 * k, (right - left) * game.Progress, 4 * k), Amber, 2);
            var stations = level.Stations;
            for (int i = 0; i < stations.Length; i++)
            {
                float t = Mathf.Clamp01(level.Platforms[stations[i]].x / level.Finish);
                float x = Mathf.Lerp(left, right, t);
                bool done = i == 0 || i <= game.checkpoint;
                bool finish = i == stations.Length - 1;
                float size = finish ? 16 : 12;
                Fill(new Rect(x - size * .5f * k, y - size * .5f * k, size * k, size * k), finish ? Enamel : done ? Green : Hex("#38536B"), size * .5f);
                if (!finish && !done) Fill(new Rect(x - 3 * k, y - 3 * k, 6 * k, 6 * k), NavyDeep, 3);
            }
            float marker = Mathf.Lerp(left, right, game.Progress);
            var loco = new Rect(marker - 13 * k, y - 9 * k, 26 * k, 18 * k);
            Fill(loco, Amber, 5);
            Fill(new Rect(loco.x + 4 * k, loco.y + 4 * k, 7 * k, 5 * k), Navy, 1);
            Fill(new Rect(loco.x + 14 * k, loco.y + 4 * k, 7 * k, 5 * k), Navy, 1);
        }
        void HitFlash()
        {
            if (game.reducedMotion || game.audioMix.ImpactPulse <= 0) return;
            var hit = new Color(1, .21f, .27f, .32f * game.audioMix.ImpactPulse);
            float t = 12 * k;
            Fill(new Rect(0, 0, Screen.width, t), hit); Fill(new Rect(0, Screen.height - t, Screen.width, t), hit);
            Fill(new Rect(0, 0, t, Screen.height), hit); Fill(new Rect(Screen.width - t, 0, t, Screen.height), hit);
        }
        void Pops()
        {
            foreach (var pop in game.chaos.pops)
            {
                if (pop.life <= 0) continue;
                Vector3 screen = game.view.WorldToScreenPoint(new Vector3(pop.at.x, pop.at.y, -.4f));
                if (screen.z <= 0) continue;
                float alpha = Mathf.Clamp01(pop.life * 3);
                var r = new Rect(screen.x - 220 * k, Screen.height - screen.y - 24 * k, 440 * k, 48 * k);
                Label(r, pop.text, condBlack, 28, new Color(pop.color.r, pop.color.g, pop.color.b, alpha), TextAnchor.MiddleCenter, true);
            }
        }

        // ---------- overlays ----------
        void Dim(float alpha = .72f) => Fill(new Rect(0, 0, Screen.width, Screen.height), A(NavyDeep, alpha * Mathf.SmoothStep(0, 1, appear)));
        float Rise() => (1 - Mathf.SmoothStep(0, 1, appear)) * 40;
        void Pause()
        {
            Dim();
            float y = 200 + Rise();
            var card = R(520, y, 560, 500);
            Panel(card, Navy, 20);
            Label(R(560, y + 30, 480, 30), "VLAK ČEKÁ  ·  " + game.Level.Name, condBold, 20, Amber);
            Label(R(556, y + 60, 480, 80), "PAUZA", condBlack, 76, Enamel);
            Label(R(560, y + 140, 480, 30), "ČAS  " + Format(game.elapsed) + "     BODY  " + game.score, condBold, 22, A(Enamel, .7f));
            if (Button(R(560, y + 190, 480, 60), "POKRAČOVAT", true, "ESC")) game.TogglePause();
            if (Button(R(560, y + 262, 480, 54), "RESTART TRATI", false)) game.StartRun();
            if (Button(R(560, y + 326, 234, 54), "NASTAVENÍ", false)) settings = true;
            if (Button(R(806, y + 326, 234, 54), "HLAVNÍ MENU", false)) GoMenu();
            Label(R(560, y + 400, 480, 60), "Hra se při přepnutí okna sama pozastaví.", body, 16, A(Enamel, .5f));
        }
        void GoMenu() { game.selectedLevel = game.LevelIndex; game.Menu(); }
        void Ticket()
        {
            Dim(.62f);
            float y = 120 + Rise();
            var level = game.Level;
            var card = R(400, y, 800, 470);
            Shadow(card, 18, .5f);
            Fill(card, Paper, 18);
            Fill(new Rect(card.x, card.y, card.width, 78 * k), Navy, 18);
            Fill(new Rect(card.x, card.y + 60 * k, card.width, 18 * k), Navy);
            Label(R(436, y + 10, 500, 60), "ČESKÉ DRÁHY  ·  JÍZDENKA", condBlack, 30, Enamel);
            Label(R(900, y + 10, 270, 60), "TRAŤ " + (game.LevelIndex + 1) + " / " + RailLevel.All.Length, condBold, 22, Amber, TextAnchor.MiddleRight);
            // Perforated stub.
            for (int i = 0; i < 18; i++) Fill(R(1016, y + 96 + i * 20, 6, 10), A(Ink, .35f), 3);
            Fill(R(1004, y + 70, 30, 30), A(NavyDeep, .62f), 15); Fill(R(1004, y + 455, 30, 30), A(NavyDeep, .62f), 15);
            Label(R(1040, y + 100, 140, 30), "ODKUD", condBold, 16, A(Ink, .5f), TextAnchor.MiddleCenter);
            Label(R(1040, y + 126, 140, 36), "START", condBlack, 26, Ink, TextAnchor.MiddleCenter);
            Label(R(1040, y + 178, 140, 30), "KAM", condBold, 16, A(Ink, .5f), TextAnchor.MiddleCenter);
            text.wordWrap = true;
            Label(R(1044, y + 204, 132, 80), level.Name, condBlack, 22, Ink, TextAnchor.UpperCenter);
            text.wordWrap = false;
            Label(R(1040, y + 380, 140, 40), "Os " + (4601 + game.LevelIndex * 2), condBold, 22, A(Ink, .6f), TextAnchor.MiddleCenter);
            Label(R(440, y + 96, 540, 30), "DOJEL JSI DO STANICE", condBold, 20, A(Ink, .55f));
            Label(R(436, y + 120, 560, 56), level.Name, condBlack, 46, Ink);
            Label(R(440, y + 184, 300, 26), "TVŮJ ČAS", condBold, 20, A(Ink, .55f));
            Label(R(434, y + 204, 560, 110), Format(game.elapsed), condBlack, 108, Ink);
            if (game.newRecord) Stamp(R(800, y + 196, 190, 110), "NOVÝ\nREKORD!");
            else Label(R(740, y + 236, 250, 60), "REKORD  " + Format(game.bestTime) + "\n+" + (game.elapsed - game.bestTime).ToString("0.00") + " s", condBold, 22, A(Ink, .7f), TextAnchor.MiddleRight);
            Fill(R(440, y + 324, 550, 2), A(Ink, .15f));
            Stat(440, y + 336, "BODY", game.score.ToString());
            Stat(580, y + 336, "JÍZDENKY", game.chaos.ticketsCollected + "/" + game.world.tickets.Count);
            Stat(720, y + 336, "ODRAZY", game.chaos.stomps.ToString());
            Stat(840, y + 336, "KOMBO", "×" + game.chaos.bestCombo);
            Stat(440, y + 396, "ŽIVOTY", game.lives + " / " + level.Lives);
            Stat(580, y + 396, "REKORD BODŮ", game.bestScore.ToString());
            bool next = game.HasNextLevel;
            if (!next) Label(R(720, y + 396, 280, 56), "VŠECH 10 TRATÍ!", condBlack, 26, Red);
            float by = y + 490;
            if (next && Button(R(400, by, 380, 64), "DALŠÍ TRAŤ", true, "ENTER")) game.FinishAction(true);
            if (Button(R(next ? 796 : 400, by, next ? 196 : 390, 64), "ZNOVU", !next, next ? "R" : "ENTER")) game.StartRun();
            if (Button(R(next ? 1008 : 806, by, next ? 192 : 394, 64), "MENU", false, "ESC")) GoMenu();
        }
        void Stat(float x, float y, string name, string value)
        {
            Label(R(x, y, 140, 22), name, condBold, 16, A(Ink, .5f));
            Label(R(x, y + 18, 140, 36), value, condBlack, 30, Ink);
        }
        void Stamp(Rect r, string value)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-9, r.center);
            Outline(r, A(Red, .9f), 5, 14);
            Outline(new Rect(r.x + 9 * k, r.y + 9 * k, r.width - 18 * k, r.height - 18 * k), A(Red, .7f), 2, 9);
            Label(r, value, condBlack, 34, A(Red, .95f), TextAnchor.MiddleCenter);
            GUI.matrix = matrix;
        }
        void Missed()
        {
            Dim(.7f);
            float y = 190 + Rise();
            var card = R(500, y, 600, 420);
            Panel(card, Navy, 20);
            Fill(new Rect(card.x, card.y, card.width, 8 * k), Red, 4);
            Label(R(540, y + 34, 520, 30), "KONEC JÍZDY  ·  " + game.Zone, condBold, 20, Red);
            Label(R(536, y + 62, 540, 90), "UJEL TI VLAK.", condBlack, 78, Enamel);
            Label(R(540, y + 150, 520, 30), "Holubi si tě napsali do jízdního řádu.", body, 20, A(Enamel, .7f));
            Label(R(540, y + 196, 520, 30), "ČAS  " + Format(game.elapsed) + "     BODY  " + game.score + "     JÍZDENKY  " + game.chaos.ticketsCollected, condBold, 22, Amber);
            if (Button(R(540, y + 256, 520, 62), "ZNOVU NA TRAŤ", true, "ENTER")) game.StartRun();
            if (Button(R(540, y + 330, 520, 54), "HLAVNÍ MENU", false, "ESC")) GoMenu();
        }
        void Settings()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), A(NavyDeep, .55f));
            var card = R(500, 150, 600, 600);
            Panel(card, Hex("#10263B"), 20);
            Label(R(540, 176, 520, 70), "NASTAVENÍ", condBlack, 58, Enamel);
            if (Toggle(250, "ZVUK", !game.muted)) { game.muted = !game.muted; game.SaveSettings(); }
            if (Toggle(316, "GRAFIKA  ·  " + (game.lowQuality ? "ÚSPORNÁ" : "VYSOKÁ"), !game.lowQuality)) { game.lowQuality = !game.lowQuality; game.SaveSettings(); }
            if (Toggle(382, "OTŘESY KAMERY A BLIKÁNÍ", !game.reducedMotion)) { game.reducedMotion = !game.reducedMotion; game.SaveSettings(); }
            if (Toggle(448, "CELÁ OBRAZOVKA", Screen.fullScreen)) game.ToggleFullscreen();
            Label(R(540, 520, 400, 30), "ŘEV PŘI ZÁSAHU", condBold, 22, Enamel);
            Label(R(900, 520, 160, 30), Mathf.RoundToInt(game.hitShoutVolume * 100) + " %", condBold, 22, Amber, TextAnchor.MiddleRight);
            var track = R(540, 562, 520, 10);
            Fill(track, A(Muted, .25f), 5);
            Fill(new Rect(track.x, track.y, track.width * game.hitShoutVolume, track.height), Amber, 5);
            var knob = new Rect(track.x + track.width * game.hitShoutVolume - 12 * k, track.y - 7 * k, 24 * k, 24 * k);
            Fill(knob, Enamel, 12);
            var hit = new Rect(track.x - 12 * k, track.y - 16 * k, track.width + 24 * k, 42 * k);
            var e = Event.current;
            if ((e.type == EventType.MouseDown && hit.Contains(e.mousePosition)) || (e.type == EventType.MouseDrag && volumeDirty))
            {
                game.hitShoutVolume = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                volumeDirty = true; e.Use();
            }
            // Persist once per drag instead of on every slider event.
            if (volumeDirty && e.rawType == EventType.MouseUp) { volumeDirty = false; game.SaveSettings(); }
            Label(R(540, 584, 520, 30), "Ovlivní jen řev. Náraz a hlášky zůstanou slyšet.", body, 16, A(Enamel, .5f));
            if (Button(R(540, 650, 520, 62), "HOTOVO", true, "ESC")) CloseSettings();
        }
        void CloseSettings()
        {
            settings = false;
            if (volumeDirty) { volumeDirty = false; game.SaveSettings(); }
        }
        bool Toggle(float y, string name, bool on)
        {
            var row = R(540, y, 520, 54);
            bool hover = row.Contains(Event.current.mousePosition);
            Fill(row, A(Color.white, hover ? .08f : .04f), 12);
            Label(new Rect(row.x + 18 * k, row.y, row.width, row.height), name, condBold, 22, Enamel);
            var track = new Rect(row.xMax - 86 * k, row.y + 14 * k, 64 * k, 26 * k);
            Fill(track, on ? Green : A(Muted, .35f), 13);
            Fill(new Rect(on ? track.xMax - 24 * k : track.x + 2 * k, track.y + 2 * k, 22 * k, 22 * k), Enamel, 11);
            return GUI.Button(row, GUIContent.none, GUIStyle.none);
        }
    }
}
