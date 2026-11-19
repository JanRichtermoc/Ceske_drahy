using UnityEngine;
using UnityEngine.InputSystem;

namespace CeskeDrahy
{
    /// <summary>Keyboard and gamepad bindings, read through the Input System. One place to change controls.</summary>
    public static class RailInput
    {
        static Keyboard Keys => Keyboard.current;
        static Gamepad Pad => Gamepad.current;
        static readonly System.Collections.Generic.Dictionary<Key,(int frame,bool held,bool edge)> keyEdges=new System.Collections.Generic.Dictionary<Key,(int,bool,bool)>();
        /// <summary>
        /// Pressed this frame. Besides the Input System's own flag, a held-now/not-held-before edge is tracked per key,
        /// so state written outside the player update (tooling, remote input) still registers as a press.
        /// </summary>
        static bool Down(Key key)
        {
            if(Keys==null)return false;
            var control=Keys[key];
            int frame=Time.frameCount;
            if(!keyEdges.TryGetValue(key,out var state) || state.frame!=frame)
            {
                bool held=control.isPressed;
                bool edge=held && (!keyEdges.TryGetValue(key,out var previous) || (!previous.held && previous.frame>=frame-1));
                state=(frame,held,edge);keyEdges[key]=state;
            }
            return control.wasPressedThisFrame || state.edge;
        }
        static bool Held(Key key)=>Keys!=null && Keys[key].isPressed;
        static bool AltHeld => Held(Key.LeftAlt)||Held(Key.RightAlt);

        /// <summary>Horizontal move in [-1, 1]: A/D, arrows, left stick or d-pad.</summary>
        public static float Move
        {
            get
            {
                float axis=(Held(Key.D)||Held(Key.RightArrow)?1:0)-(Held(Key.A)||Held(Key.LeftArrow)?1:0);
                if(Pad!=null)
                {
                    float stick=Pad.leftStick.x.ReadValue();
                    if(Mathf.Abs(stick)>.25f)axis+=Mathf.Sign(stick)*Mathf.InverseLerp(.25f,.9f,Mathf.Abs(stick));
                    if(Pad.dpad.right.isPressed)axis+=1;
                    if(Pad.dpad.left.isPressed)axis-=1;
                }
                return Mathf.Clamp(axis,-1,1);
            }
        }
        // Non-short-circuit | so every key is sampled each frame for the edge tracking.
        public static bool JumpPressed => Down(Key.Space)|Down(Key.W)|Down(Key.UpArrow)|(Pad!=null && Pad.buttonSouth.wasPressedThisFrame);
        public static bool JumpHeld => Held(Key.Space)||Held(Key.W)||Held(Key.UpArrow)||(Pad!=null && Pad.buttonSouth.isPressed);
        public static bool FastFall => Held(Key.S)||Held(Key.DownArrow)||(Pad!=null && (Pad.dpad.down.isPressed||Pad.leftStick.y.ReadValue()<-.6f));
        public static bool Dash => Down(Key.LeftShift)|Down(Key.RightShift)|(Pad!=null && (Pad.buttonWest.wasPressedThisFrame||Pad.rightShoulder.wasPressedThisFrame));
        public static bool Pause => Down(Key.Escape)|(Pad!=null && Pad.startButton.wasPressedThisFrame);
        public static bool Restart => Down(Key.R)|(Pad!=null && Pad.buttonNorth.wasPressedThisFrame);
        public static bool Mute => Down(Key.M);
        /// <summary>Enter starts or continues; Alt+Enter is the player's fullscreen toggle and must not.</summary>
        public static bool Confirm => ((Down(Key.Enter)|Down(Key.NumpadEnter)) && !AltHeld)|(Pad!=null && Pad.buttonSouth.wasPressedThisFrame);
        public static bool MenuUp => Pad!=null && Pad.dpad.up.wasPressedThisFrame;
        public static bool MenuDown => Pad!=null && Pad.dpad.down.wasPressedThisFrame;
        public static bool MouseHeld => Mouse.current!=null && Mouse.current.leftButton.isPressed;
    }
}
