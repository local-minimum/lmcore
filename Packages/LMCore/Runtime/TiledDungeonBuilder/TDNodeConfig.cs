using LMCore.Extensions;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDNodeConfig
    {
        public Vector3Int Coordinates { get; set; }
        public TileModification[] Modifications { get; private set; }
        public TiledNodeRoofRule RoofRule { get; private set; }
        public TiledObjectLayer.Point[] Points { get; private set; }
        public TiledObjectLayer.Rect[] Rects { get; private set; }

        public TDNodeConfig(
            TDLayerConfig layerConfig, 
            Vector3Int coordinates,
            TiledNodeRoofRule roofRule
        )
        {
            Coordinates = coordinates;
            RoofRule = roofRule;
            Modifications = layerConfig.getModifications(coordinates).ToArray();

            var tileRect = layerConfig.InverseCoordinates(coordinates.To2DInXZPlane())
                .ToUnitRect();

            Points = layerConfig
                .ObjectLayers
                .SelectMany(l => l.Points)
                .Where(p => p.Applies(tileRect))
                .ToArray();

            Rects = layerConfig
                .ObjectLayers                
                .SelectMany(l => l.Rects)
                .Where(r => r.Applies(tileRect))
                .ToArray();
        }

        public TDNodeConfig(TileModification[] modifications, TiledObjectLayer.Point[] points, TiledObjectLayer.Rect[] rects)
        {
            Modifications = modifications;
            RoofRule = TiledNodeRoofRule.CustomProps;
            Points = points;
            Rects = rects;
        }

        public T FirstObjectPointValue<T>(string type, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(Points.FirstOrDefault(pt => pt.Type == type).CustomProperties);

        public T FirstObjectRectValue<T>(string type, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(Rects.FirstOrDefault(pt => pt.Type == type).CustomProperties);

        IEnumerable<TiledObjectLayer.TObject> TObjects => 
            Points.Select(pt => (TiledObjectLayer.TObject)pt).Concat(Rects);

        public T FirstObjectValue<T>(string type, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(TObjects.FirstOrDefault(o => o.Type == type)?.CustomProperties);

        public T FirstObjectValue<T>(System.Func<TiledObjectLayer.TObject, bool> filter, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(TObjects.FirstOrDefault(filter)?.CustomProperties);

        public TiledCustomProperties FirstObjectProps(System.Func<TiledObjectLayer.TObject, bool> filter) =>
            TObjects.FirstOrDefault(filter)?.CustomProperties;

        public IEnumerable<T> GetObjectValues<T>(string type, System.Func<TiledCustomProperties, T> predicate)
        {
            return TObjects
                .Where(pt => pt.Type == type)
                .Select(pt => predicate(pt.CustomProperties));
        }

        public IEnumerable<TiledCustomProperties> GetObjectProps(System.Func<TiledObjectLayer.TObject, bool> filter) =>
            TObjects.Where(filter).Select(o => o.CustomProperties);

        public bool HasObjectPoint(string type, System.Func<TiledCustomProperties, bool> predicate) =>
            Points.Any(pt => pt.Type == type && predicate(pt.CustomProperties));

        public bool HasObjectRect(string type, System.Func<TiledCustomProperties, bool> predicate) =>
            Rects.Any(pt => pt.Type == type && predicate(pt.CustomProperties));

        public bool HasObject(string type, System.Func<TiledCustomProperties, bool> predicate) =>
            HasObjectPoint(type, predicate) || HasObjectRect(type, predicate);
    }
}
