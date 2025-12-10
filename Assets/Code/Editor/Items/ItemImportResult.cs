using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.EditorTools.Items
{
    [Serializable]
    public sealed class ItemImportResult
    {
        [Serializable]
        public sealed class ItemRecord
        {
            public string jsonId;
            public string displayName;
            public string assetPath;
            public ItemDef itemDef;
            public Sprite icon;
            public bool isNew;
        }

        public List<ItemRecord> items = new List<ItemRecord>();
    }
}