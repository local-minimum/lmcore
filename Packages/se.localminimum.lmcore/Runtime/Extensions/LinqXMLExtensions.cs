using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class LinqXMLExtensions
    {
        #region Attributes
        public static bool AttributeIsIfExists(this XElement element, string attributeName, string expectedValue)
        {
            var value = element.GetAttribute(attributeName);
            return value == null || value == expectedValue;
        }

        public static string GetAttribute(this XElement element, string attributeName) =>
            element.Attribute(attributeName)?.Value;

        public static int GetIntAttribute(this XElement element, string attributeName) =>
            int.Parse(element.GetAttribute(attributeName));

        public static float GetFloatAttribute(this XElement element, string attributeName)
        {
            try
            {
                return (float)Decimal.Parse(element.GetAttribute(attributeName));
            }
            catch (FormatException)
            {
                var culture = new System.Globalization.CultureInfo("en-US");
                return (float)Decimal.Parse(element.GetAttribute(attributeName), culture);
            }
        }

        public static Color GetColorAttribute(this XElement element, string attributeName)
        {
            if (ColorUtility.TryParseHtmlString(element.GetAttribute(attributeName), out var color))
            {
                return color;
            }
            return Color.magenta;
        }

        public static bool GetBoolAttribute(this XElement element, string attributeName)
        {
            switch (element.GetAttribute(attributeName).Trim().ToLower())
            {
                case "":
                case "f":
                case "0":
                case "false":
                    return false;
                default:
                    return true;
            }
        }

        public static Vector2Int GetVector2IntAttribute(this XElement element, string xAttributeName, string yAttributeName) =>
            new Vector2Int(element.GetIntAttribute(xAttributeName), element.GetIntAttribute(yAttributeName));
        #endregion Attributes

        #region Elements
        public static IEnumerable<XElement> ElementsByStringAttribute(this XElement element, string attribute, string value) =>
            element.Elements().Where(child => child.GetAttribute(attribute) == value);

        public static IEnumerable<XElement> ElementsByName(this XElement element, string elementName) =>
            element.Elements().Where(child => child.Name == elementName);

        public static IEnumerable<T> HydrateElementsByName<T>(
            this XElement element,
            string elementName,
            System.Func<XElement, T> factory,
            System.Func<T, bool> filter
        ) =>
            element.ElementsByName(elementName).Select(factory).Where(filter);

        public static IEnumerable<T> HydrateElementsByName<T>(
            this XElement element,
            string elementName,
            System.Func<XElement, T> factory
        ) =>
            element.ElementsByName(elementName).Select(factory);

        #endregion Elements
    }
}
