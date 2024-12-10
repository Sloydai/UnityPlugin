using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Sloyd.WebAPI
{
    [CustomEditor(typeof(SloydSceneObject))]
    public class SloydSceneObjectEditor : Editor
    {
        private SloydSceneObject _target;
        private string _promptText;
        private string _editingStatusMessage;

        private void OnEnable()
        {
            _target = (SloydSceneObject)target;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.TextField(nameof(_target.InteractionId), _target.InteractionId);
            EditorGUILayout.TextField(nameof(_target.ModelName), _target.ModelName);
            GUI.enabled = true;
            EditorGUILayout.Space();
            GUILayout.Label("Edit with AI:", EditorStyles.boldLabel);
            _promptText = EditorGUILayout.TextArea(_promptText, GUILayout.Height(35));

            if (GUILayout.Button("Edit"))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                RequestEdit();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            
            if (string.IsNullOrEmpty(_editingStatusMessage) == false)
            {
                EditorGUILayout.HelpBox(_editingStatusMessage, MessageType.None);
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate MeshCollider"))
            {
                _target.GenerateMeshCollider();
            }
        }
        
        private async Task RequestEdit()
        {
            _editingStatusMessage = "Editing model...";
            string responseMessage = await _target.Edit(_promptText);
            _editingStatusMessage = $"AI response:\n{responseMessage}";
        }
    }
}