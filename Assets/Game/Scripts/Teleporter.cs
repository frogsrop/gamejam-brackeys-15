using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject teleport1;
    [SerializeField] private GameObject teleport2;
    [SerializeField] private float offset = 1f;

    [SerializeField] private int roomId1;
    [SerializeField] private int roomId2;
    
    [SerializeField] private bool keepY = true;
    [SerializeField] private bool keepX = false;

    public void Teleport(GameObject teleport, GameObject objectToTeleport) {
        if (teleport == teleport1) {
            if (keepY) {
                objectToTeleport.transform.position = new Vector3(teleport2.transform.position.x, objectToTeleport.transform.position.y, objectToTeleport.transform.position.z);
            }
            if (keepX) {
                objectToTeleport.transform.position = new Vector3(objectToTeleport.transform.position.x, teleport2.transform.position.y, objectToTeleport.transform.position.z);
            }
            objectToTeleport.transform.position = objectToTeleport.transform.position + new Vector3(offset, 0, 0);
            gameManager.ActivateRoom(roomId2);
        } else {
            if (keepY) {
                objectToTeleport.transform.position = new Vector3(teleport1.transform.position.x, objectToTeleport.transform.position.y, objectToTeleport.transform.position.z);
            }
            if (keepX) {
                objectToTeleport.transform.position = new Vector3(objectToTeleport.transform.position.x, teleport1.transform.position.y, objectToTeleport.transform.position.z);
            }
            objectToTeleport.transform.position = objectToTeleport.transform.position + new Vector3(-offset, 0, 0);
            gameManager.ActivateRoom(roomId1);
        }
    }
}
