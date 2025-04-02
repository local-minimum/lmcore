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
    public class TiledCustomProperties : TiledCustomClass
    {
        // TODO: Not Supported: file, object, enums
        public SerializableDictionary<string, TiledCustomClass> Classes;
        public TiledCustomClass Class(string key, TiledCustomClass defaultValue = null) =>
            Classes.ContainsKey(key) ? Classes[key] : defaultValue;


        private static new SerializableDictionary<string, T> FromFilter<T>(
            XElement properties,
            string type,
            bool includeMissingType = false
        ) => new SerializableDictionary<string, T>(
                FilterByNonEnumType(properties, type, includeMissingType)
                    .Select(property => new KeyValuePair<string, T>(property.GetAttribute("name"), Parse<T>(property)))
            );

        public static new TiledCustomProperties From(XElement properties, TiledEnums enums) => properties == null ? new TiledCustomProperties()
        {
            ClassType = "CustomProperties",
        } : new TiledCustomProperties()
        {
            ClassType = "CustomProperties",
            Strings = FromFilter<string>(properties, "string", true),
            Ints = FromFilter<int>(properties, "int"),
            Floats = FromFilter<float>(properties, "float"),
            Bools = FromFilter<bool>(properties, "bool"),
            Colors = FromFilter<Color>(properties, "color"),
            Classes = new(
                properties
                    .ElementsByStringAttribute("type", "class")
                    .Select(customClass => new KeyValuePair<string, TiledCustomClass>(
                        customClass.GetAttribute("name"),
                        TiledCustomClass.From(customClass, enums)))
            ),
            StringEnums = new SerializableDictionary<string, TiledEnum<string>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsStringEnum(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<string>>(
                        property.GetAttribute("name"),
                        enums.ParseStringEnum(property)))
            ),
            IntEnums = new SerializableDictionary<string, TiledEnum<int>>(
                properties
                    .Elements()
                    .Where(property => TiledEnums.IsIntEnum(property))
                    .Select(property => new KeyValuePair<string, TiledEnum<int>>(
                        property.GetAttribute("name"),
                        enums.ParseIntEnum(property)))
            )
        };
    }
}
