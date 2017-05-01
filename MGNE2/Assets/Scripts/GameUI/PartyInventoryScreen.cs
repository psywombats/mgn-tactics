﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyInventoryScreen : MonoBehaviour, InputListener {

    public GridLayoutGroup InventoryGroup;
    public GridLayoutGroup CategoryGroup;
    public Text ItemDescription;
    public Text CategoryName;
    public Image ImageIcon;

    public int InventoryCellCount;
    public int CategoryCellCount;

    private static PartyInventoryScreen instance;

    private PartyInventory inventory;
    private int scrollOffset;
    private int cursorIndex;
    private bool transitioning;

    public static PartyInventoryScreen GetInstance() {
        if (instance == null) {
            instance = FindObjectOfType<PartyInventoryScreen>();
        }
        return instance;
    }

    public void Populate() {
        scrollOffset = 0;
        cursorIndex = 0;
        inventory = GGlobal.Instance().Party.Inventory;
        transitioning = false;
        ItemDescription.text = "";
        ImageIcon.sprite = null;
        DisplayInventory();
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (transitioning) {
            return true;
        }
        if (eventType != InputManager.Event.Down) {
            return true;
        }
        switch (command) {
            case InputManager.Command.Cancel:
                StartCoroutine(TransitionOut());
                break;
            case InputManager.Command.Confirm:
                Select();
                break;
            case InputManager.Command.Down:
                MoveCursor(1);
                break;
            case InputManager.Command.Up:
                MoveCursor(-1);
                break;
        }
        return true;
    }

    public void DisplayInventory() {
        for (int i = 0; i < InventoryCellCount; i += 1) {
            int index = scrollOffset + i;
            InventoryItemCell cell = InventoryGroup.GetComponentsInChildren<InventoryItemCell>(true)[i];
            if (index < inventory.ItemCount()) {
                cell.gameObject.SetActive(true);
                InventoryEntry entry = inventory.ItemAtIndex(index);
                cell.Populate(entry, cursorIndex == i);
            } else {
                cell.gameObject.SetActive(false);
            }
        }
        int selectedIndex = scrollOffset + cursorIndex;
        if (selectedIndex < inventory.ItemCount()) {
            InventoryEntry selectedEntry = inventory.ItemAtIndex(selectedIndex);
            ItemDescription.text = selectedEntry.Item.Description;
        }
    }

    public IEnumerator TransitionIn() {
        transitioning = true;

        Populate();
        Global.Instance().Input.PushListener(this);

        // placeholder
        while (GetComponent<CanvasGroup>().alpha < 1.0f) {
            GetComponent<CanvasGroup>().alpha += Time.deltaTime / 0.25f;
            if (GetComponent<CanvasGroup>().alpha > 1.0f) {
                GetComponent<CanvasGroup>().alpha = 1.0f;
            }
            yield return null;
        }

        transitioning = false;
    }

    public IEnumerator TransitionOut() {
        transitioning = true;

        // placeholder
        while (GetComponent<CanvasGroup>().alpha > 0.0f) {
            GetComponent<CanvasGroup>().alpha -= Time.deltaTime / 0.25f;
            if (GetComponent<CanvasGroup>().alpha < 0.0f) {
                GetComponent<CanvasGroup>().alpha = 0.0f;
            }
            yield return null;
        }

        Global.Instance().Input.RemoveListener(this);
        transitioning = false;
    }

    private void ShowEntry(InventoryEntry entry) {
        ItemDescription.text = entry.Item.Description;
        ImageIcon.sprite = entry.Item.IconBig;
    }

    private void MoveCursor(int delta) {
        int index = cursorIndex + scrollOffset;
        if (index == 0 && delta < 0) {
            cursorIndex = Mathf.Min(InventoryCellCount - 1, inventory.ItemCount() - 1);
            scrollOffset = inventory.ItemCount() - InventoryCellCount;
            if (scrollOffset < 0) {
                scrollOffset = 0;
            }
        } else if (index == inventory.ItemCount() - 1 && delta > 0) {
            scrollOffset = 0;
            cursorIndex = 0;
        } else {
            cursorIndex += delta;
            if (delta > 0 && (cursorIndex == InventoryCellCount - 1) && scrollOffset + (InventoryCellCount - 1) < (inventory.ItemCount() - 1)) {
                scrollOffset += 1;
                cursorIndex -= 1;
            } else if (delta < 0 && cursorIndex == 0 && scrollOffset > 0) {
                scrollOffset -= 1;
                cursorIndex += 1;
            }
        }
        DisplayInventory();
    }

    private void Select() {

    }
}