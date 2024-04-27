using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LMCore.Extensions;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;

public class TestSequenceExtensions
{
    static int[] arr = { 1, 2, 3 };
    static List<int> list = new List<int>() { 1, 2, 3};

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 3)]
    public void TestGetNthOrLastArray(int idx, int expected)
    {
        Assert.That(
            arr.GetNthOrLast(idx), 
            Is.EqualTo(expected)
        );
    }

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 3)]
    public void TestGetNthOrLastList(int idx, int expected)
    {
        Assert.That(
            list.GetNthOrLast(idx),
            Is.EqualTo(expected)
        );
    }

    [Test]
    [TestCase(0, -1, 1)]
    [TestCase(1, -1, 2)]
    [TestCase(2, -1, 3)]
    [TestCase(3, -1, -1)]
    public void TestGetNthOrDefaultArray(int idx, int def, int expected)
    {
        Assert.That(
            arr.GetNthOrDefault(idx, def),
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void TestGetNthOrDefaultEmptyArray()
    {
        Assert.That(
            new int[] { }.GetNthOrDefault(2, -1),
            Is.EqualTo(-1)
        );
    }

    [Test]
    [TestCase(0, -1, 1)]
    [TestCase(1, -1, 2)]
    [TestCase(2, -1, 3)]
    [TestCase(3, -1, -1)]
    public void TestGetNthOrDefaultList(int idx, int def, int expected)
    {
        Assert.That(
            list.GetNthOrDefault(idx, def),
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void TestGetNthOrDefaultEmptyList()
    {
        Assert.That(
            new List<int>().GetNthOrDefault(2, -1),
            Is.EqualTo(-1)
        );
    }

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 1)]
    [TestCase(73, 2)]
    public void TestGetWrappingNthArray(int idx, int expected)
    {
        Assert.That(
            arr.GetWrappingNth(idx),
            Is.EqualTo(expected)
        );
    }

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 1)]
    [TestCase(73, 2)]
    public void TestGetWrappingNthList(int idx, int expected)
    {
        Assert.That(
            list.GetWrappingNth(idx),
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void TestGetRandomElementArr()
    {
        for (int i = 0; i < 10; i++)
        {

            Assert.True(
                arr.Contains(arr.GetRandomElement())                
            );
        }
    }

    [Test]
    public void TestGetRandomElementList()
    {
        for (int i = 0; i < 10; i++)
        {

            Assert.True(
                list.Contains(list.GetRandomElement())
            );
        }
    }

}
