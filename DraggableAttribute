// based on: https://medium.com/@ProGM/show-a-draggable-point-into-the-scene-linked-to-a-vector3-field-using-the-handle-api-in-unity-bffc1a98271d


using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class DraggableAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomEditor(typeof(MonoBehaviour), true)]
public class DraggablePointDrawer : Editor
{
    private SerializedObject serObj;
    private Type targetType;
    
    private void OnEnable()
    {
        serObj = new SerializedObject(target); // unity hit an error if we use Editor's serialzedObject property
        targetType = serObj.targetObject.GetType();
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

                if (field == null)
                {
                    continue;
                }

                var draggablePoints = field.GetCustomAttributes(typeof(DraggableAttribute), false);

                if (draggablePoints.Length > 0)
                {
                    if (property.isArray)
                    {
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var prop = property.GetArrayElementAtIndex(i);
                            prop.vector3Value =
                                Handles.PositionHandle(prop.vector3Value, Quaternion.identity);
                        }
                    }
                    else
                    {
                        Handles.Label(property.vector3Value, property.name);
                        property.vector3Value = Handles.PositionHandle(property.vector3Value, Quaternion.identity);
                    }

                }
            }
        }

        if (needApply)
            serObj.ApplyModifiedProperties();
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
