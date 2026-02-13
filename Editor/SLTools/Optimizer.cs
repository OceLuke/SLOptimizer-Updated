// The code was written and tested for Unity 2021.3.17f1
// Updated with null safety fixes

using SLTools.Utils;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SLTools
{
    public sealed class Optimizer : EditorWindow
    {
        private static Material _sharedRegular;
        private static Material _sharedTransparent;

        #region Validation

        [MenuItem("GameObject/SLTools/Optimize/Cube [Only Selected Objects]", true)]
        private static bool ValidateSelectedObjectIsCube()
        {
            return Selection.gameObjects != null &&
                   Selection.gameObjects.Length > 0 &&
                   Selection.gameObjects.All(MeshUtils.IsCube);
        }

        [MenuItem("GameObject/SLTools/Optimize/Cube [Only Childs In Selected Objects]", true)]
        private static bool ValidateAnyCubeInChild()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;

            var childGOs = GameObjectUtils.GetChildGameObjects(Selection.gameObjects, false);

            return childGOs != null &&
                   childGOs.Length > 0 &&
                   childGOs.Any(MeshUtils.IsCube);
        }

        #endregion

        #region Menu Actions

        [MenuItem("GameObject/SLTools/Optimize/Cube [Only Selected Objects]", false, -10)]
        public static void OptimizeSelectedCube()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return;

            foreach (var cubeGO in Selection.gameObjects.Where(MeshUtils.IsCube))
            {
                ProcessCube(cubeGO);
            }
        }

        [MenuItem("GameObject/SLTools/Optimize/Cube [Only Childs In Selected Objects]", false, -10)]
        public static void OptimizeChildCubes()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return;

            var cubes = GameObjectUtils
                .GetChildGameObjects(Selection.gameObjects, false)
                .Where(MeshUtils.IsCube)
                .ToArray();

            foreach (var cubeGO in cubes)
            {
                ProcessCube(cubeGO);
            }
        }

        #endregion

        #region Core Logic

        private static void ProcessCube(GameObject cubeGO)
        {
            if (cubeGO == null)
                return;

            var color = Color.white;
            var isCollidable = true;
            var isVisible = true;

            if (cubeGO.TryGetComponent<PrimitiveComponent>(out var primitiveComponent))
            {
                color = primitiveComponent.Color;
                isCollidable = primitiveComponent.Collidable;
                isVisible = primitiveComponent.Visible;
            }

            var quads = new GameObject[6];

            quads[0] = SpawnQuad(cubeGO.transform, Vector3.up, "up", color, isCollidable, isVisible);
            quads[1] = SpawnQuad(cubeGO.transform, Vector3.down, "down", color, isCollidable, isVisible);
            quads[2] = SpawnQuad(cubeGO.transform, Vector3.forward, "forward", color, isCollidable, isVisible);
            quads[3] = SpawnQuad(cubeGO.transform, Vector3.back, "back", color, isCollidable, isVisible);
            quads[4] = SpawnQuad(cubeGO.transform, Vector3.left, "left", color, isCollidable, isVisible);
            quads[5] = SpawnQuad(cubeGO.transform, Vector3.right, "right", color, isCollidable, isVisible);

            OptimizeCube(cubeGO);

            GameObjectUtils.SetParentForArray(quads, cubeGO.transform);
        }

        private static GameObject SpawnQuad(Transform cubeTransform, Vector3 side, string sideName,
            Color color, bool isCollidable, bool isVisible)
        {
            if (cubeTransform == null)
                return null;

            var rotation = Quaternion.LookRotation(side);
            rotation *= Quaternion.Euler(Vector3.up * 180f);

            var sideGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sideGO.name = $"{cubeTransform.name}_{sideName}";
            sideGO.SafeSetTag("Quad");

            var transform = sideGO.transform;
            transform.SetParent(cubeTransform);
            transform.localPosition = side * 0.5f;
            transform.localRotation = rotation;
            transform.localScale = Vector3.one;
            transform.SetParent(cubeTransform.parent);

            var collider = sideGO.GetComponent<MeshCollider>();
            if (collider != null)
                DestroyImmediate(collider);

            var primitiveComponent = sideGO.AddComponent<PrimitiveComponent>();
            primitiveComponent.Color = color;
            primitiveComponent.Collidable = isCollidable;
            primitiveComponent.Visible = isVisible;

            if (_sharedRegular == null)
                _sharedRegular = Resources.Load<Material>("Materials/Regular");

            if (_sharedTransparent == null)
                _sharedTransparent = Resources.Load<Material>("Materials/Transparent");

            var meshRenderer = sideGO.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var baseMat = color.a >= 1f ? _sharedRegular : _sharedTransparent;

                if (baseMat != null)
                {
                    meshRenderer.sharedMaterial = new Material(baseMat);
                    meshRenderer.sharedMaterial.color = color;
                }
                else
                {
                    Debug.LogWarning("Regular or Transparent material not found in Resources/Materials/");
                }
            }

            return sideGO;
        }

        private static void OptimizeCube(GameObject cubeGO)
        {
            if (cubeGO == null)
                return;

#if UNITY_2018_3_OR_NEWER
            if (PrefabUtility.IsAnyPrefabInstanceRoot(cubeGO))
            {
                PrefabUtility.UnpackPrefabInstance(
                    cubeGO,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
#endif

            var transform = cubeGO.transform;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            foreach (var component in cubeGO.GetComponents<Component>())
            {
                if (component is Transform)
                    continue;

                DestroyImmediate(component);
            }

            cubeGO.name += "_optimized";
        }

        #endregion
    }
}
