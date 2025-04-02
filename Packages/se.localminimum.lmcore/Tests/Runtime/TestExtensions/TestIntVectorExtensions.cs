using LMCore.Extensions;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

public class TestIntVectorExtensions
{
    [Test]
    public void TestRandom2DDirection()
    {
        for (int i = 0; i < 10; i++)
        {
            Assert.True(
                IntVectorExtensions.Cardinal2DVectors.Contains(
                    IntVectorExtensions.Random2DDirection()
                )
            );
        }
    }

    [Test]
    public void TestRandom3DDirection()
    {
        for (int i = 0; i < 10; i++)
        {
            Assert.True(
                IntVectorExtensions.Cardinal3DVectors.Contains(
                    IntVectorExtensions.Random3DDirection()
                )
            );
        }
    }

    [Test]
    [TestCase(0, 0, 0)]
    [TestCase(1, 2, 1)]
    [TestCase(4, 2, 2)]
    [TestCase(-4, 2, 2)]
    [TestCase(10, -3, 3)]
    public void TestSmallestAxisMagnitude2D(int x, int y, int magnitude)
    {
        Assert.That(new Vector2Int(x, y).SmallestAxisMagnitude(), Is.EqualTo(magnitude));
    }

    [Test]
    [TestCase(0, 0, 0, 0)]
    [TestCase(1, 2, 3, 1)]
    [TestCase(4, 2, 3, 2)]
    [TestCase(4, 7, 3, 3)]
    [TestCase(-4, 2, 3, 2)]
    [TestCase(10, -3, 5, 3)]
    public void TestSmallestAxisMagnitude3D(int x, int y, int z, int magnitude)
    {
        Assert.That(new Vector3Int(x, y, z).SmallestAxisMagnitude(), Is.EqualTo(magnitude));
    }

    [Test]
    [TestCase(0, 0, 0)]
    [TestCase(1, 2, 2)]
    [TestCase(4, 2, 4)]
    [TestCase(-4, 2, 4)]
    [TestCase(10, -3, 10)]
    public void TestLargestAxisMagnitude2D(int x, int y, int magnitude)
    {
        Assert.That(new Vector2Int(x, y).LargestAxisMagnitude(), Is.EqualTo(magnitude));
    }

    [Test]
    [TestCase(0, 0, 0, 0)]
    [TestCase(1, 2, 3, 3)]
    [TestCase(4, 2, 3, 4)]
    [TestCase(4, 7, 3, 7)]
    [TestCase(-4, 2, 3, 4)]
    [TestCase(10, -3, 5, 10)]
    public void TestLargestAxisMagnitude3D(int x, int y, int z, int magnitude)
    {
        Assert.That(new Vector3Int(x, y, z).LargetsAxisMagnitude(), Is.EqualTo(magnitude));
    }

    [Test]
    public void TestAsUnitComponents2DIsUnit()
    {
        Assert.That(Vector2Int.up.AsUnitComponents(), Is.EqualTo(new Vector2Int[] { Vector2Int.up }));
    }

    [Test]
    public void TestAsUnitComponents2D()
    {
        Assert.That(
            new Vector2Int(3, -5).AsUnitComponents(),
            Is.EqualTo(new Vector2Int[] { Vector2Int.right, Vector2Int.down })
        );
    }

    [Test]
    public void TestAsUnitComponents3DIsUnit()
    {
        Assert.That(Vector3Int.up.AsUnitComponents(), Is.EqualTo(new Vector3Int[] { Vector3Int.up }));
    }

    [Test]
    public void TestAsUnitComponents3D()
    {
        Assert.That(
            new Vector3Int(3, -5, 2).AsUnitComponents(),
            Is.EqualTo(new Vector3Int[] { Vector3Int.right, Vector3Int.down, Vector3Int.forward })
        );
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(1, 0, true)]
    [TestCase(0, 1, true)]
    [TestCase(0, -1, true)]
    [TestCase(-1, 0, true)]
    [TestCase(2, 0, false)]
    [TestCase(1, 1, false)]
    [TestCase(1, -1, false)]
    [TestCase(2, -1, false)]
    public void TestIsUnitVector2D(int x, int y, bool unit)
    {
        Assert.That(new Vector2Int(x, y).IsUnitVector(), Is.EqualTo(unit));
    }

    [Test]
    [TestCase(0, 0, 0, false)]
    [TestCase(1, 0, 0, true)]
    [TestCase(0, 1, 0, true)]
    [TestCase(0, 0, 1, true)]
    [TestCase(0, 0, -1, true)]
    [TestCase(0, -1, 0, true)]
    [TestCase(-1, 0, 0, true)]
    [TestCase(2, 1, 1, false)]
    [TestCase(1, 1, 1, false)]
    [TestCase(1, -1, -1, false)]
    [TestCase(2, -1, 0, false)]
    public void TestIsUnitVector3D(int x, int y, int z, bool unit)
    {
        Assert.That(new Vector3Int(x, y, z).IsUnitVector(), Is.EqualTo(unit));
    }

    [Test]
    public void TestPrimaryCardinalDirection2DPoints()
    {
        Assert.That(
            new Vector2Int(1, 2).PrimaryCardinalDirection(new Vector2Int(2, 4)),
            Is.EqualTo(Vector2Int.up)
        );

        Assert.That(
            new Vector2Int(1, 2).PrimaryCardinalDirection(new Vector2Int(2, 3), true),
            Is.Not.EqualTo(Vector2Int.zero)
        );

        Assert.That(
            new Vector2Int(1, 2).PrimaryCardinalDirection(new Vector2Int(2, 3), false),
            Is.EqualTo(Vector2Int.zero)
        );
    }

    [Test]
    [TestCase(0, 0, 0, 0)]
    [TestCase(3, 0, 1, 0)]
    [TestCase(-4, 0, -1, 0)]
    [TestCase(4, 10, 0, 1)]
    [TestCase(4, -10, 0, -1)]
    [TestCase(5, 5, 0, 0)]
    public void TestPrimaryCardinalDirection2D(int x, int y, int ux, int uy)
    {
        Assert.That(
            new Vector2Int(x, y).PrimaryCardinalDirection(false),
            Is.EqualTo(new Vector2Int(ux, uy))
        );
    }

    [Test]
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(-1, -1)]
    [TestCase(-3, 3)]
    [TestCase(3, -3)]
    public void TestPrimaryCardinalDirection2DResolve(int x, int y)
    {
        Assert.That(
            new Vector2Int(x, y).PrimaryCardinalDirection(true),
            Is.Not.EqualTo(Vector2Int.zero)
        );
    }

    [Test]
    public void TestPrimaryCardinalDirection3DPoints()
    {
        Assert.That(
            new Vector3Int(1, 2, 3).PrimaryCardinalDirection(new Vector3Int(2, 4, 2)),
            Is.EqualTo(Vector3Int.up)
        );

        Assert.That(
            new Vector3Int(1, 2, 3).PrimaryCardinalDirection(new Vector3Int(2, 3, 2), true),
            Is.Not.EqualTo(Vector3Int.zero)
        );

        Assert.That(
            new Vector3Int(1, 2, 3).PrimaryCardinalDirection(new Vector3Int(2, 3, 2), false),
            Is.EqualTo(Vector3Int.zero)
        );
    }

    [Test]
    [TestCase(0, 0, 0, 0, 0, 0)]
    [TestCase(3, 0, 0, 1, 0, 0)]
    [TestCase(-4, 0, 0, -1, 0, 0)]
    [TestCase(4, 10, 1, 0, 1, 0)]
    [TestCase(4, -10, 1, 0, -1, 0)]
    [TestCase(4, 10, 100, 0, 0, 1)]
    [TestCase(4, -10, -100, 0, 0, -1)]
    [TestCase(5, 5, 0, 0, 0, 0)]
    [TestCase(0, 5, 5, 0, 0, 0)]
    [TestCase(5, 0, 5, 0, 0, 0)]
    public void TestPrimaryCardinalDirection3D(int x, int y, int z, int ux, int uy, int uz)
    {
        Assert.That(
            new Vector3Int(x, y, z).PrimaryCardinalDirection(false),
            Is.EqualTo(new Vector3Int(ux, uy, uz))
        );
    }

    [Test]
    [TestCase(1, 1, 1)]
    [TestCase(2, 2, 2)]
    [TestCase(-1, -1, -1)]
    [TestCase(-3, 3, 3)]
    [TestCase(3, -3, -3)]
    [TestCase(3, 0, -3)]
    [TestCase(0, -3, -3)]
    [TestCase(3, -3, 0)]
    public void TestPrimaryCardinalDirection3DResolve(int x, int y, int z)
    {
        Assert.That(
            new Vector3Int(x, y, z).PrimaryCardinalDirection(true),
            Is.Not.EqualTo(Vector3Int.zero)
        );
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(1, 1, false)]
    [TestCase(1, -1, false)]
    [TestCase(-1, -1, false)]
    [TestCase(0, -11, true)]
    [TestCase(0, 2, true)]
    [TestCase(4, 0, true)]
    [TestCase(-10, 0, true)]
    public void TestIsCardinal2D(int x, int y, bool cardinal)
    {
        Assert.That(new Vector2Int(x, y).IsCardinal(), Is.EqualTo(cardinal));
    }

    [Test]
    [TestCase(0, 0, 0, false)]
    [TestCase(1, 1, 1, false)]
    [TestCase(1, -1, 0, false)]
    [TestCase(-1, -1, 0, false)]
    [TestCase(0, 1, 1, false)]
    [TestCase(1, 0, -1, false)]
    [TestCase(0, -11, 0, true)]
    [TestCase(0, 2, 0, true)]
    [TestCase(4, 0, 0, true)]
    [TestCase(-10, 0, 0, true)]
    [TestCase(0, 0, 12, true)]
    [TestCase(0, 0, -11, true)]
    public void TestIsCardinal3D(int x, int y, int z, bool cardinal)
    {
        Assert.That(new Vector3Int(x, y, z).IsCardinal(), Is.EqualTo(cardinal));
    }

    [Test]
    [TestCase(1, 0, 1, 0, true, false)]
    [TestCase(1, 0, -1, 0, true, false)]
    [TestCase(0, 1, 0, 1, true, false)]
    [TestCase(0, 1, 0, -1, true, false)]
    [TestCase(0, 1, 1, 0, true, true)]
    [TestCase(0, 1, -1, 0, true, true)]
    [TestCase(1, 0, 0, 5, true, true)]
    [TestCase(1, 3, 0, 5, true, true)] // False positive because first isn't cardinal
    [TestCase(0, 3, 3, 5, true, true)] // False positive because second isn't cardinal
    [TestCase(1, 3, 0, 5, false, false)]
    public void TestIsOrhogonalCardinal2D(int x1, int y1, int x2, int y2, bool trust, bool ortho)
    {
        Assert.That(
            new Vector2Int(x1, y1).IsOrthogonalCardinal(new Vector2Int(x2, y2), trust),
            Is.EqualTo(ortho)
        );
    }

    [Test]
    [TestCase(1, 0, 0, 1, 0, 0, true, false)]
    [TestCase(1, 0, 0, -1, 0, 0, true, false)]
    [TestCase(0, 1, 0, 0, 1, 0, true, false)]
    [TestCase(0, 1, 0, 0, -1, 0, true, false)]
    [TestCase(0, 0, 1, 0, 0, 1, true, false)]
    [TestCase(0, 0, -1, 0, 0, 0, true, false)]
    [TestCase(0, 1, 0, 1, 0, 0, true, true)]
    [TestCase(0, 1, 0, -1, 0, 0, true, true)]
    [TestCase(0, 1, 0, 0, 0, -5, true, true)]
    [TestCase(1, 0, 0, 0, 5, 0, true, true)]
    [TestCase(1, 0, 0, 0, 0, 1, true, true)]
    [TestCase(0, 0, 10, 1, 0, 0, true, true)]
    [TestCase(0, 0, 2, 0, 2, 0, true, true)]
    [TestCase(1, 3, 0, 0, 5, 0, true, true)] // False positive because first isn't cardinal
    [TestCase(0, 3, 0, 3, 5, 0, true, true)] // False positive because second isn't cardinal
    [TestCase(1, 3, 0, 0, 5, 0, false, false)]
    public void TestIsOrhogonalCardinal3D(int x1, int y1, int z1, int x2, int y2, int z2, bool trust, bool ortho)
    {
        Assert.That(
            new Vector3Int(x1, y1, z1).IsOrthogonalCardinal(new Vector3Int(x2, y2, z2), trust),
            Is.EqualTo(ortho)
        );
    }

    [Test]
    [TestCase(4, 0, -1, 0, true)]
    [TestCase(-2, 0, 10, 0, true)]
    [TestCase(0, 3, 0, -2, true)]
    [TestCase(1, 0, 20, 0, false)]
    [TestCase(4, 0, 0, 0, false)]
    [TestCase(0, 5, 0, 1, false)]
    [TestCase(1, 1, -1, -1, true)]
    [TestCase(0, 0, 0, 0, true)]
    public void TestIsInverseDirection2D(int x1, int y1, int x2, int y2, bool inverse)
    {
        Assert.That(
            new Vector2Int(x1, y1).IsInverseDirection(new Vector2Int(x2, y2)),
            Is.EqualTo(inverse)
        );
    }

    [Test]
    [TestCase(4, 0, 0, -1, 0, 0, true)]
    [TestCase(-2, 0, 0, 10, 0, 0, true)]
    [TestCase(0, 3, 0, 0, -2, 0, true)]
    [TestCase(0, 0, -5, 0, 0, 12, true)]
    [TestCase(1, 0, 0, 20, 0, 0, false)]
    [TestCase(4, 0, 0, 0, 0, 0, false)]
    [TestCase(0, 5, 0, 0, 1, 0, false)]
    [TestCase(0, 0, -2, 0, 0, -4, false)]
    [TestCase(1, 1, -1, -1, -1, 1, true)]
    [TestCase(0, 0, 0, 0, 0, 0, true)]
    public void TestIsInverseDirection3D(int x1, int y1, int z1, int x2, int y2, int z2, bool inverse)
    {
        Assert.That(
            new Vector3Int(x1, y1, z1).IsInverseDirection(new Vector3Int(x2, y2, z2)),
            Is.EqualTo(inverse)
        );
    }

    [Test]
    public void TestRotateCCW2D()
    {
        Assert.That(Vector2Int.up.RotateCCW(), Is.EqualTo(Vector2Int.left));
        Assert.That(Vector2Int.left.RotateCCW(), Is.EqualTo(Vector2Int.down));
        Assert.That(Vector2Int.down.RotateCCW(), Is.EqualTo(Vector2Int.right));
        Assert.That(Vector2Int.right.RotateCCW(), Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void TestRotateCW2D()
    {
        Assert.That(Vector2Int.up.RotateCW(), Is.EqualTo(Vector2Int.right));
        Assert.That(Vector2Int.left.RotateCW(), Is.EqualTo(Vector2Int.up));
        Assert.That(Vector2Int.down.RotateCW(), Is.EqualTo(Vector2Int.left));
        Assert.That(Vector2Int.right.RotateCW(), Is.EqualTo(Vector2Int.down));
    }

    [Test]
    public void TestRotateCCW3D()
    {
        // XZ plane
        Assert.That(Vector3Int.forward.RotateCCW(Vector3Int.up), Is.EqualTo(Vector3Int.left));
        Assert.That(Vector3Int.left.RotateCCW(Vector3Int.up), Is.EqualTo(Vector3Int.back));
        Assert.That(Vector3Int.back.RotateCCW(Vector3Int.up), Is.EqualTo(Vector3Int.right));
        Assert.That(Vector3Int.right.RotateCCW(Vector3Int.up), Is.EqualTo(Vector3Int.forward));

        // XZ inverse plane
        Assert.That(Vector3Int.forward.RotateCCW(Vector3Int.down), Is.EqualTo(Vector3Int.right));

        // XY plane
        Assert.That(Vector3Int.up.RotateCCW(Vector3Int.forward), Is.EqualTo(Vector3Int.left));
        Assert.That(Vector3Int.left.RotateCCW(Vector3Int.forward), Is.EqualTo(Vector3Int.down));
        Assert.That(Vector3Int.down.RotateCCW(Vector3Int.forward), Is.EqualTo(Vector3Int.right));
        Assert.That(Vector3Int.right.RotateCCW(Vector3Int.forward), Is.EqualTo(Vector3Int.up));

        // XY inverse plane
        Assert.That(Vector3Int.down.RotateCCW(Vector3Int.back), Is.EqualTo(Vector3Int.left));

        // YZ plane
        Assert.That(Vector3Int.up.RotateCCW(Vector3Int.right), Is.EqualTo(Vector3Int.forward));
        Assert.That(Vector3Int.forward.RotateCCW(Vector3Int.right), Is.EqualTo(Vector3Int.down));
        Assert.That(Vector3Int.down.RotateCCW(Vector3Int.right), Is.EqualTo(Vector3Int.back));
        Assert.That(Vector3Int.back.RotateCCW(Vector3Int.right), Is.EqualTo(Vector3Int.up));

        // YZ inverse plane
        Assert.That(Vector3Int.up.RotateCCW(Vector3Int.left), Is.EqualTo(Vector3Int.back));
    }

    [Test]
    public void TestRotateCW3D()
    {
        // Assuming it shares implementation with CCW this is only sentinals for smoke

        // XZ plane
        Assert.That(Vector3Int.forward.RotateCW(Vector3Int.up), Is.EqualTo(Vector3Int.right));

        // XZ inverse plane
        Assert.That(Vector3Int.forward.RotateCW(Vector3Int.down), Is.EqualTo(Vector3Int.left));

        // XY plane
        Assert.That(Vector3Int.up.RotateCW(Vector3Int.forward), Is.EqualTo(Vector3Int.right));

        // XY inverse plane
        Assert.That(Vector3Int.up.RotateCW(Vector3Int.back), Is.EqualTo(Vector3Int.left));

        // YZ plane
        Assert.That(Vector3Int.up.RotateCW(Vector3Int.right), Is.EqualTo(Vector3Int.back));

        // YZ inverse plane
        Assert.That(Vector3Int.up.RotateCW(Vector3Int.left), Is.EqualTo(Vector3Int.forward));
    }

    [Test]
    public void TestIsCWRotationOf2D()
    {
        // Sentinals assuming it borrows the rotation code
        Assert.True(Vector2Int.up.IsCWRotationOf(Vector2Int.left));
        Assert.False(Vector2Int.up.IsCWRotationOf(Vector2Int.right));
        Assert.True(Vector2Int.left.IsCWRotationOf(Vector2Int.down));
    }

    [Test]
    public void TestIsCCWRotationOf2D()
    {
        // Sentinals assuming it borrows the rotation code
        Assert.True(Vector2Int.up.IsCCWRotationOf(Vector2Int.right));
        Assert.False(Vector2Int.up.IsCCWRotationOf(Vector2Int.left));
        Assert.True(Vector2Int.left.IsCCWRotationOf(Vector2Int.up));
    }

    [Test]
    public void TestIsCWRotationOf3D()
    {
        // Sentinals assuming it borrows the rotation code
        Assert.True(Vector3Int.forward.IsCWRotationOf(Vector3Int.left, Vector3Int.up));
        Assert.False(Vector3Int.forward.IsCWRotationOf(Vector3Int.right, Vector3Int.up));
        Assert.True(Vector3Int.forward.IsCWRotationOf(Vector3Int.right, Vector3Int.down));
    }

    [Test]
    public void TestIsCCWRotationOf3D()
    {
        // Sentinals assuming it borrows the rotation code
        Assert.True(Vector3Int.forward.IsCCWRotationOf(Vector3Int.right, Vector3Int.up));
        Assert.False(Vector3Int.forward.IsCCWRotationOf(Vector3Int.left, Vector3Int.up));
        Assert.True(Vector3Int.forward.IsCCWRotationOf(Vector3Int.left, Vector3Int.down));
    }

    [Test]
    [TestCase(0, 0, 0, 0, 0)]
    [TestCase(12, 2, 12, 2, 0)]
    [TestCase(1, 2, 3, 4, 4)]
    [TestCase(-3, 1, 2, -2, 8)]
    public void TestManhattanDistance2D(int x1, int y1, int x2, int y2, int d)
    {
        Assert.That(
            new Vector2Int(x1, y1).ManhattanDistance(new Vector2Int(x2, y2)),
            Is.EqualTo(d)
        );
    }

    [Test]
    [TestCase(0, 0, 0, 0, 0, 0, 0)]
    [TestCase(12, 2, 3, 12, 2, 3, 0)]
    [TestCase(1, 2, 3, 4, 5, 6, 9)]
    [TestCase(-3, 1, 0, 2, -2, 7, 15)]
    public void TestManhattanDistance3D(int x1, int y1, int z1, int x2, int y2, int z2, int d)
    {
        Assert.That(
            new Vector3Int(x1, y1, z1).ManhattanDistance(new Vector3Int(x2, y2, z2)),
            Is.EqualTo(d)
        );
    }

    [Test]
    [TestCase(0, 0, 0, 0, 0)]
    [TestCase(4, 4, 4, 4, 0)]
    [TestCase(1, 2, 3, 4, 2)]
    [TestCase(10, 3, -2, 7, 12)]
    public void TestChebyshevDistance2D(int x1, int y1, int x2, int y2, int d)
    {
        Assert.That(
            new Vector2Int(x1, y1).ChebyshevDistance(new Vector2Int(x2, y2)),
            Is.EqualTo(d)
        );
    }

    [Test]
    [TestCase(0, 0, 0, 0, 0, 0, 0)]
    [TestCase(4, 4, 4, 4, 4, 4, 0)]
    [TestCase(1, 2, 3, 4, 5, 6, 3)]
    [TestCase(10, 3, -3, -2, 7, 1, 12)]
    public void TestChebyshevDistance3D(int x1, int y1, int z1, int x2, int y2, int z2, int d)
    {
        Assert.That(
            new Vector3Int(x1, y1, z1).ChebyshevDistance(new Vector3Int(x2, y2, z2)),
            Is.EqualTo(d)
        );
    }

    [Test]
    public void TestOrthoIntersection2D()
    {
        Assert.That(
            new Vector2Int(1, 0).OrthoIntersection(new Vector2Int(3, 10), Vector2Int.right),
            Is.EqualTo(new Vector2Int(3, 0))
        );

        Assert.That(
            new Vector2Int(1, 0).OrthoIntersection(new Vector2Int(3, 10), Vector2Int.left),
            Is.EqualTo(new Vector2Int(3, 0))
        );

        Assert.That(
            new Vector2Int(1, 0).OrthoIntersection(new Vector2Int(3, 10), Vector2Int.up),
            Is.EqualTo(new Vector2Int(1, 10))
        );

        Assert.That(
            new Vector2Int(1, 0).OrthoIntersection(new Vector2Int(3, 10), Vector2Int.down),
            Is.EqualTo(new Vector2Int(1, 10))
        );
    }

    [Test]
    public void TestToPositionFromXZPlane()
    {
        Assert.That(new Vector2Int(1, 2).ToPositionFromXZPlane(10, 2), Is.EqualTo(new Vector3(2, 20, 4)));
    }

    [Test]
    public void TestToPosition()
    {
        Assert.That(new Vector3Int(1, 2, 3).ToPosition(2), Is.EqualTo(new Vector3(2, 4, 6)));
    }

    [Test]
    public void TestToDirectionFromXZPlane()
    {
        Assert.That(
            new Vector2Int(1, 2).ToDirectionFromXZPlane(),
            Is.EqualTo(new Vector3(1, 0, 2))
        );
    }

    [Test]
    public void TestToDirection()
    {
        Assert.That(
            new Vector3Int(1, 2, 3).ToDirection(),
            Is.EqualTo(new Vector3(1, 2, 3))
        );
    }
}