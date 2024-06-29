using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventory
{
    public bool HasItem(string item);
    public bool HasItem(string item, string identifier);

    public bool Consume(string item);
    public bool Consume(string item, string identifier);
}
