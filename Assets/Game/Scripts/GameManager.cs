using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> visionPanels = new List<GameObject>();


    void Start() {
        ActivateRoom(5);
    }

    public void ActivateRoom(int roomId)
    {
        if (roomId > 0 && roomId <= visionPanels.Count)
        {
            var panel = visionPanels[roomId - 1];
            foreach (var p in visionPanels)
            {
                p.SetActive(p != panel);
            }
        }
        else
        {
            Debug.LogError($"Vision panel with index {roomId} not found");
        }
    }
}
