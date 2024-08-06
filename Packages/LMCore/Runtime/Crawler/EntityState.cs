using UnityEngine;

namespace LMCore.Crawler
{
    public struct EntityState
    {
        public readonly Vector3Int Coordinates;
        public readonly Direction Anchor;
        public readonly Direction LookDirection;
        public readonly bool RotationRespectsAnchorDirection;
        public readonly TransportationMode TransportationMode;

        public EntityState(GridEntity entity)
        {
            Coordinates = entity.Position;
            Anchor = entity.Anchor;
            LookDirection = entity.LookDirection;
            RotationRespectsAnchorDirection = entity.RotationRespectsAnchorDirection;
            TransportationMode = entity.TransportationMode;
        }

        public EntityState(Vector3Int coordinates, GridEntity entity)
        {
            Coordinates = coordinates;
            Anchor = entity.Anchor;
            LookDirection = entity.LookDirection;
            RotationRespectsAnchorDirection = entity.RotationRespectsAnchorDirection;
            TransportationMode = entity.TransportationMode;
        }

        public EntityState(
            Vector3Int coordinates, 
            Direction anchor, 
            Direction lookDirection, 
            GridEntity entity)
        {
            Coordinates = coordinates;
            Anchor = anchor;
            LookDirection = lookDirection;
            RotationRespectsAnchorDirection = entity.RotationRespectsAnchorDirection;
            TransportationMode = entity.TransportationMode;
        }

        public EntityState(
            Vector3Int coordinates, 
            Direction anchor, 
            Direction lookDirection, 
            bool rotationRespectsAnchorDirection, 
            TransportationMode transportationMode)
        {
            Coordinates = coordinates;
            Anchor = anchor;
            LookDirection = lookDirection;
            RotationRespectsAnchorDirection = rotationRespectsAnchorDirection;
            TransportationMode = transportationMode;
        }

        public override string ToString() => $"<{Coordinates} Looking({LookDirection}) Anchor({Anchor}) Respect({RotationRespectsAnchorDirection}) Mode({TransportationMode})>";

        public override bool Equals(object obj)
        {
            if (obj != null && obj is not EntityState)
            {
                var other = (EntityState)obj;

                return Coordinates == other.Coordinates
                    && Anchor == other.Anchor
                    && LookDirection == other.LookDirection
                    && RotationRespectsAnchorDirection == other.RotationRespectsAnchorDirection
                    && TransportationMode == other.TransportationMode;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
