using LMCore.IO;
using System;
using System.Xml.Linq;
using LMCore.Extensions;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledEnums
    {
        public SerializableDictionary<string, SerializableDictionary<string, TiledEnum<string>>> StringEnums = new ();
        public SerializableDictionary<string, SerializableDictionary<int, TiledEnum<int>>> IntEnums = new ();

        public static bool IsEnumProperty(XElement property) => property.GetAttribute("propertytype") != null && property.Element("properties") == null;

        public static bool IsIntEnum(XElement property) => IsEnumProperty(property) && property.GetAttribute("type") == "int";
        public static bool IsStringEnum(XElement property) => IsEnumProperty(property) && (property.GetAttribute("type") == null || property.GetAttribute("type") == "string");

        public TiledEnum<int> ParseIntEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!IntEnums.ContainsKey(typeName))
            {
                IntEnums.Add(typeName, new ());
            }

            var intEnum = IntEnums[typeName];

            var intValue = property.GetIntAttribute("value");

            if (!intEnum.ContainsKey(intValue))
            {
                intEnum.Add(intValue, new() { TypeName = typeName, Value = intValue });
            }

            return new() { TypeName = typeName, Value = intValue };
        }

        public TiledEnum<string> ParseStringEnum(XElement property)
        {
            var typeName = property.GetAttribute("propertytype");

            if (!StringEnums.ContainsKey(typeName))
            {
                StringEnums.Add(typeName, new ());
            }

            var stringEnum = StringEnums[typeName];

            var stringValue = property.GetAttribute("value");

            if (!stringEnum.ContainsKey(stringValue))
            {
                stringEnum.Add(stringValue, new () { TypeName = typeName, Value = stringValue });
            }
            return new () { TypeName = typeName, Value = stringValue };
        }
    }
}
