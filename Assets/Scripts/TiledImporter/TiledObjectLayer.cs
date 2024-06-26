using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using LMCore.Extensions;
using UnityEngine;

namespace TiledImporter
{
    [Serializable]
    public class TiledObjectLayer
    {

        public abstract class TObject
        {
            // TODO: Does not support rotation

            /// <summary>
            /// Object either has same position as point or point is inside object
            /// </summary>
            /// <param name="point">A position scaled by tile size</param>
            /// <returns></returns>
            public abstract bool Applies(Vector2Int point);

            /// <summary>
            /// Object either is a point inside rect or rect is inside object
            /// </summary>
            /// <param name="rect"></param>
            /// <returns></returns>
            public abstract bool Applies(RectInt rect);

            public TiledCustomProperties CustomProperties;

            public TObject(XElement tiledObject, TiledEnums enums)
            {
                CustomProperties = TiledCustomProperties.From(tiledObject.Element("properties"), enums);
            }


            public static IEnumerable<T> Hydrate<T>(
                XElement objectLayer,
                Func<XElement, bool> filter,
                Func<XElement, T> hydrater
            ) where T : TObject => objectLayer
                .Elements("object")
                .Where(filter)
                .Select(hydrater);
        }

        [Serializable]
        public class Rect : TObject
        {
            public UnityEngine.Rect Area;

            public Rect(XElement tiledObject, TiledEnums enums) : base(tiledObject, enums)
            {
                Area = new UnityEngine.Rect(
                    tiledObject.GetFloatAttribute("x"),
                    tiledObject.GetFloatAttribute("y"),
                    tiledObject.GetFloatAttribute("width"),
                    tiledObject.GetFloatAttribute("height")
                );
            }

            public override bool Applies(RectInt rect) => Area.Contains(rect);
            public override bool Applies(Vector2Int point) => Area.Contains(point);

            public static bool IsRect(XElement tiledObject) => !tiledObject
                .Elements()
                .Any(el => el.Name != "properties");
        }

        [Serializable]
        public class Point : TObject
        {
            public Vector2 Coordinates;

            public Point(XElement tiledObject, TiledEnums enums) : base(tiledObject, enums)
            {
                Coordinates = new Vector2(
                    tiledObject.GetFloatAttribute("x"),
                    tiledObject.GetFloatAttribute("y")
                );
            }

            public override bool Applies(RectInt rect) => rect.Contains(Coordinates);
            public override bool Applies(Vector2Int point) => point == Coordinates;

            public static bool IsPoint(XElement tiledObject) => tiledObject.Element("point") != null;
        }


        [Serializable]
        public class Ellipse: TObject
        {
            public Vector2 Center;
            public int Width;
            public int Height;

            public Ellipse(XElement tiledObject, TiledEnums enums) : base(tiledObject, enums)
            {
                Center = new Vector2(tiledObject.GetFloatAttribute("x"), tiledObject.GetFloatAttribute("y"));
                Width = tiledObject.GetIntAttribute("width");
                Height = tiledObject.GetIntAttribute("height");
            }

            public override bool Applies(Vector2Int point) =>
                Mathf.Pow(point.x - Center.x, 2) / Mathf.Pow(Width/2, 2) + Mathf.Pow(point.y - Center.y, 2) / Mathf.Pow(Height / 2, 2) <= 1;

            public override bool Applies(RectInt rect) => Applies(rect.min) && Applies(rect.max);

            public static bool IsEllipse(XElement tiledObject) => tiledObject.Element("ellipse") != null;
        }

        public string Name;
        public int Id;
        public TiledCustomProperties CustomProperties;
        public Point[] Points;
        public Rect[] Rects;
        public Ellipse[] Ellipses;

        public static Func<TiledObjectLayer, bool> ShouldBeImported(bool filterLayerImport) {
            return (TiledObjectLayer objectLayer) => !filterLayerImport || (objectLayer?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false);
        }

        public static Func<XElement, TiledObjectLayer> FromFactory(TiledEnums enums) { 
            return (XElement objectLayer) => From(objectLayer, enums);
        }

        public static TiledObjectLayer From(XElement objectLayer, TiledEnums enums)
        {
            if (objectLayer == null) return null;


            return new TiledObjectLayer()
            {
                Id = objectLayer.GetIntAttribute("id"),
                Name = objectLayer.GetAttribute("name"),
                CustomProperties = TiledCustomProperties.From(objectLayer.Element("properties"), enums),
                Points = TObject.Hydrate(objectLayer, Point.IsPoint, (el) => new Point(el, enums)).ToArray(),
                Rects = TObject.Hydrate(objectLayer, Rect.IsRect, (el) => new Rect(el, enums)).ToArray(),
                Ellipses = TObject.Hydrate(objectLayer, Ellipse.IsEllipse, (el) => new Ellipse(el, enums)).ToArray()
            };
        }

    }
}
