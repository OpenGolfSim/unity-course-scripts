using UnityEngine;
using System.Collections.Generic;


public class OGSRangeData : MonoBehaviour
{
  [Header("Range Settings")]
  public Vector3 startPosition = Vector3.zero;
  public Vector3 aimPosition = Vector3.zero;

  [Header("Ground Snapping (Editor)")]
  [Tooltip("When true, the editor will snap hole tee/pin Y position to the nearest collider within the max distance.")]
  public bool snapToGroundOnEdit = true;

  [Tooltip("Maximum distance (up/down) to search for ground when snapping the Y coordinate.")]
  public float groundSnapMaxDistance = 100.0f;

  [Tooltip("Layer mask used when raycasting to find ground height. Use this to limit snapping to terrain/colliders.")]
  public LayerMask groundLayerMask = ~0; // default to everything

  void Awake()
  {
    
  }
  void Reset()
  {

    snapToGroundOnEdit = true;
    groundSnapMaxDistance = 50f;
    groundLayerMask = ~0;
  }
  
  private void OnValidate()
  {
    
  }  

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.yellow;
    
    Gizmos.DrawWireSphere(startPosition, 5f);
    Gizmos.DrawWireSphere(aimPosition, 5f);
      // float arrowLength = 1.5f;
      // Gizmos.color = Color.yellow;
      // Vector3 start = transform.position;
      // Vector3 end = start + transform.forward * arrowLength;
      // Gizmos.DrawLine(start, end);

      // // Draw arrowhead
      // Vector3 right = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
      // Vector3 left = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
      // Gizmos.DrawLine(end, end + right * arrowLength * 0.2f);
      // Gizmos.DrawLine(end, end + left * arrowLength * 0.2f);
  }
}