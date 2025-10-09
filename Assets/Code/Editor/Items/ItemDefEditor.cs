using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ItemDef), true)]
public class ItemDefEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "_quality", "_stats", "_hasEnchantment", "_enchantmentDescription");

        SerializedProperty qualityProp = serializedObject.FindProperty("_quality");
        EditorGUILayout.PropertyField(qualityProp);
        int expectedStats = GetStatCount((ItemQuality)qualityProp.enumValueIndex);

        SerializedProperty statsProp = serializedObject.FindProperty("_stats");
        while (statsProp.arraySize < expectedStats)
            statsProp.InsertArrayElementAtIndex(statsProp.arraySize);
        while (statsProp.arraySize > expectedStats)
            statsProp.DeleteArrayElementAtIndex(statsProp.arraySize - 1);

        // Track used StatTypes (enum indices)
        List<int> usedIndices = new List<int>();
        for (int i = 0; i < expectedStats; i++)
        {
            var statProp = statsProp.GetArrayElementAtIndex(i);
            var statTypeProp = statProp.FindPropertyRelative("statType");
            var statValueProp = statProp.FindPropertyRelative("value");

            List<StatType> allTypes = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().ToList();
            List<StatType> availableTypes = allTypes.Where((t, idx) =>
                !usedIndices.Contains(idx) || idx == statTypeProp.enumValueIndex
            ).ToList();

            string[] options = availableTypes.Select(t => t.ToString()).ToArray();
            int currIdx = availableTypes.IndexOf((StatType)statTypeProp.enumValueIndex);
            if (currIdx == -1) currIdx = 0;

            int chosenIdx = EditorGUILayout.Popup($"Stat {i + 1}", currIdx, options);
            statTypeProp.enumValueIndex = (int)availableTypes[chosenIdx];
            usedIndices.Add(statTypeProp.enumValueIndex);

            statValueProp.intValue = EditorGUILayout.IntField("value", statValueProp.intValue);
        }

        // Enchantment section
        if ((ItemQuality)qualityProp.enumValueIndex == ItemQuality.Rare ||
            (ItemQuality)qualityProp.enumValueIndex == ItemQuality.Legendary)
        {
            SerializedProperty enchantProp = serializedObject.FindProperty("_hasEnchantment");
            SerializedProperty enchDescProp = serializedObject.FindProperty("_enchantmentDescription");
            EditorGUILayout.PropertyField(enchantProp, new GUIContent("Has Enchantment"));
            if (enchantProp.boolValue || (ItemQuality)qualityProp.enumValueIndex == ItemQuality.Legendary)
            {
                enchDescProp.stringValue = EditorGUILayout.TextField("Enchantment Description", enchDescProp.stringValue);
                enchantProp.boolValue = true;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    int GetStatCount(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Junk: return 1;
            case ItemQuality.Common: return 2;
            case ItemQuality.Rare: return 3;
            case ItemQuality.Legendary: return 4;
            default: return 1;
        }
    }
}
