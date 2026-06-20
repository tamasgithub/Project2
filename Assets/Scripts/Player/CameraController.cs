using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform POI = null;
    public float followSpeed = 1.0f;

    // Update is called once per frame
    void Update()
    {
        FollowPOI();
    }
    
    private void FollowPOI()
    {
        if (POI == null) return;
        transform.position = Vector3.Lerp(transform.position, POI.transform.position - Vector3.forward, Time.deltaTime * followSpeed);
    }
}
