// based on: https://medium.com/@ProGM/show-a-draggable-point-into-the-scene-linked-to-a-vector3-field-using-the-handle-api-in-unity-bffc1a98271d


using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
#if UNITY_EDITOR
using UnityEditor;

#endif

public abstract class DraggableBehaviour : MonoBehaviour
{
}

public class DraggableAttribute : PropertyAttribute
{
    public bool isLocal = false;
    public float SnapValue = -1f;
    public string SnapName;

    public DraggableAttribute(bool local = false)
    {
        isLocal = local;
    }
    
    public DraggableAttribute(bool local = false, float snapValue = 0f)
    {
        SnapValue = snapValue;
        isLocal = local;
    }

    public DraggableAttribute(bool local = false, string snapName = default)
    {
        if (!snapName.IsNullOrEmpty())
            SnapName = snapName;

        isLocal = local;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DraggableBehaviour), true),CanEditMultipleObjects]
public class DraggablePointDrawer : Editor
{
    private Type targetType;
    private Transform tr;

    
    public virtual void OnEnable()
    {
        targetType = serializedObject.targetObject.GetType();
        tr = (serializedObject.targetObject as Component).transform;
    }

    public virtual void OnSceneGUI()
    {
        var serObj = new SerializedObject(target);
        serObj.Update();
        
        bool needApply = false;

        var property = serObj.GetIterator();

        while (property.Next(true))
        {
            var isArray = false;

            if (property.isArray && property.arraySize > 0)
                isArray = property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.Vector3;

            if (property.propertyType == SerializedPropertyType.Vector3 || isArray)
            {

                var field = GetFieldViaPath(targetType, property.propertyPath);

                if (field == null) continue;

                var draggablePoints = field.GetCustomAttributes(typeof(DraggableAttribute), false);

                if (draggablePoints.Length > 0)
                {
                    needApply = true;
                    
                    var attr = draggablePoints[0] as DraggableAttribute;

                    var snapVal = 0f;

                    if (attr.SnapValue > 0f)
                    {
                        snapVal = attr.SnapValue;
                    }
                    else if (!attr.SnapName.IsNullOrEmpty())
                    {
                        snapVal = serObj.FindProperty(attr.SnapName).floatValue;
                    }

                    var isSnapping = snapVal > 0f;

                    
                    if (property.isArray)
                    {
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var prop = property.GetArrayElementAtIndex(i);

                            var pos = prop.vector3Value; // local
                            var rot = Quaternion.identity;

                            if (attr.isLocal)
                            {
                                rot = tr.rotation;
                                
                                pos = tr.TransformPoint(isSnapping ? Snap(pos, snapVal) : pos); // global snapped
                                
                                pos = tr.InverseTransformPoint(Handles.PositionHandle(pos, rot)); // local UNsnapped
                                prop.vector3Value = pos = isSnapping ? Snap(pos, snapVal) : pos;
                            }
                            else
                            {
                                if (isSnapping) pos = Snap(pos, snapVal);
                                prop.vector3Value = pos = Handles.PositionHandle(pos, rot);
                            }
                        }
                    }
                    else
                    {
                        var pos = property.vector3Value;
                        var rot = Quaternion.identity;

                        if (attr.isLocal)
                        {
                            rot = tr.rotation;
                                
                            pos = tr.TransformPoint(isSnapping ? Snap(pos, snapVal) : pos); // global snapped
                                
                            pos = tr.InverseTransformPoint(Handles.PositionHandle(pos, rot)); // local UNsnapped
                            property.vector3Value = pos = isSnapping ? Snap(pos, snapVal) : pos;
                        }
                        else
                        {
                            if (isSnapping) pos = Snap(pos, snapVal);
                            property.vector3Value =pos = Handles.PositionHandle(pos, rot);
                        }
                    }
                }
            }
        }
        
        if (needApply && SceneView.lastActiveSceneView == EditorWindow.focusedWindow)
            serObj.ApplyModifiedProperties();
    }


    public static Vector3 Snap(Vector3 pos, float snapValue)
    {
        pos.x = Snap(pos.x, snapValue);
        pos.y = Snap(pos.y, snapValue);
        pos.z = Snap(pos.z, snapValue);
        return pos;
    }
    public static float Snap(float value,float snap)
    {
        return Mathf.Round(value / snap) * snap;
    }

    public static System.Reflection.FieldInfo GetFieldViaPath(Type type, string path)
    {
        var parentType = type;
        var fi = type.GetField(path);

        var perDot = path.Split('.');

        foreach (var fieldName in perDot)
        {
            fi = parentType.GetField(fieldName);
            if (fi != null)
                parentType = fi.FieldType;
            else
                return null;
        }

        return fi;
    }
}
#endif
