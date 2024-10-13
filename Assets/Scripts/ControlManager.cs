using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PointerGrid<T>
{
  public Dictionary<int, Dictionary<int, T>> elements;
  public PointerGrid()
  {
    elements = new Dictionary<int, Dictionary<int, T>>();
  }

  public bool TryGet(int x, int y, out T element)
  {
    element = default(T);
    if (!elements.ContainsKey(x))
      return false;
    if (!elements[x].ContainsKey(y))
      return false;
    element = elements[x][y];
    return true;
  }

  public bool TryRemove(int x, int y)
  {
    if (!elements.ContainsKey(x))
      return false;
    if (!elements[x].ContainsKey(y))
      return false;
    elements[x].Remove(y);
    return true;
  }

  public T Get(int x, int y)
  {
    return elements[x][y];
  }

  public void Set(int x, int y, T element)
  {
    if (!elements.ContainsKey(x))
      elements[x] = new Dictionary<int, T>();
    elements[x][y] = element;
  }
}

public class ControlManager : MonoBehaviour
{
    public int maxZoom = 3;
    public List<Item> globalResources;
    public TextMeshProUGUI globalResourcesText;
    public List<ItemProcessor> items;
    public PointerGrid<ItemProcessor> pointerGrid;
    public PointerGrid<GameObject> references;
    Grid grid;
    ParticleSystem particles;

    void Start()
    {
      grid = GetComponent<Grid>();
      particles = GameObject.Find("ParticleSystem").GetComponent<ParticleSystem>();
      pointerGrid = new PointerGrid<ItemProcessor>();
      references = new PointerGrid<GameObject>();
    }

    public void UpdateGlobalResources()
    {
      float concentratedMana = 0.0f;
      float purifiedMana = 0.0f;
      float fireMana = 0.0f;
      foreach (var item in globalResources)
      {
        if (item.item == "Concentrated Mana")
          concentratedMana += item.queued;
        else if (item.item == "Purified Mana")
          purifiedMana += item.queued;
        else if (item.item == "Fire Mana")
          fireMana += item.queued;
      }
      maxZoom = 3 + (int)(2*Mathf.Log(concentratedMana+1f, 10) + Mathf.Log(purifiedMana+1f, 2));
      var outputText = $"Concentrated Mana: {concentratedMana:F1}";
      if (purifiedMana > 0)
        outputText += $"\nPurified Mana: {purifiedMana:F1}";
      if (fireMana > 0)
        outputText += $"\nFire Mana: {fireMana:F1}";
      globalResourcesText.text = outputText;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in items)
        {
          var singleOutputs = item.Output(Time.deltaTime);
          foreach (var output in singleOutputs)
          {
            // Determine the split by directions and then look for those tiles
            int n_directions = (
                (Item.LEFT & output.direction)
                + ((Item.RIGHT & output.direction) >> 1)
                + ((Item.UP & output.direction) >> 2)
                + ((Item.DOWN & output.direction) >> 3)
                + ((Item.DISPERSE & output.direction) >> 4)
            );
            var total = 0.0f;
            var divided = output.Times(1.0f / n_directions);
            if (output.Left)
            {
              ItemProcessor target = null;
              if (pointerGrid.TryGet(item.x - 1, item.y, out target))
              {
                /*Debug.Log("Outputting Left: " + target.x + "," + target.y);*/
                var single = target.Accept(divided.From(Item.RIGHT));
                if (single > 0)
                  SpawnParticles(item.x, item.y, target.x, target.y, single);
                total += single;
              }
            }
            if (output.Right)
            {
              ItemProcessor target = null;
              if (pointerGrid.TryGet(item.x + 1, item.y, out target))
              {
                /*Debug.Log("Outputting Right: " + target.x + "," + target.y);*/
                var single = target.Accept(divided.From(Item.LEFT));
                if (single > 0)
                  SpawnParticles(item.x, item.y, target.x, target.y, single);
                total += single;
              }
            }
            if (output.Up)
            {
              ItemProcessor target = null;
              if (pointerGrid.TryGet(item.x, item.y + 1, out target))
              {
                /*Debug.Log("Outputting Up: " + target.x + "," + target.y + ": " + tmp.ToString());*/
                var single = target.Accept(divided.From(Item.DOWN));
                if (single > 0)
                  SpawnParticles(item.x, item.y, target.x, target.y, single);
                total += single;
              }
            }
            if (output.Down)
            {
              ItemProcessor target = null;
              if (pointerGrid.TryGet(item.x, item.y - 1, out target))
              {
                /*Debug.Log("Outputting Down: " + target.x + "," + target.y);*/
                var single = target.Accept(divided.From(Item.UP));
                if (single > 0)
                  SpawnParticles(item.x, item.y, target.x, target.y, single);
                total += single;
              }
            }
            if (output.Disperse)
            {
              total += divided.queued;
              AddGlobal(divided);
            }
            if (total > 0)
            {
              /*Debug.Log($"Tried outputting {output.queued} but output {total} of {output.item}");*/
            }
            item.DidOutput(output, total);
          }
        }
        UpdateGlobalResources();
    }

    public void AddGlobal(Item item)
    {
      foreach (var globalItem in globalResources)
      {
        if (globalItem.item == item.item)
        {
          globalItem.queued += item.queued;
          return;
        }
      }
      globalResources.Add(item);
    }

    public bool Add(int x, int y, ItemMapping itemMapping, int rotations)
    {
      ItemProcessor tmp;
      if (pointerGrid.TryGet(x, y, out tmp))
        return false;
      var itemProcessor = itemMapping.itemProcessor.Rotate(rotations);
      itemProcessor.x = x;
      itemProcessor.y = y;
      items.Add(itemProcessor);
      pointerGrid.Set(x, y, itemProcessor);

      var go = Instantiate(itemMapping.prefab, grid.CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
      go.name = x + "," + y;
      go.transform.Rotate(new Vector3(0, 0, -rotations * 90));
      references.Set(x, y, go);
      /*Debug.Log("Pointered To: " + x + "," + y);*/
      return true;
    }

    public void SpawnParticles(int x, int y, int toX, int toY, float amount)
    {
      if (Random.value > 0.01)
        return;
      var emitParams = new ParticleSystem.EmitParams();
      emitParams.position = grid.CellToWorld(new Vector3Int(x, y, 0));
      emitParams.velocity = new Vector3(toX - x, toY - y, 0.0f);
      emitParams.startLifetime = 1.0f;
      emitParams.startSize = Mathf.Log(amount * 500.0f, 20);
      particles.Emit(emitParams, 1);
      /*particles.Play(); // Continue normal emissions*/
    }


    public void GraphFromBox(int x, int y, int toX, int toY)
    {
      var globals = new ItemProcessor();
      var processors = new List<ItemProcessor>();
      for (int i = x; i <= toX; i++)
      {
        for (int j = y; j <= toY; j++)
        {
          ItemProcessor proc;
          if (pointerGrid.TryGet(i, j, out proc))
          {
            processors.Add(proc);
            foreach (var item in proc.inputItems)
            {
              if ((item.Left && i == x) || (item.Right && i == toX) ||
                  (item.Up && j == y) || (item.Down && j == toY))
              {
                globals.AddInput(item.Copy());
              }
            }
            foreach (var item in proc.outputItems)
            {
              var left = item.Left && i == x;
              var right = item.Right && i == toX;
              var up = item.Up && j == y;
              var down = item.Down && j == toY;
              if (left || right || up || down)
              {
                var globalItem = item.Copy();
                globalItem.direction = 0;
                if (left)
                  globalItem.direction += Item.LEFT;
                if (right)
                  globalItem.direction += Item.RIGHT;
                if (up)
                  globalItem.direction += Item.UP;
                if (down)
                  globalItem.direction += Item.DOWN;
                  
                globals.AddOutput(globalItem);
              }
              ItemProcessor toProc;
              if (item.Left && i > x)
              {
                if (pointerGrid.TryGet(i-1, j, out toProc))
                {
                  if (toProc.Accept(item.From(Item.RIGHT), true) > 0.0f)
                  {
                    
                  }
                }
                else
                {
                  // TODO: Confirm whether we're allowed to change this; reducing n_directions
                  // increases flow per output which could confuse things later
                  /*item.direction ^= Item.LEFT;*/
                }
              }
            }
          }
        }
      }
      Debug.Log($"Created factory ({x}, {y}, {toX}, {toY}) with potential {processors.Count} elems\n" + globals.ToString());
    }

    public ItemProcessor CreateFromBox(int x, int y, int toX, int toY)
    {
      for (int i = x; i <= toX; i++)
      {
        for (int j = y; j <= toY; j++)
        {
          
        }
      }
      return default(ItemProcessor);
    }
}
