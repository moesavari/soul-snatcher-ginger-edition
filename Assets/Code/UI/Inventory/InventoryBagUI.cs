using UnityEngine;
using System.Collections.Generic;
using Game.Core.Inventory;
using TMPro;

public class InventoryBagUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject _root;
    [SerializeField] private ItemContextMenuUI _context;
    [SerializeField] private TMP_Text _goldText;

    [Header("Grid")]
    [SerializeField] private UIGridPool<InventoryCellView> _pool;
    [SerializeField, Min(1)] private int _slotsVisible = 20;

    [Header("Optional data source")]
    [SerializeField] private Inventory _inventoryOverride;

    private Inventory _inv;
    private int _lastToggleFrame = -1;

    private void Awake()
    {
        if (_pool)
            _pool.BuildOnce();

        foreach (var cell in _pool.Cells)
        {
            cell.Clicked -= OnCellClicked;
            cell.Clicked += OnCellClicked;
        }

        for (int i = 0; i < _pool.Cells.Count; i++)
            _pool.Cells[i].gameObject.SetActive(i < _slotsVisible);

        DrawEmpty();
    }

    private void OnEnable()
    {
        ResolveInventory();

        if (_inv != null)
        {
            _inv.OnItemAdded += HandleInvChanged;
            _inv.OnItemRemoved += HandleInvChanged;
        }

        var wallet = CurrencyWallet.Instance;
        if (wallet != null)
        {
            wallet.OnGoldChanged += UpdateGold;
            UpdateGold(wallet.gold);
        }
        else
        {
            UpdateGold(0);
        }

        InputManager.ToggleInventoryPressed += OnToggleInventory;
        InputManager.EscapePressed += OnEscape;

        RedrawFromInventory();
    }

    private void OnDisable()
    {
        if (_inv != null)
        {
            _inv.OnItemAdded -= HandleInvChanged;
            _inv.OnItemRemoved -= HandleInvChanged;
            _inv = null;
        }

        var wallet = CurrencyWallet.Instance;
        if (wallet != null) wallet.OnGoldChanged -= UpdateGold;

        InputManager.ToggleInventoryPressed -= OnToggleInventory;
        InputManager.EscapePressed          -= OnEscape;
    }

    private void OnToggleInventory()
    {
        if(!_root) return;
        if (_lastToggleFrame == Time.frameCount) return;

        _lastToggleFrame = Time.frameCount;
        _root.SetActive(!_root.activeSelf);
    }

    private void OnEscape()
    {
        if(_root && _root.activeSelf) _root.SetActive(false);
    }

    private void OnCellClicked(ItemDef def, int amount, Vector2 screenPos)
    {
        if (def == null || _context == null) return;

        bool shopOpen =
            ShopController.Instance != null &&
            ShopController.Instance.activeVendor != null;

        if (shopOpen)
        {
            _context.ShowShopSell(def, screenPos);
        }
        else
        {
            _context.ShowInventory(def, false, screenPos);
        }
    }

    private void ResolveInventory()
    {
        if (_inventoryOverride != null) { _inv = _inventoryOverride; return; }
        _inv = PlayerContext.Instance?.facade?.inventory;
    }

    private void HandleInvChanged(Inventory _, ItemStack __)
    {
        RedrawFromInventory();
    }

    private void UpdateGold(int amount)
    {
        if(_goldText) _goldText.text = CurrencyWallet.Instance.gold.ToString();
    }

    public void Show() { if (_root) _root.SetActive(true); }
    public void Hide() { if (_root) _root.SetActive(false); }
    public void Toggle() { if (_root) _root.SetActive(!_root.activeSelf); }

    public void DrawEmpty()
    {
        if (_pool == null) return;

        _pool.ForEach((cell, i) =>
        {
            if (!cell.gameObject.activeSelf) return;
            cell.ShowEmpty();
        });
    }

    public void RedrawFromInventory()
    {
        if (_pool == null)
        {
            DebugManager.LogWarning("Pool not assigned.", this);
            return;
        }

        if (_inv == null || _inv.contents == null)
        {
            DrawEmpty();
            return;
        }

        IReadOnlyList<ItemStack> list = _inv.contents;

        int i = 0;
        for (; i < _pool.Cells.Count; i++)
        {
            var cell = _pool.Cells[i];
            if (!cell.gameObject.activeSelf) continue;

            if (i < list.Count)
            {
                var s = list[i];

                bool empty = s.IsEmpty || s.def == null || s.amount <= 0;

                if (empty)
                    cell.ShowEmpty();
                else
                    cell.Bind(s.def, s.amount);
            }
            else
            {
                cell.ShowEmpty();
            }
        }
    }
}
