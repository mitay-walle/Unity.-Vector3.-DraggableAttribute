// based on: https://medium.com/@ProGM/show-a-draggable-point-into-the-scene-linked-to-a-vector3-field-using-the-handle-api-in-unity-bffc1a98271d


using System;
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

    public DraggableAttribute(bool local = false, float snapValue = -1f)
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
[CustomEditor(typeof(DraggableBehaviour), true)]
public class DraggablePointDrawer : Editor
{
    private SerializedObject serObj;
    private Type targetType;
    private Transform tr;

    private void OnEnable()
    {
        serObj = new SerializedObject(target); // unity hit an error if we use Editor's serialzedObject property
        targetType = serObj.targetObject.GetType();
        tr = (serObj.targetObject as Component).transform;
    }

    private void OnSceneGUI()
    {
        var property = serObj.GetIterator();

        var needApply = false;

        while (property.Next(true))
        {
            var isArray = false;

            if (property.isArray && property.arraySize > 0)
                isArray = property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.Vector3;

            if (property.propertyType == SerializedPropertyType.Vector3 || isArray)
            {
                needApply = true;

                var field = GetFieldViaPath(targetType, property.propertyPath);

                if (field == null) continue;

                var draggablePoints = field.GetCustomAttributes(typeof(DraggableAttribute), false);

                if (draggablePoints.Length > 0)
                {
                    var attr = draggablePoints[0] as DraggableAttribute;

                    var snapVal = -1f;

                    if (attr.SnapValue > -1)
                    {
                        snapVal = attr.SnapValue;
                    }
                    else if (!attr.SnapName.IsNullOrEmpty())
                    {
                        snapVal = serObj.FindProperty(attr.SnapName).floatValue;
                    }

                    var isSnapping = snapVal > -1f;

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
                                prop.vector3Value = isSnapping ? Snap(pos, snapVal) : pos;
                            }
                            else
                            {
                                if (isSnapping) pos = Snap(pos, snapVal);
                                prop.vector3Value = Handles.PositionHandle(pos, rot);
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
                            property.vector3Value = isSnapping ? Snap(pos, snapVal) : pos;
                        }
                        else
                        {
                            if (isSnapping) pos = Snap(pos, snapVal);
                            property.vector3Value = Handles.PositionHandle(pos, rot);
                        }
                    }
                }
            }
        }

        if (needApply)
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
