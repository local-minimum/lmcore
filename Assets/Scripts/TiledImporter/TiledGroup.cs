using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

        public static Func<XElement, TiledGroup> fromFactory(TiledEnums enums, bool filterImport)
        {
            return (XElement group) => from(group, enums, filterImport);
        }

        public static TiledGroup from(XElement group, TiledEnums enums, bool filterImport) => group == null ? null : new TiledGroup()
        {
            Id = group.GetIntAttribute("id"),
            Name = group.GetAttribute("name"),
            Layers = group.HydrateElementsByName("layer", TiledLayer.fromFactory(enums), TiledLayer.ShouldBeImported(filterImport)).ToList(),
            Groups = group.HydrateElementsByName("group", fromFactory(enums, filterImport), ShouldBeImported(filterImport)).ToList(),
            CustomProperties = TiledCustomProperties.from(group.Element("properties"), enums),
        };
    }
}
