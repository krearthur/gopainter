#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// Provides extra features via Context Menu under /GameObject.
    /// </summary>
    public class GOPainterMenu : MonoBehaviour
    {
    
        [MenuItem("GameObject/GO Painter/Run Physics On Selected", false, 49)]
        static void RunPhysicsOnSelected()
        {
            if (Selection.activeGameObject != null)
            {
                PhysicsSimulation.RunSimulation(Selection.gameObjects);
            }
        
            Selection.activeGameObject = null;
            Selection.activeObject = null;
        }

        [MenuItem("GameObject/GO Painter/Run Physics On Selected", true)]
        static bool ValidateRunPhysicsOnSelected()
        {
            return Selection.gameObjects != null && Selection.activeGameObject != null;
        }    
    
        [MenuItem("GameObject/GO Painter/Undo Physics Sim", false, 50)]
        static void ResetPhysicsOnSelected()
        {
            PhysicsSimulation.ResetAllBodies();
        }


    }

}
#endif