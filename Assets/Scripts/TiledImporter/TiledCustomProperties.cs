using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace TiledImporter
{
    [SerializeField]
    public class TiledCustomProperties
    {
        // Not Supported: file, object, enums
        public SerializableDictionary<string, string> Strings;
        public SerializableDictionary<string, int> Ints;
        public SerializableDictionary<string, float> Floats;
        public SerializableDictionary<string, bool> Bools;
        public SerializableDictionary<string, Color> Colors;
        public SerializableDictionary<string, TiledCustomProperties> Classes;
        public SerializableDictionary<string, TiledEnum<string>> StringEnums;
        public SerializableDictionary<string, TiledEnum<int>> IntEnums;

        private static IEnumerable<XElement> FilterByNonEnumType(
            XElement properties,
            string type,
            bool includeMissingAttribute
        ) =>
            properties.Elements().Where(element =>
                !TiledEnums.IsEnumProperty(element) &&
                (element.GetAttribute("type") == type || includeMissingAttribute && element.GetAttribute("type") == null)
            );

        private static T Parse<T>(XElement property, TiledEnums enums)
        {
            var t = typeof(T);

            if (t == typeof(string))
            {
                return (T)Convert.ChangeType(property.GetAttribute("value"), t);
            }

            if (t == typeof(int))
            {
                return (T)Convert.ChangeType(property.GetIntAttribute("value"), t);
            }

            if (t == typeof(float))
            {
                return (T)Convert.ChangeType(property.GetFloatAttribute("value"), t);
            }

            if (t == typeof(bool))
            {
                return (T)Convert.ChangeType(property.GetBoolAttribute("value"), t);
            }

            if (t == typeof(Color))
            {
                return (T)Convert.ChangeType(property.GetBoolAttribute("value"), t);
            }

            if (t == typeof(TiledCustomProperties))
            {
                return (T)Convert.ChangeType(from(property.Element("properties"), enums), t);
            }

            throw new NotImplementedException($"Custom properties of type {t} not supported");
        }

        private static SerializableDictionary<string, T> FromFilter<T>(
            XElement properties,
            string type,
            TiledEnums enums,
            bool includeMissingType = false
        ) => new SerializableDictionary<string, T>(
                FilterByNonEnumType(properties, type, includeMissingType)
                    .Select(property => new KeyValuePair<string, T>(property.GetAttribute("name"), Parse<T>(property, enums)))
            );

        public static TiledCustomProperties from(XElement properties, TiledEnums enums) => properties == null ? new TiledCustomProperties() : new TiledCustomProperties()
        {
            Strings = FromFilter<string>(properties, "string", enums, true),
            Ints = FromFilter<int>(properties, "int", enums),
            Floats = FromFilter<float>(properties, "float", enums),
            Bools = FromFilter<bool>(properties, "bool", enums),
            Colors = FromFilter<Color>(properties, "color", enums),
            StringEnums = new SerializableDictionary<string, TiledEnum<string>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsStringEnumProperty(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<string>>(property.GetAttribute("name"), enums.parseStringEnum(property)))
            ),
            IntEnums = new SerializableDictionary<string, TiledEnum<int>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsIntEnumProperty(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<int>>(property.GetAttribute("name"), enums.parseIntEnum(property)))
            )
        };
    }
}
