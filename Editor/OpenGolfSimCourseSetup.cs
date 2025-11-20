using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple property drawer for the Hole class:
/// - Shows the element as a foldout
/// - Displays 'name' as read-only (disabled)
/// - Draws the other fields normally
/// This allows the default array UI (add/remove/reorder) to remain available.
/// Put this file in an Editor folder (e.g. Assets/Editor).
/// </summary>
[CustomPropertyDrawer(typeof(OGSCourseHole))]
public class HolePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // fetch sub-properties
        SerializedProperty nameProp = property.FindPropertyRelative("name");
        SerializedProperty teeProp = property.FindPropertyRelative("teeLocalPosition");
        SerializedProperty holePosProp = property.FindPropertyRelative("holeLocalPosition");
        SerializedProperty hasAimPoint = property.FindPropertyRelative("hasAimPoint");
        SerializedProperty aimPosProp = property.FindPropertyRelative("aimLocalPosition");
        SerializedProperty parProp = property.FindPropertyRelative("par");
        // SerializedProperty radiusProp = property.FindPropertyRelative("radius");

        // use name as foldout label when available
        string foldLabel = !string.IsNullOrEmpty(nameProp.stringValue) ? nameProp.stringValue : label.text;

        EditorGUI.BeginProperty(position, label, property);

        float line = EditorGUIUtility.singleLineHeight;
        float pad = 2f;

        // Foldout
        Rect foldRect = new Rect(position.x, position.y, position.width, line);
        property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, foldLabel, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + line + pad;

            // Name (read-only)
            Rect nameRect = new Rect(position.x, y, position.width, line);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Name"));
            EditorGUI.EndDisabledGroup();
            y += line + pad;

            // Tee position
            Rect teeRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(teeRect, teeProp, new GUIContent("Tee Position (local)"));
            y += line + pad;

            // Hole position
            Rect holeRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(holeRect, holePosProp, new GUIContent("Hole Position (local)"));
            y += line + pad;

            // Par
            Rect parRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(parRect, parProp, new GUIContent("Par"));
            y += line + pad;

            // Use Aim toggle
            Rect useAimRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(useAimRect, hasAimPoint, new GUIContent("Use Aim Point"));
            y += line + pad;

            if (hasAimPoint.boolValue) {
                // Aim position
                Rect aimRect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(aimRect, aimPosProp, new GUIContent("Aim Position (local)"));
            }
            y += line + pad;
            // // Radius
            // Rect radiusRect = new Rect(position.x, y, position.width, line);
            // EditorGUI.PropertyField(radiusRect, radiusProp, new GUIContent("Radius"));

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float pad = 2f;

        if (property.isExpanded)
        {
            // fold + name + tee + useAim + (aim?) + hole + par + radius + paddings
            SerializedProperty useAimProp = property.FindPropertyRelative("hasAimPoint");
            bool useAim = useAimProp != null && useAimProp.boolValue;
            int lines = 1 + 1 + 1 + (useAim ? 1 : 0) + 1 + 1 + 1; // fold not counted separately here
            return line * lines + pad * (lines);
        }
        else
        {
            return line + pad;
        }
    }
}
/// <summary>
/// Custom editor for OGSCourse. Draws interactive handles in Scene view to move tee and hole positions
/// without instantiating extra GameObjects. Uses SerializedProperty so changes are undoable and prefab-friendly.
/// Put this file in an Editor folder (e.g. Assets/Editor).
/// </summary>
[CustomEditor(typeof(OGSCourse))]
public class OpenGolfSimCourseSetup : Editor
{
    private float holeRadius = 1.5f;
    SerializedProperty holeCountProp;
    SerializedProperty holesProp;
    SerializedProperty snapToGroundProp;
    SerializedProperty groundSnapMaxDistanceProp;
    SerializedProperty groundLayerMaskProp;

    private void OnEnable()
    {
        holeCountProp = serializedObject.FindProperty("holeCount");
        holesProp = serializedObject.FindProperty("holes");

        snapToGroundProp = serializedObject.FindProperty("snapToGroundOnEdit");
        groundSnapMaxDistanceProp = serializedObject.FindProperty("groundSnapMaxDistance");
        groundLayerMaskProp = serializedObject.FindProperty("groundLayerMask");        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(holeCountProp);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Ground Snapping (Editor)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(snapToGroundProp, new GUIContent("Snap To Ground"));
        EditorGUILayout.PropertyField(groundSnapMaxDistanceProp, new GUIContent("Max Distance"));
        EditorGUILayout.PropertyField(groundLayerMaskProp, new GUIContent("Ground Layer Mask"));

        EditorGUILayout.Space();

        // Small utility buttons
        EditorGUILayout.BeginHorizontal();
        
        // if (GUILayout.Button("Reset Hole Names"))
        // {
        //     for (int i = 0; i < holesProp.arraySize; i++)
        //     {
        //         SerializedProperty hole = holesProp.GetArrayElementAtIndex(i);
        //         hole.FindPropertyRelative("name").stringValue = $"Hole {i + 1}";
        //     }
        // }
        // if (GUILayout.Button("Set Typical Pars (3-4-5 pattern)"))
        // {
        //     for (int i = 0; i < holesProp.arraySize; i++)
        //     {
        //         SerializedProperty hole = holesProp.GetArrayElementAtIndex(i);
        //         int par = 4;
        //         if ((i % 3) == 0) par = 4;
        //         else if ((i % 3) == 1) par = 3;
        //         else par = 5;
        //         hole.FindPropertyRelative("par").intValue = par;
        //     }
        // }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Draw the list (expandable)
        EditorGUILayout.PropertyField(holesProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    // Scene GUI for interactive positioning
    private void OnSceneGUI()
    {
        OGSCourse gc = (OGSCourse)target;

        // Make sure we have up-to-date serialized properties
        serializedObject.Update();

        Transform t = gc.transform;

        for (int i = 0; i < holesProp.arraySize; i++)
        {
            SerializedProperty holeProp = holesProp.GetArrayElementAtIndex(i);
            SerializedProperty teeProp = holeProp.FindPropertyRelative("teeLocalPosition");
            SerializedProperty holePosProp = holeProp.FindPropertyRelative("holeLocalPosition");
            SerializedProperty parProp = holeProp.FindPropertyRelative("par");
            SerializedProperty nameProp = holeProp.FindPropertyRelative("name");
            SerializedProperty aimProp = holeProp.FindPropertyRelative("aimLocalPosition");
            SerializedProperty hasAimPointProp = holeProp.FindPropertyRelative("hasAimPoint");

            Vector3 aimLocal = aimProp.vector3Value;
            bool hasAimPoint = hasAimPointProp != null && hasAimPointProp.boolValue;

            Vector3 teeLocal = teeProp.vector3Value;
            Vector3 holeLocal = holePosProp.vector3Value;
            // float radius = radiusProp.floatValue;
            int par = parProp.intValue;
            string name = nameProp.stringValue;

            Vector3 teeWorld = t.TransformPoint(teeLocal);
            Vector3 holeWorld = t.TransformPoint(holeLocal);
            Vector3 aimWorld = t.TransformPoint(aimLocal);


            Color handleColor = new Color(1.0f, 0.3f, 0.0f);
            // Handles.color = Color.Lerp(Color.green, Color.red, 0.5f);
            // Handles.DrawLine(teeWorld, holeWorld);
            Handles.color = handleColor;

            if (hasAimPoint)
            {
                Handles.DrawLine(teeWorld, aimWorld, 2f);
                Handles.DrawLine(aimWorld, holeWorld, 2f);
            }
            else
            {
                Handles.DrawLine(teeWorld, holeWorld, 2.0f);
            }

            // Tee handle
            Handles.color = new Color(0.2f, 0.8f, 0.2f);
            EditorGUI.BeginChangeCheck();
            Vector3 newTeeWorld = Handles.PositionHandle(teeWorld, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                if (gc.snapToGroundOnEdit)
                {
                    float snappedY = GetGroundY(newTeeWorld, gc);
                    newTeeWorld.y = snappedY;
                }

                Undo.RecordObject(gc, $"Move Tee for {name}");
                teeProp.vector3Value = t.InverseTransformPoint(newTeeWorld);
                serializedObject.ApplyModifiedProperties();
            }

            // Aim handle (blue) - only if useAim enabled
            Vector3 newAimWorld = Vector3.zero;
            if (hasAimPoint)
            {
                Handles.color = new Color(0.2f, 0.5f, 1f);
                EditorGUI.BeginChangeCheck();
                newAimWorld = Handles.PositionHandle(aimWorld, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    if (gc.snapToGroundOnEdit)
                    {
                        float snappedY = GetGroundY(newAimWorld, gc);
                        newAimWorld.y = snappedY;
                    }

                    Undo.RecordObject(gc, $"Move Aim for {name}");
                    aimProp.vector3Value = t.InverseTransformPoint(newAimWorld);
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Hole handle
            Handles.color = new Color(1.0f, 0.3f, 0.0f);
            EditorGUI.BeginChangeCheck();
            Vector3 newHoleWorld = Handles.PositionHandle(holeWorld, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                if (gc.snapToGroundOnEdit)
                {
                    float snappedY = GetGroundY(newHoleWorld, gc);
                    newHoleWorld.y = snappedY;
                }

                Undo.RecordObject(gc, $"Move Pin for {name}");
                holePosProp.vector3Value = t.InverseTransformPoint(newHoleWorld);
                serializedObject.ApplyModifiedProperties();
            }

            // Handles.color = new Color(1f, 0.5f, 0.2f, 0.3f);
            Handles.color = new Color(1.0f, 0.4f, 0f, 0.25f);
            Handles.DrawSolidDisc(t.TransformPoint(holeLocal), Vector3.up, holeRadius);
            if (hasAimPoint) {
                Handles.DrawSolidDisc(t.TransformPoint(aimLocal), Vector3.up, holeRadius);
            }
            Handles.DrawSolidDisc(t.TransformPoint(teeLocal), Vector3.up, holeRadius);

            Handles.color = Color.green;
            Handles.DotHandleCap(0, t.TransformPoint(teeLocal), Quaternion.identity, 0.1f, EventType.Repaint);
            

            if (hasAimPoint)
            {
                Handles.color = new Color(0.2f, 0.5f, 1f);
                Handles.DotHandleCap(0, t.TransformPoint(aimLocal), Quaternion.identity, 0.09f, EventType.Repaint);
            }

            Handles.DotHandleCap(0, t.TransformPoint(holeLocal), Quaternion.identity, 0.1f, EventType.Repaint);
            // Handles.Disc(0, Quaternion.identity, t.TransformPoint(holeLocal), 0.1f, EventType.Repaint);

            Vector3 labelPos = (t.TransformPoint(teeLocal) + t.TransformPoint(holeLocal)) * 0.5f;
            float distanceMeters = Vector3.Distance(newTeeWorld, newHoleWorld);

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
            Handles.Label(t.TransformPoint(teeLocal) + Vector3.up * 0.5f, $"Tee Box {i + 1}\n{Mathf.Round(MetersToYards(distanceMeters))} yd", labelStyle);
            Handles.Label(t.TransformPoint(holeLocal) + Vector3.up * 0.5f, $"Hole {i + 1}\nPar {par}", labelStyle);

            
            GUIStyle boxStyle = EditorStyles.helpBox;
            boxStyle.normal.textColor = Color.white;
            boxStyle.fontStyle = FontStyle.Bold;
            boxStyle.fontSize = 10;

            string lineLabel = $"{Mathf.Round(MetersToYards(distanceMeters))} yd";

            if (hasAimPoint) {
                labelPos = (t.TransformPoint(aimLocal) + t.TransformPoint(holeLocal)) * 0.5f;
                Vector3 aimLabelPos = (t.TransformPoint(teeLocal) + t.TransformPoint(aimLocal)) * 0.5f;

                float distanceToAimMeters = Vector3.Distance(newTeeWorld, newAimWorld);
                float distanceToHoleMeters = Vector3.Distance(newAimWorld, newHoleWorld);

                Handles.Label(t.TransformPoint(aimLocal) + Vector3.up * 0.5f, $"Aim Point {i + 1}", labelStyle);
                // info box between points
                Handles.Label(aimLabelPos + Vector3.up * 0.8f, $"{Mathf.Round(MetersToYards(distanceToAimMeters))} yd", boxStyle);
                
                lineLabel = $"{Mathf.Round(MetersToYards(distanceToHoleMeters))} yd";
            }
            
            // info box between points
            Handles.Label(labelPos + Vector3.up * 0.8f, lineLabel, boxStyle);

        }

        serializedObject.ApplyModifiedProperties();
    }
    private float MetersToYards(float meters)
    {
        return meters * 1.09361f;
    }
    private float GetGroundY(Vector3 worldPosition, OGSCourse gc)
    {
        // Use the LayerMask value stored on the component
        int mask = (gc.groundLayerMask).value;

        // Maximum distance up/down to search
        float max = Mathf.Max(0.01f, gc.groundSnapMaxDistance);

        // First try casting down from above
        Vector3 originAbove = worldPosition + Vector3.up * max;
        if (Physics.Raycast(originAbove, Vector3.down, out RaycastHit hitDown, max * 2f, mask))
        {
            return hitDown.point.y;
        }

        // If nothing below, try casting up from below
        Vector3 originBelow = worldPosition + Vector3.down * max;
        if (Physics.Raycast(originBelow, Vector3.up, out RaycastHit hitUp, max * 2f, mask))
        {
            return hitUp.point.y;
        }

        // No ground found within range; return original Y
        return worldPosition.y;
    }
}