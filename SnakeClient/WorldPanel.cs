using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using World;



namespace SnakeGame;
public class WorldPanel : IDrawable
{
    private IImage wall;
    private IImage background;
    private int viewSize = 500;
    private World.World theWorld = new();

    public delegate void ObjectDrawer(object o, ICanvas canvas);

    private GraphicsView graphicsView = new();
    private bool initializedForDrawing = false;

    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    public WorldPanel()
    {
       
        
    }

    public void SetWorld(World.World w)
    {
        theWorld = w;
    }

    private void InitializeDrawing()
    {
        wall = loadImage( "wallsprite.png" );
        background = loadImage( "background.png" );
        initializedForDrawing = true;
    }


    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The wall to draw</param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall p = o as Wall;
        Vector2D diff = p.p1 - p.p2;
        //if wall is horizontal
        if (diff.Y == 0)
        {
            //total wall lengths
            int wallNum = (int)diff.X / 50;
            for (int i = 0; i < wallNum; i++) 
            {
                float locX = (float)(p.p1.X + (50 * i)+25);
               
                canvas.DrawImage(wall,locX,wall.Height/2,wall.Width,wall.Height);

            }

        }
        //if wall is vertical
        else if (diff.X == 0)
        {
            //total wall lengths
            int wallNum = (int)diff.Y / 50;
            for (int i = 0; i < wallNum; i++)
            {
                float locY = (float)(p.p1.Y + (50 * i) + 25);

                canvas.DrawImage(wall, wall.Width, locY / 2, wall.Width, wall.Height);

            }

        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if ( !initializedForDrawing )
            InitializeDrawing();
        float playerX = 0;
        float playerY = 0;

        canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));

        canvas.DrawImage(background, -1200 / 2, -1200 / 2, 1200, 1200);
        // undo previous transformations from last frame
        canvas.ResetState();
        // center the view on the middle of the world
        
        // example code for how to draw
        // (the image is not visible in the starter code)
        foreach (var p in theWorld.Walls)
            WallDrawer(p, canvas);
              
        
    }

}
