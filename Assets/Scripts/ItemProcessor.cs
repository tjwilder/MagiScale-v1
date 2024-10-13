using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class Item
{
  public const int LEFT = 1;
  public const int RIGHT = 2;
  public const int UP = 4;
  public const int DOWN = 8;
  public const int DISPERSE = 16;

  public string item;
  public float amountPerSecond;
  public float queued = 0.0f;
  public int direction = 0;

  public bool Left { get { return (direction & LEFT) > 0; } }
  public bool Right { get { return (direction & RIGHT) > 0; } }
  public bool Up { get { return (direction & UP) > 0; } }
  public bool Down { get { return (direction & DOWN) > 0; } }
  public bool Disperse { get { return (direction & DISPERSE) > 0; } }

  public override string ToString()
  {
      var dir = "";
      if (Left)
        dir += "L";
      else
        dir += "_";
      if (Right)
        dir += "R";
      else
        dir += "_";
      if (Up)
        dir += "U";
      else
        dir += "_";
      if (Down)
        dir += "D";
      else
        dir += "_";
      if (Disperse)
        dir += "O";
      else
        dir += "_";
      return $"{item}:{queued:F1}:{dir}";
  }

  public Item Copy()
  {
    var newItem = new Item();
    newItem.item = item;
    newItem.amountPerSecond = amountPerSecond;
    newItem.queued = queued;
    newItem.direction = direction;
    return newItem;
  }

  public Item Flip()
  {
    var newItem = Copy();
    newItem.direction = (
        ((LEFT & direction) << 1)
        + ((RIGHT & direction) >> 1)
        + ((UP & direction) << 1)
        + ((DOWN & direction) >> 1)
        + (DISPERSE & direction)
    );
    return newItem;
  }

  public Item From(int newDirection)
  {
    var newItem = Copy();
    newItem.direction = newDirection;
    return newItem;
  }
  
  public Item Rotate(int times)
  {
    var newItem = Copy();
    for (int i = 0; i < times; i++)
    {
      newItem.direction = (
          ((LEFT & newItem.direction) << 2)
          + ((RIGHT & newItem.direction) << 2)
          + ((UP & newItem.direction) >> 1)
          + ((DOWN & newItem.direction) >> 3)
          + (DISPERSE & direction)
      );
    }
    return newItem;
  }

  public Item Times(float multiplier)
  {
    return With(queued * multiplier);
  }

  public Item With(float newQueued)
  {
    var newItem = new Item();
    newItem.item = item;
    newItem.amountPerSecond = amountPerSecond;
    newItem.queued = newQueued;
    newItem.direction = direction;
    return newItem;
  }
}

[Serializable]
public class ItemProcessor
{
    public int x;
    public int y;
    public List<Item> inputItems;
    public List<Item> outputItems;

    public ItemProcessor()
    {
      x = 0;
      y = 0;
      inputItems = new List<Item>();
      outputItems = new List<Item>();
    }

    public ItemProcessor Copy()
    {
      var n = new ItemProcessor();
      n.x = x;
      n.y = y;
      n.inputItems = inputItems;
      n.outputItems = outputItems;
      return n;
    }

    public ItemProcessor Rotate(int times)
    {
      var n = new ItemProcessor();
      n.x = x;
      n.y = y;
      n.inputItems = new List<Item>();
      n.outputItems = new List<Item>();
      for (int i = 0; i < inputItems.Count; i++)
      {
        n.inputItems.Add(inputItems[i].Rotate(times));
      }
      for (int i = 0; i < outputItems.Count; i++)
      {
        n.outputItems.Add(outputItems[i].Rotate(times));
      }
      return n;
    }

    public void AddInput(Item input)
    {
      foreach (var item in inputItems)
      {
        if (item.item == input.item)
        {
          item.amountPerSecond += input.amountPerSecond;
          item.direction |= input.direction;
          return;
        }
      }
      inputItems.Add(input);
    }

    public void AddOutput(Item output)
    {
      foreach (var item in outputItems)
      {
        if (item.item == output.item)
        {
          if (item.direction == output.direction)
          {
            item.amountPerSecond += output.amountPerSecond;
            return;
          }
          item.direction |= output.direction;
          return;
        }
      }
      outputItems.Add(output);
    }

    public float Accept(Item input, bool ignoreAmount=false)
    {
      foreach (var item in inputItems)
      {
        /*Debug.Log($"{item.item},{input.item};{item.direction},{input.direction};{item.direction & input.direction}");*/
        if ((item.item == "Any" || item.item == input.item) && ((item.direction & input.direction) > 0))
        {
          if (ignoreAmount)
            return item.amountPerSecond;
          if (item.item == "Any")
          {
            item.item = input.item;
            foreach (var output in outputItems)
            {
              if (output.item == "Any")
                output.item = input.item;
            }
          }
          var changedBy = Mathf.Max(0, Mathf.Min(item.amountPerSecond - item.queued, input.queued));
          /*Debug.Log($"ChangedBy {changedBy}");*/
          item.queued += changedBy;
          return changedBy;
        }
      }
      return 0.0f;
    }

    public List<Item> Output(float deltaTime)
    {
      foreach (var item in inputItems)
      {
        // If we don't have enough input, fail
        if (item.queued < item.amountPerSecond * deltaTime)
          return new List<Item>();
      }
      // If we don't have enough space for more output, just output what we have
      var outputs = new List<Item>();
      foreach (var item in outputItems)
      {
        if (item.amountPerSecond * deltaTime + item.queued > item.amountPerSecond)
        {
          var maxOutput = item.amountPerSecond * deltaTime;
          maxOutput = Mathf.Min(maxOutput, item.queued);
          outputs.Add(item.With(maxOutput));
        }
      }
      if (outputs.Count > 0)
        return outputs;

      foreach (var item in inputItems)
      {
        // Delete the required inputs
        item.queued -= item.amountPerSecond * deltaTime;
      }
      outputs = new List<Item>();
      foreach (var item in outputItems)
      {
        // Create/queue the outputs
        item.queued += item.amountPerSecond * deltaTime;
        outputs.Add(item.With(item.amountPerSecond * deltaTime));
      }
      return outputs;
    }

    public void DidOutput(Item output, float amountConsumed)
    {
      foreach (var item in outputItems)
      {
        if (item.item == output.item) {
          item.queued -= amountConsumed;
          return;
        }
      }
    }

    public override string ToString()
    {
      string output = "";
      foreach (var item in inputItems)
      {
        var dir = "";
        if (item.Left)
          dir += "L";
        else
          dir += "_";
        if (item.Right)
          dir += "R";
        else
          dir += "_";
        if (item.Up)
          dir += "U";
        else
          dir += "_";
        if (item.Down)
          dir += "D";
        else
          dir += "_";
        output += $"{item.item}:{item.amountPerSecond}:{item.queued:F1}:{dir}, ";
      }
      output += " >\n";
      foreach (var item in outputItems)
      {
        var dir = "";
        if (item.Left)
          dir += "L";
        else
          dir += "_";
        if (item.Right)
          dir += "R";
        else
          dir += "_";
        if (item.Up)
          dir += "U";
        else
          dir += "_";
        if (item.Down)
          dir += "D";
        else
          dir += "_";
        if (item.Disperse)
          dir += "O";
        else
          dir += "_";
        output += $"{item.item}:{item.amountPerSecond}:{item.queued:F1}:{dir}, ";
      }
      return output;
    }
}
