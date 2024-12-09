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

        static void ConfigureFireplace(TDNode node)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.FirePlaceClass)) return;
            node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.FirePlaceClass,
                node.NodeStyle);
        }
        /// <summary>
        /// Position the entity in the level with its relevant anchor.
        /// </summary>
        static void ConfigureEntity(TDNode node, GridEntity entity)
        {
            if (entity != null)
            {
                if (entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    entity.AnchorDirection = Direction.None;
                    entity.Node = node;
                } else if (entity.AnchorDirection != Direction.None)
                {
                    if (!node.CanAnchorOn(entity, entity.AnchorDirection))
                    {
                        entity.AnchorDirection = Direction.None;
                        entity.Node = node;
                    } else
                    {
                        entity.NodeAnchor = node.GetAnchor(entity.AnchorDirection);
                    }
                } else
                {
                    if (node.CanAnchorOn(entity, Direction.Down))
                    {
                        entity.NodeAnchor = node.GetAnchor(Direction.Down);
                    } else
                    {
                        entity.AnchorDirection = Direction.None;
                        entity.Node = node;
                    }
                }
            } 
        }

        private struct EnemyInfo
        {
            public string ClassId;
            public string Id;

            public static EnemyInfo From(TiledCustomProperties props) =>
                new EnemyInfo()
                {
                    ClassId = props.String(TiledConfiguration.instance.ObjEnemyClassIdKey, null),
                    Id = props.String(TiledConfiguration.instance.ObjEnemyIdKey, null),
                };
        }

        static void ConfigureEnemy(TDNode node, TDNodeConfig nodeConfig)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.EnemyClass)) return;

            var pool = TDEnemyPool.InstanceOrResource("EnemyPool");
            if (pool == null) return;

            foreach (var info in nodeConfig.GetObjectValues(
                TiledConfiguration.instance.EnemyClass,
                props => EnemyInfo.From(props)))
            {
                if (string.IsNullOrEmpty(info.ClassId)) continue;

                var enemy = pool.GetInstance(info.ClassId, node.transform);
                if (enemy == null) continue;

                enemy.Configure(info.Id);
                enemy.name = $"Enemy {info.ClassId}: {info.Id}";
                ConfigureEntity(node, enemy.GetComponent<GridEntity>());
            }
        }

        /// <summary>
        /// Add any checkpoint or area marker on either a TDNode or an Anchor game object 
        /// </summary>
        static void ConfigureCheckpointsAndAreaMarkers(GameObject gameObject, TDNodeConfig nodeConfig)
        {
            // Inform user that their level is weirdly configured
            Direction direction = Direction.None;
            if (gameObject.GetComponent<TDNode>() != null)
            {

            }
            else
            {
                var anchor = gameObject.GetComponent<Anchor>();
                if (anchor == null)
                {
                    Debug.LogWarning($"{gameObject} is neither a TDNode or an Anchor so can't have checkpoints or area markers");
                    return;
                }
                direction = anchor.CubeFace;
            }

            var checkpoints = nodeConfig.GetObjectProps(obj => 
                obj.Type == TiledConfiguration.instance.ObjPathKey &&
                obj.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey, TDEnumDirection.None).AsDirection() == direction);

            foreach (var checkpoint in checkpoints)
            {
                gameObject
                    .AddComponent<TDPathCheckpoint>()
                    .Configure(checkpoint);
            }

            var areas = nodeConfig.GetObjectProps(obj => 
                obj.Type == TiledConfiguration.instance.ObjHomeAreaKey &&
                obj.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey, TDEnumDirection.None).AsDirection() == direction);

            foreach (var area in areas)
            {
                gameObject
                    .AddComponent<TDAreaMarker>()
                    .Configure(area);
            }

        }

        static void ConfigureNPC(TDNode node, TDNodeConfig nodeConfig)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.NPCClass)) return;

            var roster = NPCRoster.InstanceOrResource("NPCRoster");
            if (roster == null) return;

            foreach (var npcId in nodeConfig.GetObjectValues(
                TiledConfiguration.instance.NPCClass, 
                props => props.String(TiledConfiguration.instance.ObjIdKey)))
            {
                var npc = roster.GetInstance(npcId, node.transform);
                if (npc == null) continue;

                ConfigureEntity(node,  npc.GetComponent<GridEntity>());
            }
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
            var buttons = node.modifications.Where(mod => mod.Tile.Type == TiledConfiguration.instance.WallButtonClass);

            foreach (var button in buttons)
            {
                var direction = button.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection();

                var go = node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.WallButtonClass,
                    direction,
                    node.NodeStyle
                );

                if (go != null) 
                    go.GetComponent<TDActuator>()?.Configure(node, direction);
            }
        }

        static void ConfigureFence(TDNode node)
        {
            var fences = node.modifications.Where(mod => mod.Tile.Type == TiledConfiguration.instance.FenceClass);

            foreach (var fence in fences)
            {
                var go = node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.FenceClass,
                    fence.Tile.CustomProperties.Direction(TiledConfiguration.instance.DirectionKey).AsDirection(),
                    node.NodeStyle
                );
            }
        }

        /// <summary>
        /// Updates anchor and checks for checkpoints or area markers that should be applied
        /// </summary>
        static void ApplyAnchorRotation(GameObject obj, Direction direction, TDNodeConfig config)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = direction.AsYRotation();

            ConfigureCheckpointsAndAreaMarkers(obj, config);
        }

        /// <summary>
        /// Updates anchor and checks for checkpoints or area markers that should be applied
        /// </summary>
        static void ApplyAnchorRotation(GameObject obj, TDEnumOrientation orientation, TDNodeConfig config)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = orientation == TDEnumOrientation.Horizontal ? AnchorYRotation.CW : AnchorYRotation.None;

            ConfigureCheckpointsAndAreaMarkers(obj, config);
        }

        static bool ConfigureRamps(TDNode node, TDNodeConfig config)
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

            ApplyAnchorRotation(go, downSlopeDirection.Inverse(), config);

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

        static void ConfigureLadders(TDNode node, TDNodeConfig config)
        {
            foreach (var tdDirection in TDEnumDirectionExtensions.PlanarDirections)
            {
                var direction = tdDirection.AsDirection();
                if (!node.HasLadder(direction)) continue;

                var dungeon = node.Dungeon;
                GameObject go = null;

                var upCoordinates = Direction.Up.Translate(node.Coordinates);
                if (!node.HasSide(Direction.Up) && dungeon.HasNodeAt(upCoordinates))
                {
                    var aboveNode = dungeon[upCoordinates];
                    var upForwardCoordinates = direction.Translate(upCoordinates);
                    if (!aboveNode.HasSide(direction) && !aboveNode.HasSide(Direction.Down)
                        && dungeon.HasNodeAt(upForwardCoordinates))
                    {
                        var upForwardNode = dungeon[upForwardCoordinates];
                        if (upForwardNode.HasSide(Direction.Down))
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

                ApplyAnchorRotation(go, direction, config);
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
            if (floor.GetComponent<TDMovingPlatform>() != null)
            {
                return;
            }

            var mover = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.MovingPlatformClass);
            if (mover == null) { 
                TDPassivePlatform.Configure(node, Direction.Down, floor);
                return;
            }

            var conf = node.Dungeon.GetNodeConfig(node.Coordinates);
            var platform = floor.AddComponent<TDMovingPlatform>();
            platform.Configure(conf);
        }

        static bool ConfigureShooter(TDNode node, TDNodeConfig config, Direction direction)
        {
            var shooter = node.modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.InstanceOrCreate().ShooterClass && 
                mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().AnchorKey, TDEnumDirection.Unknown).AsDirection() == direction);

            if (shooter == null) return false;

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().ShooterClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return false;

            go.GetComponentInChildren<TDProjectileShooter>()?.Configure(direction);

            ApplyAnchorRotation(go, direction, config);
            return true;
        }

        static bool ConfigureWallSpike(TDNode node, TDNodeConfig config, Direction direction)
        {
            var spikes = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().WallSpikeTrapClass);

            if (spikes == null || spikes.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().AnchorKey).AsDirection() != direction) return false; 

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.WallSpikeTrapClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return false;

            go.GetComponent<TDSpikeTrap>()?.Configure(node, node.modifications);

            ApplyAnchorRotation(go, direction, config);
            return true;
        }

        static void ConfigureIllusory(TDNode node, TDNodeConfig config, Direction direction)
        {
            node.Log($"I have illusionary wall {direction}", Debug.Log);

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().IllusoryTileClass,
                direction,
                node.NodeStyle
            );

            go.GetComponent<TDIllusoryCubeSide>().Configure(direction);

            ApplyAnchorRotation(go, direction, config);
        }

        static void ConfigureCube(TDNode node, TDNodeConfig config)
        {
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
                        var orientation = node.TrapdoorModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);
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
                                node.modifications.Where(TDNode.TrapDoorFilter).ToArray()
                            );

                            ApplyAnchorRotation(trapdoor, orientation, config);

                            ConfigureMovingPlatform(node, trapdoor);
                            continue;
                        } else
                        {
                            Debug.LogWarning($"Failed to spawn trapdoor at {node.Coordinates} with orientation {orientation} and style {node.NodeStyle}");
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

                            pressurePlate?.Configure(node, direction);

                            var orientation = node.PressurePlateModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);
                            ApplyAnchorRotation(plate, orientation, config);

                            ConfigureMovingPlatform(node, plate);
                            continue;
                        } else
                        {
                            Debug.LogWarning($"Failed to spawn preasure plate at {node.Coordinates} with style {node.NodeStyle}");
                        }
                    }
                    // Area markers and checkpoints configured inside
                    else if (ConfigureRamps(node, config))
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

                            ApplyAnchorRotation(stairs, stairsDirection, config);

                            continue;
                        } else
                        {
                            Debug.LogWarning($"Failed to spawn stairs at {node.Coordinates} {stairsDirection} with style {node.NodeStyle}");
                        }
                    }
                } else if (direction == Direction.Up)
                {
                    if (node.HasSide(Direction.Up) && upNode)
                    {
                        if (upNode.HasIllusion(Direction.Down)) {
                            ConfigureIllusory(node, config, Direction.Up);
                            continue;
                        }

                    }
                }

                if (illusionMods.Any(mod => mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection() == direction))
                {
                    ConfigureIllusory(node, config, direction);
                    continue;
                }

                if (!node.HasSide(direction)) continue;

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

                        spikes.GetComponent<TDSpikeTrap>()?.Configure(node, node.modifications);

                        // Spikes don't have any rotation but could have anchor with checkpoint / area marker
                        ConfigureCheckpointsAndAreaMarkers(spikes, config);
                        continue;
                    } else { 
                        Debug.LogWarning($"Failed to spawn spikes at {node.Coordinates} {direction} with style {node.NodeStyle}");
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
                        
                        ApplyAnchorRotation(alcove, direction, config);

                        continue;
                    } else if (ConfigureWallSpike(node, config, direction) || ConfigureShooter(node, config, direction))
                    {
                        continue;
                    }
                }

                var go = Dungeon.Style.Get(
                    node.transform, 
                    TiledConfiguration.instance.BaseTileClass, 
                    direction, 
                    node.NodeStyle);

                if (go == null)
                {
                    Debug.LogError($"Failed to generate {node.Coordinates} side {direction} with style '{node.NodeStyle}'");
                }
                else
                {

                    go.name = direction.ToString();
                    if (direction.IsPlanarCardinal())
                    {
                        ApplyAnchorRotation(go, direction, config);
                    }
                }

                if (direction == Direction.Up && upNode != null) 
                {
                    var upIsMovingFloor = upNode.GetComponentInChildren<TDMovingPlatform>();
                    if (upIsMovingFloor != null)
                    {
                        upIsMovingFloor.AddAttachedObject(go.transform, direction);
                    }
                    var upIsPassiveFloor = upNode.GetComponentInChildren<TDPassivePlatform>();
                    if (upIsPassiveFloor != null)
                    {
                        upIsPassiveFloor.Backside = go.transform;
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

            node.UpdateSides(TDSidesClass.From(
                tile.CustomProperties.Classes[TiledConfiguration.instance.SidesClassKey],
                config.RoofRule 
            ));

            node.transform.localPosition = node.Coordinates.ToPosition(dungeon.GridSize);
            node.name = $"TileNode Elevation {node.Coordinates.y} ({node.Coordinates.x}, {node.Coordinates.z})";

            ConfigureCube(node, config);
            ConfigureGrates(node);
            ConfigureObstructions(node);
            ConfigureDoors(node);
            ConfigureLadders(node, config);
            ConfigureFireplace(node);
            ConfigureFence(node);
            ConfigureTeleporter(node);
            ConfigureWallButtons(node);
            ConfigurePillar(node, config);
            ConfigurePedistal(node, config);
            ConfigureChest(node, config);
            ConfigureNPC(node, config);
            ConfigureEnemy(node, config);
            TDRelay.Configure(node);
            ConfigureCheckpointsAndAreaMarkers(node.gameObject, config);

            node.Log("Generated", Debug.Log);
        }
    }
}
