using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sloyd.WebAPI
{
    public static class SloydClientAPI
    {
        private const string AuthUrl = "https://api.sloyd.ai/authenticate";
        private const string CreateUrl = "https://api.sloyd.ai/create";
        private const string EditUrl = "https://api.sloyd.ai/edit";


        public static UserSettings UserSettings
        {
            get
            {
                if (_userSettings == null)
                {
                    _userSettings = Resources.Load<UserSettings>(UserSettings.AssetName);
                }

                return _userSettings;
            }
        }

        private static UserSettings _userSettings;

        public static AuthStatus AuthenticationStatus { get; private set; } =
            AuthStatus.NotAuthenticated;


        private static async Task<T> PostRequest<T>(string url, object body) where T : ISloydAPIResponse
        {
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            string jsonBody = JsonUtility.ToJson(body);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation webRequestOperation = request.SendWebRequest();

            while (webRequestOperation.isDone == false)
            {
                await Task.Yield();
            }

            T response;

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError
                or UnityWebRequest.Result.DataProcessingError)
            {
                response = default;

                if (response != null)
                {
                    response.Success = false;
                    response.ErrorMessage = request.error;
                }
            }
            else
            {
                response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                response.Success = true;
            }

            request.Dispose();
            return response;
        }

        public static async Task Authenticate()
        {
            if (AuthenticationStatus is AuthStatus.Authenticated or AuthStatus.TryingToAuthenticate)
            {
                return;
            }

            Log("Authentication started...");
            AuthenticationStatus = AuthStatus.TryingToAuthenticate;

            AuthenticationRequest request = new AuthenticationRequest()
            {
                ClientId = UserSettings.ClientId,
                ClientSecret = UserSettings.ClientSecret
            };

            AuthenticationResponse response = await PostRequest<AuthenticationResponse>(AuthUrl, request);
            if (response.Success)
            {
                UserSettings.Token = response.token;
                AuthenticationStatus = AuthStatus.Authenticated;
                Log("Authentication succeeded");
            }
            else
            {
                AuthenticationStatus = AuthStatus.NotAuthenticated;
                Log($"<b>Authentication Error:</b> {response.ErrorMessage}", LogType.Error);
            }
        }
        

        private static void Log(string message, LogType logType = LogType.Log)
        {
            if (UserSettings.EnableLogging == false)
            {
                return;
            }
            
            const string messagePrefix = "<color=cyan>[Sloyd]</color>";
            Debug.unityLogger.Log(logType, $"{messagePrefix} {message}");
        }

        public static async Task<CreateResponse> Create(string prompt, PromptModifier modifier = PromptModifier.None)
        {
            await Authenticate();
            CreateRequest createRequest = new CreateRequest()
            {
                Prompt = prompt,
                Token = UserSettings.Token,
                AiPromptModifiers = modifier.ToString(),
                ModelOutputType = "glb"
            };
            
            Log($"<b>Create</b> request started...\nPrompt: {prompt}");

            CreateResponse response = await PostRequest<CreateResponse>(CreateUrl, createRequest);

            if (response.Success)
            {
                Log($"<b>Create</b> response is received\nAI Confidence Score: {response.ConfidenceScore}");
            }
            else
            {
                Log($"<b>Create</b> request error: {response.ErrorMessage}", LogType.Error);
            }

            return response;
        }

        public static async Task<EditResponse> Edit(string interactionId, string prompt)
        {
            await Authenticate();

            EditRequest editRequest = new EditRequest()
            {
                Prompt = prompt,
                Token = UserSettings.Token,
                InteractionId = interactionId,
                ModelOutputType = "glb"
            };
            
            Log($"<b>Edit</b> request started...\nPrompt: {prompt}");
            
            EditResponse response = await PostRequest<EditResponse>(EditUrl, editRequest);

            if (response.Success)
            {
                LogType logType = LogType.Log;

                if (response.ResponseType is EditResponseType.WARNING)
                {
                    logType = LogType.Warning;
                }
                else if (response.ResponseType is EditResponseType.ERROR)
                {
                    logType = LogType.Error;
                }
                
                Log($"<b>Edit</b> response is received\nResponse message: {response.ResponseMessage}", logType);
            }
            else
            {
                Log($"<b>Edit</b> request error: {response.ErrorMessage}", LogType.Error);
            }
            
            return response;
        }

        #region DataTypes


        private interface ISloydAPIResponse
        {
            bool Success { get; set; }
            string ErrorMessage { get; set; }
        }

        [Serializable]
        private struct AuthenticationRequest
        {
            public string ClientId;
            public string ClientSecret;
        }

        [Serializable]
        private struct AuthenticationResponse : ISloydAPIResponse
        {
            public bool isAuthenticated;
            public string token;
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        [Serializable]
        private struct CreateRequest
        {
            public string Token;
            public string Prompt;
            public string AiPromptModifiers;
            public string ModelOutputType;
        }

        [Serializable]
        private struct EditRequest
        {
            public string Token;
            public string Prompt;
            public string InteractionId;
            public string ModelOutputType;
        }

        [Serializable]
        public struct CreateResponse : ISloydAPIResponse
        {
            public string InteractionId;
            public string Name;
            public float ConfidenceScore;
            public string ResponseEncoding;
            public string ModelOutputType;
            public string ModelData; // Base64
            public string ThumbnailPreviewExportType;
            public string ThumbnailPreview; // Base64
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        [Serializable]
        public struct EditResponse : ISloydAPIResponse
        {
            public string InteractionId;
            public EditResponseType ResponseType;
            public string ResponseMessage;
            public string ModelOutputType;
            public string ModelData; // Base64
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        [Serializable]
        public enum EditResponseType
        {
            NONE,
            INFO,
            WARNING,
            ERROR
        }

        [Serializable]
        public enum PromptModifier
        {
            None = 0,
            ReColoring = 1,
            Texturing = 2
        }
        
        public enum AuthStatus
        {
            NotAuthenticated,
            TryingToAuthenticate,
            Authenticated
        }

        #endregion
    }
}