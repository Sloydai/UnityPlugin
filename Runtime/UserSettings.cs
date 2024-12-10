using System;
using UnityEngine;

namespace Sloyd.WebAPI
{
    [CreateAssetMenu(fileName = AssetName, menuName = "Sloyd/UserSettings", order = 0)]
    public class UserSettings : ScriptableObject
    {
        public const string AssetName = "SloydUserSettings";
        
        public string ClientId = string.Empty;
        public string ClientSecret = string.Empty;

        public SloydClientAPI.PromptModifier DefaultPromptModifier = SloydClientAPI.PromptModifier.None;
        public bool AutoPlacement = true;
        public bool AutoGenerateMeshCollider = false;
        public bool EnableLogging = true;
        public string CachePath = string.Empty;
        
        [NonSerialized]
        public string Token = string.Empty;
    }
}