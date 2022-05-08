using UnityEngine.UIElements;

namespace UIElementsKits
{
    public static class UIElementExtension
    {
        public static void SetDisplay(this VisualElement ve, bool display)
        {
            ve.style.display = display
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        
        public static void SetTextLoading(this TextElement ve)
        {
            ve.text = "↻";
        }
    }
}