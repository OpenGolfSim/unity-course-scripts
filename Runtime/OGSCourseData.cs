using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OGSCourseDataTeeBoxes
{
    public Vector3 blue = Vector3.zero;
    public Vector3 white = Vector3.zero;
    public Vector3 red = Vector3.zero;
    public Vector3 green = Vector3.zero;
}

[System.Serializable]
public class OGSCourseDataHole
{
    // [Header("Hole Settings")]
    public string name = "Hole";
    
    [Tooltip("Par for this hole (typical values: 3, 4, 5).")]
    public int par = 4;

    // legacy naming
    [Tooltip("White tee box position")]
    public Vector3 teeLocalPosition = Vector3.zero;
    
    // [Tooltip("Blue tee box positions")]
    // public Vector3 teeBluePosition = Vector3.zero;
    // [Tooltip("White tee box positions")]
    // public Vector3 teeWhite = Vector3.zero;
    // [Tooltip("Red tee box positions")]
    // public Vector3 teeRed = Vector3.zero;
    [Tooltip("Green tee box positions (par 3 mode)")]
    public Vector3 teeGreenPosition = Vector3.zero;
    // public OGSCourseDataTeeBoxes teeBoxes = new OGSCourseDataTeeBoxes();
    
    [Tooltip("Hole (pin) position, local to the parent GameObject's transform).")]
    public Vector3 holeLocalPosition = Vector3.forward * 10f;
    
    [Tooltip("Enable an aim point on this hole")]
    public bool hasAimPoint = false;

    [Tooltip("Aim point position")]
    public Vector3 aimLocalPosition = Vector3.forward * 5f;


}

public class OGSCourseData : MonoBehaviour
{
  [Header("Course Settings")]
  // [Range(1, 18)]
  // [Tooltip("Number of holes to define (1-18).")]
  // public int holeCount = 18;
  
  [Tooltip("List of holes. Positions are stored as local positions relative to the GameObject this component is attached to.")]
  public List<OGSCourseDataHole> holes = new List<OGSCourseDataHole>();

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