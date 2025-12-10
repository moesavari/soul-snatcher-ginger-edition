using UnityEditor;
using UnityEngine;
using Game.Core.Inventory;

namespace Game.EditorTools.Items
{




    public sealed class ItemImportReportWindow : EditorWindow
    {
        private static ItemImportResult _result;

        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private int _selectedIndex = -1;

        public static void Open(ItemImportResult result)
        {
            _result = result;
            var window = GetWindow<ItemImportReportWindow>("Item Import Report");
            window.minSize = new Vector2(650f, 350f);
            window.Show();
        }

        // Core logic for OnGUI. Involves multiple steps and state changes.
        private void OnGUI()
        {
            if (_result == null || _result.items == null)
            {
                EditorGUILayout.HelpBox("No import data available.", MessageType.Info);

                if (GUILayout.Button("Close"))
                    Close();

                return;
            }

            EditorGUILayout.LabelField(
                $"Imported Items ({_result.items.Count})",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPanel();
                DrawSeparator();
                DrawDetailPanel();
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Ping Selected Asset", GUILayout.Width(160f)))
                    PingSelectedAsset();
            }
        }

        // Core logic for DrawListPanel. Involves multiple steps and state changes.
        private void DrawListPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.45f)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

                var items = _result.items;
                for (var i = 0; i < items.Count; i++)
                {
                    var record = items[i];
                    var isSelected = i == _selectedIndex;

                    var style = new GUIStyle(EditorStyles.helpBox)
                    {
                        normal =
                        {
                            background = isSelected ? Texture2D.grayTexture : null
                        }
                    };

                    using (new EditorGUILayout.VerticalScope(style))
                    {
                        if (GUILayout.Button(
                                $"{record.displayName}  (ID: {record.jsonId})",
                                EditorStyles.label))
                        {
                            _selectedIndex = i;
                        }


                        EditorGUILayout.LabelField(
                            $"Path: {record.assetPath}",
                            EditorStyles.miniLabel);
                    }

                    EditorGUILayout.Space(2f);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // Core logic for DrawDetailPanel. Involves multiple steps and state changes.
        private void DrawDetailPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                var items = _result.items;

                if (_selectedIndex < 0 || _selectedIndex >= items.Count)
                {
                    EditorGUILayout.HelpBox(
                        "Select an item on the left to view its details.",
                        MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    return;
                }

                var record = items[_selectedIndex];
                var def = record.itemDef;


                EditorGUILayout.LabelField(record.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"JSON ID: {record.jsonId}");
                EditorGUILayout.LabelField($"Asset Path: {record.assetPath}");
                EditorGUILayout.LabelField($"Status: {(record.isNew ? "Created" : "Updated")}");

                EditorGUILayout.Space(6f);


                def = (ItemDef)EditorGUILayout.ObjectField(
                    "ItemDef",
                    def,
                    typeof(ItemDef),
                    false);

                record.itemDef = def;

                record.icon = (Sprite)EditorGUILayout.ObjectField(
                    "Icon",
                    record.icon,
                    typeof(Sprite),
                    false);

                EditorGUILayout.Space(8f);


                if (def != null)
                {
                    DrawItemDetails(def);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "ItemDef reference is missing. The asset may have been moved or deleted.",
                        MessageType.Warning);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // Core logic for DrawItemDetails. Involves multiple steps and state changes.
        private void DrawItemDetails(ItemDef def)
        {
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);


            if (!string.IsNullOrWhiteSpace(def.description))
            {
                EditorGUILayout.LabelField("Description:", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(def.description, MessageType.None);
            }


            EditorGUILayout.LabelField("Item Code", def.itemCode);
            EditorGUILayout.LabelField("Value", def.value.ToString());

            var stackInfo = def.stackable
                ? $"Stackable (Max: {def.maxStack})"
                : "Not stackable";
            EditorGUILayout.LabelField("Stacking", stackInfo);


            EditorGUILayout.LabelField("Quality", def.quality.ToString());
            EditorGUILayout.LabelField("Kind", def.kind.ToString());
            EditorGUILayout.LabelField("Equip Slot", def.equipSlot.ToString());
            EditorGUILayout.LabelField("Two-Handed", def.twoHanded ? "Yes" : "No");

            EditorGUILayout.Space(4f);


            if (def.stats != null && def.stats.Count > 0)
            {
                EditorGUILayout.LabelField("Stats", EditorStyles.miniBoldLabel);
                foreach (var s in def.stats)
                {
                    EditorGUILayout.LabelField($" {s.statType}: {s.value}");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Stats", "None");
            }

            EditorGUILayout.Space(4f);


            if (def.hasEnchantment && !string.IsNullOrWhiteSpace(def.enchantmentDescription))
            {
                EditorGUILayout.LabelField("Enchantment", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(def.enchantmentDescription, MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField("Enchantment", "None");
            }
        }

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1f));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }

        private void PingSelectedAsset()
        {
            var items = _result.items;
            if (_selectedIndex < 0 || _selectedIndex >= items.Count)
                return;

            var def = items[_selectedIndex].itemDef;
            if (def == null)
                return;

            EditorGUIUtility.PingObject(def);
            Selection.activeObject = def;
        }
    }
}