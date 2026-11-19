using UnityEditor;

/// <summary>
/// Import rules for the game's textures, so a fresh checkout imports them identically:
/// Poly Haven sets (Resources/Textures/PH) keep their 1K/2K size, normals are normal maps, ORM maps are linear.
/// </summary>
public sealed class RailAssetRules : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Textures/")) return;
        var importer = (TextureImporter)assetImporter;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        importer.mipmapEnabled = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        importer.filterMode = UnityEngine.FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        if (name.EndsWith("_N"))
        {
            importer.textureType = TextureImporterType.NormalMap;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            // Occlusion/roughness/metal are data, not colour.
            importer.sRGBTexture = !name.EndsWith("_ORM");
            importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
        }
    }
}
