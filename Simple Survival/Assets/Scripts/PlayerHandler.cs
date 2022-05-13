using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryItem {
    Log, Stone, None
}

static class InventoryItemMethods {
	public static string GetPath(this InventoryItem item) {
		switch (item) {
			case InventoryItem.Stone:
				return "stone";
			case InventoryItem.Log:
				return "log";
			default:
				return "";
		}
	}
}


public class PlayerHandler : MonoBehaviour {

    private Camera cam;
    public Image inventoryImage;
    public TerrainGenerator terrainGen;
    public InventoryItem[] inventory = Enumerable.Repeat<InventoryItem>(InventoryItem.None, 40).ToArray();

    // Start is called before the first frame update
    void Start() {
        cam = GetComponentInChildren<Camera>();
        inventoryImage.enabled = false;
    }

    bool InventoryHasSpace(int count = 0) {
        int available = 0;
        for (int i = 0; i < inventory.Length; i++)
            if (inventory[i] == InventoryItem.None)
                available++;
        return count <= available;
    }

    void AddInventoryItem(InventoryItem item, int count) {
        for (int i = 0; i < inventory.Length; i++) {
            if (inventory[i] == InventoryItem.None) {
                inventory[i] = item;
                count--;
                if (count == 0) return;
            }
        }
    }

    void ClearInventoryGUI() {
        int childrenCount = inventoryImage.transform.childCount;
        for (int i = 0; i < childrenCount; i++) Destroy(inventoryImage.transform.GetChild(i).gameObject);
    }

    void UpdateInventoryGUI() {
        ClearInventoryGUI();
        Vector3 offset = new Vector3(Screen.width/2 - 0.7f * 1198f/2f, Screen.height/2 + 0.7f * 490f/2f, 0f);

        for (int i = 0; i < inventory.Length; i++) {
            if (inventory[i] == InventoryItem.None) continue;
            GameObject NewObj = new GameObject();
            NewObj.AddComponent<Image>().sprite = Resources.Load<Sprite>(inventory[i].GetPath());
            RectTransform t = NewObj.GetComponent<RectTransform>();
            t.SetParent(inventoryImage.transform);
            t.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            t.position = offset + 0.7f * new Vector3(68f + (i%10) * 118f, -68f - (float) (i/10) * 118f, 0f);
            NewObj.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit hitInfo)) {
                ObjectHandler objHandler = hitInfo.collider.gameObject.GetComponent<ObjectHandler>();
                if (objHandler != null && objHandler.item != InventoryItem.None && InventoryHasSpace(objHandler.count)) {
                    AddInventoryItem(objHandler.item, objHandler.count);
                    Destroy(hitInfo.collider.gameObject);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            inventoryImage.enabled = !inventoryImage.enabled;

            if (inventoryImage.enabled) {
                UpdateInventoryGUI();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else {
                ClearInventoryGUI();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

    }
}
