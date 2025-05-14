using System.Numerics;
using Raylib_cs;

namespace ded;

public class Cursor
{
    private Vector2 _editorCoordinates = Vector2.Zero;
    public Vector2 EditorCoordinates => _editorCoordinates;
    private Vector2 _worldCoordinates = Vector2.Zero;
    public Vector2 WorldCoordinates
    {
        get
        {
            return _worldCoordinates;
        }
    }
    private int _width;
    private int _height;
    private Color _color;

    public Cursor(int width, int height, Color color)
    {
        _width = width;
        _height = height;
        _color = color;
    }

    public void Back()
    {
        _editorCoordinates.X--;
    }

    public void Forward()
    {
        _editorCoordinates.X++;
    }

    public void Up(int x)
    {
        _editorCoordinates = new Vector2(x, --_editorCoordinates.Y);
    }

    public void Down(int x)
    {
        _editorCoordinates = new Vector2(x, ++_editorCoordinates.Y);
    }

    public void Draw()
    {
        UpdateWorldCoordinates();
        Raylib.DrawRectangleV(_worldCoordinates, new Vector2(_width, _height), _color);
    }

    public void UpdateWorldCoordinates()
    {
        // +1 for font spacing
        // BUG: need to replace 1 with _fontSpacing variable. but dont know how
        _worldCoordinates.X = EditorCoordinates.X * (_width + 1);
        // +2 for line spacing
        _worldCoordinates.Y = EditorCoordinates.Y * (_height + 2);
    }
}
