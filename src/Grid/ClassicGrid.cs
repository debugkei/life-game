﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim {
  /// <summary>
  /// Classic grid implementation
  /// </summary>
  internal class ClassicGrid : IGrid {
    private Random _rand;
    public ClassicGrid(int width, int height) {
      _rand = new Random();
      Reset(width, height);
    }

    /// <summary>
    /// Width of the grid
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Height of the grid
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Empty the whole grid, assign default values to every cell
    /// </summary>
    public void Empty() {
      var grid = (BitArray[])Grid;

      for (var i = 0; i < Width; ++i) {
        for (var j = 0; j < Height; ++j) {
          grid[i][j] = false;
        }
      }
    }

    /// <summary>
    /// Randomly assigns values to the cells, with given density
    /// </summary>
    /// <param name="density"></param>
    public void Random(int density) {
      var grid = (BitArray[])Grid;

      for (var i = 0; i < Width; ++i) {
        for (var j = 0; j < Height; ++j) {
          grid[i][j] = _rand.Next(0, density) == 0;
        }
      }
    }

    /// <summary>
    /// Resets the whole grid with given values
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Reset(int width, int height) {
      var grid = new BitArray[width];

      for (var i = 0; i < width; ++i) {
        grid[i] = new BitArray(height);
      }

      Width = width;
      Height = height;

      Grid = grid;
    }

    /// <summary>
    /// Moves the grid by 2 offsets
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Move(int x, int y) {
      var grid = (BitArray[])Grid;
      
      //In place move of the grid, could write less code but then less optimized
      //X
      if (x > 0) {
        //If requested move too large
        if (x >= Width) {
          //Move to right
          for (var i = Width - 1 - x; i >= 0; --i) {
            for (var j = 0; j < Height; ++j) {
              grid[i + x][j] = grid[i][j];
              grid[i][j] = false;
            }
          }
        }
      }
      else if (x < 0) {
        //If requested move too large
        if (x <= -Width) {
          //Move to left
          for (var i = 0 - x; i < Width; ++i) {
            for (var j = 0; j < Height; ++j) {
              grid[i + x][j] = grid[i][j];
              grid[i][j] = false;
            }
          }
        }
      }
      //Y
      if (y > 0) {
        //If requested move too large
        if (y >= Height) {
          //Move up
          for (var i = 0; i < Width; ++i) {
            for (var j = 0 + y; j < Height; ++j) {
              grid[i][j - y] = grid[i][j];
              grid[i][j] = false;
            }
          }
        }
      }
      else if (y < 0) {
        //If requested move too large
        if (y <= -Height) {
          //Move down
          for (var i = 0; i < Width; ++i) {
            for (var j = Height - 1 + y; j >= 0; --j) {
              grid[i][j - y] = grid[i][j];
              grid[i][j] = false;
            }
          }
        }
      }
    }

    /// <summary>
    /// Moves the grid by 2 offsets in multitask mode
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void MoveMT(int x, int y, int nThreads) {
      //Create
      var parts = Funcs.DivideGridIntoParts(nThreads, Width, Height);
      var tasks = new List<Task>();
      var newGrid = new BitArray[Width];
      for (var i = 0; i < Width; ++i) {
        newGrid[i] = new BitArray(Height);
      }

      //Start tasks
      for (var i = 0; i < nThreads; ++i) {
        var task_i = i;
        tasks.Add(Task.Run(() => {
          SingleThreadMove(x, y,
            newGrid, (BitArray[])Grid,
            parts[task_i][0][0], parts[task_i][0][1], parts[task_i][1][0], parts[task_i][1][1]);
        }));
      }

      //Wait for tasks to finish
      foreach (var i in tasks) {
        i.Wait();
      }

      //Assign newGrid to grid
      Grid = newGrid;
    }

    /// <summary>
    /// Move that is executed on a single thread, with specific grid to change and specific grid to read from, not an in place operation, and indexes to start at and
    /// to end at.
    /// </summary>
    private void SingleThreadMove(int x, int y, BitArray[] toChange, BitArray[] readFrom, int xStart, int xEnd, int yStart, int yEnd) {
      for (; xStart < xEnd; ++xStart) {
        for (; yStart < yEnd; ++yStart) {
          var _x = xStart - x;
          var _y = yStart - y;

          //If move results out of border, skip it
          if (_x < 0 || _y < 0 || _x > readFrom.Length || _y > readFrom[0].Length) continue;

          //Move the grid by x and y
          toChange[xStart][yStart] = readFrom[_x][_y];
        }
      }
    }

    /// <summary>
    /// The indexer, allows indexed access to the grid
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public object this[int x, int y] {
      get => ((BitArray[])Grid)[x][y]; set => ((BitArray[])Grid)[x][y] = (bool)value;
    }

    /// <summary>
    /// The grid property itself, to set the grid itself
    /// </summary>
    public object Grid { get; set; }

    ///<summary>
    /// Resizes the grid (Resets), and initializes with old values with offsets
    /// </summary>
    public void InitReset(int width, int height, int xOffset, int yOffset) {
      //Reference to a grid
      var grid = (BitArray[])Grid;

      //Cache old grid
      var oldGrid = new BitArray[Width];
      for (var i = 0; i < Width; ++i) {
        oldGrid[i] = new BitArray(grid[i]);
      }

      //Reset the new grid, reference still stays
      Reset(width, height);

      //Iterate over the grid
      for (var i = 0; i < width && i < oldGrid.Length; ++i) {
        for (var j = 0; j < height && j < oldGrid[i].Length; ++j) {
          //Calculate cordinates, plus (+) to get expectable behavior: with -n the old values moved to left, with n the old values are moved to right, both on new grid
          var x = i + xOffset;
          var y = j + yOffset;

          //If cordinate point outside the grid
          if (x < 0 || y < 0 || x >= Width || y >= Height) continue;

          //Assigning to that reference to a new grid value with an offset
          grid[x][y] = oldGrid[i][j];
        }
      }
    }
  }
}
