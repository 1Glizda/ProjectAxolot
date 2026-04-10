using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


public sealed class SpriteCutterTool
{
    
    //settings
    private const int MaxRawChunkSize = 2040;
    private const int ChunkPadding = 4;
    private const int TrimPadding = 4;
    
    
    [MenuItem("Assets/Sprite Tools/Sprite Cutter (2K)")]
    public static void Cutter()
    {
        Texture2D sourceTex = Selection.activeObject as Texture2D;
        if (sourceTex == null)
        {
            Debug.LogError("Selected file is not a Texture2D.");
            return;
        }
        CutTexture(sourceTex, false);
    }

    [MenuItem("Assets/Sprite Tools/Sprite Cutter (2K) + Prefab Creator")]
    public static void CutterPrefabCreator()
    {
        Texture2D sourceTex = Selection.activeObject as Texture2D;
        if (sourceTex == null)
        {
            Debug.LogError("Selected file is not a Texture2D.");
            return;
        }
        CutTexture(sourceTex, true);
    }
    
    private static void CutTexture(Texture2D sourceTex, bool passToPrefabCreator)
    {
        string assetPath = AssetDatabase.GetAssetPath(sourceTex);
        
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.compressionQuality = 0; 
            importer.SaveAndReimport();
        }
        
        //initial visible pixels check
        RectInt texBounds = GetTrimmedBounds(sourceTex);
        if (texBounds.height <= 0 || texBounds.width <= 0)
        {
            Debug.LogWarning($"Texture: {sourceTex} has no visible pixels. Cut aborted.");
            return;
        }
        
        List<CutPieceData> pieces = new();
        string folderPath = Path.GetDirectoryName(assetPath);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);


        for (int y = texBounds.y; y < texBounds.yMax; y += MaxRawChunkSize)
        {
            //allow it to have a height <2048px
            int rowHeight = Mathf.Min(MaxRawChunkSize, texBounds.yMax - y);

            RectInt rowArea = new RectInt(texBounds.x, y, texBounds.width, rowHeight);

            //trim the row, ignore it if empty 
            RectInt trimmedRowArea = GetTrimmedBounds(sourceTex, rowArea);
            if (trimmedRowArea.width <= 0 || trimmedRowArea.height <= 0) continue;

            for (int x = texBounds.x; x < texBounds.xMax; x += MaxRawChunkSize)
            {
                int colWidth = Mathf.Min(MaxRawChunkSize, texBounds.width - x);

                RectInt colArea = new RectInt(x, trimmedRowArea.y, colWidth, trimmedRowArea.height);

                //final trim, ignore if it results in an empty cell
                RectInt finalBounds = GetTrimmedBounds(sourceTex, colArea);
                if (finalBounds.width <= 0 || finalBounds.height <= 0) continue;

                CutPieceData piece = ProcessCutPiece(sourceTex, finalBounds, folderPath, fileName, pieces.Count);
                pieces.Add(piece);
            }
        }
        
        AssetDatabase.Refresh();
        ApplyImportSettingsToPieces(pieces);
        
        Debug.Log($"Cut {fileName} into {pieces.Count} pieces.");
        if(passToPrefabCreator) SpriteCutterPrefabCreator.BuildPrefabFromCutPieces(fileName, pieces);
        
    }

    private static CutPieceData ProcessCutPiece(Texture2D tex, RectInt bounds, string folderPath, string fileName, int index)
    {
        //adding padding so the seams are not visible in game
        int startX = Mathf.Max(0, bounds.x - ChunkPadding);
        int paddedWidth = Mathf.Min(bounds.xMax + ChunkPadding, tex.width) - startX;
        
        int startY = Mathf.Max(0, bounds.y - ChunkPadding);
        int paddedHeight = Mathf.Min(bounds.yMax + ChunkPadding, tex.height) - startY;
        
        Color[] pixels = tex.GetPixels(startX, startY, paddedWidth, paddedHeight);
        
        //make sure it allows 4x4 block compression
        int correctedWidth = (paddedWidth % 4 != 0) ? paddedWidth + 4 - paddedWidth % 4 : paddedWidth;
        int correctedHeight = (paddedHeight % 4 != 0) ? paddedHeight + 4 - paddedHeight % 4 : paddedHeight;
        
        //create new texture and save it
        Texture2D cutPieceTex = new Texture2D(correctedWidth, correctedHeight, TextureFormat.RGBA32, false);
        
        //make sure initial tex is transparent
        Color[] clearCanvas = new Color[correctedWidth * correctedHeight];
        for (int i =0; i < clearCanvas.Length; i++) clearCanvas[i] = Color.clear;
        cutPieceTex.SetPixels(clearCanvas);
        
        cutPieceTex.SetPixels(0,0, paddedWidth, paddedHeight, pixels);
        cutPieceTex.Apply();

        if (!Directory.Exists($"{folderPath}/{fileName}"))
        {
            Directory.CreateDirectory($"{folderPath}/{fileName}");
        }
        
        string pieceFilePath = $"{folderPath}/{fileName}/{fileName}_{index:D2}.png";
        File.WriteAllBytes(pieceFilePath, cutPieceTex.EncodeToPNG());
        
        
        CutPieceData cutPiece = new()
        {
            path = pieceFilePath,
            bounds = bounds,
            pivotOffset = new Vector2(startX, startY),
        };
        
        return cutPiece;
    }

    private static void ApplyImportSettingsToPieces(List<CutPieceData> pieces)
    {
        foreach (CutPieceData piece in pieces)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(piece.path);

            if (importer)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }
    }
    
    #region GetTrimmedBounds
    private static RectInt GetTrimmedBounds(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        bool foundPixel = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //find visible pixels
                if (pixels[y * width + x].a > 5)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    foundPixel = true;
                }
            }
        }
        
        //empty tex
        if(!foundPixel) return new RectInt(0,0,0,0);
        
        minX -= TrimPadding;
        minY -= TrimPadding;
        maxX += TrimPadding;
        maxY += TrimPadding;
        
        minX = Mathf.Max(minX, 0);
        maxX = Mathf.Min(maxX, width - 1);
        minY = Mathf.Max(minY, 0);
        maxY = Mathf.Min(maxY, height - 1);
        
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static RectInt GetTrimmedBounds(Texture2D tex, RectInt area)
    {
        
        
        Color[] pixels = tex.GetPixels(area.x, area.y, area.width, area.height);
        int width = area.width;
        int height = area.height;

        
        
        int minX = area.width;
        int minY = area.height;
        int maxX = -1;
        int maxY = -1;
        bool foundPixel = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //find visible pixels
                if (pixels[y * width + x].a > 0.02f)
                {
                    if(x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    foundPixel = true;
                }
            }
        }
        
        //empty tex
        if(!foundPixel) return new RectInt(0,0,0,0);

        minX += area.x;
        maxX += area.x;
        minY += area.y;
        maxY += area.y;

        minX -= TrimPadding;
        minY -= TrimPadding;
        maxX += TrimPadding;
        maxY += TrimPadding;

        minX = Mathf.Max(minX, area.x);
        maxX = Mathf.Min(maxX, area.xMax - 1);
        minY = Mathf.Max(minY, area.y);
        maxY = Mathf.Min(maxY, area.yMax - 1);
        
        
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
    #endregion
}

public sealed class CutPieceData
{
    public string path;
    public RectInt bounds;
    public Vector2 pivotOffset;
        
}