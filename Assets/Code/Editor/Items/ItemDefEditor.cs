using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDef), true)]
public class ItemDefEditor : Editor
{
    private SerializedProperty FindProp(params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var p = serializedObject.FindProperty(c);
            if (p != null) return p;
        }
        return null;
    }

    private static string[] NamesOf(params SerializedProperty[] props)
        => props.Where(p => p != null).Select(p => p.propertyPath).ToArray();

    private static T SafeEnum<T>(SerializedProperty prop, T fallback) where T : struct, Enum
    {
        if (prop == null) return fallback;
        try { return (T)Enum.ToObject(typeof(T), prop.enumValueIndex); }
        catch { return fallback; }
    }

    private int GetStatCount(ItemQuality q) => q switch
    {
        ItemQuality.Junk        => 1,
        ItemQuality.Common      => 2,
        ItemQuality.Rare        => 3,
        ItemQuality.Legendary   => 4,
        ItemQuality.Cursed      => 4,
        _ => 1,
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var kindProp = FindProp("_kind", "kind");
        var slotProp = FindProp("_equipSlot", "equipSlot");
        var twoHProp = FindProp("_twoHanded", "twoHanded");

        var qualityProp = FindProp("_quality", "quality");
        var statsProp = FindProp("_stats", "stats");
        var enchFlag = FindProp("_hasEnchantment", "hasEnchantment");
        var enchDesc = FindProp("_enchantmentDescription", "enchantmentDescription");

        DrawPropertiesExcluding(serializedObject,
            NamesOf(kindProp, slotProp, twoHProp, qualityProp, statsProp, enchFlag, enchDesc));

        var kind = SafeEnum(kindProp, ItemKind.Other);
        bool isGear = kind is ItemKind.Weapon or ItemKind.Armor or ItemKind.Accessory;

        if (isGear)
        {
            if (slotProp != null) EditorGUILayout.PropertyField(slotProp, new GUIContent("Equip Slot"));
            if (twoHProp != null) EditorGUILayout.PropertyField(twoHProp, new GUIContent("Two Handed"));
        }

        if (isGear && qualityProp != null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Gear System", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(qualityProp, new GUIContent("Quality"));
            var quality = SafeEnum(qualityProp, ItemQuality.Common);

            if (statsProp != null)
            {
                int expected = GetStatCount(quality);
                while (statsProp.arraySize < expected) statsProp.InsertArrayElementAtIndex(statsProp.arraySize);
                while (statsProp.arraySize > expected) statsProp.DeleteArrayElementAtIndex(statsProp.arraySize - 1);

                var allTypes = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToList();
                var currentSelections = new int[statsProp.arraySize];
                for (int i = 0; i < statsProp.arraySize; i++)
                {
                    var stat = statsProp.GetArrayElementAtIndex(i);
                    var typeProp = stat.FindPropertyRelative("statType");
                    currentSelections[i] = typeProp.enumValueIndex;
                }

                for (int i = 0; i < statsProp.arraySize; i++)
                {
                    var stat = statsProp.GetArrayElementAtIndex(i);
                    var typeProp = stat.FindPropertyRelative("statType");
                    var valueProp = stat.FindPropertyRelative("value");

                    var usedByOthers = new HashSet<int>(currentSelections.Where((v, idx) => idx != i));
                    var available = new List<int>();
                    for (int idx = 0; idx < allTypes.Count; idx++)
                        if (!usedByOthers.Contains(idx) || idx == typeProp.enumValueIndex)
                            available.Add(idx);

                    string[] optionNames = available.Select(idx => allTypes[idx].ToString()).ToArray();
                    int currInAvail = Mathf.Max(0, available.IndexOf(typeProp.enumValueIndex));

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"Stat {i + 1}", GUILayout.Width(60));

                    int chosen = EditorGUILayout.Popup(currInAvail, optionNames, GUILayout.MinWidth(100));
                    typeProp.enumValueIndex = available[Mathf.Clamp(chosen, 0, available.Count - 1)];
                    currentSelections[i] = typeProp.enumValueIndex;

                    EditorGUILayout.LabelField("Value", GUILayout.Width(42));
                    valueProp.intValue = EditorGUILayout.IntField(valueProp.intValue, GUILayout.Width(70));

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (quality is ItemQuality.Rare or ItemQuality.Legendary)
            {
                EditorGUILayout.Space(4);

                if (enchFlag != null && enchDesc != null)
                {
                    bool isLegendary = quality == ItemQuality.Legendary;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (isLegendary)
                        {

                            enchFlag.boolValue = true;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ToggleLeft("Has Enchantment", true, GUILayout.Width(150));
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {

                            EditorGUILayout.PropertyField(enchFlag, new GUIContent("Has Enchantment"), GUILayout.Width(200));
                        }
                    }

                    if (enchFlag.boolValue)
                    {
                        EditorGUILayout.PropertyField(enchDesc, new GUIContent("Enchantment Description"));
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
