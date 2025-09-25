using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.UI;

public class InventoryBagUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private CharacterSheetUI _characterSheetUI;
    [SerializeField] private Transform _gridRoot;   
    [SerializeField] private GameObject _buttonPrefab;

    private void OnEnable()
    {
        Redraw();
        _inventory.OnItemAdded += (_, __) => Redraw();
        _inventory.OnItemRemoved += (_, __) => Redraw();
    }

    private void OnDisable()
    {
        _inventory.OnItemAdded -= (_, __) => Redraw();
        _inventory.OnItemRemoved -= (_, __) => Redraw();
    }

    private void Redraw()
    {
        foreach (Transform c in _gridRoot) Destroy(c.gameObject);

        foreach (var s in _inventory.contents)
        {
            if (s.IsEmpty) continue;
            var go = Instantiate(_buttonPrefab, _gridRoot);
            var img = go.GetComponentInChildren<Image>();
            var txt = go.GetComponentInChildren<Text>();
            if (img) img.sprite = s.def.icon;
            if (txt) txt.text = s.def.stackable ? $"{s.def.displayName} x{s.amount}" : s.def.displayName;

            var b = go.GetComponent<Button>();
            var capturedDef = s.def;
            b.onClick.AddListener(() => _characterSheetUI.SetSelectedItem(capturedDef));
        }
    }
}
