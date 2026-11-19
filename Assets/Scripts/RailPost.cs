using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Per-level colour grade, filled from the route's sky preset.</summary>
    public struct RailGrade
    {
        public Color lift, gain;
        public float exposure, contrast, saturation, bloom, threshold, vignette;
    }

    /// <summary>Built-in pipeline image effect: dual-filter bloom, ACES, grading and vignette. Off in low quality.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RailPost : MonoBehaviour
    {
        const int Levels = 5;
        readonly RenderTexture[] chain = new RenderTexture[Levels];
        Material material;
        RailGrade grade = new RailGrade{lift=Color.black,gain=Color.white,exposure=1.1f,contrast=1.04f,saturation=1.06f,bloom=.55f,threshold=1,vignette=.26f};

        void Awake()
        {
            var shader = Resources.Load<Shader>("Shaders/RailPost");
            if (shader && shader.isSupported) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            GetComponent<Camera>().allowHDR = true;
        }

        void OnDestroy() { if (material) Destroy(material); }

        public void Apply(RailGrade value) => grade = value;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!material) { Graphics.Blit(source, destination); return; }
            material.SetVector("_Threshold", new Vector4(grade.threshold, grade.threshold * .5f));
            material.SetVector("_Lift", grade.lift);
            material.SetVector("_Gain", grade.gain);
            material.SetVector("_Grade", new Vector4(grade.exposure, grade.contrast, grade.saturation, grade.bloom));
            material.SetVector("_Vignette", new Vector4(grade.vignette, (float)source.width / source.height, .004f));
            var format = source.format;
            int width = source.width / 2, height = source.height / 2, count = 0;
            chain[count] = RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(source, chain[count++], material, 0);
            while (count < Levels && height > 8)
            {
                width /= 2; height /= 2;
                chain[count] = RenderTexture.GetTemporary(width, height, 0, format);
                Graphics.Blit(chain[count - 1], chain[count], material, 1);
                count++;
            }
            for (int i = count - 2; i >= 0; i--)
            {
                Graphics.Blit(chain[i + 1], chain[i], material, 2);
                RenderTexture.ReleaseTemporary(chain[i + 1]);
                chain[i + 1] = null;
            }
            material.SetTexture("_BloomTex", chain[0]);
            Graphics.Blit(source, destination, material, 3);
            RenderTexture.ReleaseTemporary(chain[0]);
            chain[0] = null;
        }
    }
}
