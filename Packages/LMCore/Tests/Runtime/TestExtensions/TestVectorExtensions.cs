using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LMCore.Extensions;

public class TestVectorExtensions
{
    [Test]
    public void TestToVector3Int()
    {
        Assert.That(
            new Vector3(3, 9, 12).ToVector3Int(), 
            Is.EqualTo(new Vector3Int(1, 3, 4))
        );
    }

    [Test]
    public void TestToVector3IntWithScale()
    {
        Assert.That(
            new Vector3(2, 4, 8).ToVector3Int(2),
            Is.EqualTo(new Vector3Int(1, 2, 4))
        );
    }

    [Test]
    public void TestToVector2Int()
    {
        Assert.That(
            new Vector3(3, 9, 12).ToVector2Int(),
            Is.EqualTo(new Vector2Int(1, 4))
        );
    }

    [Test]
    public void TestToVector2IntWithScale()
    {
        Assert.That(
            new Vector3(2, 4, 8).ToVector2Int(2),
            Is.EqualTo(new Vector2Int(1, 4))
        );
    }
}
