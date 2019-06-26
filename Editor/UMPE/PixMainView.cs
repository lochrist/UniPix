using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor
{
    internal class PixMainView : MainView
    {
        private const float kToolbarHeight = 40;
        private const float kStatusBarHeight = 20;

        private static readonly Vector2 kMinSize = new Vector2(300, 300);
        private static readonly Vector2 kMaxSize = new Vector2(10000, 10000);

        public static void Toggle(bool visible)
        {
            ContainerWindow mainContainerWindow = null;
            var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode != ShowMode.MainWindow)
                    continue;

                mainContainerWindow = window;
                break;
            }

            if (mainContainerWindow == null)
                return;

            var mainView = (PixMainView)mainContainerWindow.rootView;
            mainView.children[0].RemoveChild(0);
        }

        static PixMainView()
        {
            // EditorApplication.updateMainWindowTitle += SetWindowTitleForKids;
        }

        private static void SetWindowTitleForKids(ApplicationTitleDescriptor desc)
        {
            
        }

        [UsedImplicitly]
        internal void OnEnable()
        {
            SetMinMaxSizes(kMinSize, kMaxSize);
        }

        protected override void SetPosition(Rect newPos)
        {
            SetPositionOnly(newPos);
            if (children.Length == 0)
                return;

            children[0].position = new Rect(0, 0, newPos.width, newPos.height);
        }

        protected override void ChildrenMinMaxChanged()
        {
            if (children.Length != 3)
                return;
            var min = new Vector2(kMinSize.x, Mathf.Max(kMinSize.y, children[0].minSize.y));
            SetMinMaxSizes(min, kMaxSize);
        }
    }
}
