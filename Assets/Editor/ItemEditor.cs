using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Item))]
public class ItemEditor : Editor
{
    private System.Type[] _moduleTypes;

    private void OnEnable()
    {
        _moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(DynamicAdditionalInfo).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                !t.IsGenericType)
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "dynamicInfoModules");

        EditorGUILayout.Space(10);
        DrawDynamicModules();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDynamicModules()
    {
        var property = serializedObject.FindProperty("dynamicInfoModules");

        EditorGUILayout.LabelField("Dynamic Info Modules", EditorStyles.boldLabel);

        for (int i = 0; i < property.arraySize; i++)
        {
            var element = property.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            string typeName = element.managedReferenceValue?.GetType().Name ?? "Null";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                property.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(element, true);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Module"))
        {
            ShowAddModuleMenu(property);
        }
    }

    private void ShowAddModuleMenu(SerializedProperty property)
    {
        GenericMenu menu = new GenericMenu();

        foreach (var type in _moduleTypes)
        {
            menu.AddItem(
                new GUIContent(type.Name),
                false,
                () =>
                {
                    serializedObject.Update();

                    int index = property.arraySize;
                    property.InsertArrayElementAtIndex(index);

                    var element = property.GetArrayElementAtIndex(index);
                    element.managedReferenceValue = Activator.CreateInstance(type);

                    serializedObject.ApplyModifiedProperties();
                });
        }

        menu.ShowAsContext();
    }
}