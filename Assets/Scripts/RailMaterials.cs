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
        static bool Cutout(string part) => part == "Hair" || part == "Lashes";

        /// <summary>
        /// Replaces a character's imported materials (named &lt;prefix&gt;_&lt;Part&gt;) with Railway/Surface materials
        /// using &lt;folder&gt;&lt;Part&gt;_A (albedo, alpha), _N (normal) and _ORM (occlusion/roughness/metal) from Resources.
        /// </summary>
        public static void DressCharacter(GameObject model, string folder)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string raw = materials[i] ? materials[i].name.Replace(" (Instance)", "") : renderer.name;
                    string part = raw.Contains("_") ? raw.Substring(raw.IndexOf('_') + 1) : raw;
                    materials[i] = Character(folder, part);
                }
                renderer.sharedMaterials = materials;
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
        }

        static Material Character(string folder, string part)
        {
            string key = folder + part;
            if (cache.TryGetValue(key, out var existing) && existing) return existing;
            var template = Resources.Load<Material>(Cutout(part) ? "SurfaceCutout" : "Surface");
            var material = new Material(template) { name = "Character " + part, enableInstancing = true, color = Color.white };
            var albedo = Resources.Load<Texture2D>(folder + part + "_A");
            var normal = Resources.Load<Texture2D>(folder + part + "_N");
            var orm = Resources.Load<Texture2D>(folder + part + "_ORM");
            if (albedo) material.mainTexture = albedo;
            if (normal) { material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", 1); }
            material.SetFloat("_UseORM", orm ? 1 : 0);
            if (orm) material.SetTexture("_ORMMap", orm);
            material.SetFloat("_Glossiness", Smoothness(part));
            material.SetFloat("_SmoothScale", .9f);
            material.SetFloat("_Detail", 0);
            material.SetFloat("_Macro", 0);
            material.SetFloat("_SpecAA", 1.5f);
            material.SetFloat("_RimMask", .55f);
            material.SetFloat("_Metallic", 0);
            if (Cutout(part)) material.SetFloat("_Cutoff", .42f);
            cache[key] = material;
            return material;
        }
    }
}
