using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Node : MonoBehaviour
{
    public List<Node> nodeChildren = new List<Node>();
    public abstract void UpdateNode();
    public abstract void ActivateNode();
    public abstract void RestoreNode();
    public bool activated = false;
}
