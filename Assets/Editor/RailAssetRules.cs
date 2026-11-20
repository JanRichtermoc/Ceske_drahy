using UnityEditor;

/// <summary>
/// Import rules for the game's textures, so a fresh checkout imports them identically:
/// Poly Haven sets (Resources/Textures/PH) and model textures import at up to 1K (CJ 2K), *_N are normal maps, *_ORM are linear.
/// </summary>
public sealed class RailAssetRules : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // World texture sets and the textures of the imported characters and props.
        if (!assetPath.StartsWith("Assets/Resources/Textures/") && !(assetPath.StartsWith("Assets/Resources/Models/") && assetPath.Contains("/Textures/"))) return;
        var importer = (TextureImporter)assetImporter;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        importer.mipmapEnabled = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        importer.filterMode = UnityEngine.FilterMode.Trilinear;
        importer.anisoLevel = 8;
        // The orthographic view shows about 90 px per metre: 1K is plenty for tiled sets and props; only CJ keeps 2K.
        importer.maxTextureSize = assetPath.Contains("/Characters/CJ/") ? 2048 : 1024;
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
