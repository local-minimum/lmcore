using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.DungeonFeatures;

namespace LMCore.TiledDungeon
{
    public static class TDNodeGenerator
    {
        static GameObject ConfigurePotentiallyRotated(TDNode node, string className, string rotationClassName, TDNodeConfig config, out Direction direction)
        {
            if (config.Modifications.Any(mod => mod.Tile.Type == className))
            {
                direction = config.FirstObjectValue(
                    rotationClassName, 
                    props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                );
                if (direction == Direction.None)
                {
                    direction = config.FirstObjectValue(
                        className, 
                        props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                    );
                }

                if (direction == Direction.None)
                {
                    node.Log($"Getting Un-Rotated version of {className}", Debug.Log);
                    return node.Dungeon.Style.Get(
                        node.transform,
                        className,
                        node.NodeStyle);
                } else
                {
                    node.Log($"Getting Rotated {direction} version of {className}", Debug.Log);
                    return node.Dungeon.Style.Get(
                        node.transform,
                        className,
                        direction,
                        node.NodeStyle);

                }
            }

            direction = Direction.None;
            return null;
        }

        static void ConfigureContainer(TDNode node, GameObject tile, Direction anchor, Direction facingDirection, string containerClass, TDNodeConfig nodeConfig, bool blockingPassage)
        {
            if (tile == null) return;

            var container = tile.GetComponent<TDContainer>();

            if (container == null) {
                // Only error if it seems Tiled assumes there should be a chest
                if (facingDirection != Direction.None)
                {
                    node.Log("Container lacks script to configure", Debug.LogError);
                }
                return;
            };

            container.Configure(
                nodeConfig, 
                node.Coordinates, 
                anchor,
                facingDirection, 
                containerClass, 
                node.modifications, 
                blockingPassage);

        }

        static void ConfigureChest(TDNode node, TDNodeConfig nodeConfig)
        {
            var chest = ConfigurePotentiallyRotated(
                node,
                TiledConfiguration.instance.ChestClass,
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig,
                out Direction direction);

            ConfigureContainer(node, chest, Direction.Down, direction, TiledConfiguration.instance.ChestClass, nodeConfig, true);
        }

        static void ConfigurePedistal(TDNode node, TDNodeConfig nodeConfig)
        {
            var pedistal = ConfigurePotentiallyRotated(
                node,
                TiledConfiguration.instance.PedistalClass, 
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig, 
                out Direction direction
            );
            ConfigureContainer(node, pedistal, Direction.Down, direction, TiledConfiguration.instance.PedistalClass, nodeConfig, true);
        }

        static void ConfigurePillar(TDNode node, TDNodeConfig config) => ConfigurePotentiallyRotated(
            node,
            TiledConfiguration.instance.PillarClass, 
            TiledConfiguration.instance.PillarClass, 
            config, 
            out Direction _);

        static void ConfigureWallButtons(TDNode node)
        {
            var button = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.WallButtonClass);

            if (button == null) return;

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.WallButtonClass,
                button.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection(),
                node.NodeStyle
            );

            if (go == null) return;

            go.GetComponent<TDActuator>()?.Configure(node);
        }

        static void ApplyAnchorRotation(GameObject obj, Direction direction)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = direction.AsYRotation();
        }

        static void ApplyAnchorRotation(GameObject obj, TDEnumOrientation orientation)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = orientation == TDEnumOrientation.Horizontal ? AnchorYRotation.CW : AnchorYRotation.None;
        }

        static bool ConfigureRamps(TDNode node)
        {
            var ramp = node.RampModification;
            if (ramp == null) return false;

            var downSlopeDirection = ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection();

            var go = node.Dungeon.Style.Get(
                node.transform, 
                TiledConfiguration.instance.RampClass, 
                ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey),
                downSlopeDirection,
                node.NodeStyle
            );

            ApplyAnchorRotation(go, downSlopeDirection.Inverse());

            return true;
        }

        static void ConfigureTeleporter(TDNode node)
        {
            var teleporterMod = node.modifications.FirstOrDefault(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

            if (teleporterMod != null)
            {
                node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.TeleporterClass,
                    teleporterMod.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey),
                    node.NodeStyle
                    );
                
                node.Log($"Teleporter Entry({node.HasActiveTeleporter}) Id({node.TeleporterWormholdId})", Debug.Log);
            }
        }

        static void ConfigureLadders(TDNode node)
        {
            foreach (var tdDirection in TDEnumDirectionExtensions.PlanarDirections)
            {
                var direction = tdDirection.AsDirection();
                if (!node.HasLadder(direction)) continue;

                var dungeon = node.Dungeon;
                GameObject go = null;

                var upCoordinates = Direction.Up.Translate(node.Coordinates);
                if (!node.sides.Has(Direction.Up) && dungeon.HasNodeAt(upCoordinates))
                {
                    var aboveNode = dungeon[upCoordinates];
                    var upForwardCoordinates = direction.Translate(upCoordinates);
                    if (!aboveNode.sides.Has(direction) && !aboveNode.sides.Down && dungeon.HasNodeAt(upForwardCoordinates))
                    {
                        var upForwardNode = dungeon[upForwardCoordinates];
                        if (upForwardNode.sides.Down)
                        {
                            go = node.Dungeon.Style.Get(
                                node.transform,
                                TiledConfiguration.instance.LadderTopClass,
                                TiledConfiguration.instance.LadderClass,
                                direction,
                                node.NodeStyle
                            );
                        }
                    }
                }
                if (go == null)
                {
                    go = node.Dungeon.Style.Get(
                        node.transform,
                        TiledConfiguration.instance.LadderClass,
                        direction,
                        node.NodeStyle
                    );
                }

                ApplyAnchorRotation(go, direction);
            }
        }

        static void ConfigureDoors(TDNode node)
        {

            var doorInfo = node.modifications.FirstOrDefault(TDNode.DoorFilter);
            if (doorInfo == null) return;

            node.Dungeon.Style.Get(
                node.transform, 
                TiledConfiguration.instance.DoorClass, 
                doorInfo.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                doorInfo.Tile.CustomProperties.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Closed),
                node.NodeStyle
            );

            node.Door?.Configure(
                node,
                node.Coordinates, 
                node.modifications.Where(TDNode.DoorFilter).ToArray()
            );
        }

        static void ConfigureObstructions(TDNode node)
        {
            var obstruction = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.ObstructionClass);
            if (obstruction == null) return;

            node.Dungeon.Style.Get(
                node.transform, 
                TiledConfiguration.instance.ObstructionClass, 
                obstruction.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                node.NodeStyle
            );
        }

        static void ConfigureGrates(TDNode node)
        {
            var grate = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.GrateClass);
            if (grate == null) return;

            node.Dungeon.Style.Get(
                node.transform, 
                TiledConfiguration.InstanceOrCreate().GrateClass, 
                grate.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                node.NodeStyle
            );
        }

        static void ConfigureMovingPlatform(TDNode node, GameObject floor)
        {
            var mover = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.MovingPlatformClass);
            if (mover == null) { return; }

            var conf = node.Dungeon.GetNodeConfig(node.Coordinates);
            var platform = floor.AddComponent<TDMovingPlatform>();
            platform.Configure(conf);
        }

        static bool ConfigureWallSpike(TDNode node, Direction direction)
        {
            var spikes = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.WallSpikeTrapClass);

            if (spikes == null || spikes.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() != direction) return false; 

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.WallSpikeTrapClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return false;

            go.GetComponent<TDSpikeTrap>()?.Configure(node, node.Coordinates, node.modifications);

            ApplyAnchorRotation(go, direction);
            return true;
        }

        static void ConfigureIllusory(TDNode node, Direction direction)
        {
            node.Log($"I have illusionary wall {direction}", Debug.Log);

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().IllusoryTileClass,
                direction,
                node.NodeStyle
            );

            go.GetComponent<TDIllusoryCubeSide>().Configure(direction);

            ApplyAnchorRotation(go, direction);
        }

        static void ConfigureCube(TDNode node)
        {
            if (node.sides == null)
            {
                node.Log($"{node.tile} lacks a sides class, can't be used for layouting", Debug.LogError);
                return;
            }

            var Dungeon = node.Dungeon;
            var hasTrapDoor = node.HasTrapDoor;
            var aboveNode = node.Coordinates + Vector3Int.up;
            var illusionMods = node.modifications.Where(TDNode.IllusoryFilter).ToList();

            foreach (var direction in DirectionExtensions.AllDirections)
            {
                var upNode = direction == Direction.Up && Dungeon.HasNodeAt(aboveNode) ? Dungeon[aboveNode] : null;

                if (direction == Direction.Down)
                {
                    if (hasTrapDoor)
                    {
                        var orientation = node.TrapdoorModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey);
                        var trapdoor = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.TrapDoorClass,
                            orientation,
                            node.NodeStyle
                        );

                        if (trapdoor != null)
                        {
                            trapdoor.name = $"TrapDoor ({direction})";

                            var door = trapdoor.GetComponent<TDDoor>();

                            door?.Configure(
                                node,
                                node.Coordinates,
                                node.modifications.Where(TDNode.DoorFilter).ToArray()
                            );

                            ApplyAnchorRotation(trapdoor, orientation);
                            continue;
                        }
                    }
                    else if (node.HasPressurePlate)
                    {
                        var plate = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.PressurePlateClass,
                            node.NodeStyle
                        );

                        if (plate != null)
                        {
                            plate.name = $"Pressure Plate ({direction})";

                            var pressurePlate = plate.GetComponent<TDActuator>();

                            pressurePlate?.Configure(node);

                            ApplyAnchorRotation(plate, direction);
                            continue;
                        }
                    }
                    else if (ConfigureRamps(node))
                    {
                        continue;
                    }
                    else if (node.HasStairs)
                    {
                        var stairsDirection = node.StairsModification.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey).AsDirection();
                        var stairs = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.StairsClass,
                            stairsDirection,
                            node.NodeStyle
                        );
                        if (stairs != null)
                        {
                            stairs.name = $"TrapDoor UpIs({stairsDirection})";

                            ApplyAnchorRotation(stairs, stairsDirection);
                            continue;
                        }
                    }
                } else if (direction == Direction.Up)
                {
                    if (node.sides.Up && upNode)
                    {
                        if (upNode.HasIllusion(Direction.Down)) {
                            ConfigureIllusory(node, Direction.Up);
                            continue;
                        }

                    }
                }

                if (illusionMods.Any(mod => mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection() == direction))
                {
                    ConfigureIllusory(node, direction);
                    continue;
                }

                if (!node.sides.Has(direction)) continue;

                if (node.HasSpikes(direction))
                {
                    var spikes = Dungeon.Style.Get(
                        node.transform,
                        TiledConfiguration.instance.SpikeTrapClass,
                        direction,
                        node.NodeStyle
                    );

                    if (spikes != null)
                    {
                        spikes.name = $"Spikes ({direction})";

                        spikes.GetComponent<TDSpikeTrap>()?.Configure(node, node.Coordinates, node.modifications);

                        // Spikes have no rotation so hopefully sentinells are like they should be
                        // ApplyAnchorRotation(spikes, direction);
                        continue;
                    }
                }

                if (upNode != null && upNode.HasTrapDoor) continue;

                if (direction.IsPlanarCardinal())
                {
                    var neighbour = direction.Translate(node.Coordinates);
                    var neighbourConfig = Dungeon.GetNodeConfig(neighbour);
                    var hasAlcove = neighbourConfig 
                        ?.Modifications
                        .Any(nMod => 
                            nMod.Tile.Type == TiledConfiguration.instance.AlcoveClass
                            && nMod.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() == direction.Inverse()
                        ) ?? false;

                    if (hasAlcove)
                    {
                        // TODO: Possibly it should get its styling from the neighbour tile rather than this
                        var alcove = Dungeon.Style.Get(node.transform, TiledConfiguration.instance.AlcoveClass, direction, node.NodeStyle);
                        alcove.name = direction.ToString();

                        ConfigureContainer(node, alcove, direction, direction.Inverse(), TiledConfiguration.instance.AlcoveClass, neighbourConfig, false);
                        
                        ApplyAnchorRotation(alcove, direction);

                        continue;
                    } else if (ConfigureWallSpike(node, direction))
                    {
                        continue;
                    }
                }
                var go = Dungeon.Style.Get(
                    node.transform, 
                    TiledConfiguration.instance.BaseTileClass, 
                    direction, 
                    node.NodeStyle);

                go.name = direction.ToString();
                if (direction.IsPlanarCardinal())
                {
                    ApplyAnchorRotation(go, direction);
                }

                if (upNode != null) 
                {
                    var upIsMovingFloor = upNode.GetComponentInChildren<TDMovingPlatform>();
                    if (upIsMovingFloor != null)
                    {
                        upIsMovingFloor.AddAttachedObject(go.transform, direction);
                    }
                } else if (direction == Direction.Down)
                {
                    ConfigureMovingPlatform(node, go);
                }
            }
        }

        public static void Configure(
            TDNode node,
            TiledTile tile, 
            TDNodeConfig config,
            TiledDungeon dungeon
        )
        {
            node.tile = tile;
            node.modifications = config.Modifications;
            node.Points = config.Points;
            node.Rects = config.Rects;

            node.Dungeon = dungeon;

            node.sides = TDSidesClass.From(
                tile.CustomProperties.Classes[TiledConfiguration.instance.SidesClassKey],
                config.RoofRule 
            );

            node.transform.localPosition = node.Coordinates.ToPosition(dungeon.GridSize);
            node.name = $"TileNode Elevation {node.Coordinates.y} ({node.Coordinates.x}, {node.Coordinates.z})";

            ConfigureCube(node);
            ConfigureGrates(node);
            ConfigureObstructions(node);
            ConfigureDoors(node);
            ConfigureLadders(node);
            ConfigureTeleporter(node);
            ConfigureWallButtons(node);
            ConfigurePillar(node, config);
            ConfigurePedistal(node, config);
            ConfigureChest(node, config);

            node.Log("Generated", Debug.Log);
        }
    }
}
