using UnityEngine;

public class FlashlightFollowCamera : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("Assign the FPS camera or camera pivot here.")]
    public Transform targetCamera;

    [Header("Position")]
    public bool followPosition = true;
    public Vector3 localOffset = new Vector3(0.18f, -0.12f, 0.35f);

    [Header("Rotation")]
    public bool followRotation = true;

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        if (followPosition)
            transform.position = targetCamera.TransformPoint(localOffset);

        if (followRotation)
            transform.rotation = targetCamera.rotation;
    }
}
