
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[Serializable]
public class ItemMapping
{
  public GameObject prefab;
  public GameObject uiPrefab;
  public ItemProcessor itemProcessor;
}

public class InputManager : MonoBehaviour
{
  public GameObject panel;
  public GameObject hovered;
  public TextMeshProUGUI selectedText;
  public int currentMapping = 0;
  public List<ItemMapping> mappings;
  public bool copyMode = false;
  public GameObject outline;
  Grid grid;
  ControlManager controlManager;
  int rotations = 0;
  Vector3 dragPosition = new Vector3(0, 0, 0);

  public void Start()
  {
    grid = GetComponent<Grid>();
    controlManager = GetComponent<ControlManager>();
    int i = 0;
    foreach (var mapping in mappings)
    {
        var go = Instantiate(mapping.uiPrefab);
        go.transform.SetParent(panel.transform);
        go.GetComponent<Button>().onClick.AddListener(SetMapping(i));
        i++;
    }

    selectedText.text = mappings[currentMapping].itemProcessor.ToString();
  }

  public UnityAction SetMapping(int i)
  {
    return () => {
      currentMapping = i;
      copyMode = false;
      selectedText.text = mappings[currentMapping].itemProcessor.ToString();
    };
  }

  public void Update()
  {
    if (!EventSystem.current.IsPointerOverGameObject())
    {
      if (Input.GetMouseButtonDown(0))
      {
          var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
          // Add .5s to handle grid size and centers
          Vector3Int cellPos = grid.WorldToCell(new Vector3(pos.x+.5f, pos.y+.5f, 0));
          if (copyMode)
          {
            outline.SetActive(true);
            dragPosition = pos;
            outline.transform.position = new Vector3(pos.x, pos.y, 0);
            outline.GetComponent<SpriteRenderer>().size = new Vector2(0, 0);
          }
          else
          {
            // If we successfully add the object, create the object there
            ItemProcessor originalItem;
            if (controlManager.pointerGrid.TryGet(cellPos.x, cellPos.y, out originalItem))
            {
              Debug.Log(originalItem.ToString());
            }
            else if (controlManager.Add(cellPos.x, cellPos.y, mappings[currentMapping], rotations))
            {
              /*var go = Instantiate(mappings[currentMapping].prefab, grid.CellToWorld(cellPos), Quaternion.identity);*/
                /*go.name = cellPos.x + "," + cellPos.y;*/
                /*go.transform.Rotate(new Vector3(0, 0, -rotations * 90));*/
            }
          }
      }
      else if (Input.GetMouseButtonDown(1))
      {
          var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
          // Add .5s to handle grid size and centers
          Vector3Int cellPos = grid.WorldToCell(new Vector3(pos.x+.5f, pos.y+.5f, 0));
          if (controlManager.pointerGrid.TryRemove(cellPos.x, cellPos.y))
          {
            Destroy(controlManager.references.elements[cellPos.x][cellPos.y]);
            controlManager.references.elements[cellPos.x].Remove(cellPos.y);
          }
      }
      else if (Input.GetMouseButtonDown(2))
      {
          dragPosition = Input.mousePosition;
      }
      else
      {
          var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
          // Add .5s to handle grid size and centers
          Vector3Int cellPos = grid.WorldToCell(new Vector3(pos.x+.5f, pos.y+.5f, 0));
          // If we successfully add the object, create the object there
          ItemProcessor originalItem;
          if (controlManager.pointerGrid.TryGet(cellPos.x, cellPos.y, out originalItem))
          {
            HoverItemProcessor(originalItem);
          }
          else
            hovered.SetActive(false);
      }
    }
    if (Input.GetMouseButton(2))
    {
        var curPos = Input.mousePosition;
        curPos -= dragPosition;
        Camera.main.gameObject.transform.position -= 0.05f * curPos;
        dragPosition = Input.mousePosition;
    }
    else if (copyMode && Input.GetMouseButton(0))
    {
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        /*outline.transform.position = new Vector3(pos.x, pos.y, 0);*/
        outline.GetComponent<SpriteRenderer>().size = new Vector2(pos.x - dragPosition.x, -(pos.y - dragPosition.y));
    }
    else if (copyMode && Input.GetMouseButtonUp(0))
    {
      // Time to do the actual copy creation
      outline.SetActive(false);
      var curPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      // Add .5s to handle grid size and centers
      Vector3Int originalPos = grid.WorldToCell(new Vector3(dragPosition.x+.5f, dragPosition.y+.5f, 0));
      Vector3Int cellPos = grid.WorldToCell(new Vector3(curPos.x+.5f, curPos.y+.5f, 0));
      if (originalPos.x > cellPos.x)
      {
        var tmp = cellPos.x;
        cellPos.x = originalPos.x;
        originalPos.x = tmp;
      }
      if (originalPos.y > cellPos.y)
      {
        var tmp = cellPos.y;
        cellPos.y = originalPos.y;
        originalPos.y = tmp;
      }
      controlManager.GraphFromBox(originalPos.x, originalPos.y, cellPos.x, cellPos.y);
    }
    var scroll = Input.mouseScrollDelta.y;
    Camera.main.orthographicSize -= scroll;
    Camera.main.orthographicSize = Math.Max(3, Math.Min(controlManager.maxZoom, Camera.main.orthographicSize));
    Camera.main.gameObject.transform.Find("Camera").GetComponent<Camera>().orthographicSize = Camera.main.orthographicSize;
    if (Input.GetKeyDown("r"))
    {
      rotations++;
    }
    if (Input.GetKeyDown("1"))
    {
      SetMapping(0)();
    }
    if (Input.GetKeyDown("2"))
    {
      SetMapping(1)();
    }
    if (Input.GetKeyDown("3"))
    {
      SetMapping(2)();
    }
    if (Input.GetKeyDown("4"))
    {
      SetMapping(3)();
    }
    if (Input.GetKeyDown("5"))
    {
      SetMapping(4)();
    }
    if (Input.GetKeyDown("6"))
    {
      SetMapping(5)();
    }
    if (Input.GetKey("c") && (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)))
    {
      copyMode = true;
    }
  }

  public void HoverItemProcessor(ItemProcessor processor)
  {
        hovered.SetActive(true);
        hovered.transform.Find("HoveredText").GetComponent<TextMeshProUGUI>().text
          = processor.ToString();
  }
}
