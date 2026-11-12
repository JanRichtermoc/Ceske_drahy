using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailAudio : MonoBehaviour
    {
        public static readonly string[] Barks =
        {
            "Rohlík má přednost v jízdě!",
            "Ten holub mi snědl jízdní řád!",
            "Moje ponožky jsou na výluce!",
            "Pozor! Útočí tvarůžková jednotka!",
            "Vrať se! Můj kufr chce podpis!",
            "Výpravčí je ze sýra!"
        };

        public static readonly string[] Announcements =
        {
            "Vlak do obýváku odjíždí z lednice.",
            "Cestující, přestaňte prosím skákat přes jízdní řád."
        };

        RailGame game;
        AudioSource ambience, rails, effects, screams, dialogue;
        AudioClip step1, step2, land, ticket, spring, wings, whoosh, impact, whistle;
        readonly AudioClip[] screamClips = new AudioClip[3];
        readonly AudioClip[] voiceClips = new AudioClip[8];
        float stepClock, wingClock, duck, voiceCooldown;
        bool wasGrounded;
        int nextStep, nextScream, nextBark;
        public float ImpactPulse { get; private set; }

        public void Initialize(RailGame owner)
        {
            game = owner;
            ambience = Source();
            rails = Source();
            effects = Source();
            screams = Source();
            dialogue = Source();
            ambience.clip = Load("station");
            rails.clip = Load("train");
            foreach (var source in new[] { ambience, rails })
            {
                source.loop = true;
                source.volume = 0;
                if (source.clip) source.Play();
            }

            step1 = Load("step1"); step2 = Load("step2");
            land = Load("land"); ticket = Load("ticket"); spring = Load("spring");
            wings = Load("wings"); whoosh = Load("whoosh");
            impact = Load("impact"); whistle = Load("whistle");
            for (int i = 0; i < screamClips.Length; i++) screamClips[i] = Load("scream" + (i + 1));
            for (int i = 0; i < voiceClips.Length; i++) voiceClips[i] = Load("voice" + (i + 1));
        }

        AudioSource Source()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }

        static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/" + name);

        void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (!game.muted && clip && volume > 0) source.PlayOneShot(clip, volume);
        }

        public void ResetRun()
        {
            screams.Stop();
            dialogue.Stop();
            effects.Stop();
            stepClock = 0;
            wingClock = 0;
            voiceCooldown = 0;
            duck = 0;
            wasGrounded = false;
            ImpactPulse = 0;
        }

        public void Jump() => Play(effects, step2, .13f);
        public void Ticket() => Play(effects, ticket, .21f);
        public void Spring() => Play(effects, spring, .29f);
        public void Dash() => Play(effects, whoosh, .27f);
        public void Celebrate() => Play(effects, whistle, .25f);

        public void Hit()
        {
            Play(effects, impact, .25f);
            if (game.hitShoutVolume > 0)
            {
                screams.Stop();
                Play(screams, screamClips[nextScream++ % screamClips.Length], .86f * game.hitShoutVolume);
                duck = 1.25f;
            }
            ImpactPulse = 1;
        }

        public string Bark(float sourceX)
        {
            if (voiceCooldown > 0) return null;
            int i = nextBark++ % Barks.Length;
            Speak(i, Mathf.Clamp((sourceX - game.position.x) / 11f, -.78f, .78f), .59f);
            return Barks[i];
        }

        public void Announce(int line)
        {
            if (voiceCooldown > 0) return;
            Speak(Barks.Length + line, 0, .37f);
        }

        void Speak(int clipIndex, float pan, float volume)
        {
            AudioClip clip = voiceClips[clipIndex];
            dialogue.panStereo = pan;
            Play(dialogue, clip, volume);
            voiceCooldown = clip ? clip.length + .32f : 2.2f;
        }

        void Update()
        {
            if (game == null) return;
            float dt = Time.unscaledDeltaTime;
            ImpactPulse = Mathf.MoveTowards(ImpactPulse, 0, dt * 3.8f);
            duck = Mathf.Max(0, duck - dt);
            voiceCooldown = Mathf.Max(0, voiceCooldown - dt);
            bool playing = game.mode == RailGame.Mode.Playing;
            float background = game.muted ? 0 : duck > 0 ? .24f : 1;
            ambience.volume = background * (playing ? .16f : .095f);
            rails.volume = background * (playing ? .105f : .035f);
            dialogue.volume = duck > 0 ? .3f : 1;
            bool silence = game.muted || game.mode == RailGame.Mode.Paused;
            effects.mute = screams.mute = dialogue.mute = silence;
            if (!playing) return;

            if (game.grounded && !wasGrounded)
                Play(effects, land, .14f);
            wasGrounded = game.grounded;

            stepClock -= dt;
            float speed = Mathf.Abs(game.velocity.x);
            if (game.grounded && speed > 1.3f && stepClock <= 0)
            {
                stepClock = Mathf.Lerp(.35f, .17f, Mathf.Clamp01(speed / 10));
                Play(effects, nextStep++ % 2 == 0 ? step1 : step2, .15f);
            }

            wingClock -= dt;
            if (wingClock > 0) return;
            foreach (var bird in game.world.birds)
            {
                if (!bird.gameObject.activeSelf || Mathf.Abs(bird.position.x - game.position.x) > 5) continue;
                Play(effects, wings, .17f);
                wingClock = 2.4f;
                break;
            }
        }
    }
}
