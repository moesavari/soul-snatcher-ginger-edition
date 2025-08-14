using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HairVisuals))]
public class HairVisualsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var genderProp = serializedObject.FindProperty("_gender");
        var spriteRendererProp = serializedObject.FindProperty("_spriteRenderer");

        EditorGUILayout.PropertyField(genderProp);
        EditorGUILayout.PropertyField(spriteRendererProp);

        var gender = (HairVisuals.GenderType)genderProp.enumValueIndex;

        if (gender == HairVisuals.GenderType.Male)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hair Stage Sprites - Male", EditorStyles.boldLabel);
            DrawSprite("_maleStage1");
            DrawSprite("_maleStage2");
            DrawSprite("_maleStage3");
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hair Stage Sprites - Female", EditorStyles.boldLabel);
            DrawSprite("_femaleStage1");
            DrawSprite("_femaleStage2");
            DrawSprite("_femaleStage3");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSprite(string propertyName)
    {
        var prop = serializedObject.FindProperty(propertyName);
        if (prop != null) EditorGUILayout.PropertyField(prop);
    }
}
