using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OGSCourseHole
{
    public string name = "Hole";
    [Tooltip("Tee position, local to the parent GameObject's transform.")]
    public Vector3 teeLocalPosition = Vector3.zero;

    [Tooltip("Hole (pin) position, local to the parent GameObject's transform).")]
    public Vector3 holeLocalPosition = Vector3.forward * 10f;
    
    [Tooltip("Enable an aim point on this hole")]
    public bool hasAimPoint = false;

    [Tooltip("Aim point position")]
    public Vector3 aimLocalPosition = Vector3.forward * 5f;

    [Tooltip("Par for this hole (typical values: 3, 4, 5).")]
    public int par = 4;

}

public class OGSCourse : MonoBehaviour
{
  [Header("Course Settings")]
  [Range(1, 18)]
  [Tooltip("Number of holes to define (1-18).")]
  public int holeCount = 18;
  
  [Tooltip("List of holes. Positions are stored as local positions relative to the GameObject this component is attached to.")]
  public List<OGSCourseHole> holes = new List<OGSCourseHole>();

  [Header("Ground Snapping (Editor)")]
  [Tooltip("When true, the editor will snap hole tee/pin Y position to the nearest collider within the max distance.")]
  public bool snapToGroundOnEdit = true;

  [Tooltip("Maximum distance (up/down) to search for ground when snapping the Y coordinate.")]
  public float groundSnapMaxDistance = 50f;

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
    // Keep simple validation for each hole
    for (int i = 0; i < holes.Count; i++)
    {
      holes[i].name = $"Hole {i + 1}";
    }

    if (groundSnapMaxDistance < 0f) {
      groundSnapMaxDistance = 0f;
    }
  }  
}