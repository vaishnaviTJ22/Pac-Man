using UnityEngine;

/// <summary>
/// Follows the player by maintaining a fixed offset.
/// Rotation is NEVER changed — the camera keeps whatever angle you set in the Editor.
///
/// SETUP:
/// 1. Attach this script to your Camera.
/// 2. Drag the Pac-Man sphere into the "Player" field.
/// 3. Press Play — the offset is calculated automatically from the camera's
///    current position relative to the player, so just position the camera
///    the way you want it in the Editor and it will stay that way.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Smoothing (0 = instant)")]
    [Range(0f, 20f)]
    public float smoothSpeed = 10f;

    // Offset is captured once at Start from the Editor-set position
    private Vector3 offset;

    void Start()
    {
        if (player != null)
            offset = transform.position - player.position;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 desiredPosition = player.position + offset;

        if (smoothSpeed <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
        }

        // Rotation is intentionally never touched
    }
}
