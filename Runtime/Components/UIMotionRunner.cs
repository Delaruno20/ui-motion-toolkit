using UnityEngine;

namespace UIMotionToolkit
{
    /// <summary>Hidden object ticking <see cref="UIMotion"/> on unscaled time in play mode (created on demand).</summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(1000)]
    internal sealed class UIMotionRunner : MonoBehaviour
    {
        private static UIMotionRunner s_instance;

        public static void Ensure()
        {
            if (s_instance != null)
                return;
            var go = new GameObject("[UIMotionToolkit]") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<UIMotionRunner>();
        }

        private void Update() => UIMotion.Tick(Time.unscaledDeltaTime);

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }
    }
}
