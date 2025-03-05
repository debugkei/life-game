﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim {
  /// <summary>
  /// Classic mouse logic implementation
  /// </summary>
  class ClassicMouseHandler {
    private bool _isMoving;
    private bool _isDrawing;
    private bool _isErasing;
    private int _previousX;
    private int _previousY;
    private Point _brushCenter;
    private Rectangle[,] _mouseStepsRects;
    private ClassicRenderer _renderer;
    //Mouse steps "Preview of draw. Color below cursor."
    private SolidBrush _mouseStepsBrush;
    public Color MouseStepsColor { get => MouseStepsColor; set { MouseStepsColor = value; _mouseStepsBrush = GetMouseStepsBrush(); } }
    public byte MouseStepsAlpha { get => MouseStepsAlpha; set { MouseStepsAlpha = value; _mouseStepsBrush = GetMouseStepsBrush(); } }
    private View _view;
    public ClassicMouseHandler(int brushWidth, int brushHeight, int x, int y, ClassicRenderer renderer, Color mouseStepsColor, byte mouseStepsAlpha, View view) {
      //Init
      BrushWidth = brushWidth;
      BrushHeight = brushHeight;
      X = x;
      Y = y;
      _renderer = renderer;
      MouseStepsColor = mouseStepsColor;
      MouseStepsAlpha = mouseStepsAlpha;
      _mouseStepsRects = new Rectangle[BrushWidth, BrushHeight];
      _brushCenter = Funcs.GetBrushCenter(X, Y, BrushWidth, BrushHeight);
      _view = view;
    }

    /// <summary>
    /// Cursor X cordinate on the grid
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Cursor Y cordinate on the grid
    /// </summary>
    public int Y { get; set; }
    /// <summary>
    /// Width of the brush (to paint on the grid)
    /// </summary>
    //Callback to update rectangles on change
    public int BrushWidth { get => BrushWidth; set { 
        BrushWidth = value;
        _mouseStepsRects = new Rectangle[BrushWidth, BrushHeight];
        _brushCenter = Funcs.GetBrushCenter(X, Y, BrushWidth, BrushHeight);
      }
    }
    /// <summary>
    /// Height of the brush (to paint on the grid)
    /// </summary>
    //Callback to update rectangles on change
    public int BrushHeight { get => BrushHeight; set { 
        BrushHeight = value;
        _mouseStepsRects = new Rectangle[BrushWidth, BrushHeight];
        _brushCenter = Funcs.GetBrushCenter(X, Y, BrushWidth, BrushHeight);
      }
    }

    /// <summary>
    /// Applies only all the computations that are made to the grid.
    /// Necessary to be called to draw or erase.
    /// </summary>
    public void ApplyGridChanges(ClassicGrid grid) {
      //Draw
      if (_isDrawing) Draw(grid);
      //Erase
      else if (_isErasing) Erase(grid);
    }

    /// <summary>
    /// Applies only all the visual changes.
    /// Necessary to be called to move or zoom.
    /// </summary>
    public void ApplyVisualChanges(ClassicGrid grid) {
      //Move
      if (_isMoving) grid.Move(_previousX - X, _previousY - Y);
      //Set the rectangles for renderer to draw as mouse steps
      if (_previousY != Y || _previousX != X) {
        for (var i = 0; i < BrushWidth; ++i) {
          for (var j = 0; j < BrushWidth; ++j) {
            //It makes a cursor (center) be in the middle, if the width or height is even if focuses rectangle to the left (or up)
            _mouseStepsRects[i, j] = new Rectangle((i - _brushCenter.X + X), (j - _brushCenter.Y + Y), _renderer.CellWidth, _renderer.CellWidth);
          }
        }
      }
      //Ask renderer to render mouse steps
      for (var i = 0; i < BrushWidth; ++i) {
        for (var j = 0; j < BrushWidth; ++j) {
          var x = _mouseStepsRects[i, j].X / _renderer.Resolution;
          var y = _mouseStepsRects[i, j].Y / _renderer.Resolution;
          if (x >= 0 && y >= 0 && x < grid.Width && y < grid.Height) _renderer.RenderRect(_mouseStepsBrush, _mouseStepsRects[i, j]);
        }
      }

      _previousX = X;
      _previousY = Y;
    }

    private void Draw(ClassicGrid grid) {
      for (var i = 0; i < BrushWidth; ++i) {
        for (var j = 0; j < BrushWidth; ++j) {
          //Draw all elements including the brush thickness
          grid[i - _brushCenter.X + X, j - _brushCenter.Y + Y] = true;
        }
      }
    }
    private void Erase(ClassicGrid grid) {
      for (var i = 0; i < BrushWidth; ++i) {
        for (var j = 0; j < BrushWidth; ++j) {
          //Draw all elements including the brush thickness
          grid[i - _brushCenter.X + X, j - _brushCenter.Y + Y] = false;
        }
      }
    }
    /// <summary>
    /// Informs that specific mouse button was clicked
    /// </summary>
    /// <param name="grid"></param>
    public void HandleMouseDown(MouseButtonType type) {
      switch (type) {
        case MouseButtonType.Left:
          _isDrawing = true;
          _isErasing = false;
          break;
        case MouseButtonType.Right:
          _isErasing = true;
          _isDrawing = false;
          break;
        case MouseButtonType.Middle:
          _isMoving = true;
          break;
      }
    }

    /// <summary>
    /// Informs that specific mousebutton went up
    /// </summary>
    /// <param name="grid"></param>
    public void HandleMouseUp(MouseButtonType type) {
      switch (type) {
        case MouseButtonType.Left:
          _isDrawing = false;
          break;
        case MouseButtonType.Right:
          _isErasing = false;
          break;
        case MouseButtonType.Middle:
          _isMoving = false;
          break;
      }
    }

    /// <summary>
    /// Informs that mouse moved.
    /// </summary>
    /// <param name="grid"></param>
    public void HandleMouseMove(int x, int y) {
      _previousX = X;
      _previousY = Y;
      X = x;
      Y = y;
    }

    /// <summary>
    /// Changes the resolution, centered on cursor.
    /// </summary>
    /// <param name="grid"></param>
    public void HandleMouseWheel(ClassicGrid grid, int delta) {
      //Check if the mouse is on the grid
      if (X >= 0 && Y >= 0 && X < grid.Width && Y < grid.Height) {
        //Change resolution, delta will be checked inside
        _view.ChangeResolution(grid, delta, X, Y);
      }
    }

    /// <summary>
    /// Gets the mouse steps brush
    /// </summary>
    private SolidBrush GetMouseStepsBrush() {
      return new SolidBrush(Color.FromArgb(MouseStepsAlpha, MouseStepsColor));
    }
  }
}
