using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Sloyd.WebAPI
{
    public class SloydPluginWindow : EditorWindow
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string AssetsFolderPath = "Assets";

        private Dictionary<SloydClientAPI.PromptModifier, string> _promptModifierLabels = new()
        {
            { SloydClientAPI.PromptModifier.Texturing, "AI texture" },
            { SloydClientAPI.PromptModifier.ReColoring, "AI custom color" },
            { SloydClientAPI.PromptModifier.None, "Default color" },
        };

        private Dictionary<SloydClientAPI.AuthStatus, string> _authenticationStatusLabels = new()
        {
            { SloydClientAPI.AuthStatus.Authenticated, "Authenticated" },
            { SloydClientAPI.AuthStatus.NotAuthenticated, "Not Authenticated" },
            { SloydClientAPI.AuthStatus.TryingToAuthenticate, "Trying To Authenticate..." }
        };

        private UserSettings _userSettings;
        private string _promptText;
        private string _creationStatusMessage;

        private bool _activationSectionFoldout = true;
        private bool _promptSectionFoldout = true;
        private bool _settingsSectionFoldout = true;
        private GUIStyle _foldoutStyle;
        private GUIStyle _promptTipStyle;
        private GUIStyle _webLinkStyle;
        private string _promptTip;
        private string _generateKeyText;
        private string _trackUsageText;
        private string _docsText;

        private const string GenerateKeyLink = "app.sloyd.ai";
        private const string TrackUsageLink = "app.sloyd.ai";
        private const string DocsLink = "www.sloyd.ai/documentation";
        
        private static Texture2D _folderIcon;
        private static Texture2D _greenStatusIcon;
        private static Texture2D _yellowStatusIcon;
        private static Texture2D _redStatusIcon;
        private GUIStyle _statusIconStyle;
        private Vector2 _scrollPosition;


        [MenuItem("Tools/Sloyd Plugin", false, -100)]
        static void Initialize()
        {
            SloydPluginWindow window =
                (SloydPluginWindow)GetWindow(typeof(SloydPluginWindow), false, "Sloyd Plugin");
            window.Show();
        }

        private void OnEnable()
        {
            InitializeUserSettings();
            InitializeIcons();
            InitializeTextBlocks();
        }

        private void InitializeTextBlocks()
        {
            string webLinkColorHex = $"#{ColorUtility.ToHtmlStringRGB(new Color(0.564f, 0.627f, 0.984f))}";

            _promptTip =
                "Provide a simple description. Include the object name and one or two adjectives.\n\u2713 Weapons, buildings, furniture, props\nx  People, animals, scenes";

            _generateKeyText = $"Subscribe and generate key <color={webLinkColorHex}>{GenerateKeyLink}</color>";
            _trackUsageText = $"Track usage <color={webLinkColorHex}>{TrackUsageLink}</color>";
            _docsText = $"Help <color={webLinkColorHex}>{DocsLink}</color>";
        }

        private void InitializeIcons()
        {
            if (_folderIcon == null)
            {
                _folderIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon").image as Texture2D;
            }

            if (_greenStatusIcon == null)
            {
                _greenStatusIcon = EditorGUIUtility.IconContent("greenLight").image as Texture2D;
            }

            if (_redStatusIcon == null)
            {
                _redStatusIcon = EditorGUIUtility.IconContent("redLight").image as Texture2D;
            }

            if (_yellowStatusIcon == null)
            {
                _yellowStatusIcon = EditorGUIUtility.IconContent("orangeLight").image as Texture2D;
            }
        }

        private void InitializeStyles()
        {
            if (_foldoutStyle == null)
            {
                _foldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            if (_statusIconStyle == null)
            {
                _statusIconStyle = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(3, 3, 3, 3),
                    fixedWidth = EditorGUIUtility.singleLineHeight,
                    fixedHeight = EditorGUIUtility.singleLineHeight
                };
            }

            if (_promptTipStyle == null)
            {
                _promptTipStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontStyle = FontStyle.Italic
                };
            }

            if (_webLinkStyle == null)
            {
                _webLinkStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic,
                    richText = true
                };
            }
        }

        private void DrawBackgroundWithBorders(Rect rect, Color backgroundColor, Color topBorderColor,
            Color bottomBorderColor)
        {
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), topBorderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), bottomBorderColor);
        }

        private bool DrawFoldout(ref bool foldout, string label)
        {
            Rect foldoutRect = EditorGUILayout.GetControlRect();
            DrawBackgroundWithBorders(foldoutRect, new Color(0.3f, 0.3f, 0.3f), Color.black, Color.black);
            foldout = EditorGUI.Foldout(foldoutRect, foldout, label, true, _foldoutStyle);
            return foldout;
        }

        private void DrawHelpLinks()
        {
            string GetUrl(string link) => $"https://{link}";

            if (GUILayout.Button(_generateKeyText, _webLinkStyle))
            {
                Application.OpenURL(GetUrl(GenerateKeyLink));
            }

            if (GUILayout.Button(_trackUsageText, _webLinkStyle))
            {
                Application.OpenURL(GetUrl(TrackUsageLink));
            }

            if (GUILayout.Button(_docsText, _webLinkStyle))
            {
                Application.OpenURL(GetUrl(DocsLink));
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (DrawFoldout(ref _activationSectionFoldout, "Activation"))
            {
                EditorGUI.BeginChangeCheck();
                {
                    DrawCredentialsSection();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_userSettings);
                }
                
                DrawAuthenticationStatus();
                EditorGUILayout.Space();
                DrawHelpLinks();
                EditorGUILayout.Space();
            }

            if (DrawFoldout(ref _promptSectionFoldout, "AI Prompt"))
            {
                if (SloydClientAPI.AuthenticationStatus != SloydClientAPI.AuthStatus.Authenticated)
                {
                    GUI.enabled = false;
                }

                DrawPromptingSection();

                GUI.enabled = true;

                EditorGUILayout.Space();
            }

            if (DrawFoldout(ref _settingsSectionFoldout, "Asset Settings"))
            {
                EditorGUI.BeginChangeCheck();
                {
                    DrawFolderPathSection();
                    EditorGUILayout.Space();
                    DrawLoggingToggle();
                    DrawAutoPlacementToggle();
                    DrawAutoGenerationMeshColliderToggle();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_userSettings);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAuthenticationStatus()
        {
            GUILayout.BeginHorizontal();

            switch (SloydClientAPI.AuthenticationStatus)
            {
                case SloydClientAPI.AuthStatus.NotAuthenticated:
                {
                    GUILayout.Label(_redStatusIcon, _statusIconStyle);
                }
                    break;
                case SloydClientAPI.AuthStatus.TryingToAuthenticate:
                {
                    GUILayout.Label(_yellowStatusIcon, _statusIconStyle);
                }
                    break;
                case SloydClientAPI.AuthStatus.Authenticated:
                {
                    GUILayout.Label(_greenStatusIcon, _statusIconStyle);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUILayout.Label(_authenticationStatusLabels[SloydClientAPI.AuthenticationStatus]);
            GUILayout.EndHorizontal();
        }

        private void DrawCredentialsSection()
        {
            EditorGUILayout.Space();
            _userSettings.ClientId = EditorGUILayout.TextField("Client ID", _userSettings.ClientId);
            _userSettings.ClientSecret = EditorGUILayout.TextField("Client Secret", _userSettings.ClientSecret);

            if (SloydClientAPI.AuthenticationStatus is SloydClientAPI.AuthStatus.NotAuthenticated &&
                string.IsNullOrEmpty(_userSettings.ClientId) == false &&
                string.IsNullOrEmpty(_userSettings.ClientSecret) == false)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Authenticate();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            }
        }

        private async Task Authenticate()
        {
            await SloydClientAPI.Authenticate();
            Repaint();
        }

        private void DrawFolderPathSection()
        {
            EditorGUILayout.LabelField("Save downloaded models to:", EditorStyles.boldLabel);
            DrawFolderPickerControl(ref _userSettings.CachePath);
        }

        private void DrawLoggingToggle()
        {
            _userSettings.EnableLogging = EditorGUILayout.Toggle("Enable logging", _userSettings.EnableLogging);
        }

        private void DrawAutoPlacementToggle()
        {
            _userSettings.AutoPlacement = EditorGUILayout.Toggle("Enable auto placement", _userSettings.AutoPlacement);
        }
        
        private void DrawAutoGenerationMeshColliderToggle()
        {
            _userSettings.AutoGenerateMeshCollider = EditorGUILayout.Toggle("Generate MeshCollider", _userSettings.AutoGenerateMeshCollider);
        }

        private void DrawFolderPickerControl(ref string currentFolderPath)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_folderIcon, new GUIStyle(GUI.skin.button)
                    {
                        padding = new RectOffset(1, 2, 2, 2),

                        fixedWidth = EditorGUIUtility.singleLineHeight,
                        fixedHeight = EditorGUIUtility.singleLineHeight
                    }, GUILayout.Width(EditorGUIUtility.singleLineHeight),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Folder",
                    Path.Combine(Application.dataPath, currentFolderPath), "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        if (selectedPath.Length == Application.dataPath.Length)
                        {
                            currentFolderPath = string.Empty;
                        }
                        else
                        {
                            currentFolderPath = Path.GetRelativePath(Application.dataPath, selectedPath);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder",
                            "Please select a folder within the Assets directory.", "OK");
                    }
                }
            }

            string cachePath = string.IsNullOrEmpty(currentFolderPath)
                ? AssetsFolderPath
                : $"{AssetsFolderPath}/{currentFolderPath}";
            EditorGUILayout.LabelField(cachePath);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPromptingSection()
        {

            EditorGUILayout.LabelField(_promptTip, _promptTipStyle);
            EditorGUILayout.Space();
            _promptText = EditorGUILayout.TextArea(_promptText, GUILayout.Height(35));

            if (GUILayout.Button("Create"))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                RequestCreate();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            if (string.IsNullOrEmpty(_creationStatusMessage) == false)
            {
                EditorGUILayout.HelpBox(_creationStatusMessage, MessageType.None);
            }
            EditorGUILayout.Space();

            int selectedIndex = _promptModifierLabels.Keys.ToList().IndexOf(_userSettings.DefaultPromptModifier);

            GUILayout.Label("Material:");
            selectedIndex = GUILayout.SelectionGrid(
                selectedIndex,
                _promptModifierLabels.Values.ToArray(),
                1,
                EditorStyles.radioButton
            );

            _userSettings.DefaultPromptModifier = _promptModifierLabels.Keys.ElementAt(selectedIndex);
        }

        private async Task RequestCreate()
        {
            _creationStatusMessage = "Creating model...";
            SloydSceneObject sceneObject =
                await SloydSceneObject.Create(_promptText, _userSettings.DefaultPromptModifier);

            if (sceneObject == null)
            {
                _creationStatusMessage = "An error occured. See Console";
            }
            else
            {
                _creationStatusMessage = $"Model \"{sceneObject.ModelName}\" created";
            }
            
            Repaint();
        }

        private void InitializeUserSettings()
        {
            if (_userSettings == null)
            {
                _userSettings = Resources.Load<UserSettings>(UserSettings.AssetName);

                if (_userSettings == null)
                {
                    UserSettings userSettings = ScriptableObject.CreateInstance<UserSettings>();
                    string path = Path.Combine(ResourcesFolderPath, $"{UserSettings.AssetName}.asset");

                    InitializeResourcesFolder();

                    AssetDatabase.CreateAsset(userSettings, path);
                    AssetDatabase.SaveAssets();
                    _userSettings = (UserSettings)AssetDatabase.LoadAssetAtPath(path, typeof(UserSettings));
                }
            }
        }

        public static void InitializeResourcesFolder()
        {
            if (AssetDatabase.IsValidFolder(ResourcesFolderPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.Refresh();
            }
        }
    }
}