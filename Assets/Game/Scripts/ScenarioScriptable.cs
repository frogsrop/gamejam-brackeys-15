using System.Collections.Generic;
using UnityEngine;

public class ScenarioScriptable : ScriptableObject
{
    [SerializeField] private List<List<RunnableScriptable>> components;
}
