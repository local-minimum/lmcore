using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LMCore.Extensions;

public class TestRectExtensions
{
    static RectInt emptyRect = new RectInt(0, 1, 0, 0);
    static RectInt r1 = new RectInt(0, 1, 2, 3);
    static RectInt r2 = new RectInt(0, 4, 2, 5);
    static RectInt r3 = new RectInt(2, 4, 2, 5);
    static RectInt r4 = new RectInt(0, 3, 2, 9);
    static RectInt r5 = new RectInt(1, 1, 4, 3);
    static RectInt r6 = new RectInt(10, 1, 4, 3);

    [Test]
    public void TestArea()
    {
        Assert.That(
            r1.Area(), 
            Is.EqualTo(6)
        );
    }

    [Test]
    public void TestApplyForRect()
    {        
        var coords = new HashSet<Vector2Int>();

        System.Action<int, int> check = (x, y) =>
        {
            coords.Add(new Vector2Int(x, y));
            Assert.That(x, Is.AtLeast(0));
            Assert.That(x, Is.AtMost(1));
            Assert.That(y, Is.AtLeast(1));
            Assert.That(y, Is.AtMost(3));
        };

        r1.ApplyForRect(check);

        Assert.That(coords.Count, Is.EqualTo(6));
    }

    [Test]
    public void TestUnion()
    {
        Assert.That(r1.Union(r1), Is.EqualTo(r1));
        Assert.That(r1.Union(emptyRect), Is.EqualTo(r1));
        // Touching with same shape
        Assert.That(r1.Union(r2), Is.EqualTo(new RectInt(0, 1, 2, 8)));
        // Non-touching
        Assert.That(r1.Union(r6), Is.EqualTo(new RectInt(0, 1, 14, 3)));
    }

    [Test]
    public void TestUnionIsRect()
    {
        Assert.That(emptyRect.UnionIsRect(r1), Is.True);
        Assert.That(r1.UnionIsRect(r1), Is.True);
        Assert.That(r1.UnionIsRect(r2), Is.True);
        Assert.That(r1.UnionIsRect(r3), Is.False);
        Assert.That(r1.UnionIsRect(r4), Is.True);
        // Starting inside other
        Assert.That(r1.UnionIsRect(r5), Is.True);
        // These are aligned but not touching
        Assert.That(r1.UnionIsRect(r6), Is.False);
    }
}
