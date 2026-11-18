using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Campground.Editor
{
    [InitializeOnLoad]
    public static class CampgroundSceneView
    {
        private const string DisabledKey = "Campground.CloseUpClipping.Disabled";
        private struct OriginalClipping
        {
            public SceneView view;
            public bool dynamicClip;
            public bool occlusionCulling;
            public float nearClip;
            public float farClip;
        }
        private static readonly Dictionary<SceneView, OriginalClipping> originals =
            new Dictionary<SceneView, OriginalClipping>();

        static CampgroundSceneView()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAll;
            EditorApplication.delayCall += ApplyToOpenViews;
        }

        private static bool IsCampground()
        {
            return SceneManager.GetActiveScene().path.EndsWith("/CampgroundLayout.unity");
        }

        private static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (IsCampground()) ApplyToOpenViews();
            else RestoreAll();
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (IsCampground() && !SessionState.GetBool(DisabledKey, false)) Apply(view);
        }

        private static void ApplyToOpenViews()
        {
            if (!IsCampground() || SessionState.GetBool(DisabledKey, false)) return;
            foreach (SceneView view in SceneView.sceneViews) Apply(view);
        }

        private static void Apply(SceneView view)
        {
            if (view == null || originals.ContainsKey(view)) return;
            SceneView.CameraSettings settings = view.cameraSettings;
            originals.Add(view, new OriginalClipping
            {
                view = view,
                dynamicClip = settings.dynamicClip,
                occlusionCulling = settings.occlusionCulling,
                nearClip = settings.nearClip,
                farClip = settings.farClip
            });
            settings.dynamicClip = false;
            settings.nearClip = 0.03f;
            settings.farClip = 600f;
            settings.occlusionCulling = false;
            view.cameraSettings = settings;
            view.Repaint();
        }

        [MenuItem("Tools/Campground/Close-up Scene View")]
        public static void CloseUpView()
        {
            SessionState.SetBool(DisabledKey, false);
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) view = EditorWindow.GetWindow<SceneView>();
            Apply(view);
            view.in2DMode = false;
            view.orthographic = false;
            view.Repaint();
        }

        [MenuItem("Tools/Campground/Restore Previous Scene View Clipping")]
        public static void RestoreClipping()
        {
            SessionState.SetBool(DisabledKey, true);
            RestoreAll();
        }

        private static void RestoreAll()
        {
            foreach (OriginalClipping original in originals.Values)
            {
                if (original.view == null) continue;
                SceneView.CameraSettings settings = original.view.cameraSettings;
                settings.dynamicClip = original.dynamicClip;
                settings.nearClip = original.nearClip;
                settings.farClip = original.farClip;
                settings.occlusionCulling = original.occlusionCulling;
                original.view.cameraSettings = settings;
                original.view.Repaint();
            }
            originals.Clear();
        }
    }
}
