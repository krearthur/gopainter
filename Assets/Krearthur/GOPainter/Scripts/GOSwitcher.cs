#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Krearthur.GOP
{
    /// <summary>
    /// Provides switching prefabs automatically in a configured way.
    /// </summary>
    [ExecuteInEditMode]
    public class GOSwitcher : MonoBehaviour, GOComponent
    {
        protected GOPainter goPainter;   
        protected ObjectFactory factory;

        [Tooltip("Switches objects after painting to one of these object ids, referring to the ones in GOPainter 'Paint Objects' list.")]
        public int[] paintObjectIds = { 0, 1, 2, 3 };
        [SerializeField] protected int currentIndex;

        public enum Mode
        {
            Randomly,
            Sequentially
        }
        public Mode mode = Mode.Randomly;

        private void Start(){}

        public void Register()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted += SwitchPrefab;

            if (factory == null)
            {
                factory = GetComponent<ObjectFactory>();
            }

            factory.OnObjectProduced += SwitchProduced;

            currentIndex = 0;
            SceneView.duringSceneGui += OnScene;
        }

        public void DeRegister()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted -= SwitchPrefab;

            if (factory == null)
            {
                factory = GetComponent<ObjectFactory>();
            }

            factory.OnObjectProduced -= SwitchProduced;
            
            SceneView.duringSceneGui -= OnScene;
        }

        void SwitchPrefab(GameObject go, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            if (this != null && !enabled || goPainter.mode != GOPainter.OperationMode.Freehand)
            {
                return;
            }

            int newId = CalculateNextIndex();

            goPainter.SwitchPrefab(newId, false);
            
        }

        protected int CalculateNextIndex()
        {
            int newId = goPainter.PrefabID;
            if (mode == Mode.Randomly)
            {
                newId = paintObjectIds[UnityEngine.Random.Range(0, paintObjectIds.Length)];
            }
            else
            {

                if (currentIndex >= paintObjectIds.Length)
                {
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = (currentIndex + 1) % paintObjectIds.Length;
                }
                newId = paintObjectIds[currentIndex];
            }
            return newId;
        }

        void SwitchProduced(GameObject product, int index, int length)
        {
            if (!enabled) return;
            int newId = CalculateNextIndex();
            factory.SwapProductObject(index, goPainter.paintObjects[newId]);
        }

        private void OnScene(SceneView scene)
        {
            if (!enabled) return;
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            if (goPainter.active == false || !goPainter.showInfoBar) return;
            float offset = 0;
            if (TryGetComponent(out GORandomizer rand) && rand.enabled)
            {
                offset += 100;
            }
            if (TryGetComponent(out GOSurfaceSnapper grav) && grav.enabled)
            {
                offset += 100;
            }
            if (!goPainter.alignWithStroke && !goPainter.alignWithSurface)
            {
                offset -= 140;
            }
            Handles.BeginGUI();

            GUI.Box(new Rect(offset + 140, scene.position.height - 77, 100, 30), "");
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUI.Label(new Rect(offset + 155, scene.position.height - 70, 100, 30), "Switcher");

            Handles.EndGUI();
        }
    }

}
#endif