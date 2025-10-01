using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
using TMPro;
#endif

public class InventoryBagUI : MonoBehaviour
{
    public enum RenderMode { StaticGrid, DynamicList }

    [Header("Mode")]
    [SerializeField] private RenderMode _mode = RenderMode.StaticGrid;

    [Header("Data source")]
    [Tooltip("Leave null to auto-use the player's inventory from PlayerContext.")]
    [SerializeField] private Inventory _inventoryOverride;

    [Header("Refs")]
    [SerializeField] private CharacterSheetUI _characterSheetUI;
    [SerializeField] private Transform _gridRoot;

    [Header("DynamicList mode only")]
    [SerializeField] private GameObject _buttonPrefab; // prefab asset, not a scene object

    private Inventory _inv;                                   // bound inventory
    private readonly List<Button> _buttons = new();           // static grid buttons
    private readonly List<Sprite> _baseSprites = new();       // cached frame sprites
    private readonly List<BagCellView> _cells = new();

    private void Awake()
    {
        if (_mode == RenderMode.StaticGrid)
            CacheStaticButtons();
    }

    private void OnEnable()
    {
        BindInventory();
        Redraw();
    }

    private void OnDisable()
    {
        UnbindInventory();
        if (_mode == RenderMode.DynamicList)
            ClearDynamicChildren();
    }

    // ---------- binding ----------
    private Inventory ResolveInventory()
    {
        if (_inventoryOverride) return _inventoryOverride;
        if (PlayerContext.Instance != null && PlayerContext.Instance.facade != null)
            return PlayerContext.Instance.facade.inventory;
        return null;
    }

    private void BindInventory()
    {
        UnbindInventory();
        _inv = ResolveInventory();
        if (_inv == null)
        {
            DebugManager.LogWarning("No inventory found.", this);
            return;
        }
        _inv.OnItemAdded += OnInvChanged;
        _inv.OnItemRemoved += OnInvChanged;
    }

    private void UnbindInventory()
    {
        if (_inv != null)
        {
            _inv.OnItemAdded -= OnInvChanged;
            _inv.OnItemRemoved -= OnInvChanged;
            _inv = null;
        }
    }

    private void OnInvChanged(Inventory inv, ItemStack _) => Redraw();

    // ---------- UI ----------
    public void Redraw()
    {
        if (_inv == null || _gridRoot == null) return;

        switch (_mode)
        {
            case RenderMode.StaticGrid:
                PaintStaticGrid();
                break;

            case RenderMode.DynamicList:
                BuildDynamicList();
                break;
        }
    }

    // ===== STATIC GRID =====
    private void CacheStaticButtons()
    {
        _buttons.Clear();
        _baseSprites.Clear();
        _cells.Clear();

        if (!_gridRoot) return;

        // grab buttons in layout order
        _gridRoot.GetComponentsInChildren(true, _buttons);

        foreach (var b in _buttons)
        {
            // cache original frame
            var img = b.GetComponent<Image>();
            _baseSprites.Add(img ? img.sprite : null);

            // cache the BagCellView that handles hover/click
            var cell = b.GetComponent<BagCellView>();
            _cells.Add(cell);
        }
    }

    private void PaintStaticGrid()
    {
        if (_buttons.Count == 0) CacheStaticButtons();
        if (_inv == null) return;

        var slots = _inv.contents;
        int count = Mathf.Min(_buttons.Count, slots.Count);

        for (int i = 0; i < count; i++)
        {
            var b = _buttons[i];
            var cell = _cells[i];
            var s = slots[i];

            if (!b) continue;

            if (cell != null)
            {
                cell.Bind(s.def, s.amount);
                cell.Refresh();
            }

            var img = b.GetComponent<Image>();
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
            var label = b.GetComponentInChildren<TMP_Text>(true);
#else
            var label = b.GetComponentInChildren<Text>(true);
#endif
            // clear old listeners then add a fresh one
            b.onClick.RemoveAllListeners();

            if (s.IsEmpty || s.def == null)
            {
                if (img) img.sprite = _baseSprites.Count > i ? _baseSprites[i] : img.sprite;
                if (label) label.text = "";
            }
            else
            {
                if (img) img.sprite = s.def.icon ? s.def.icon : (_baseSprites.Count > i ? _baseSprites[i] : img.sprite);
                if (label)
                    label.text = s.def.stackable && s.amount > 1
                        ? $"{s.def.displayName} x{s.amount}"
                        : s.def.displayName;

                if (_characterSheetUI != null)
                {
                    var captured = s.def;
                    b.onClick.AddListener(() => _characterSheetUI.SetSelectedItem(captured));
                }
            }
        }
    }

    // ===== DYNAMIC LIST =====
    private void BuildDynamicList()
    {
        if (_buttonPrefab == null)
        {
            DebugManager.LogWarning("Button Prefab is null (DynamicList mode). Assign a prefab asset.", this);
            return;
        }

        ClearDynamicChildren();

        var list = _inv.contents;
        foreach (var s in list)
        {
            if (s.IsEmpty || s.def == null) continue;
            var go = Instantiate(_buttonPrefab, _gridRoot);

            var img = go.GetComponentInChildren<Image>(true);
            if (img) img.sprite = s.def.icon;

#if TMP_PRESENT || TEXTMESHPRO_PRESENT
            var label = go.GetComponentInChildren<TMP_Text>(true);
#else
            var label = go.GetComponentInChildren<Text>(true);
#endif
            if (label)
                label.text = s.def.stackable && s.amount > 1
                    ? $"{s.def.displayName} x{s.amount}"
                    : s.def.displayName;

            var btn = go.GetComponent<Button>();
            if (btn && _characterSheetUI)
            {
                var captured = s.def;
                btn.onClick.AddListener(() => _characterSheetUI.SetSelectedItem(captured));
            }
        }
    }

    private void ClearDynamicChildren()
    {
        foreach (Transform c in _gridRoot) Destroy(c.gameObject);
    }
}
