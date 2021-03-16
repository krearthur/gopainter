
using UnityEngine;
using System;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
#endif
using System.Collections.Generic;
using Krearthur.Utils;

namespace Krearthur.GOP
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor Tool that enables to place objects in the scene in a convienient and handy way. I tried to design it like a paint tool.
    /// </summary>
    [ExecuteInEditMode]
    public class GOPainter : MonoBehaviour
    {
        public enum OperationMode
        {
            Freehand,
            Line,
            Rect,
            Circle,
            Scale
        }

        public enum SnappingBase
        {
            Pivot,
            ColliderBounds,
            MeshBounds
        }

        public enum CanvasAxis
        {
            X,
            Y,
            Z
        }

        public enum AlignAxis
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
            PositiveZ = 4,
            NegativeZ = 5
        }

        public enum AreaPosition
        {
            WorldOrigin,
            HalfOfAreaSize,
            TerrainPosition
        }


        // -- Public Settings --
        [Tooltip("When in scene view, you can also use TAB to quickly toggle on/off")]
        public bool active;
        protected bool wasActive;

        [Tooltip("Drawing mode. Hotkeys for switching: Q=Freehand, W=Line, E=Rect, R=Circle, S=Scale. Make sure that the SceneView is focused (by clicking in it) first and that GO Painter is active.")]
        public OperationMode mode = OperationMode.Freehand;

        [Tooltip("Switch canvas axis. Default is the Y axis which could be seen as painting on the 'ground'. Hotkey: Space")]
        public CanvasAxis canvasAxis = CanvasAxis.Y;

        [Tooltip("Put in any game object that you want to paint with. You can then instantly switch with the numbers 1 - 0 to that object and draw with it. The numbers on a keyboard are limited to 10 so you shouldn't put more elements in. Tip: Since you can also pick any object with right click in the scene view, you can just drag a Prefab from the project view into scene, right click on it and then paint with it.")]
        public GameObject[] paintObjects = new GameObject[10];

        [Range(0, 100), Tooltip("Paint objects randomly inside brush radius. Set to 0 to just paint exactly at cursor position.")]
        public float brushRadius = 0;

        [Tooltip("Min distance of paint stroke to paint new objects [Freehand mode].")]
        public float density = 1f;

        [Tooltip("Hold the alt key and then scroll with the mouse to rotate the holding object around the current canvasAxis <rotationStep> degrees.")]
        public int rotationStep = 45;

        [Tooltip("Size of the painting canvas. The canvas follows the mouse position.")]
        public int paintCanvasSize = 500;

        [Tooltip("With this checked the Paint canvas cant get below 0")]
        public bool preventCanvasBelow0 = true;

        [Tooltip("Snap objects to grid. Hotkey: G")]
        public bool snapToGrid = true;

        [Tooltip("Determines wether the pivot (origin), the collider bounds or the mesh bounds should be used for snapping. If collider is selected but the object doesn't have a collider, the mesh bounds are used instead.")]
        public SnappingBase snappingBase = SnappingBase.ColliderBounds;

        [Tooltip("When grid snapping is enabled, painted objects snap to the nearest grid position. Grid Size defines how big a grid cell is.")]
        public Vector3 gridSize = Vector3.one;

        [Tooltip("Add an extra position offset to painted objects.")]
        public Vector3 placementOffset = Vector3.zero;

        [Tooltip("Calculate grid size based on object dimensions. Hotkey: D")]
        public bool dynamicGrid = false;
        protected bool wasDynamicGrid;

        [Tooltip("When this is set and grid snapping is active you can paint objects even when they are colliding with others.")]
        public bool paintIgnoreColliding = false;

        [Tooltip("Painted GameObjects will be rotated such that they align with the surface normal they are painted on. Hotkey: A")]
        public bool alignWithSurface = false;
        [Tooltip("Paint only on painting canvas or terrain. This is useful when painting many objects at once and you dont want them to be placed on each other.")]
        public bool paintOnlyOnGround = true;
        protected bool wasAlignWithSurface;

        [Tooltip("Painted GameObjects will be rotated such that they align with the drawn line or circle. Hotkey: A")]
        public bool alignWithStroke = false;
        protected bool wasAlignWithStroke;

        [Tooltip("When align with surface is on, switch alignemnt axis of painted object. Hotkey: X")]
        [HideInInspector] public AlignAxis alignAxis = AlignAxis.PositiveY;

        [Tooltip("When checked and in Rectangle Mode, draws a filled rectangle of objects. Hotkey: E")]
        public bool fillRect = true;

        [HideInInspector] public Vector3 placeOffset = new Vector3(0.5f, 0, 0.5f);

        [Tooltip("Relative space between objects when using Line, Rectangle or Circle Tool")]
        [Range(0.2f, 10f)]
        public float padding = 1;

        [Tooltip("When in scale mode (S) constrain the scale to min (x) and max (y).")]
        public Vector2 scaleMinMax = new Vector2(0.1f, 10);

        [Tooltip("When this is checked, it ignores the prefabs scale setting and takes the scale of the previous created.")]
        public bool sameScaleForObjects = true;
        protected float minPadding = 0.25f;
        protected float maxPadding = 10;

        [Tooltip("The preview object (the object that follows the mouse) will temporarily set to this layer until it is actually placed in the scene. This is necessary for raycasting stuff to work. You can type in the layer name, but the layer must exist and it is also not advisable to use the 'Default' layer.")]
        public string editLayer = "Ignore Raycast";

        [Tooltip("The paint-canvas (just a plane where objects are painted on) will have this layer. Tip: If you want to paint on terrain, make it the same layer as this.")]
        public string canvasLayer = "Water";

        [Tooltip("Should the Paint-Canvas be hidden while drawing? Hint: If the canvas is visible it also works as a 'layer-filter', which means objects underneath the Paint-Canvas will not be picked or deleted by painting operations.")]
        public bool hideCanvas = false;

        [Tooltip("Every object that is painted into scene will be a child of this root group object.")]
        public string rootGroupObjectName = "[LevelTiles]";

        [Tooltip("GO Painter will create a temporary canvas object with this name. The paint-canvas is just a simple plane that the mouse raycasts will be checked against to be able to place objects on. This enables you to paint objects in the scene even if there is no terrain. And you can change the canvas position where you paint objects with SHIFT+SCROLL.")]
        public string paintCanvasName = "[GOP-Canvas]";

        [HideInInspector] public float sampleScreenDistance = 5;

        [Tooltip("If enabled it plays some juicy blop sounds when painting, deleting, rotating stuff to make level building more fun (hopefully).")]
        public bool enableSounds = true;

        [Tooltip("When enabled, shows the info bar at the bottom of the scene view")]
        public bool showInfoBar = true;

        [Tooltip("When enabled, sets a terrains layer automatically to the value of 'Canvas Layer' property.")]
        public bool autoSetTerrainLayer = true;

        // -- Logic --
        protected float canvasPositionX = 0;
        protected float canvasPositionY = 0;
        protected float canvasPositionZ = 0;
        protected int prefabId = 0;
        public int PrefabID { get { return prefabId; } }
        protected CanvasAxis axisBefore;
        protected SnappingBase snappingBaseBefore;
        protected Vector3 startingEulerAngles;
        protected Vector3 currentRotationDelta = Vector3.zero;
        protected Vector3 currentScale = Vector3.one;
        protected bool SnapToGrid { get { return (snapToGrid); } }
        protected bool wasGrid;
        protected bool camAxisPolarityInverted;
        protected bool wasCamAxisPolarityInverted;
        protected List<Transform> objectGroups;
        protected int originalLayer;
        protected string objectGroupPrefix = "Group of: ";
        protected string holdingPrefix = "(Holding) ";
        protected string pickedPrefix = "(Picked)";
        public string PickedPrefix { get { return pickedPrefix; } }

        protected string extraInfoText = "";
        [NonSerialized] private string controlInfo = "L-Click: Paint | Ctrl+Click: Delete | R-Click: Pick | Alt+Scroll: Rotate | Shift+Scroll: Plane Up/Down | 1-9: Switch Object | Tab: De/Activate";
        [NonSerialized] private string circleInfo = "L-Click: Paint | Ctrl+Click: Delete | R-Click: Pick | Alt+Scroll: Rotate | Shift: Circle! | Shift+Scroll: Change Degrees | 1-9: Switch Object | Tab: De/Activate";
        [NonSerialized] private string scaleInfo = "L-Click / S: Confirm | R-Click / Esc: Cancel | Move Right: Scale up | Move Left: Scale down | 1-9: Set scale = 1..9";

        // ------- Events -------
        public event Action OnGOPainterActivated;
        public event Action OnGOPainterDeactivated;
        /// <summary>
        /// Called when the holded object's position is changed.
        /// </summary>
        public event Action<GameObject, Vector3, bool> OnHoldingObjectPositionChanged;
        /// <summary>
        /// Called when the holded object is painted into scene.
        /// GameObject: The object that is painted.
        /// Vector3: The current painting axis.
        /// Bool: Snap to grid
        /// </summary>
        public event Action<GameObject, Vector3, bool> OnObjectPainted;
        /// <summary>
        /// Called when objects were mass painted into scene, via Line, Rect or Circle Tool
        /// </summary>
        public event Action<GameObject[], Vector3, bool> OnObjectMassPainted;
        public event Action<GameObject[], Vector3, bool> OnObjectMassPaintedLate;
        public event Action<GameObject> OnObjectPicked;
        /// <summary>
        /// Called when objects were mass deleted from scene (in fact they just get deactivated), via Line, Rect or Circle Tool
        /// </summary>
        public event Action<GameObject[], Vector3, bool> OnObjectMassDeleted;
        /// <summary>
        /// Called when an object is deleted from scene (in fact it gets deactivated).
        /// </summary>
        public event Action<GameObject> OnObjectDeleted;
        public event Action<List<GameObject>> OnBeforeUndoAdded;
        public event Action<List<GameObject>> OnUndoRemoved;

        // -- Resources --
        protected AudioClip placeSound;
        protected Texture2D cursor;
        protected Texture2D cursorLine;
        protected Texture2D cursorLineDelete;
        protected Texture2D cursorRect;
        protected Texture2D cursorRectFill;
        protected Texture2D cursorRectDelete;
        protected Texture2D cursorCircle;
        protected Texture2D cursorCircleFill;
        protected Texture2D cursorCircleDelete;
        protected Texture2D cursorDelete;
        protected Texture2D cursorPick;
        protected Material canvasMaterial;
        protected Material brushMaterial;

        // -- State --
        protected bool alt;
        protected bool control;
        protected bool shift;
        protected bool isDragging = false;
        protected bool leftDown;
        protected bool rightDown;
        protected bool planeChanged;
        protected bool hasMouseMoved;
        protected OperationMode previousMode;
        protected Event lastEvent;

        protected Vector3 lastObjectPos;
        protected Vector2 lastMouseClickPos;
        protected Vector2 mousePos;
        protected Vector2 lastMousePos;
        protected Vector2 sampledMousePos;
        protected Vector2 lastSampledMousePos;

        protected Vector3 mouseWorldPos;
        protected Vector3 lastMouseWorldPos;
        protected Vector3 sampledMouseWorldPos;
        protected Vector3 lastSampledMouseWorldPos;
        protected bool placeSampleAllowed;

        protected Vector3 lastMouseClickWorldPos;
        protected Vector3 lastGridPos;
        protected Quaternion lastPaintRotation = Quaternion.identity;
        protected GameObject lastAddedObject;
        protected Vector3 lastPaintedObjectPos;
        protected GameObject[] lastMassSelection;
        protected List<GameObject> paintCollection;
        protected Tool lastTool;
        protected bool cursorHasNoSurface;

        [HideInInspector] public int MASK_ONLY_OBJECTS;
        [HideInInspector] public int MASK_CANVAS_ONLY;
        [HideInInspector] public int MASK_EDITLAYER_ONLY;

        // -- Components --
        protected LineFactory lineFactory;
        protected RectFactory rectFactory;
        protected CircleFactory circleFactory;
        protected ObjectFactory objectFactory;
        protected new AudioSource audio;
        [SerializeField] protected Stack<PaintAction> history;

        // -- References --
        protected SceneView scene;
        [HideInInspector] public GameObject paintCanvas;
        protected GameObject groupRoot;
        protected GameObject holdingObject;
        protected GameObject selectedPrefab;
        protected GameObject parkedPicked;
        protected GameObject brushPrefab;
        protected GameObject circleBrush;
        protected Color brushColorBefore;
        protected Terrain lastFoundTerrain;
        protected GameObject[] IgnoredObjectsToPick;

        public const float MAX_RAY_DIST = 5000f;

        private void Update()
        {
            if (!Application.isPlaying && active)
            {
                if (paintCanvas != null && planeChanged)
                {
                    UpdatePaintCanvasSize();
                    planeChanged = false;
                }
                if (wasActive == false)
                {
                    OnActivation();
                }
            }
            else if (!active && wasActive && GameObject.FindObjectOfType<PaintCanvas>() != null)
            {
                OnDeactivation();
            }
            wasActive = active;
        }

        protected void UpdatePaintCanvasSize()
        {
            if (canvasAxis == CanvasAxis.Y) // Ground Plane
            {
                paintCanvas.transform.localScale = new Vector3(paintCanvasSize, 0.01f, paintCanvasSize);
            }
            else if (canvasAxis == CanvasAxis.X) // Left-Right Wall
            {
                paintCanvas.transform.localScale = new Vector3(0.01f, paintCanvasSize, paintCanvasSize);
            }
            else if (canvasAxis == CanvasAxis.Z) // Forward-Backward Wall
            {
                paintCanvas.transform.localScale = new Vector3(paintCanvasSize, paintCanvasSize, 0.01f);
            }
            planeChanged = true;
            paintCanvas.GetComponent<Renderer>().enabled = true;
            UpdatePaintCanvasPosition();
        }

        protected void UpdatePaintCanvasPosition()
        {
            if (canvasAxis == CanvasAxis.X)
            {
                paintCanvas.transform.position = new Vector3(
                        canvasPositionX,
                        mouseWorldPos.y,
                        mouseWorldPos.z);
            }
            else if(canvasAxis == CanvasAxis.Y)
            {
                paintCanvas.transform.position = new Vector3(
                        mouseWorldPos.x,
                        canvasPositionY,
                        mouseWorldPos.z);
            }
            else if (canvasAxis == CanvasAxis.Z)
            {
                paintCanvas.transform.position = new Vector3(
                        mouseWorldPos.x,
                        mouseWorldPos.y,
                        canvasPositionZ);
            }
            canvasPositionX = paintCanvas.transform.position.x;
            canvasPositionY = paintCanvas.transform.position.y;
            canvasPositionZ = paintCanvas.transform.position.z;
        }

        protected void OnEnable()
        {
            if (!Application.isPlaying)
            {
                if (history == null) history = new Stack<PaintAction>();
                active = false;
                InitDrawModes();
                //CleanObjectGroups();
                SceneView.duringSceneGui += OnScene;
                LoadAssets();
            }
            else
            {
                OnDeactivation();
                Destroy(gameObject);
            }
        }

        protected void LoadAssets()
        {
            string basePath = "Assets/Krearthur/GOPainter/Resources/";

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

        protected void OnDisable()
        {
            if (!Application.isPlaying)
            {
                SceneView.duringSceneGui -= OnScene;
            }

            OnDeactivation();
        }

        protected void OnActivation()
        {
            if (!Application.isEditor)
            {
                Destroy(this);
                return;
            }
            if (history == null)
            {
                history = new Stack<PaintAction>();
            }

            MASK_CANVAS_ONLY = 1 << LayerMask.NameToLayer(canvasLayer);
            MASK_EDITLAYER_ONLY = 1 << LayerMask.NameToLayer(editLayer);
            MASK_ONLY_OBJECTS = ~(MASK_CANVAS_ONLY | MASK_EDITLAYER_ONLY);

            CheckForTerrains();

            InitDrawModes();
            paintCollection = new List<GameObject>();

            lastTool = Tools.current;
            Tools.current = Tool.None;
            transform.position = new Vector3(10000, 10000, 10000);
            Cursor.SetCursor(cursor, new Vector2(0, 31), CursorMode.Auto);
            currentRotationDelta = Vector3.zero;
            groupRoot = GameObject.Find(rootGroupObjectName);
            if (groupRoot == null)
            {
                groupRoot = new GameObject();
                groupRoot.name = rootGroupObjectName;
            }

            CleanObjectGroups(true);

            if (!TryGetComponent(out AudioSource audioSource))
            {
                audio = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                audio = audioSource;
            }

            paintCanvas = GameObject.Find(paintCanvasName);
            if (paintCanvas == null)
            {
                paintCanvas = GameObject.CreatePrimitive(PrimitiveType.Cube);

                paintCanvas.GetComponent<Renderer>().sharedMaterial = canvasMaterial;
                paintCanvas.GetComponent<Renderer>().enabled = !hideCanvas;
                paintCanvas.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                paintCanvas.AddComponent<PaintCanvas>();
                paintCanvas.hideFlags = HideFlags.HideInHierarchy;
                paintCanvas.name = paintCanvasName;
                paintCanvas.layer = LayerMask.NameToLayer(canvasLayer);
                paintCanvas.transform.SetParent(groupRoot.transform, true);    
            }
            UpdatePaintCanvasSize();

            IgnoredObjectsToPick = new GameObject[] { paintCanvas };

            parkedPicked = null;
            selectedPrefab = null;
            if (prefabId < 0) prefabId = 0;

            currentScale = paintObjects[prefabId].transform.localScale;
            SwitchPrefab(prefabId);

            Selection.activeGameObject = this.gameObject;
            ActiveEditorTracker.sharedTracker.isLocked = true;

            if (SceneView.sceneViews.Count > 0)
            {
                ((SceneView)SceneView.sceneViews[0]).Focus();
            }

            SetupOtherComponents(true);

            OnGOPainterActivated?.Invoke();
            active = wasActive = true;
        }

        protected void CheckForTerrains()
        {
            // Adjust level size according to found terrins
            foreach(Terrain t in FindObjectsOfType<Terrain>())
            {
                lastFoundTerrain = t;

                if (autoSetTerrainLayer)
                {
                    t.gameObject.layer = LayerMask.NameToLayer(canvasLayer);
                    print("GOPainter: Found terrain '" + t.name + "' and changed its layer to GOPainters CanvasLayer: '" + canvasLayer + "' to be able to paint on it. You can disable this automatic behaviour by unchecking the 'Auto Set Terrain Layer' property in GOPainter.");
                }
            }

        }

        protected void SetupOtherComponents(bool isActivation)
        {
            foreach (GOComponent comp in GetComponents<GOComponent>())
            {
                if (isActivation)
                {
                    comp.Register();
                }
                else
                {
                    comp.DeRegister();
                }
            }

            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene != null && activeScene.isLoaded)
            {
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    Brush[] brushes = rootObjects[i].GetComponentsInChildren<Brush>(true);
                    for (int ib = 0; ib < brushes.Length; ib++)
                    {
                        if (ib == 0)
                            circleBrush = brushes[ib].gameObject;
                        if (ib > 0)
                        {
                            DestroyImmediate(brushes[ib].gameObject);
                        }
                    }
                }
            }

            if (isActivation)
            {
                if (circleBrush == null)
                {
                    CreateCircleBrush();
                }
            }
            else
            {
                if (circleBrush != null)
                {
                    DestroyImmediate(circleBrush);
                }
            }
        }

        protected void CreateCircleBrush()
        {
            circleBrush = (GameObject)PrefabUtility.InstantiatePrefab(brushPrefab);
            circleBrush.transform.localScale = new Vector3(0, 1f, 0);
            circleBrush.GetComponent<Collider>().enabled = false;
            circleBrush.layer = LayerMask.NameToLayer(editLayer);
            circleBrush.GetComponent<Renderer>().sharedMaterial = brushMaterial;
            circleBrush.name = "[Brush]";
            //circleBrush.hideFlags = HideFlags.HideInHierarchy;
            circleBrush.SetActive(false);
        }

        protected void OnDeactivation()
        {
            if (holdingObject != null)
            {
                DestroyImmediate(holdingObject);
            }

            if (parkedPicked != null)
            {
                DestroyImmediate(parkedPicked);
            }

            Selection.activeGameObject = this.gameObject;

            objectFactory?.Release();
            isDragging = false;

            Tools.current = Tool.Move;
            CleanObjectGroups(false);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (paintCanvas == null)
            {
                PaintCanvas test = GameObject.FindObjectOfType<PaintCanvas>();
                if (test != null) paintCanvas = test.gameObject;
            }

            if (Application.isPlaying)
            {
                Destroy(paintCanvas);
            }
            else
            {
                DestroyImmediate(paintCanvas);
            }

            SetupOtherComponents(false);

            active = false;
            ActiveEditorTracker.sharedTracker.isLocked = false;
            OnGOPainterDeactivated?.Invoke();
        }


        void OnScene(SceneView scene)
        {
            if (this == null)
            {
                print("deleted GOPainter, removing listener...");
                SceneView.duringSceneGui -= OnScene;
            }
            this.scene = scene;
            Event e = Event.current;

            lastEvent = e;
            alt = e.alt;
            control = e.control;
            shift = e.shift;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab)
            {
                if (!active)
                {
                    OnActivation();
                }
                else
                {
                    OnDeactivation();
                }
            }

            if (!active) return;

            if (paintCanvas == null)
            {
                OnDeactivation();
                return;
            }

            camAxisPolarityInverted = GetSceneCamAxisPolarity() > 0;
            if (wasCamAxisPolarityInverted != camAxisPolarityInverted)
            {
                UpdateOffsetForObject(holdingObject);
            }
            wasCamAxisPolarityInverted = camAxisPolarityInverted;

            // --- Checking if user changed settings from inspector ---
            if (wasGrid != SnapToGrid)
            {
                UpdateOffsetForObject(holdingObject);
            }
            wasGrid = SnapToGrid;

            if (wasAlignWithStroke != alignWithStroke)
            {
                UpdateOffsetForObject(holdingObject);
            }
            wasAlignWithStroke = alignWithStroke;

            if (wasAlignWithSurface != alignWithSurface)
            {
                UpdateOffsetForObject(holdingObject);
            }
            wasAlignWithSurface = alignWithSurface;

            if (wasDynamicGrid != dynamicGrid)
            {
                UpdateOffsetForObject(holdingObject);
            }
            wasDynamicGrid = dynamicGrid;

            if (axisBefore != canvasAxis)
            {
                UpdateOffsetForObject(holdingObject);
            }
            axisBefore = canvasAxis;

            // --- Storing some input events in member variables ---
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    leftDown = true;
                }
                else if (e.button == 1)
                {
                    rightDown = true;
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 0)
                {
                    leftDown = false;
                }
                else if (e.button == 1)
                {
                    rightDown = false;
                }
            }
            else if (e.type == EventType.MouseMove)
            {
                hasMouseMoved = true;
            }

            // Disable default behaviour of left click+drag creating a rectangle to select
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Update mouse screen positions
            lastMousePos = mousePos;
            mousePos = GetMousePos(e);
            // Update mouse world positions (based on canvas)
            lastMouseWorldPos = mouseWorldPos;
            mouseWorldPos = GetCursorRayPosCanvas(mousePos);
            if (circleBrush != null && mode == OperationMode.Freehand && (!alt))
            {
                circleBrush.transform.position = mouseWorldPos;
                if (canvasAxis == CanvasAxis.X) circleBrush.transform.eulerAngles = new Vector3(0, 0, 90);
                else if (canvasAxis == CanvasAxis.Y) circleBrush.transform.eulerAngles = new Vector3(0, 0, 0);
                else if (canvasAxis == CanvasAxis.Z) circleBrush.transform.eulerAngles = new Vector3(90, 0, 0);
            }

            // If mouse screen position difference is bigger than sample rate => take a new sample!
            if (Vector2.SqrMagnitude(lastSampledMousePos - mousePos) >= (sampleScreenDistance * sampleScreenDistance))
            {
                lastSampledMousePos = sampledMousePos;
                sampledMousePos = mousePos;
            }

            // If mouse world position difference is bigger than sample rate => take a new sample!
            if (Vector3.SqrMagnitude(lastSampledMouseWorldPos - mouseWorldPos) >= (density * density))
            {
                lastSampledMouseWorldPos = sampledMouseWorldPos;
                sampledMouseWorldPos = mouseWorldPos;

                placeSampleAllowed = true;
            }

            if (selectedPrefab == null)
            {
                selectedPrefab = paintObjects[prefabId];
                if (selectedPrefab == null)
                {
                    prefabId = 0;
                    selectedPrefab = paintObjects[prefabId];
                }
            }

            if (selectedPrefab == null)
            {
                extraInfoText = "Please put at least a GameObject in Element 0 of the 'Paint Objects' array.";

            }
            else
            {
                if (cursorHasNoSurface) extraInfoText = "--- Cursor is outside any surface to paint on ---";
                else if (mode == OperationMode.Circle) extraInfoText = circleInfo;
                else if (mode == OperationMode.Scale) extraInfoText = scaleInfo;
                else extraInfoText = controlInfo;

                if (IsCursorInSceneView(e))
                {
                    if (!e.control && Selection.activeGameObject != holdingObject && !rightDown)
                    {
                        Selection.activeGameObject = holdingObject;
                    }
                    // Select Object where mouse hovers over
                    else if (e.control && mode == OperationMode.Freehand || rightDown)
                    {
                        GameObject selected = RaycastFirst(e);
                        Selection.activeGameObject = selected;
                    }

                    // --- MAIN METHOD --- 
                    HandleSceneEvents(e);

                    if (e.type == EventType.MouseUp)
                    {
                        hasMouseMoved = false;
                    }
                }
                else if (holdingObject != null)
                {
                    if (e.type == EventType.Layout || e.type == EventType.ValidateCommand) return;

                    DestroyImmediate(holdingObject);
                    DestroyImmediate(circleBrush);
                }

            }

            DrawGUI();
            SetCustomCursor(e);
            // Workaround for continous update calls in Editor mode
            EditorUtility.SetDirty(gameObject);
            SceneView.RepaintAll();

        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnScene;
            OnDeactivation();
        }

        protected void InitDrawModes()
        {
            if (lineFactory == null)
            {
                lineFactory = GetComponent<LineFactory>();
                if (lineFactory == null)
                {
                    lineFactory = gameObject.AddComponent<LineFactory>();
                }
            }
            if (rectFactory == null)
            {
                rectFactory = GetComponent<RectFactory>();
                if (rectFactory == null)
                {
                    rectFactory = gameObject.AddComponent<RectFactory>();
                }
            }
            if (circleFactory == null)
            {
                circleFactory = GetComponent<CircleFactory>();
                if (circleFactory == null)
                {
                    circleFactory = gameObject.AddComponent<CircleFactory>();
                }
            }
            if (objectFactory == null)
            {
                objectFactory = GetComponent<ObjectFactory>();
                if (objectFactory == null)
                {
                    objectFactory = gameObject.AddComponent<ObjectFactory>();
                }
            }
            objectFactory.createUnderGroup = true;
            objectFactory.relativePos = false;

            lineFactory.segmentFactory = objectFactory;
            lineFactory.produceOnUpdate = false;
            lineFactory.startOffset = Vector3.zero;
            lineFactory.targetOffset = Vector3.zero;
            lineFactory.trimStart = 0;
            lineFactory.trimEnd = 1;
            lineFactory.padding = padding;
            lineFactory.calculateNumberByPaddingAndDistance = true;

            rectFactory.segmentFactory = objectFactory;
            rectFactory.produceOnUpdate = false;
            rectFactory.startOffset = Vector3.zero;
            rectFactory.targetOffset = Vector3.zero;
            rectFactory.padding = padding;
            rectFactory.calculateNumberByPaddingAndDistance = true;

            circleFactory.segmentFactory = objectFactory;
            circleFactory.produceOnUpdate = false;
            circleFactory.startOffset = Vector3.zero;
            circleFactory.targetOffset = Vector3.zero;
            circleFactory.padding = padding;
            circleFactory.calculateNumberByPaddingAndDistance = true;

        }

        public GameObject GetRootGroup()
        {
            GameObject gRoot = GameObject.Find(rootGroupObjectName);
            if (gRoot == null)
            {
                Debug.LogError("root group object " + rootGroupObjectName + " not found");
                return null;
            }
            return gRoot;
        }

        protected void CleanObjectGroups(bool isOnActivation)
        {
            List<Transform> deleteMe = new List<Transform>();

            if (groupRoot == null)
            {
                return;
            }

            objectGroups?.Clear();
            if (isOnActivation && objectGroups == null)
            {
                objectGroups = new List<Transform>();
            }

            // Step 1: Go through all objects inside object-groups and delete the inactive ones
            for (int i = 0; i < groupRoot.transform.childCount; i++)
            {
                Transform child = groupRoot.transform.GetChild(i);
                if (child.name.StartsWith(objectGroupPrefix))
                {
                    for (int ii = 0; ii < child.transform.childCount; ii++)
                    {
                        Transform subChild = child.transform.GetChild(ii);
                        if (subChild.gameObject.activeSelf == false)
                        {
                            deleteMe.Add(subChild);
                        }
                    }
                }
            }
            foreach (Transform t in deleteMe)
            {
                if (Application.isPlaying)
                {
                    Destroy(t.gameObject);
                }
                else
                {
                    DestroyImmediate(t.gameObject);
                }
            }
            deleteMe.Clear();

            // Step 2: Look into every objectGroup and delete the empty objectGroups
            for (int i = 0; i < groupRoot.transform.childCount; i++)
            {
                Transform child = groupRoot.transform.GetChild(i);
                if (child.name.StartsWith(holdingPrefix) || child.name.StartsWith(pickedPrefix)
                    || (child.name.StartsWith(objectGroupPrefix) && child.childCount == 0))
                {
                    deleteMe.Add(child);
                }
                else if (isOnActivation)
                {
                    objectGroups.Add(child);
                }

            }
            foreach (Transform t in deleteMe)
            {
                if (Application.isPlaying)
                {
                    Destroy(t.gameObject);
                }
                else
                {
                    DestroyImmediate(t.gameObject);
                }
            }

        }

        protected void DrawGUI()
        {
            if (!showInfoBar) return;

            Handles.BeginGUI();
            float sceneWidth = scene.position.width;
            float sceneHeight = scene.position.height - 20;

            GUI.Box(new Rect(0, sceneHeight - 28, sceneWidth, 30), "");

            GUI.skin.label.fontSize = 15;
            GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;

            GUI.Label(new Rect(5, sceneHeight - 25, 140, 30), "[GOPainter]");

            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;


            if (selectedPrefab == null)
            {
                GUI.skin.label.fontSize = 14;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                style.fontSize = 15;
                style.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(110, sceneHeight - 23, sceneWidth, 30), extraInfoText, style);
            }
            else
            {
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.fontStyle = FontStyle.Normal;
                if (cursorHasNoSurface)
                {
                    GUI.skin.label.fontSize = 15;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                }
                GUI.Label(new Rect(110, sceneHeight - 23, sceneWidth, 30), extraInfoText);
            }

            if (alignWithSurface || alignWithStroke)
            {
                GUI.Box(new Rect(0, sceneHeight - 57, 140, 30), "");
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.fontStyle = FontStyle.Normal;
                if (alignWithStroke)
                {
                    GUI.Label(new Rect(5, sceneHeight - 50, 340, 30), "Align with Stroke");
                }
                else
                {
                    GUI.Label(new Rect(5, sceneHeight - 50, 340, 30), "Align with Surface");
                }
            }

            if (shift)
            {
                float posX = sceneWidth / 2;
                float posY = sceneHeight - 60;
                if (mode != OperationMode.Circle)
                {
                    GUI.Box(new Rect(posX - 95, posY - 20, 180, 45), "");
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.black;
                    style.fontSize = 30;
                    style.fontStyle = FontStyle.Bold;
                    string level = canvasAxis == CanvasAxis.X ? "X: " + canvasPositionX : canvasAxis == CanvasAxis.Y ? "Y: " + canvasPositionY : "Z: " + canvasPositionZ;
                    GUI.Label(new Rect(posX - 90, posY - 15, sceneWidth, 30), level, style);

                }
                else
                {
                    GUI.Box(new Rect(posX - 95, posY - 20, 250, 45), "");
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.black;
                    style.fontSize = 30;
                    style.fontStyle = FontStyle.Bold;
                    string text = "Degrees: " + circleFactory.degrees;
                    GUI.Label(new Rect(posX - 90, posY - 15, sceneWidth, 30), text, style);

                }

            }

            Handles.EndGUI();
        }

        // ==== Main Method =======
        protected void HandleSceneEvents(Event e)
        {
            // MOUSE DRAG
            if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt && !cursorHasNoSurface)
            {
                UpdatePaintCanvasPosition();
                if (IsCursorOnPlane(e))
                {
                    isDragging = true;
                    Vector3 start = lastMouseClickWorldPos;
                    Vector3 end = mouseWorldPos;

                    objectFactory.positionOffset = placeOffset;
                    objectFactory.createUnderGroup = true;
                    objectFactory.objectScale = currentScale;
                    if (holdingObject != null)
                    {
                        objectFactory.group = holdingObject.transform.parent;
                        objectFactory.upAxis = GetCanvasAxis();
                        Transform rotationBody = holdingObject.transform.Find("Center");
                        if (rotationBody != null) objectFactory.objectEuler = rotationBody.eulerAngles;
                        else objectFactory.objectEuler = holdingObject.transform.eulerAngles;
                    }
                    switch (mode)
                    {
                        case OperationMode.Freehand:
                            {
                                HandleFreehandDrag(e);
                                break;
                            }
                        case OperationMode.Line:
                            {
                                HandleMouseDragLine(start, end);
                                break;
                            }
                        case OperationMode.Rect:
                            {
                                HandleMouseDragRect(start, end);
                                break;
                            }
                        case OperationMode.Circle:
                            {
                                HandleMouseDragCircle(start, end);
                                break;
                            }
                    }
                    e.Use();
                }
            }
            
            // Left Mouse UP 
            else if (e.type == EventType.MouseUp && e.button == 0 && !cursorHasNoSurface)
            {
                if (isDragging)
                {
                    isDragging = false;
                    if (mode != OperationMode.Freehand)
                    {
                        HandleReleaseMouseDragMassTools();
                    }
                }

                if (mode == OperationMode.Freehand && paintCollection.Count > 0)
                {
                    PaintAction action = new PaintAction(paintCollection, control ? PaintAction.ActionType.Removed : PaintAction.ActionType.Added);
                    paintCollection.Clear();
                    history.Push(action);
                }

            }

            // LEFT MOUSE Down
            else if (e.type == EventType.MouseDown && e.button == 0 && !cursorHasNoSurface)
            {
                lastMouseClickPos = GetMousePos(e);
                lastMouseClickWorldPos = GetCursorRayPosCanvas(lastMouseClickPos);

                if (mode == OperationMode.Freehand)
                {
                    if (e.control)
                    {
                        DestroyObjectOnCursor(e);
                    }
                    else if (!alt)
                    {
                        ApplySelectedObject();
                    }
                }
                else if (mode == OperationMode.Scale)
                {
                    SubmitScaleMode();
                }

            }

            // Right MOUSE CLICK
            else if (e.type == EventType.MouseUp && e.button == 1)
            {
                if (isDragging)
                {
                    // cancel dragging operation
                    CancelDragging();
                }
                else
                {
                    if (mode == OperationMode.Scale) CancelScaleMode(true);
                    else PickObjectOnCursor(e);
                }

            }

            // MOUSE MOVE
            else if (e.type == EventType.MouseMove && !isDragging)
            {
                UpdatePaintCanvasPosition();
                cursorHasNoSurface = false;
                RaycastHit hit;
                if (RaycastCursor(e, out hit))
                {
                    if (holdingObject == null)
                    {
                        holdingObject = CreateNew(lastObjectPos);
                        UpdateOffsetForObject(holdingObject);
                    }
                    if (circleBrush == null)
                    {
                        CreateCircleBrush();
                        circleBrush.SetActive(true);
                    }


                    Vector3 pos = hit.point;
                    if (mode != OperationMode.Freehand)
                    {
                        pos = GetCursorRayPosPlane(e);
                    }
                    if (mode != OperationMode.Scale)
                    {
                        SetObjectPosition(holdingObject, pos);
                    }
                    else
                    {
                        if (e.delta.x > 0 && holdingObject.transform.localScale.magnitude < scaleMinMax.y)
                        {
                            holdingObject.transform.localScale *= 1.02f;
                        }
                        else if (e.delta.x < 0 && holdingObject.transform.localScale.magnitude > scaleMinMax.x)
                        {
                            holdingObject.transform.localScale *= 0.98f;
                        }
                        UpdateOffsetForObject(holdingObject);
                    }
                }
                else
                {
                    cursorHasNoSurface = true;
                }

            }

            // MOUSE SCROLL 
            else if (e.type == EventType.ScrollWheel && !shift)
            {
                HandleMouseScroll(e);
            }

            // Key Z History Undo or toggle transparency of plane
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Z)
            {
                if (e.control)
                {
                    if (mode == OperationMode.Scale)
                    {
                        CancelScaleMode(false);
                    }
                    else if (history.Count > 0)
                    {
                        PaintAction action = history.Pop();
                        if (action.type == PaintAction.ActionType.Added)
                        {
                            OnBeforeUndoAdded?.Invoke(action.paintObjects);
                        }

                        action.Undo(!SnapToGrid);
                        if (action.type == PaintAction.ActionType.Removed)
                        {
                            OnUndoRemoved?.Invoke(action.paintObjects);
                        }
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    }
                }
                else
                {
                    hideCanvas = !hideCanvas;
                    paintCanvas.GetComponent<Renderer>().enabled = !hideCanvas;
                }

            }

            // Key S scale mode / Control+S save and end paint mode
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S && !rightDown)
            {
                if (control)
                {
                    OnDeactivation();
                    EditorSceneManager.SaveOpenScenes();
                }
                else if (mode != OperationMode.Scale)
                {
                    lastMouseClickPos = GetMousePos(e);
                    lastMouseClickWorldPos = GetCursorRayPosCanvas(lastMouseClickPos);
                    previousMode = mode;
                    currentScale = holdingObject.transform.localScale;
                    mode = OperationMode.Scale;
                }
                else
                {
                    SubmitScaleMode();
                }

            }

            // Shift + Scroll => Change Canvas Position OR Circle Degrees
            else if (e.type == EventType.ScrollWheel && e.shift)
            {
                HandleShiftScroll(e);
            }

            // Key A => Toggle align surface / stroke
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.A && !rightDown)
            {
                currentRotationDelta = Vector3.zero;
                holdingObject.transform.rotation = GetObjectToCreate().transform.rotation;

                if (alignWithStroke)
                {
                    alignWithStroke = false;
                    alignWithSurface = true;
                }
                else if (alignWithSurface)
                {
                    alignWithSurface = false;
                }
                else
                {
                    alignWithStroke = true;
                }
                if (!alignWithSurface)
                {
                    if (parkedPicked != null)
                    {
                        holdingObject.transform.rotation = parkedPicked.transform.rotation;
                    }
                    else
                    {
                        holdingObject.transform.rotation = selectedPrefab.transform.rotation;
                    }
                }
                UpdateOffsetForObject(holdingObject);
                PlaySoundRotate();
                e.Use();
            }

            // Key X => Change Align Axis
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.X)
            {
                if (alignAxis == AlignAxis.NegativeZ)
                {
                    alignAxis = AlignAxis.PositiveX;
                }
                else
                {
                    alignAxis++;
                }
                PlaySoundRotate();
                e.Use();
            }

            // Key G => Toggle Grid
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G)
            {
                snapToGrid = !snapToGrid;
                UpdateOffsetForObject(holdingObject);
                PlaySoundRotate();
            }

            // Key Q => Free Hand
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Q && !rightDown)
            {
                Tools.current = Tool.None;
                previousMode = mode;
                mode = OperationMode.Freehand;
                e.Use();
            }

            // Key W => Line Tool
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.W && !rightDown)
            {
                Tools.current = Tool.None;
                currentRotationDelta = Vector3.zero;
                if (holdingObject != null) holdingObject.transform.eulerAngles = startingEulerAngles;
                objectFactory.objectEuler = Vector3.zero;
                previousMode = mode;
                mode = OperationMode.Line;
                e.Use();
            }

            // Key E => Rect Tool
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.E && !rightDown)
            {
                Tools.current = Tool.None;
                if (mode == OperationMode.Rect)
                {
                    fillRect = !fillRect;
                }
                currentRotationDelta = Vector3.zero;
                if (holdingObject != null) holdingObject.transform.eulerAngles = startingEulerAngles;
                objectFactory.objectEuler = Vector3.zero;
                previousMode = mode;
                mode = OperationMode.Rect;
                e.Use();
            }

            // Key R => Circle Tool
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
            {
                Tools.current = Tool.None;
                currentRotationDelta = Vector3.zero;
                if (holdingObject != null) holdingObject.transform.eulerAngles = startingEulerAngles;
                objectFactory.objectEuler = Vector3.zero;
                previousMode = mode;
                mode = OperationMode.Circle;
                e.Use();
            }

            // Key D => toggle dynamic grid
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.D && !rightDown)
            {
                HandleToggleDynamicGrid(e);
                e.Use();
            }

            // SPACE => Change Plane Axis, change axis
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                switch (canvasAxis)
                {
                    case CanvasAxis.X:
                        {
                            canvasAxis = CanvasAxis.Y;
                            currentRotationDelta.x = 0;
                            break;
                        }
                    case CanvasAxis.Y:
                        {
                            canvasAxis = CanvasAxis.Z;
                            currentRotationDelta.y = 0;
                            break;
                        }
                    case CanvasAxis.Z:
                        {
                            canvasAxis = CanvasAxis.X;
                            currentRotationDelta.z = 0;
                            break;
                        }
                }
                hideCanvas = false;
                UpdatePaintCanvasSize();
                UpdateOffsetForObject(holdingObject);
                PlaySoundRotate();
                e.Use();
            }

            // Key Escape
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (isDragging)
                {
                    CancelDragging();
                }
                else if (mode == OperationMode.Scale)
                {
                    CancelScaleMode(true);
                }
                else
                {
                    OnDeactivation();
                    return;
                }
            }

            // Number Keys -> Switch Prefab
            else if (e.type == EventType.KeyDown)
            {
                HandleSwitchPrefabs(e);
            }

            if (e.type == EventType.MouseUp)
            {
                isDragging = false;
            }

            if (IsCursorInSceneView(e))
            {
                if (e.shift)
                {
                    paintCanvas.GetComponent<Renderer>().enabled = true;
                }
                else if (!e.shift && paintCanvas != null)
                {
                    paintCanvas.GetComponent<Renderer>().enabled = !hideCanvas;
                }
            }
        }

        protected void CancelScaleMode(bool goOutOfScaleMode)
        {
            // cancel scale and go back to previous scale
            holdingObject.transform.localScale = currentScale;
            if (goOutOfScaleMode)
            {
                mode = previousMode != mode ? previousMode : OperationMode.Freehand;
            }
            holdingObject.AddComponent<Pop>();
            PlaySoundDelete();
        }

        protected void SubmitScaleMode()
        {
            currentScale = holdingObject.transform.localScale;
            mode = previousMode != mode ? previousMode : OperationMode.Freehand;
            holdingObject.AddComponent<Pop>();
            PlaySoundRotate();
        }

        protected void CancelDragging()
        {
            isDragging = false;
            lastMouseClickPos = GetMousePos(lastEvent);
            lastMouseClickWorldPos = GetCursorRayPosCanvas(lastMouseClickPos);
            objectFactory.DestroyAll();
        }

        protected void HandleFreehandDrag(Event e)
        {
            if (control) // delete
            {
                if (brushRadius == 0)
                {
                    DestroyObjectOnCursor(e);
                }
                else if (circleBrush != null)
                {

                    CapsuleCollider cc = circleBrush.GetComponent<CapsuleCollider>();
                    cc.enabled = true;

                    Vector3 upperCapsule = circleBrush.transform.position + cc.center + circleBrush.transform.up * cc.height * 0.5f;
                    Vector3 bottomCapsule = circleBrush.transform.position + cc.center - circleBrush.transform.up * cc.height * 0.5f;

                    RaycastHit[] hits = Physics.CapsuleCastAll(upperCapsule, bottomCapsule, brushRadius, Vector3.forward, 0.01f, MASK_ONLY_OBJECTS);
                    bool atleastOne = false;
                    foreach (RaycastHit hit in hits)
                    {
                        GameObject hitObject = hit.collider.gameObject;
                        if (hit.collider.gameObject.IsPrefab())
                        {
                            hitObject = PrefabUtility.GetNearestPrefabInstanceRoot(hitObject);
                        }

                        Transform groupT = GetObjectGroupFor(hitObject);
                        if (hitObject.transform.parent != groupT)
                        {
                            hitObject.transform.parent = groupT;
                        }
                        paintCollection.Add(hitObject);
                        hitObject.SetActive(false);
                        atleastOne = true;
                    }
                    if (atleastOne)
                    {
                        PlaySoundDelete();
                    }
                }

            }
            else if (placeSampleAllowed)
            {
                PaintObjectOnCursor(e);
            }

        }

        protected void HandleMouseDragLine(Vector3 start, Vector3 end)
        {
            if (control)
            {
                lastMassSelection = Selection.gameObjects;

                Vector3 camAxisOffset = GetCanvasAxis() * 0.3f;
                if (camAxisPolarityInverted) camAxisOffset *= -1;
                if (canvasAxis == CanvasAxis.Z) camAxisOffset *= -1;

                SelectObjectsInLine(start + camAxisOffset, end + camAxisOffset);
                if (lastMassSelection != null && lastMassSelection.Length != Selection.gameObjects.Length)
                {
                    PlaySoundLineRectDelete();
                }
            }
            else
            {
                lineFactory.padding = padding;
                lineFactory.startPos = start;
                lineFactory.endPos = end;
                lineFactory.snapToGrid = SnapToGrid;
                lineFactory.gridSize = gridSize;
                lineFactory.alignObjects = alignWithStroke;
                lineFactory.Produce(start);
                if (lastAddedObject != objectFactory.GetLatest())
                {
                    PlaySoundLineRectTool();
                }
                lastAddedObject = objectFactory.GetLatest();
            }
        }

        protected void HandleMouseDragRect(Vector3 start, Vector3 end)
        {
            if (control)
            {
                object[] result = CreateRectFromTwoPositions(start, end);
                Rect cursorRect = (Rect)result[0];

                lastMassSelection = Selection.gameObjects;
                SelectObjectsInRect(cursorRect, (float)result[1], (float)result[2]);
                if (lastMassSelection != null && lastMassSelection.Length != Selection.gameObjects.Length)
                {
                    PlaySoundLineRectDelete();
                }
            }
            else
            {
                if (alignWithStroke) objectFactory.objectEuler = currentRotationDelta;
                rectFactory.padding = padding;
                rectFactory.axis = canvasAxis;
                rectFactory.startPos = start;
                rectFactory.endPos = end;
                rectFactory.snapToGrid = SnapToGrid;
                rectFactory.gridSize = gridSize;
                rectFactory.fill = fillRect;
                rectFactory.alignObjects = alignWithStroke;
                rectFactory.Produce(start);
                if (lastAddedObject != objectFactory.GetLatest())
                {
                    PlaySoundLineRectTool();
                }
                lastAddedObject = objectFactory.GetLatest();
            }
        }

        protected object[] CreateRectFromTwoPositions(Vector3 start, Vector3 end)
        {
            float startHeight = start.y;
            float endHeight = end.y;
            Rect cursorRect = new Rect(start.x, start.z, (end - start).x, (end - start).z);
            if (canvasAxis == CanvasAxis.X)
            {
                cursorRect = new Rect(start.z, start.y, (end - start).z, (end - start).y);
                startHeight = start.x;
                endHeight = end.x;
            }
            if (canvasAxis == CanvasAxis.Z)
            {
                cursorRect = new Rect(start.x, start.y, (end - start).x, (end - start).y);
                startHeight = start.z;
                endHeight = end.z;
            }
            return new object[] { cursorRect, startHeight, endHeight };
        }

        protected void HandleMouseDragCircle(Vector3 start, Vector3 end)
        {
            if (control)
            {
                // todo delete with circle
                object[] result = CreateRectFromTwoPositions(start, end);
                Rect cursorRect = (Rect)result[0];

                lastMassSelection = Selection.gameObjects;
                SelectObjectsInRect(cursorRect, (float)result[1], (float)result[2]);
                if (lastMassSelection != null && lastMassSelection.Length != Selection.gameObjects.Length)
                {
                    PlaySoundLineRectDelete();
                }
            }
            else
            {
                objectFactory.objectEuler = currentRotationDelta;
                circleFactory.padding = padding;
                circleFactory.axis = canvasAxis;
                circleFactory.alignWithNormal = alignWithStroke;
                circleFactory.startPos = start;
                circleFactory.endPos = end;
                circleFactory.snapToGrid = SnapToGrid;
                circleFactory.gridSize = gridSize;
                circleFactory.drawEllipses = true;
                circleFactory.forceEven = shift;
                circleFactory.fill = false;
                circleFactory.Produce();

                if (lastAddedObject != objectFactory.GetLatest())
                {
                    PlaySoundLineRectTool();
                }
                lastAddedObject = objectFactory.GetLatest();
            }
        }


        protected void HandleReleaseMouseDragMassTools()
        {
            circleFactory.drawEllipses = false;
            if (control) // Delete selected objects
            {
                if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
                {
                    PlaySoundDelete();
                    history.Push(new PaintAction(Selection.gameObjects, PaintAction.ActionType.Removed));
                }
                foreach (GameObject go in Selection.gameObjects)
                {
                    Transform groupT = GetObjectGroupFor(go);
                    if (go.transform.parent != groupT)
                    {
                        go.transform.parent = groupT;
                    }
                    go.SetActive(false);
                }
                OnObjectMassDeleted?.Invoke(Selection.gameObjects, GetCanvasAxis(), SnapToGrid);
            }
            else if (!objectFactory.IsEmpty()) // Create objects
            {
                for (int i = 0; i < objectFactory.GetAllProducts().Count; i++)
                {
                    GameObject go = objectFactory.GetAt(i);

                    if (!SnapToGrid || alignWithStroke) continue;

                    if (!go.activeSelf || (!CheckOnlyCollisionWithTestGroup(go, objectFactory.GetAllProducts()) && !paintIgnoreColliding))
                    {
                        // Destroy if place is used
                        DestroyImmediate(go);
                    }

                }

                history.Push(new PaintAction(objectFactory.GetActiveProducts(), PaintAction.ActionType.Added));

                PlaySoundPaint();
                // FireEvent
                OnObjectMassPainted?.Invoke(objectFactory.GetActiveProducts().ToArray(), GetCanvasAxis(), SnapToGrid);
                OnObjectMassPaintedLate?.Invoke(objectFactory.GetActiveProducts().ToArray(), GetCanvasAxis(), SnapToGrid);
                objectFactory.Release();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        protected void HandleToggleDynamicGrid(Event e)
        {
            dynamicGrid = !dynamicGrid;
            if (!dynamicGrid)
            {
                gridSize = Vector3.one;
                if (canvasAxis == CanvasAxis.X) canvasPositionX = canvasPositionX.GridPos(gridSize.x);
                if (canvasAxis == CanvasAxis.Y) canvasPositionY = canvasPositionY.GridPos(gridSize.y);
                if (canvasAxis == CanvasAxis.Z) canvasPositionZ = canvasPositionZ.GridPos(gridSize.z);
                UpdatePaintCanvasSize();
            }
            else CalculateGrid(holdingObject);
            UpdateOffsetForObject(holdingObject);
            PlaySoundRotate();

        }

        protected void HandleMouseScroll(Event e)
        {
            if (alt) // RotateObject
            {
                Vector3 newRotationDelta = currentRotationDelta;
                if (canvasAxis == CanvasAxis.X) newRotationDelta.x += rotationStep * (e.delta.y < 0 ? 1 : -1);
                if (canvasAxis == CanvasAxis.Y) newRotationDelta.y += rotationStep * (e.delta.y < 0 ? 1 : -1);
                if (canvasAxis == CanvasAxis.Z) newRotationDelta.z += rotationStep * (e.delta.y < 0 ? 1 : -1);

                currentRotationDelta = newRotationDelta;
                Transform rotationBody = holdingObject.transform.Find("Center");
                if (!alignWithSurface)
                {
                    if (rotationBody != null)
                    {
                        if (canvasAxis == CanvasAxis.X) rotationBody.Rotate(rotationStep * (e.delta.y < 0 ? 1 : -1), 0, 0, Space.World);
                        if (canvasAxis == CanvasAxis.Y) rotationBody.Rotate(0, rotationStep * (e.delta.y < 0 ? 1 : -1), 0, Space.World);
                        if (canvasAxis == CanvasAxis.Z) rotationBody.Rotate(0, 0, rotationStep * (e.delta.y < 0 ? 1 : -1), Space.World);
                    }
                    else
                    {
                        if (canvasAxis == CanvasAxis.X) holdingObject.transform.Rotate(rotationStep * (e.delta.y < 0 ? 1 : -1), 0, 0, Space.World);
                        if (canvasAxis == CanvasAxis.Y) holdingObject.transform.Rotate(0, rotationStep * (e.delta.y < 0 ? 1 : -1), 0, Space.World);
                        if (canvasAxis == CanvasAxis.Z) holdingObject.transform.Rotate(0, 0, rotationStep * (e.delta.y < 0 ? 1 : -1), Space.World);
                    }
                }
                else
                {
                    holdingObject.transform.Rotate(0, rotationStep * (e.delta.y < 0 ? 1 : -1), 0, Space.Self);
                }

                PlaySoundRotate();
                if (mode == OperationMode.Line || mode == OperationMode.Rect || mode == OperationMode.Circle)
                {
                    objectFactory.UpdateAllProductsRotation(holdingObject.transform.eulerAngles);
                }
                e.Use();
            }
            else if (control)
            {
                float before = brushRadius;
                brushRadius += e.delta.y < 0 ? 1 : -1;
                brushRadius = Mathf.Max(0, brushRadius);
                brushRadius = Mathf.Min(100, brushRadius);

                circleBrush.transform.localScale = new Vector3(brushRadius, 1f, brushRadius);

                if (before != brushRadius) PlaySoundRotate();
                e.Use();
            }
            else if (isDragging)
            {
                float gridSizeAxis = canvasAxis == CanvasAxis.X ? gridSize.x
                                     : (canvasAxis == CanvasAxis.Y) ? gridSize.y : gridSize.z;
                float paddingDelta = SnapToGrid ? gridSizeAxis : minPadding;
                float paddingBefore = 0;
                float paddingAfter = 0;

                paddingBefore = padding;
                padding += (e.delta.y > 0 ? paddingDelta : -paddingDelta);
                if (padding < minPadding) padding = SnapToGrid ? 1 : minPadding;
                if (padding > maxPadding) padding = maxPadding;
                paddingAfter = padding;

                if (mode == OperationMode.Line)
                {
                    lineFactory.padding = padding;
                    lineFactory.Produce(Vector3.zero);
                }
                else if (mode == OperationMode.Rect)
                {
                    rectFactory.padding = padding;
                    rectFactory.Produce(Vector3.zero);
                }
                else if (mode == OperationMode.Circle)
                {
                    circleFactory.padding = padding;
                    circleFactory.Produce();
                    circleFactory.Produce();
                }

                if (paddingBefore != paddingAfter) PlaySoundRotate();
                e.Use();
            }

        }

        protected void HandleShiftScroll(Event e)
        {
            // set circle degrees
            if (mode == OperationMode.Circle)
            {
                float degreesBefore = circleFactory.degrees;
                circleFactory.degrees += e.delta.y < 0 ? 10 : -10;
                if (circleFactory.degrees >= 360) circleFactory.degrees = 360;
                if (circleFactory.degrees <= 0) circleFactory.degrees = 0;
                float degreesAfter = circleFactory.degrees;
                if (degreesBefore != degreesAfter) PlaySoundRotate();
                if (isDragging) circleFactory.Produce();

            }
            else
            {// set canvas position

                if (canvasAxis == CanvasAxis.X)
                {
                    if (e.delta.y < 0 && canvasPositionX < paintCanvasSize - 1)
                    {
                        PlaySoundMovePlaneUp();
                        canvasPositionX += gridSize.x;
                        if (canvasPositionX >= paintCanvasSize / 2) canvasPositionX = paintCanvasSize / 2 - 1;
                    }
                    else if (e.delta.y > 0 && canvasPositionX > -paintCanvasSize / 2)
                    {
                        PlaySoundMovePlaneDown();
                        canvasPositionX -= gridSize.x;
                        if (canvasPositionX < -paintCanvasSize / 2) canvasPositionX = -paintCanvasSize / 2;
                    }
                }
                else if (canvasAxis == CanvasAxis.Y)
                {
                    if (e.delta.y < 0 && canvasPositionY < paintCanvasSize - 1)
                    {
                        PlaySoundMovePlaneUp();
                        canvasPositionY += gridSize.y;
                        if (canvasPositionY >= paintCanvasSize / 2) canvasPositionY = paintCanvasSize / 2 - 1;
                    }
                    else if (e.delta.y > 0 && canvasPositionY > -paintCanvasSize / 2)
                    {
                        if (!(preventCanvasBelow0 && canvasPositionY <= 0))
                        {
                            PlaySoundMovePlaneDown();
                            canvasPositionY -= gridSize.y;
                            if (canvasPositionY < -paintCanvasSize / 2) canvasPositionY = -paintCanvasSize / 2;
                        }
                    }
                }
                else if (canvasAxis == CanvasAxis.Z)
                {
                    if (e.delta.y < 0 && canvasPositionZ < paintCanvasSize - 1)
                    {
                        PlaySoundMovePlaneUp();
                        canvasPositionZ += gridSize.z;
                        if (canvasPositionZ >= paintCanvasSize / 2) canvasPositionZ = paintCanvasSize / 2 - 1;
                    }
                    else if (e.delta.y > 0 && canvasPositionZ > -paintCanvasSize / 2)
                    {
                        PlaySoundMovePlaneDown();
                        canvasPositionZ -= gridSize.z;
                        if (canvasPositionZ < -paintCanvasSize / 2) canvasPositionZ = -paintCanvasSize / 2;
                    }
                }
                UpdatePaintCanvasSize();
            }
            e.Use();
        }

        protected void HandleSwitchPrefabs(Event e)
        {
            if (mode != OperationMode.Scale)
            {

                switch (e.keyCode)
                {
                    case KeyCode.Alpha1:
                        {
                            SwitchPrefab(0);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha2:
                        {
                            SwitchPrefab(1);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha3:
                        {
                            SwitchPrefab(2);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha4:
                        {
                            SwitchPrefab(3);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha5:
                        {
                            SwitchPrefab(4);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha6:
                        {
                            SwitchPrefab(5);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha7:
                        {
                            SwitchPrefab(6);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha8:
                        {
                            SwitchPrefab(7);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha9:
                        {
                            SwitchPrefab(8);
                            e.Use();
                            break;
                        }
                    case KeyCode.Alpha0:
                        {
                            SwitchPrefab(9);
                            e.Use();
                            break;
                        }
                }
            }
            else
            {
                bool changed = false;
                switch (e.keyCode)
                {
                    case KeyCode.Alpha1:
                        {
                            currentScale = Vector3.one * 1;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha2:
                        {
                            currentScale = Vector3.one * 2;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha3:
                        {
                            currentScale = Vector3.one * 3;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha4:
                        {
                            currentScale = Vector3.one * 4;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha5:
                        {
                            currentScale = Vector3.one * 5;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha6:
                        {
                            currentScale = Vector3.one * 6;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha7:
                        {
                            currentScale = Vector3.one * 7;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha8:
                        {
                            currentScale = Vector3.one * 8;
                            e.Use();
                            changed = true;
                            break;
                        }
                    case KeyCode.Alpha9:
                        {
                            currentScale = Vector3.one * 9;
                            e.Use();
                            changed = true;
                            break;
                        }
                }
                if (changed)
                {
                    holdingObject.transform.localScale = currentScale;
                    PlaySoundRotate();
                    holdingObject.AddComponent<Pop>();
                    UpdateOffsetForObject(holdingObject);
                }

            }
        }

        protected GameObject GetObjectOrPrefabInstance(GameObject go)
        {
            if (go == null) return go;
            return go.IsPrefab() ? PrefabUtility.GetNearestPrefabInstanceRoot(go) : go;
        }

        protected GameObject GetObjectToCreate()
        {
            if (parkedPicked != null)
            {
                return parkedPicked;
            }
            return selectedPrefab;
        }

        protected GameObject[] GetObjectsInRect(Rect rect, float startHeight, float endHeight)
        {
            int layerMask = ~((1 << paintCanvas.layer) | (1 << LayerMask.NameToLayer(editLayer)));
            Vector3 center = Vector3.zero;
            Collider[] colliders = null;

            float camAxisOffset = 0.5f * (camAxisPolarityInverted ? -1 : 1);

            // Create the axis aligned collider and call Physics.OverlapBox
            if (canvasAxis == CanvasAxis.Y)
            {
                center = new Vector3(rect.x + rect.width / 2, startHeight + camAxisOffset, rect.y + rect.height / 2);
                colliders = Physics.OverlapBox(center,
                    new Vector3(
                        Mathf.Abs(rect.width) / 2,
                        0.45f,
                        Mathf.Abs(rect.height) / 2),
                    Quaternion.LookRotation(Vector3.forward, Vector3.up),
                    layerMask);
            }
            else if (canvasAxis == CanvasAxis.X) // ZY-Plane
            {
                center = new Vector3(startHeight + camAxisOffset, rect.y + rect.height / 2, rect.x + rect.width / 2);
                colliders = Physics.OverlapBox(center,
                    new Vector3(
                        0.45f,
                        Mathf.Abs(rect.height) / 2,
                        Mathf.Abs(rect.width) / 2),
                    Quaternion.LookRotation(Vector3.forward, Vector3.up),
                    layerMask);
            }
            else if (canvasAxis == CanvasAxis.Z) // XY-Plane
            {
                center = new Vector3(rect.x + rect.width / 2, rect.y + rect.height / 2, startHeight - camAxisOffset);
                colliders = Physics.OverlapBox(center,
                    new Vector3(
                        Mathf.Abs(rect.width) / 2,
                        Mathf.Abs(rect.height) / 2,
                        0.45f),
                    Quaternion.LookRotation(Vector3.forward, Vector3.up),
                    layerMask);
            }

            GameObject[] gos = new GameObject[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                gos[i] = GetObjectOrPrefabInstance(colliders[i].gameObject);
            }

            return gos;
        }

        protected GameObject[] GetObjectsInLine(Vector3 start, Vector3 end)
        {
            int layerMask = ~((1 << paintCanvas.layer) | (1 << LayerMask.NameToLayer(editLayer)));
            Vector3 delta = (end - start);
            Vector3 direction = delta.normalized;
            RaycastHit[] hits = Physics.RaycastAll(start, direction, delta.magnitude, layerMask);

            GameObject[] gos = new GameObject[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                gos[i] = GetObjectOrPrefabInstance(hits[i].collider.gameObject);
            }

            return gos;
        }

        protected void SelectObjectsInLine(Vector3 start, Vector3 end)
        {
            Selection.objects = GetObjectsInLine(start, end);
        }

        protected void SelectObjectsInRect(Rect rect, float startHeight, float endHeight)
        {
            Selection.objects = GetObjectsInRect(rect, startHeight, endHeight);
        }

        public void SwitchPrefab(int id, bool resetRotation = true)
        {
            prefabId = id;
            if (paintObjects[prefabId] != null)
            {
                if (holdingObject != null)
                {
                    DestroyImmediate(holdingObject);
                }
                if (parkedPicked != null)
                {
                    DestroyImmediate(parkedPicked);
                }
                if (resetRotation) currentRotationDelta = Vector3.zero;
                selectedPrefab = paintObjects[prefabId];
                objectFactory.objectToCreate = paintObjects[prefabId];

                if (!sameScaleForObjects)
                {
                    currentScale = paintObjects[prefabId].transform.localScale;
                }

                holdingObject = CreateNew(lastObjectPos);
                holdingObject.transform.position = Vector3.zero;
                UpdateOffsetForObject(holdingObject);
                CalculateGrid(holdingObject);
                SetObjectPosition(holdingObject, lastObjectPos);
            }
        }

        public Vector3 GetCanvasAxis()
        {
            return canvasAxis == CanvasAxis.X ? Vector3.right
                : (canvasAxis == CanvasAxis.Y) ? Vector3.up
                : Vector3.forward;
        }

        // SetPosition of object
        public void SetObjectPosition(GameObject go, Vector3 pos) { SetObjectPosition(go, pos, Vector3.zero); }
        public void SetObjectPosition(GameObject go, Vector3 pos, Vector3 normal, bool skipAlignStroke = false)
        {
            Vector3 gridPos = GridPos(pos);
            if (circleBrush != null)
            {
                circleBrush.SetActive(mode == OperationMode.Freehand);
                if (alt)
                    circleBrush.transform.position = pos;
                circleBrush.transform.localScale = new Vector3(brushRadius, 1f, brushRadius);
            }

            //// Automatically position inactive canvas pos to object position
            //if (canvasAxis == CanvasAxis.X)
            //{
            //    canvasPositionY = gridPos.y;
            //    canvasPositionZ = gridPos.z;

            //}
            //else if (canvasAxis == CanvasAxis.Y)
            //{
            //    canvasPositionX = gridPos.x;
            //    canvasPositionZ = gridPos.z;
            //}
            //else if (canvasAxis == CanvasAxis.Z)
            //{
            //    canvasPositionX = gridPos.x;
            //    canvasPositionY = gridPos.y;
            //}

            if (SnapToGrid)
            {
                pos = gridPos;
            }

            RaycastHit hit;
            if (!RaycastCursor(lastEvent, out hit)) return;
            //if (!IsInLevelBounds(pos)) return;

            go.transform.position = pos + placeOffset;

            lastObjectPos = go.transform.position;

            if (alignWithSurface) // -- ALIGN WITH SURFACE --
            {
                // remove offset
                go.transform.position -= placeOffset;

                if (normal == Vector3.zero)
                {
                    Vector3 raycastDir = GetCanvasAxis();
                    if (camAxisPolarityInverted) raycastDir *= -1;
                    if (canvasAxis == CanvasAxis.Z) raycastDir *= -1;

                    //normal = RaycastSurfaceNormal(go, pos + raycastDir * 5, -raycastDir);
                    int layerMask = 0;
                    if (paintOnlyOnGround)
                    {
                        layerMask = (1 << LayerMask.NameToLayer(canvasLayer));
                    }
                    normal = RaycastSurfaceNormal(lastEvent, layerMask);
                }

                go.transform.rotation = Quaternion.FromToRotation(go.transform.up, normal) * go.transform.rotation;
                float alignFactor = camAxisPolarityInverted ? -1 : 1;
                Vector3 orientedOffset = Vector3.zero;
                // re-apply offset
                //if (canvasAxis == CanvasAxis.X)
                //{
                //    orientedOffset = alignFactor * go.transform.up * placeOffset.x;
                //}
                if (canvasAxis == CanvasAxis.Y)
                {
                    orientedOffset = alignFactor * go.transform.up * placeOffset.y;
                }
                //else if (canvasAxis == CanvasAxis.Z)
                //{
                //    orientedOffset = -alignFactor * go.transform.up * placeOffset.y;
                //}

                go.transform.position += orientedOffset;
            }

            if (alignWithStroke && !skipAlignStroke && mode == OperationMode.Freehand)
            {
                Vector3 paintDirection = (mouseWorldPos - lastSampledMouseWorldPos);
                if (isDragging)
                {
                    paintDirection = pos - lastPaintedObjectPos;
                }

                if (paintDirection.sqrMagnitude > 0.01f)
                {
                    paintDirection = paintDirection.normalized;
                    Vector3 upDirection = Vector3.up;
                    Ray cRay = CreateRay(mousePos);

                    if (canvasAxis == CanvasAxis.X)
                    {
                        upDirection = -Vector3.right * (cRay.direction.x < 0 ? 1 : -1);
                        paintDirection.x = 0;
                        paintDirection = -paintDirection;
                    }
                    else if (canvasAxis == CanvasAxis.Y)
                    {
                        upDirection = Vector3.up * (cRay.direction.y < 0 ? 1 : -1);
                        paintDirection.y = 0;
                    }
                    else if (canvasAxis == CanvasAxis.Z)
                    {
                        upDirection = Vector3.forward * (cRay.direction.z > 0 ? 1 : -1);
                        paintDirection.z = 0;
                        paintDirection = -paintDirection;
                    }
                    Quaternion lookRotation = Quaternion.LookRotation(paintDirection, upDirection);

                    if (lastPaintRotation == Quaternion.identity)
                    {
                        go.transform.rotation = lookRotation;
                    }
                    else
                    {
                        go.transform.rotation = Quaternion.Lerp(lastPaintRotation, lookRotation, 0.3f);
                    }

                    lastPaintRotation = lookRotation;
                    if (currentRotationDelta != Vector3.zero)
                    {
                        go.transform.eulerAngles += currentRotationDelta;
                    }

                    if (parkedPicked != null)
                    {
                        go.transform.eulerAngles += parkedPicked.transform.eulerAngles;
                    }
                    else
                    {
                        go.transform.eulerAngles += selectedPrefab.transform.eulerAngles;
                    }
                }
            }

            // Fire Event
            if (OnHoldingObjectPositionChanged != null)
            {
                OnHoldingObjectPositionChanged(go, GetCanvasAxis(), SnapToGrid);
            }

        }

        // SetOffset, CalculateOffset, Calculate Offset
        public Vector3 UpdateOffsetForObject(GameObject go)
        {
            if (go == null) return Vector3.zero;

            placeOffset = Vector3.zero;

            if (snappingBase == SnappingBase.Pivot)
            {
                return (placeOffset += placementOffset);
            }

            if (go.TryGetComponent(out IGridTransform gt))
            {
                placeOffset = -gt.Offset;
                placeOffset.y = 0;
            }
            else
            {
                Quaternion rotationBefore = go.transform.rotation;
                go.transform.rotation = Quaternion.identity;

                Bounds bounds = new Bounds(go.transform.position, go.transform.localScale);
                if (go.TryGetComponent(out Collider colli) && snappingBase == SnappingBase.ColliderBounds)
                {
                    colli.enabled = true;
                    Physics.SyncTransforms();
                    bounds = colli.bounds;
                }
                else
                {
                    Renderer renderer = go.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        bounds = renderer.bounds;
                    }

                }

                placeOffset = go.transform.position - bounds.center;
                placeOffset += placementOffset;

                camAxisPolarityInverted = GetSceneCamAxisPolarity() > 0;

                switch (canvasAxis)
                {
                    case CanvasAxis.X:
                        {
                            placeOffset.x += bounds.extents.x;

                            if (camAxisPolarityInverted)
                            { // looking from left
                                placeOffset.x -= bounds.extents.x * 2;
                            }
                            break;
                        }
                    case CanvasAxis.Y:
                        {
                            placeOffset.y += bounds.extents.y;
                            if (camAxisPolarityInverted)
                            { // looking from below
                                placeOffset.y -= bounds.extents.y * 2;
                            }
                            break;
                        }
                    case CanvasAxis.Z:
                        {
                            placeOffset.z -= bounds.extents.z;
                            if (camAxisPolarityInverted)
                            { // looking behind
                                placeOffset.z += bounds.extents.z * 2;
                            }
                            break;
                        }
                }
                go.transform.rotation = rotationBefore;

                if (snapToGrid)
                {
                    if (canvasAxis != CanvasAxis.X) placeOffset.x -= gridSize.x * 0.5f;
                    if (canvasAxis != CanvasAxis.Y) placeOffset.y -= gridSize.y * 0.5f;
                    if (canvasAxis != CanvasAxis.Z) placeOffset.z -= gridSize.z * 0.5f;
                }

                if (colli != null) colli.enabled = false;
            }

            return placeOffset;
        }

        /// <summary>
        /// Returns whether the camera is looking to the paint canvas from the default side (<0) or the other side (>0)
        /// </summary>
        public float GetSceneCamAxisPolarity()
        {
            if (canvasAxis == CanvasAxis.X)
            {
                return -(SceneView.lastActiveSceneView.camera.transform.position.x - paintCanvas.transform.position.x);
            }
            else if (canvasAxis == CanvasAxis.Y)
            {
                return -(SceneView.lastActiveSceneView.camera.transform.position.y - paintCanvas.transform.position.y);
            }
            else
            {
                return (SceneView.lastActiveSceneView.camera.transform.position.z - paintCanvas.transform.position.z);
            }
        }

        public GameObject PickObjectWithoutCollider(Vector2 screenPos)
        {
            GameObject go = HandleUtility.PickGameObject(screenPos, true, IgnoredObjectsToPick);
            if (go == null) return null;
            // Dont pick the terrain!
            if (go != null && go.TryGetComponent(out Terrain terry)) return null;
            if (go != null && go.layer == LayerMask.NameToLayer(canvasLayer)) return null;

            if (canvasAxis == CanvasAxis.X)
            {
                if (go.transform.position.x < paintCanvas.transform.position.x) return null;
            }
            if (canvasAxis == CanvasAxis.Y)
            {
                if (go.transform.position.y < paintCanvas.transform.position.y) return null;
            }
            if (canvasAxis == CanvasAxis.Z)
            {
                if (go.transform.position.z < paintCanvas.transform.position.z) return null;
            }
            return go;
        }

        // DeleteObject 
        protected void DestroyObjectOnCursor(Event e)
        {
            GameObject target = RaycastFirst(e);
            if (target != null && target.layer == LayerMask.NameToLayer(canvasLayer)) target = null;

            if (target == null)
            {
                target = PickObjectWithoutCollider(e.mousePosition);
            }
            if (target != null)
            {
                if (holdingObject != null)
                {
                    target.SetActive(false);
                    if (objectGroups.Contains(target.transform.parent) == false)
                    {
                        target.transform.parent = GetObjectGroupFor(target);
                    }
                    history.Push(new PaintAction(target, PaintAction.ActionType.Removed));
                    OnObjectDeleted?.Invoke(target);
                    target = null;
                }

                PlaySoundDelete();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                e.Use();
            }
        }

        protected void ApplySelectedObject()
        {
            if (holdingObject != null)
            {
                Vector3 holdingObjPos = holdingObject.transform.position;
                Vector3 holdingObjGridPos = holdingObjPos - placeOffset;

                if (holdingObject.TryGetComponent(out IGridTransform gt))
                {
                    holdingObjGridPos = holdingObjPos + gt.Offset;
                }
                lastSampledMouseWorldPos = sampledMouseWorldPos = mouseWorldPos;

                placeSampleAllowed = false;
                ActuallyPlaceHoldingObjectAndCreateNew(holdingObject, holdingObjPos, holdingObjGridPos);
            }
        }

        protected void PaintObjectOnCursor(Event e, bool onlyPlane = true)
        {
            if (holdingObject != null)
            {
                Vector3 cursorPos = GetCursorRayPosPlane(e);
                Vector3 gridPos = GridPos(cursorPos);

                if (lastGridPos == gridPos) return;
                lastGridPos = gridPos;

                SetObjectPosition(holdingObject, cursorPos);

                ActuallyPlaceHoldingObjectAndCreateNew(holdingObject, cursorPos, gridPos);
                lastSampledMouseWorldPos = sampledMouseWorldPos = mouseWorldPos;
                placeSampleAllowed = false;
            }
        }

        /// <summary>
        /// Returns true if subject collides only with objects of the provided list or nothing.
        /// Returns false if subject collides with an object that is not part of the list.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="testAgainst"></param>
        /// <returns></returns>
        protected bool CheckOnlyCollisionWithTestGroup(GameObject subject, List<GameObject> testAgainst)
        {
            List<GameObject> collis = null;
            CheckCollisionAll(subject, out collis);
            foreach (GameObject collided in collis)
            {
                if (!testAgainst.Contains(collided))
                {
                    return false;
                }
            }
            return true;
        }

        protected bool CheckCollision(GameObject go)
        {
            GameObject dummy = null;
            return CheckCollision(go, out dummy);
        }

        protected bool CheckCollision(GameObject go, out GameObject collided)
        {
            List<GameObject> collis = null;
            if (CheckCollisionAll(go, out collis))
            {
                collided = collis[0];
                return true;
            }
            collided = null;
            return false;
        }

        protected bool CheckCollisionAll(GameObject go, out List<GameObject> allColliding)
        {
            int notPlaneNorEditLayer = ~(1 << paintCanvas.layer | 1 << LayerMask.NameToLayer(editLayer));
            Vector3 pos = go.transform.position;
            if (go.TryGetComponent(out IGridTransform gT))
            {
                pos += gT.Offset;
            }

            Collider[] collis = null;
            if (go.TryGetComponent(out Collider goColli))
            {
                collis = Physics.OverlapBox(goColli.bounds.center, goColli.bounds.extents * 0.9f, go.transform.rotation, notPlaneNorEditLayer);
            }

            allColliding = new List<GameObject>();

            if (collis != null)
            {
                for (int i = 0; i < collis.Length; i++)
                {
                    if (collis[i].gameObject != null && collis[i].gameObject != go && !collis[i].transform.IsChildOf(go.transform))
                    {
                        allColliding.Add(collis[i].gameObject);
                    }
                }
                return allColliding.Count > 0;
            }
            else
            {
                return false;
            }
        }

        protected void ActuallyPlaceHoldingObjectAndCreateNew(GameObject go, Vector3 objPos, Vector3 gridPos)
        {
            int notPlaneOrEditLayer = ~(1 << paintCanvas.layer | 1 << LayerMask.NameToLayer(editLayer));
            bool collides = false;

            if (go.TryGetComponent(out Collider goColli))
            {
                goColli.enabled = true;
                Physics.SyncTransforms();

                Vector3 center = goColli.bounds.center;
                Vector3 extents = goColli.bounds.extents * 0.7f;

                goColli.enabled = false;
                Physics.SyncTransforms();

                collides = Physics.CheckBox(center, extents, go.transform.rotation, notPlaneOrEditLayer) && !alignWithSurface;
            }

            if (!collides || alt || paintIgnoreColliding || !SnapToGrid)
            {
                holdingObject.layer = originalLayer;
                holdingObject.name = holdingObject.name.Remove(0, holdingPrefix.Length);
                if (goColli != null) goColli.enabled = true;

                PlaySoundPaint();


                paintCollection.Add(holdingObject);

                lastAddedObject = holdingObject;
                lastPaintedObjectPos = SnapToGrid ? gridPos : objPos;
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                if (brushRadius > 0)
                {
                    Vector2 radi = UnityEngine.Random.insideUnitCircle * brushRadius;
                    if (canvasAxis == CanvasAxis.Y)
                    {
                        holdingObject.transform.position += new Vector3(radi.x, 0, radi.y);
                    }
                    else if (canvasAxis == CanvasAxis.X)
                    {
                        holdingObject.transform.position += new Vector3(0, radi.x, radi.y);
                    }
                    else if (canvasAxis == CanvasAxis.Z)
                    {
                        holdingObject.transform.position += new Vector3(radi.x, radi.y, 0);
                    }

                }

                holdingObject = CreateNew(SnapToGrid ? gridPos : objPos);
                // Fire Events
                OnObjectPainted?.Invoke(lastAddedObject, GetCanvasAxis(), SnapToGrid);

            }
        }

        protected void PickObjectOnCursor(Event e)
        {
            GameObject pickedNow = RaycastFirst(e);
            if (pickedNow == null)
            {
                pickedNow = PickObjectWithoutCollider(e.mousePosition);
            }
            if (pickedNow != null)
            {
                OnObjectPicked?.Invoke(pickedNow);

                if (parkedPicked != null)
                {
                    DestroyImmediate(parkedPicked);
                }
                string namebefore = pickedNow.name;

                if (pickedNow.IsPrefab())
                {
                    selectedPrefab = pickedNow.GetPrefabAsset();
                    objectFactory.objectToCreate = selectedPrefab;
                }
                else
                {
                    parkedPicked = Instantiate(pickedNow);
                    parkedPicked.transform.position += Vector3.forward * 5000;
                    parkedPicked.transform.parent = groupRoot.transform;
                    parkedPicked.name = pickedPrefix + namebefore;
                    objectFactory.objectToCreate = parkedPicked;
                }

                currentRotationDelta = Vector3.zero;
                currentScale = pickedNow.transform.localScale;


                if (alt) // Snap paint canvas to picked objects position
                {
                    canvasPositionX = ((int)pickedNow.transform.position.x);
                    canvasPositionY = ((int)pickedNow.transform.position.y);
                    canvasPositionZ = ((int)pickedNow.transform.position.z);
                    UpdatePaintCanvasSize();
                }

                if (holdingObject != null)
                {
                    DestroyImmediate(holdingObject);
                }
                originalLayer = pickedNow.layer;

                // GrabObject
                if (control)
                {
                    holdingObject = pickedNow;
                    holdingObject.name = holdingPrefix + holdingObject.name;
                    holdingObject.layer = LayerMask.NameToLayer(editLayer);
                }
                else
                {
                    holdingObject = CreateNew(pickedNow.transform.position);
                }

                holdingObject.transform.localScale = currentScale;
                UpdateOffsetForObject(holdingObject);
                Transform objectGroup = GetObjectGroupFor(holdingObject);
                if (holdingObject.transform.parent != objectGroup.transform)
                {
                    holdingObject.transform.parent = objectGroup.transform;
                }
                Selection.activeGameObject = holdingObject;
                //holdingObject.name = holdingPrefix + holdingObject.name;
                holdingObject.layer = LayerMask.NameToLayer(editLayer);
                CalculateGrid(holdingObject);
                PlaySoundPick();
            }

        }

        protected void CalculateGrid(GameObject go)
        {
            if (dynamicGrid)
            {
                if (go.TryGetComponent(out Collider col))
                {
                    if (col.enabled == false)
                    {
                        col.enabled = true;
                        gridSize = col.bounds.size;
                        col.enabled = false;
                    }
                    else
                    {
                        gridSize = col.bounds.size;
                    }
                }
                else
                {
                    gridSize = go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size;
                }
            }

        }

        protected void PlaySoundMovePlaneUp()
        {
            float pitch = ((float)(canvasPositionY + 1) / paintCanvasSize + 0.5f) * 0.6f;
            if (canvasAxis == CanvasAxis.X) pitch = ((float)(canvasPositionX + 1) / paintCanvasSize + 0.5f) * 0.6f;
            if (canvasAxis == CanvasAxis.Z) pitch = ((float)(canvasPositionZ + 1) / paintCanvasSize + 0.5f) * 0.6f;

            PlaySound(placeSound, pitch);
        }

        protected void PlaySoundMovePlaneDown()
        {
            float pitch = ((float)(canvasPositionY - 1) / paintCanvasSize + 0.5f) * 0.6f;
            if (canvasAxis == CanvasAxis.X) pitch = ((float)(canvasPositionX - 1) / paintCanvasSize + 0.5f) * 0.6f;
            if (canvasAxis == CanvasAxis.Z) pitch = ((float)(canvasPositionZ - 1) / paintCanvasSize + 0.5f) * 0.6f;

            PlaySound(placeSound, pitch);
        }

        protected void PlaySoundRotate()
        {
            PlaySound(placeSound, 3f);
        }

        protected void PlaySoundPaint()
        {
            PlaySound(placeSound, 1);
        }

        protected void PlaySoundDelete()
        {
            PlaySound(placeSound, 0.75f);
        }

        protected void PlaySoundPick()
        {
            PlaySound(placeSound, 1.5f);
        }

        protected void PlaySoundLineRectTool()
        {
            PlaySound(placeSound, 1.5f);
        }

        protected void PlaySoundLineRectDelete()
        {
            PlaySound(placeSound, 3f);
        }

        protected void PlaySound(AudioClip clip, float pitch)
        {
            if (!enableSounds) return;
            audio.pitch = pitch;
            audio.PlayOneShot(clip);
        }

        protected Transform GetObjectGroupFor(GameObject go)
        {
            string objectName = go.name;
            if (objectName.StartsWith(holdingPrefix))
            {
                objectName = objectName.Substring(holdingPrefix.Length);
            }
            // remove ending _<num> and (num) because thats not part of original name
            objectName = Regex.Replace(objectName, @"[( \(\d+\))(_\d+)+]", "");

            Transform objectGroup = groupRoot.transform.Find(objectGroupPrefix + objectName);
            if (objectGroup == null)
            {
                GameObject group = new GameObject(objectGroupPrefix + objectName);
                group.transform.parent = groupRoot.transform;
                objectGroup = group.transform;
                objectGroups.Add(objectGroup);
            }

            return objectGroup;
        }

        protected GameObject CreateNew(Vector3 pos)
        {
            GameObject newInstance = null;
            if (parkedPicked != null)
            {
                if (parkedPicked.IsPrefab())
                {
                    newInstance = PrefabUtility.InstantiatePrefab(parkedPicked.GetPrefabAsset()) as GameObject;
                }
                else
                {
                    newInstance = Instantiate(parkedPicked);
                    newInstance.name = newInstance.name.Remove(newInstance.name.IndexOf("(Clone)"));
                    newInstance.name = newInstance.name.Remove(newInstance.name.IndexOf(pickedPrefix), pickedPrefix.Length);
                }
            }
            else
            {
                if (selectedPrefab.IsPrefab())
                {
                    newInstance = PrefabUtility.InstantiatePrefab(selectedPrefab.GetPrefabAsset()) as GameObject;
                }
                else
                {
                    newInstance = Instantiate(selectedPrefab);
                    newInstance.name = newInstance.name.Remove(newInstance.name.IndexOf("(Clone)"));
                    //newInstance.name = newInstance.name.Remove(newInstance.name.IndexOf(pickedPrefix), pickedPrefix.Length);
                }

            }
            if (dynamicGrid)
            {
                CalculateGrid(newInstance);
                UpdateOffsetForObject(newInstance);
                UpdatePaintCanvasSize();
            }
            else if (paintCanvas.transform.position.x % gridSize.x != 0
               || paintCanvas.transform.position.y % gridSize.y != 0
               || paintCanvas.transform.position.y % gridSize.z != 0)
            {
                gridSize = Vector3.one;
                if (canvasAxis == CanvasAxis.X) canvasPositionX = canvasPositionX.GridPos(gridSize.x);
                if (canvasAxis == CanvasAxis.Y) canvasPositionY = canvasPositionY.GridPos(gridSize.y);
                if (canvasAxis == CanvasAxis.Z) canvasPositionZ = canvasPositionZ.GridPos(gridSize.z);
                UpdatePaintCanvasSize();
            }
            SetObjectPosition(newInstance, pos);

            Transform objectGroup = GetObjectGroupFor(newInstance);

            newInstance.transform.parent = objectGroup;
            Transform center = newInstance.transform.Find("Center");

            if (center != null)
            {
                startingEulerAngles = center.eulerAngles;
                center.eulerAngles += currentRotationDelta;
            }
            else
            {
                startingEulerAngles = newInstance.transform.eulerAngles;
                newInstance.transform.eulerAngles += currentRotationDelta;
            }
            newInstance.transform.localScale = currentScale;

            newInstance.name = holdingPrefix + newInstance.name;
            Selection.activeGameObject = newInstance;
            originalLayer = newInstance.layer;
            newInstance.layer = LayerMask.NameToLayer(editLayer);

            return newInstance;
        }

        protected bool IsCursorOnPlane(Event e)
        {
            RaycastHit hit;
            return RaycastCursor(e, out hit, (1 << paintCanvas.layer));
        }

        protected bool IsCursorInSceneView(Event e)
        {
            Vector2 pos = e.mousePosition;
            // If F is pressed to frame an object, the mouse position instantly jumps to a crazy negative value for some frames, which caused gop to thing the cursor is out of scene view wich results in destroying the object
            // this calculation fixes it
            //if (Vector2.SqrMagnitude(lastMousePos - mousePos) > 62500) return true;
            return (pos.x >= 0 && pos.x <= scene.position.width) &&
                (pos.y >= 0 && pos.y <= scene.position.height);
        }

        protected void SetCustomCursor(Event e)
        {
            if (!IsCursorInSceneView(e)) return;

            float ppp = EditorGUIUtility.pixelsPerPoint;

            float width = Screen.width * ppp;
            float height = Screen.height * ppp;
            if (scene != null)
            {
                width = scene.position.width;
                height = scene.position.height;
            }

            if (cursorHasNoSurface)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            if (control && !shift) // Delete Cursor
            {
                // Hide holding object
                if (holdingObject != null)
                {
                    holdingObject.SetActive(false);
                }

                if (mode == OperationMode.Freehand) Cursor.SetCursor(cursorDelete, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Line) Cursor.SetCursor(cursorLineDelete, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Rect) Cursor.SetCursor(cursorRectDelete, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Circle) Cursor.SetCursor(cursorCircleDelete, new Vector2(0, 31), CursorMode.Auto);
            }
            else if (!shift) // Set Paint Cursor
            {
                // Hide holding object
                if (rightDown || (isDragging && mode != OperationMode.Freehand))
                {
                    if (holdingObject != null) holdingObject.SetActive(false);
                }
                else if (holdingObject != null)
                {
                    if (holdingObject != null) holdingObject?.SetActive(true);
                }

                if (rightDown) Cursor.SetCursor(cursorPick, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Freehand) Cursor.SetCursor(cursor, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Line) Cursor.SetCursor(cursorLine, new Vector2(0, 31), CursorMode.Auto);
                else if (mode == OperationMode.Rect)
                {
                    if (fillRect)
                        Cursor.SetCursor(cursorRectFill, new Vector2(0, 31), CursorMode.Auto);
                    else
                        Cursor.SetCursor(cursorRect, new Vector2(0, 31), CursorMode.Auto);
                }
                else if (mode == OperationMode.Circle)
                {
                    //if (fillCircle)
                    //    Cursor.SetCursor(cursorCircleFill, new Vector2(0, 31), CursorMode.Auto);
                    //else
                    Cursor.SetCursor(cursorCircle, new Vector2(0, 31), CursorMode.Auto);
                }
                else if (mode == OperationMode.Scale)
                {
                    EditorGUIUtility.AddCursorRect(new Rect(0, 0, width, height), MouseCursor.ResizeHorizontal);
                }
            }

            if (shift) // Sliding Cursor
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, width, height), MouseCursor.ResizeVertical);
            }
            else // Set Custom Cursor
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, width, height), MouseCursor.CustomCursor);
            }
        }

        protected Ray CreateRay(Event e)
        {
            return scene.camera.ScreenPointToRay(GetMousePos(e));
        }
        protected Ray CreateRay(Vector2 cursorPos)
        {
            if (scene != null && scene.camera != null)
                return scene.camera.ScreenPointToRay(cursorPos);
            return new Ray();
        }

        public Vector2 GetMousePos(Event e)
        {
            Vector3 mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;

            return mousePos;
        }

        protected bool RaycastCursor(Event e, out RaycastHit hit, int layerMask = 0)
        {
            if (paintOnlyOnGround)
            {
                layerMask = (1 << paintCanvas.layer);
            }
            Ray ray = CreateRay(e);
            if (layerMask == 0)
            {
                return Physics.Raycast(ray, out hit, MAX_RAY_DIST);
            }
            else
            {
                return Physics.Raycast(ray, out hit, MAX_RAY_DIST, layerMask);
            }

        }

        protected GameObject RaycastFirst(Event e)
        {
            RaycastHit hit;
            GameObject go = null;

            int layerMask = ~((1 << paintCanvas.layer) | (1 << LayerMask.NameToLayer(editLayer)));
            if (hideCanvas == false)
            {
                layerMask = ~((1 << LayerMask.NameToLayer(editLayer)));
            }

            if (Physics.Raycast(CreateRay(e), out hit, MAX_RAY_DIST, layerMask))
            {
                if (hit.collider.gameObject != paintCanvas && !hit.collider.gameObject.TryGetComponent(out Terrain terry))
                {
                    go = hit.collider.gameObject;
                }
            }

            return GetObjectOrPrefabInstance(go);
        }

        protected Vector3 GetCursorRayPosPlane(Event e)
        {
            // Only raycast against the plane
            int planeLayerMask = (1 << paintCanvas.layer);
            RaycastHit hit;
            if (Physics.Raycast(CreateRay(e), out hit, MAX_RAY_DIST, planeLayerMask))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        protected Vector3 RaycastSurfaceNormal(Event e, int layerMask = 0)
        {
            RaycastHit hit;

            if (layerMask == 0)
            {
                layerMask = ~((1 << paintCanvas.layer) | (1 << LayerMask.NameToLayer(editLayer)));
                if (hideCanvas == false)
                {
                    layerMask = ~((1 << LayerMask.NameToLayer(editLayer)));
                }
            }

            if (Physics.Raycast(CreateRay(e), out hit, MAX_RAY_DIST, layerMask))
            {
                return hit.normal;
            }
            return Vector3.zero;
        }

        protected Vector3 RaycastSurfaceNormal(GameObject ignoreObject, Vector3 origin, Vector3 direction, int layerMask = 0)
        {
            if (layerMask == 0)
            {
                layerMask = ~((1 << LayerMask.NameToLayer(editLayer)));
            }

            RaycastHit[] hits = null;
            hits = Physics.RaycastAll(origin, direction, MAX_RAY_DIST, layerMask);
            RaycastHit nearestHit = new RaycastHit();
            float nearestDistance = 99999;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != ignoreObject && hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            if (nearestHit.collider != null && nearestHit.collider.gameObject != ignoreObject)
            {
                return nearestHit.normal;
            }
            return Vector3.zero;
        }

        public Vector3 GetCursorRayPosCanvas(Vector2 cursorPos)
        {
            if (paintCanvas == null) return Vector3.zero;
            // Only raycast against the plane
            int planeLayerMask = (1 << paintCanvas.layer);
            RaycastHit hit;
            if (Physics.Raycast(CreateRay(cursorPos), out hit, MAX_RAY_DIST, planeLayerMask))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        protected Vector3 GetCursorRayPosEverything(Event e)
        {
            // Only raycast against the plane
            int allExceptEditLayer = ~(1 << LayerMask.NameToLayer(editLayer));
            RaycastHit hit;
            if (Physics.Raycast(CreateRay(e), out hit, MAX_RAY_DIST, allExceptEditLayer))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        protected Vector3 GetCursorRayPosEverything(Vector2 cursorPos)
        {
            // Only raycast against the plane
            int allExceptEditLayer = ~(1 << LayerMask.NameToLayer(editLayer));
            RaycastHit hit;
            if (Physics.Raycast(CreateRay(cursorPos), out hit, MAX_RAY_DIST, allExceptEditLayer))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        protected List<GameObject> RaycastAll(Event e)
        {
            List<GameObject> list = new List<GameObject>();
            Ray ray = CreateRay(e);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, MAX_RAY_DIST))
            {
                list.Add(hit.collider.gameObject);
            }
            return list;
        }

        protected Vector3 GridPos(Vector3 pos)
        {
            return pos.GridPos(gridSize);
        }

        protected void OnDrawGizmos()
        {
            if (!active) return;

            if (circleBrush != null)
            {
                if (control)
                {
                    if (brushColorBefore != Color.red)
                    {
                        circleBrush.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                        brushColorBefore = Color.red;
                    }
                }
                else
                {
                    Color blue = new Color(0.15f, 0.15f, 1);
                    if (brushColorBefore != blue)
                    {
                        circleBrush.GetComponent<Renderer>().sharedMaterial.color = blue;
                        brushColorBefore = blue;
                    }
                }
            }

            if (isDragging && IsCursorOnPlane(lastEvent))
            {
                if (mode == OperationMode.Rect || mode == OperationMode.Circle) // there is no drawCircle method, so at least we draw a rect
                {
                    Vector3 start = GetCursorRayPosCanvas(lastMouseClickPos);
                    Vector3 end = GetCursorRayPosPlane(lastEvent);

                    Gizmos.color = Color.yellow;

                    if (canvasAxis == CanvasAxis.X)
                    {
                        Gizmos.DrawLine(start, new Vector3(end.x, start.y, end.z));
                        Gizmos.DrawLine(start, new Vector3(end.x, end.y, start.z));
                        Gizmos.DrawLine(new Vector3(start.x, start.y, end.z), end);
                        Gizmos.DrawLine(new Vector3(start.x, end.y, start.z), end);
                    }
                    else if (canvasAxis == CanvasAxis.Y)
                    {
                        Gizmos.DrawLine(start, new Vector3(end.x, start.y, start.z));
                        Gizmos.DrawLine(start, new Vector3(start.x, end.y, end.z));
                        Gizmos.DrawLine(new Vector3(end.x, start.y, start.z), end);
                        Gizmos.DrawLine(new Vector3(start.x, end.y, end.z), end);
                    }
                    else if (canvasAxis == CanvasAxis.Z)
                    {
                        Gizmos.DrawLine(start, new Vector3(end.x, start.y, end.z));
                        Gizmos.DrawLine(start, new Vector3(start.x, end.y, end.z));
                        Gizmos.DrawLine(new Vector3(end.x, start.y, end.z), end);
                        Gizmos.DrawLine(new Vector3(start.x, end.y, end.z), end);
                    }

                }

                else if (mode == OperationMode.Line)
                {
                    Vector3 startPoint = GetCursorRayPosCanvas(lastMouseClickPos);
                    Vector3 endPoint = GetCursorRayPosPlane(lastEvent);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(startPoint, endPoint);
                }
            }
        }
    }

#endif
    public interface GOComponent
    {
        void Register();
        void DeRegister();
    }
}