using LMCore.Crawler;
using LMCore.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class TestDirection
{
    [Test]
    [TestCase(4, 0, Direction.East)]
    [TestCase(-1, 0, Direction.West)]
    [TestCase(0, 4, Direction.North)]
    [TestCase(0, -3, Direction.South)]
    public void TestAsDirection(int x, int y, Direction direction)
    {
        Assert.AreEqual(direction, new Vector2Int(x, y).AsDirection());
    }

    [Test]
    [TestCase(1, 0, 0, Direction.East)]
    [TestCase(-3, 0, 0, Direction.West)]
    [TestCase(0, 2, 0, Direction.Up)]
    [TestCase(0, -1, 0, Direction.Down)]
    [TestCase(0, 0, 1, Direction.North)]
    [TestCase(0, 0, -1, Direction.South)]
    public void TestAsDirection(int x, int y, int z, Direction direction)
    {
        Assert.AreEqual(direction, new Vector3Int(x, y, z).AsDirection());
    }

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
    [TestCase(Direction.North, Direction.Up, Direction.West)]
    [TestCase(Direction.North, Direction.Down, Direction.East)]
    [TestCase(Direction.West, Direction.North, Direction.Down)]
    [TestCase(Direction.South, Direction.East, Direction.Up)]
    public void TestRotate3DCCW(Direction from, Direction up, Direction to)
    {
        Assert.AreEqual(to, from.Rotate3DCCW(up));
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
    [TestCase(Direction.North, Direction.Up, Direction.East)]
    [TestCase(Direction.North, Direction.Down, Direction.West)]
    [TestCase(Direction.West, Direction.North, Direction.Up)]
    [TestCase(Direction.South, Direction.East, Direction.Down)]
    public void TestRotate3DCW(Direction from, Direction down, Direction to)
    {
        Assert.AreEqual(to, from.Rotate3DCW(down));
    }

    [Test]
    [TestCase(Direction.North, Direction.South)]
    [TestCase(Direction.South, Direction.North)]
    [TestCase(Direction.West, Direction.East)]
    [TestCase(Direction.East, Direction.West)]
    [TestCase(Direction.Up, Direction.Down)]
    [TestCase(Direction.Down, Direction.Up)]
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
    [TestCase(Direction.North, 1, 2, 3, 1, 2, 4)]
    [TestCase(Direction.South, 1, 2, 3, 1, 2, 2)]
    [TestCase(Direction.West, 1, 2, 3, 0, 2, 3)]
    [TestCase(Direction.East, 1, 2, 3, 2, 2, 3)]
    [TestCase(Direction.Up, 1, 2, 3, 1, 3, 3)]
    [TestCase(Direction.Down, 1, 2, 3, 1, 1, 3)]
    public void TestTranslateVector3(Direction direction, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Assert.AreEqual(
            new Vector3Int(x2, y2, z2),
            direction.Translate(new Vector3Int(x1, y1, z1))
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
            direction.AsQuaternion(Direction.Down)
        );
    }

    [Test]
    // These are not rotations so nothing changes
    [TestCase(Direction.West, Movement.Backward, Direction.West, Direction.Down)]
    [TestCase(Direction.South, Movement.Forward, Direction.South, Direction.Down)]
    [TestCase(Direction.East, Movement.StrafeRight, Direction.East, Direction.Down)]
    [TestCase(Direction.West, Movement.StrafeLeft, Direction.West, Direction.Down)]
    // These are rotations
    [TestCase(Direction.North, Movement.YawCW, Direction.East, Direction.Down)]
    [TestCase(Direction.North, Movement.YawCCW, Direction.West, Direction.Down)]
    [TestCase(Direction.North, Movement.PitchUp, Direction.Up, Direction.North)]
    [TestCase(Direction.North, Movement.PitchDown, Direction.Down, Direction.South)]
    public void TestApplyRotation(Direction direction, Movement movement, Direction expected, Direction expectedNewDown)
    {
        Assert.AreEqual(expected, direction.ApplyRotation(Direction.Down, movement, out Direction newDown));
        Assert.AreEqual(newDown, expectedNewDown);
    }

    [Test]
    [TestCase(Direction.West, Movement.Forward, Direction.West)]
    [TestCase(Direction.West, Movement.Backward, Direction.East)]
    [TestCase(Direction.West, Movement.StrafeLeft, Direction.South)]
    [TestCase(Direction.West, Movement.StrafeRight, Direction.North)]
    // These are not translations
    [TestCase(Direction.South, Movement.YawCCW, Direction.South)]
    [TestCase(Direction.East, Movement.YawCW, Direction.East)]
    public void TestRelativeTranslation(Direction direction, Movement movement, Direction expected)
    {
        Assert.AreEqual(expected, direction.RelativeTranslation(movement));
    }
}