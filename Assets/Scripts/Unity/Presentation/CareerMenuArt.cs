using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Menu-only photography. Stable catalog IDs; no career or race state.</summary>
    public static class CareerMenuArt
    {
        public const string ResourceRoot = "Biathlon/UI/Art/CareerV2/";

        public static Image Create(string id, string name)
        {
            var texture = Resources.Load<Texture2D>(ResourceRoot + id);
            bool rifle = id.StartsWith("rifle_");
            var image = new Image
            {
                name = name, image = texture, scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            image.style.height = 220;
            image.style.width = Length.Percent(100);
            image.style.minWidth = 0;
            image.style.flexShrink = 0;
            image.style.backgroundColor = FrontEndTheme.Color("#CDE7F5");
            // Rifle photos have deliberate studio headroom; remove only that empty
            // backdrop so the complete barrel, sights and stock remain prominent.
            if (rifle) image.uv = new Rect(0, .23f, 1, .54f);
            // Keep the entire athlete/product visible at every supported viewport.
            image.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (texture == null || e.newRect.width <= 0) return;
                float height = e.newRect.width * texture.height / texture.width * (rifle ? .54f : 1);
                if (!id.StartsWith("training_")) height = Mathf.Min(rifle ? 220 : 200, height);
                if (Mathf.Abs(e.newRect.height - height) > .5f) image.style.height = height;
            });
            return image;
        }
    }

    public sealed partial class FrontEndMenu
    {
        private VisualElement ArtCard(VisualElement parent, string id, string artId, string artName, float width, bool selected)
        {
            var tile = Tile(parent, id);
            tile.style.width = Length.Percent(width);
            tile.style.paddingTop = tile.style.paddingBottom = tile.style.paddingLeft = tile.style.paddingRight = 0;
            tile.style.borderTopWidth = tile.style.borderBottomWidth = tile.style.borderLeftWidth = tile.style.borderRightWidth = 2;
            tile.Add(CareerMenuArt.Create(artId, artName));
            var body = new VisualElement { name = id + "_body" };
            body.style.paddingTop = body.style.paddingBottom = 16;
            body.style.paddingLeft = body.style.paddingRight = 18;
            body.style.flexGrow = 1;
            tile.Add(body);
            bool hover = false, focused = false;
            void UpdateBorder()
            {
                var color = HighContrast ? Color.white : selected || focused ? FrostlineStyle.Accent : hover ? FrostlineStyle.Muted : FrostlineStyle.Surface;
                tile.style.borderTopColor = tile.style.borderBottomColor = tile.style.borderLeftColor = tile.style.borderRightColor = color;
            }
            tile.RegisterCallback<PointerEnterEvent>(e => { hover = true; UpdateBorder(); });
            tile.RegisterCallback<PointerLeaveEvent>(e => { hover = false; UpdateBorder(); });
            tile.RegisterCallback<FocusInEvent>(e => { focused = true; UpdateBorder(); });
            tile.RegisterCallback<FocusOutEvent>(e => { focused = false; UpdateBorder(); });
            UpdateBorder();
            return body;
        }
    }
}
