using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Sync
{
    public sealed class SceneSync
    {
        private string _pendingScene;
        private AsyncOperation _loading;
        private bool _isLoading;

        public string CurrentSceneName => SceneManager.GetActiveScene().name;
        public string PendingScene => _pendingScene;
        public bool IsLoading => _isLoading;

        public void RequestSceneLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (sceneName == CurrentSceneName) return;
            _pendingScene = sceneName;
        }

        public void Update()
        {
            if (_isLoading)
            {
                if (_loading != null && _loading.isDone)
                {
                    _isLoading = false;
                    _loading = null;
                }

                return;
            }

            if (!string.IsNullOrEmpty(_pendingScene) && _pendingScene != CurrentSceneName)
            {
                _isLoading = true;
                _loading = SceneManager.LoadSceneAsync(_pendingScene);
                _pendingScene = null;
            }
        }
    }
}
