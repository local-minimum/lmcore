using LMCore.Extensions;
using LMCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledCustomClass
    {
        /// <summary>
        /// The Class field in Tiled
        /// </summary>
        public string ClassType;

        public SerializableDictionary<string, string> Strings;
        public SerializableDictionary<string, int> Ints;
        public SerializableDictionary<string, float> Floats;
        public SerializableDictionary<string, bool> Bools;
        public SerializableDictionary<string, Color> Colors;
        public SerializableDictionary<string, TiledEnum<string>> StringEnums;
        public SerializableDictionary<string, TiledEnum<int>> IntEnums;

        public override string ToString()
        {
            var parts = new List<string>()
            {
                string.Join("|", Strings.Select(kvp => $"{kvp.Key}=>'{kvp.Value}'")).DecorateNonEmpty("<STRINGS: ",">"),
                string.Join("|", Ints.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<INTS: ",">"),
                string.Join("|", Floats.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<FLOATS: ",">"),
                string.Join("|", Bools.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<BOOLS: ",">"),
                string.Join("|", Colors.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<COLORS: ",">"),
                string.Join("|", StringEnums.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<STR ENUMS: ",">"),
                string.Join("|", IntEnums.Select(kvp => $"{kvp.Key}=>{kvp.Value}")).DecorateNonEmpty("<INT ENUMS: ",">"),
            }.Where(p => !string.IsNullOrEmpty(p));

            return $"[CUSTOM PROPS: {string.Join(" ", parts)}]";
        }

        public string String(string key) => Strings.GetValueOrDefault(key);
        public string String(string key, string defaultValue) => Strings.ContainsKey(key) ? Strings[key] : defaultValue;

        public int Int(string key) => Ints.GetValueOrDefault(key);
        public int Int(string key, int defaultValue) => Ints.ContainsKey(key) ? Ints[key] : defaultValue;

        public float Float(string key) => Floats.GetValueOrDefault(key);
        public float Float(string key, float defaultValue) => Floats.ContainsKey(key) ? Floats[key] : defaultValue;

        public bool Bool(string key) => Bools.GetValueOrDefault(key);
        public bool Bool(string key, bool defaultValue) => Bools.ContainsKey(key) ? Bools[key] : defaultValue;

        public Color Color(string key) => Colors.GetValueOrDefault(key);
        public Color Color(string key, Color defaultValue) => Colors.ContainsKey(key) ? Colors[key] : defaultValue;

        protected static T Parse<T>(XElement property)
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

            throw new NotImplementedException($"Custom properties of type {t} not supported");
        }
        protected static IEnumerable<XElement> FilterByNonEnumType(
            XElement properties,
            string type,
            bool includeMissingAttribute
        ) =>
            properties.Elements().Where(element =>
                !TiledEnums.IsEnumProperty(element) &&
                (element.GetAttribute("type") == type || (includeMissingAttribute && element.GetAttribute("type") == null))
            );

        protected static SerializableDictionary<string, T> FromFilter<T>(
            XElement properties,
            string type,
            bool includeMissingType = false
        ) => new SerializableDictionary<string, T>(
                FilterByNonEnumType(properties, type, includeMissingType)
                    .Select(property => new KeyValuePair<string, T>(property.GetAttribute("name"), Parse<T>(property)))
            );


        public static TiledCustomClass From(XElement property, TiledEnums enums)
        {
            if (property == null) return new();

            var properties = property.Element("properties");
            var classType = property.GetAttribute("propertytype");

            if (properties == null) return new() { ClassType = classType };

            return new TiledCustomClass()
            {
                ClassType = classType,
                Strings = FromFilter<string>(properties, "string", true),
                Ints = FromFilter<int>(properties, "int"),
                Floats = FromFilter<float>(properties, "float"),
                Bools = FromFilter<bool>(properties, "bool"),
                Colors = FromFilter<Color>(properties, "color"),
                StringEnums = new SerializableDictionary<string, TiledEnum<string>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsStringEnum(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<string>>(property.GetAttribute("name"), enums.ParseStringEnum(property)))
            ),
                IntEnums = new SerializableDictionary<string, TiledEnum<int>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsIntEnum(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<int>>(property.GetAttribute("name"), enums.ParseIntEnum(property)))
            )
            };
        }
    }
}
