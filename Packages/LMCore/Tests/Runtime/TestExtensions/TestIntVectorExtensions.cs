using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LMCore.Extensions;
using System.Linq;

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
    public void TestIsUnitVector2D( int x, int y, bool unit ) { 
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

}
