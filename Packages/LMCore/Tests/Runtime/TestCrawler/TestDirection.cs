using System.Collections;
using System.Collections.Generic;
using LMCore.Crawler;
using LMCore.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestDirection
{
    [Test]
    [TestCase(Direction.North, Direction.West)]
    [TestCase(Direction.West, Direction.South)]
    [TestCase(Direction.South, Direction.East)]
    [TestCase(Direction.East, Direction.North)]
    public void TestRotateCCW(Direction from, Direction to)
    {
        Assert.AreEqual(to, from.RotateCCW());
    }

    [Test]
    [TestCase(Direction.West, Direction.North)]
    [TestCase(Direction.South, Direction.West)]
    [TestCase(Direction.East, Direction.South)]
    [TestCase(Direction.North, Direction.East)]
    public void TestRotateCW(Direction from, Direction to)
    {
        Assert.AreEqual(to, from.RotateCW());
    }

    [Test]
    [TestCase(Direction.North, Direction.South)]
    [TestCase(Direction.South, Direction.North)]
    [TestCase(Direction.West, Direction.East)]
    [TestCase(Direction.East, Direction.West)]
    public void TestInverse(Direction from, Direction to)
    {
        Assert.AreEqual(to, from.Inverse());
    }

    [Test]
    [TestCase(Direction.North, 1, 2, 1, 3)]
    [TestCase(Direction.South, 1, 2, 1, 1)]
    [TestCase(Direction.West, 1, 2, 0, 2)]
    [TestCase(Direction.East, 1, 2, 2, 2)]
    public void TestTranslate(Direction direction, int x1, int y1, int x2, int y2)
    {
        Assert.AreEqual(
            new Vector2Int(x2, y2),
            direction.Translate(new Vector2Int(x1, y1))            
        );
    }

    [Test]
    [TestCase(Direction.North, 0, 1)]
    [TestCase(Direction.South, 0, -1)]
    [TestCase(Direction.West, -1, 0)]
    [TestCase(Direction.East, 1, 0)]
    public void TestAsLookVector(Direction direction, int x, int y)
    {
        Assert.AreEqual(new Vector2Int(x, y), direction.AsLookVector());
    }

    [Test]
    [TestCase(Direction.North, 0, 0, 1)]
    [TestCase(Direction.South, 0, 0, -1)]
    [TestCase(Direction.West, -1, 0, 0)]
    [TestCase(Direction.East, 1, 0, 0)]
    public void TestAsQuaternion(Direction direction, float x, float y, float z)
    {
        Assert.AreEqual(
            Quaternion.LookRotation(new Vector3(x, y, z), Vector3.up),
            direction.AsQuaternion()
        );
    }

    [Test]
    // These are not rotations so nothing changes
    [TestCase(Direction.West, Movement.Backward, Direction.West)]
    [TestCase(Direction.South, Movement.Forward, Direction.South)]
    [TestCase(Direction.East, Movement.StrafeRight, Direction.East)]
    [TestCase(Direction.West, Movement.StrafeLeft, Direction.West)]
    // These are rotations
    [TestCase(Direction.North, Movement.TurnCW, Direction.East)]
    [TestCase(Direction.North, Movement.TurnCCW, Direction.West)]
    public void TestApplyRotation(Direction direction, Movement movement, Direction expected)
    {
        Assert.AreEqual(expected, direction.ApplyRotation(movement));
    }

    [Test]
    [TestCase(Direction.West, Movement.Forward, Direction.West)]
    [TestCase(Direction.West, Movement.Backward, Direction.East)]
    [TestCase(Direction.West, Movement.StrafeLeft, Direction.South)]
    [TestCase(Direction.West, Movement.StrafeRight, Direction.North)]
    // These are not translations
    [TestCase(Direction.South, Movement.TurnCCW, Direction.South)]
    [TestCase(Direction.East, Movement.TurnCW, Direction.East)]
    public void TestRelativeTranslation(Direction direction, Movement movement, Direction expected)
    {
        Assert.AreEqual(expected, direction.RelativeTranslation(movement));
    }
}
