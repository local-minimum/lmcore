using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace TiledImporter
{
    [Serializable]
    public class TiledGroup
    {
        public string Name;
        public int Id;
        public List<TiledLayer> Layers;
        public List<TiledGroup> Groups;
        public TiledCustomProperties CustomProperties;

        public string LayerNames() => string.Join(", ", Layers.Select(layer => layer.Name));

        public static Func<TiledGroup, bool> ShouldBeImported(bool filterLayerImport) {
            return (TiledGroup group) => !filterLayerImport || (group?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false);
        }

        public static Func<XElement, TiledGroup> FromFactory(TiledEnums enums, bool filterImport)
        {
            return (XElement group) => From(group, enums, filterImport);
        }

        public static TiledGroup From(XElement group, TiledEnums enums, bool filterImport)
        {
            if (group == null) return null;

            return new TiledGroup()
            {
                Id = group.GetIntAttribute("id"),
                Name = group.GetAttribute("name"),
                Layers = group.HydrateElementsByName("layer", TiledLayer.fromFactory(enums), TiledLayer.ShouldBeImported(filterImport)).ToList(),
                Groups = group.HydrateElementsByName("group", FromFactory(enums, filterImport), ShouldBeImported(filterImport)).ToList(),
                CustomProperties = TiledCustomProperties.from(group.Element("properties"), enums),
            };
        }
    }
}
