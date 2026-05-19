using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteImportProcessor : AssetPostprocessor
{
    private const string SpritesFolderPath = "Assets/Art/Sprites";
    
    
    private readonly Dictionary<string, int> _ppuByLayer = new()
    {
        //PPU values for each layer
        {"BG3", 10},
        {"BG2", 20},
        {"BG1", 50},
        {"Main", 200},
        {"FG1", 500},
    };
    
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SpritesFolderPath)) return;
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        
        TextureImporter importer = assetImporter as TextureImporter;
        if(importer == null) return;
        
        
        //auto change texture type
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
        }
        
        //check 4x4 blocks
        
        //auto change PPU
        foreach (string prefix in _ppuByLayer.Keys)
        {
            if (fileName.StartsWith(prefix))
            {
                importer.spritePixelsPerUnit = _ppuByLayer[prefix];
                break;
            }
        }
        
        
        int texWidth;
        int texHeight;
        importer.GetSourceTextureWidthAndHeight(out  texWidth, out texHeight);

        if (texWidth % 4 != 0 || texHeight % 4 != 0)
        {
            Debug.LogWarning($"Texture {assetPath} does not comply with 4x4 block rule ({texWidth}x{texHeight}px). Consider resizing.");
        }
    }
}
