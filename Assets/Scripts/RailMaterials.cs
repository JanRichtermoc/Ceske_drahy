using System.Collections.Generic;
using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Puts imported models on the game's own Railway/Surface shaders so they light like the rest of the world.</summary>
    public static class RailMaterials
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// <summary>Per-part look of the characters: smoothness when the part has no ORM map, cutout parts.</summary>
        static float Smoothness(string part)
        {
            switch (part)
            {
                case "Head": case "Hands": case "Skin": return .42f;
                case "Teeth": return .55f;
                case "Eyes": return .6f;
                case "Hair": return .28f;
                case "Shoes": return .32f;
                default: return .22f;
            }
        }
        static bool Cutout(string part) => part == "Hair" || part == "Lashes" || part.EndsWith("Cutout");
        [System.Serializable] sealed class PartInfo { public string name; public float[] color; public float metallic, roughness; }
        [System.Serializable] sealed class PartList { public PartInfo[] parts; }
        static readonly Dictionary<string, Dictionary<string, PartInfo>> partInfo = new Dictionary<string, Dictionary<string, PartInfo>>();
        /// <summary>Per-part base colour and finish written by Tools/blender/character_prep.py (parts without textures).</summary>
        static PartInfo Info(string folder, string part)
        {
            if (!partInfo.TryGetValue(folder, out var parts))
            {
                parts = new Dictionary<string, PartInfo>();
                var json = Resources.Load<TextAsset>(folder + "materials");
                if (json) foreach (var p in JsonUtility.FromJson<PartList>(json.text).parts) parts[p.name] = p;
                partInfo[folder] = parts;
            }
            return parts.TryGetValue(part, out var info) ? info : null;
        }

        /// <summary>
        /// Replaces a character's imported materials (named &lt;prefix&gt;_&lt;Part&gt;) with Railway/Surface materials
        /// using &lt;folder&gt;&lt;Part&gt;_A (albedo, alpha), _N (normal) and _ORM (occlusion/roughness/metal) from Resources.
        /// </summary>
        public static void DressCharacter(GameObject model, string folder, float brightness = 1)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string raw = materials[i] ? materials[i].name.Replace(" (Instance)", "") : renderer.name;
                    string part = raw.Contains("_") ? raw.Substring(raw.IndexOf('_') + 1) : raw;
                    materials[i] = Character(folder, part, brightness);
                }
                renderer.sharedMaterials = materials;
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
        }

        /// <summary>Static props (trees, lamps): like characters, with an optional colour tint per instance group.</summary>
        public static void DressProp(GameObject model, string folder, Color tint)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string raw = materials[i] ? materials[i].name.Replace(" (Instance)", "") : renderer.name;
                    string part = raw.Contains("_") ? raw.Substring(raw.IndexOf('_') + 1) : raw;
                    string key = folder + part + "|" + ColorUtility.ToHtmlStringRGB(tint);
                    if (!cache.TryGetValue(key, out var material) || !material)
                    {
                        material = new Material(Character(folder, part, 1)) { name = "Prop " + part };
                        material.color = material.color * tint;
                        material.SetFloat("_Macro", .3f);
                        cache[key] = material;
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        static Material Character(string folder, string part, float brightness)
        {
            string key = folder + part + brightness;
            if (cache.TryGetValue(key, out var existing) && existing) return existing;
            var template = Resources.Load<Material>(Cutout(part) ? "SurfaceCutout" : "Surface");
            var material = new Material(template) { name = "Character " + part, enableInstancing = true, color = Color.white };
            var albedo = Resources.Load<Texture2D>(folder + part + "_A");
            var normal = Resources.Load<Texture2D>(folder + part + "_N");
            var orm = Resources.Load<Texture2D>(folder + part + "_ORM");
            if (albedo) material.mainTexture = albedo;
            var info = Info(folder, part);
            if (!albedo && info != null && info.color != null && info.color.Length >= 3)
                material.color = new Color(info.color[0], info.color[1], info.color[2]).gamma;
            if (normal) { material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", 1); }
            material.SetFloat("_UseORM", orm ? 1 : 0);
            if (orm) material.SetTexture("_ORMMap", orm);
            material.SetFloat("_Glossiness", info != null && !albedo ? 1 - info.roughness : Smoothness(part));
            material.SetFloat("_SmoothScale", .9f);
            material.SetFloat("_Detail", 0);
            material.SetFloat("_Macro", 0);
            material.SetFloat("_SpecAA", 1.5f);
            material.SetFloat("_RimMask", .55f);
            material.SetFloat("_Metallic", info != null && !albedo ? info.metallic : 0);
            if (Cutout(part)) material.SetFloat("_Cutoff", .42f);
            material.SetFloat("_ColorBoost", brightness);
            cache[key] = material;
            return material;
        }
    }
}
