using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Node : MonoBehaviour
{
    public float probability = 1;
    public List<Node> children = new List<Node>();
    

    public abstract void RunNode();
}
