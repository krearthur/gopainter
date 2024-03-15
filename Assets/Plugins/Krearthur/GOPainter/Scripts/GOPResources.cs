
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GOPResources
{
    public AudioClip placeSound { get; private set; }
    public Texture2D cursor { get; private set; }
    public Texture2D cursorLine { get; private set; }
    public Texture2D cursorLineDelete { get; private set; }
    public Texture2D cursorRect { get; private set; }
    public Texture2D cursorRectFill { get; private set; }
    public Texture2D cursorRectDelete { get; private set; }
    public Texture2D cursorCircle { get; private set; }
    public Texture2D cursorCircleFill { get; private set; }
    public Texture2D cursorCircleDelete { get; private set; }
    public Texture2D cursorDelete { get; private set; }
    public Texture2D cursorPick { get; private set; }
    public Material canvasMaterial { get; private set; }
    public Material brushMaterial { get; private set; }
    public GameObject brushPrefab { get; private set; }

    public void LoadResources()
    {
        string basePath = "Assets/Plugins/Krearthur/GOPainter/Resources/";

        cursor = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint.png");
        cursorDelete = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_delete.png");
        cursorLine = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_line.png");
        cursorLineDelete = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_line_delete.png");
        cursorRect = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_rect.png");
        cursorRectFill = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_rect_fill.png");
        cursorRectDelete = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_rect_delete.png");
        cursorCircle = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_circle.png");
        cursorCircleFill = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_circle_fill.png");
        cursorCircleDelete = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_circle_delete.png");
        cursorPick = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "paint_pick.png");

        canvasMaterial = AssetDatabase.LoadAssetAtPath<Material>(basePath + "PaintCanvasMat.mat");
        brushMaterial = AssetDatabase.LoadAssetAtPath<Material>(basePath + "PaintBrushMat.mat");

        brushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "BrushRadius.prefab");
        placeSound = AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "blop.ogg");
    }
}