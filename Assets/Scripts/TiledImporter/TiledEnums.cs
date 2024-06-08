using LMCore.IO;
using System.Xml.Linq;
using UnityEngine;

namespace TiledImporter
{
    [SerializeField]
    public class TiledEnums
    {
        public SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>> StringEnums = new SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>>();
        public SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>> IntEnums = new SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>>();

        public static bool IsEnumProperty(XElement property) => property.GetAttribute("propertytype") != null && property.Element("properties") == null;

        public static bool IsIntEnum(XElement property) => IsEnumProperty(property) && property.GetAttribute("type") == "int";
        public static bool IsStringEnum(XElement property) => IsEnumProperty(property) && (property.GetAttribute("type") == null || property.GetAttribute("type") == "string");

        public TiledEnum<int> ParseIntEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!IntEnums.ContainsKey(typeName))
            {
                IntEnums.Add(typeName, new SerializableDictionary<int, TiledEnum<int>>());
            }

            var intEnum = IntEnums[typeName];

            var intValue = property.GetIntAttribute("value");

            if (!intEnum.ContainsKey(intValue))
            {
                intEnum.Add(intValue, new() { TypeName = typeName, Value = intValue });
            }

            return intEnum[intValue];
        }

        public TiledEnum<string> ParseStringEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!StringEnums.ContainsKey(typeName))
            {
                StringEnums.Add(typeName, new SerializableDictionary<string, TiledEnum<string>>());
            }

            var stringEnum = StringEnums[typeName];

            var stringValue = property.GetAttribute("value");

            if (!stringEnum.ContainsKey(stringValue))
            {
                stringEnum.Add(stringValue, new TiledEnum<string>() { TypeName = typeName, Value = stringValue });
            }
            return stringEnum[stringValue];
        }
    }
}
