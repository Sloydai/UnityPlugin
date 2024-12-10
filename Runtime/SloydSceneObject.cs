using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Sloyd.WebAPI
{
    public class SloydSceneObject : MonoBehaviour
    {
        [SerializeField] private string _interactionId = string.Empty;
        [SerializeField] private string _modelName = string.Empty;
        private GltfImport _gltfImport;

        public string InteractionId
        {
            get => _interactionId;
            set
            {
                if (string.IsNullOrEmpty(_interactionId))
                {
                    _interactionId = value;
                }
            }
        }
        
        public string ModelName => _modelName;


        public void GenerateMeshCollider()
        {
            MeshCollider meshCollider = GetComponent<MeshCollider>();

            if (meshCollider == false)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = GetCombinedMesh();
        }

        private Mesh GetCombinedMesh()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            List<CombineInstance> combineInstances = new List<CombineInstance>(meshFilters.Length);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                
                if (mesh && mesh.vertexCount > 0)
                {
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = mesh;
                    combineInstance.transform = transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                    combineInstances.Add(combineInstance);
                }
            }
            
            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combineInstances.ToArray());
            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateBounds();
            return combinedMesh;
        }

        public static async Task<SloydSceneObject> Create(string prompt, SloydClientAPI.PromptModifier modifier = SloydClientAPI.PromptModifier.None)
        {
            SloydClientAPI.CreateResponse response = await SloydClientAPI.Create(prompt, modifier);

            if (response.Success == false)
            {
                return null;
            }

            SloydSceneObject sceneObject;
            
            if (Application.isPlaying)
            {
                sceneObject = new GameObject(nameof(SloydSceneObject)).AddComponent<SloydSceneObject>();
                await sceneObject.SpawnGlbModel(Convert.FromBase64String(response.ModelData), sceneObject.transform);
            }
#if UNITY_EDITOR
            else
            {
                string filePath = GetModelPath(response.Name);
                await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(response.ModelData));
                UnityEditor.AssetDatabase.Refresh();
                string fileName = Path.GetFileName(filePath);
                string assetPath = Path.Combine("Assets", SloydClientAPI.UserSettings.CachePath, fileName);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                GameObject model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                sceneObject = new GameObject(response.Name).AddComponent<SloydSceneObject>();
                model.transform.SetParent(sceneObject.transform);
            }
            
            UnityEditor.Selection.activeGameObject = sceneObject.gameObject;
#endif
            
            sceneObject._interactionId = response.InteractionId;
            sceneObject._modelName = response.Name;

            if (SloydClientAPI.UserSettings.AutoPlacement)
            {
                sceneObject.transform.position = AutoPlacementUtility.FindNearestFreeSpace(sceneObject.gameObject);
            }

            return sceneObject;
        }
        
        private static string GetModelPath(string name)
        {
            string fileName = $"{name}.glb";
            string filePath = Path.Combine(Application.dataPath, SloydClientAPI.UserSettings.CachePath, fileName);

            int index = 0;
            
            while (File.Exists(filePath))
            {
                fileName = $"{name} {++index}.glb";
                filePath = Path.Combine(Application.dataPath, SloydClientAPI.UserSettings.CachePath, fileName);
            }

            return filePath;
        }

        public async Task<string> Edit(string prompt)
        {
            SloydClientAPI.EditResponse response = await SloydClientAPI.Edit(InteractionId, prompt);
            
            if (response.Success == false)
            {
                return $"Error: {response.ErrorMessage}";
            }
            
            ClearChildren();
            
            if (Application.isPlaying)
            {
                await SpawnGlbModel(Convert.FromBase64String(response.ModelData), transform);
            }
#if UNITY_EDITOR
            else
            {
                string filePath = GetModelPath(_modelName);
                await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(response.ModelData));
                
                UnityEditor.AssetDatabase.Refresh();
                string fileName = Path.GetFileName(filePath);
                string assetPath = Path.Combine("Assets", SloydClientAPI.UserSettings.CachePath, fileName);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                GameObject model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                model.transform.SetParent(transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
#endif
            }

            return response.ResponseMessage;
        }

        private void ClearChildren()
        {
            _gltfImport?.Dispose();
            
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                GameObject child = transform.GetChild(i).gameObject;
                
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private async Task SpawnGlbModel(byte[] data, Transform parent)
        {
            _gltfImport = new GltfImport();
            bool success = await _gltfImport.LoadGltfBinary(data);
            if (success)
            {
                success = await _gltfImport.InstantiateMainSceneAsync(parent);

                if (success == false)
                {
                    Debug.LogError("Failed to instantiate gltf model.");
                }
            }
            else
            {
                Debug.LogError("Failed to load gltf model.");
            }
        }
    }
}