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
    /// A method that can be used as an ObjectDrawer delegate to draw a wall
    /// </summary>
    /// <param name="o">The wall to draw</param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall p = o as Wall;
        
        canvas.DrawImage(wall, -wall.Width / 2, -wall.Height/ 2, wall.Width, wall.Height);

    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate to draw a Powerup
    /// </summary>
    /// <param name="o">The PowerUp to draw</param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        PowerUp p = o as PowerUp;
        int width = 10;
        if (p.power % 2 == 0)
            canvas.FillColor = Colors.Orange;
        else
            canvas.FillColor = Colors.Green;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }



    private void SnakeSegmentDrawer(object o, ICanvas canvas)
    {
        double snakeSegmentLength = (double)o;
        canvas.FillRectangle(20, 20, 20, -(float)snakeSegmentLength);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!initializedForDrawing)
            InitializeDrawing();

        //if player does not exist go to center


        float playerX = 100;
        float playerY = 100;



        //if player exists
        if (theWorld.Snakes.TryGetValue(theWorld.PlayerID, out Snake player))

        {

            //player location
            playerX = (float)player.body[player.body.Count - 1].X;
            playerY = (float)player.body[player.body.Count - 1].Y;
        }





        canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));

        canvas.DrawImage(background, -theWorld.WorldSize / 2, -theWorld.WorldSize / 2, theWorld.WorldSize, theWorld.WorldSize);
        // undo previous transformations from last frame
        canvas.ResetState();
        // center the view on the middle of the world

        // example code for how to draw
        // (the image is not visible in the starter code)
        foreach (var p in theWorld.Walls)
        {
            double drawAngle = Vector2D.AngleBetweenPoints(p.p1, p.p2);
            int segmentSize = -50;
            if (drawAngle < 0)
                segmentSize = 50;

            Vector2D diff = p.p1 - p.p2;
            double locX;
            double locY;
            //if wall is horizontal
            if (diff.Y == 0)
            {
                //total wall lengths
                int wallNum = Math.Abs((int)diff.X / 50);
                for (int i = 0; i < wallNum; i++)
                {

                    locX = (p.p1.X + (segmentSize * i));
                    locY = p.p1.Y;
                    DrawObjectWithTransform(canvas, p, locX, locY, drawAngle, WallDrawer);

                }

            }
            //if wall is vertical
            else if (diff.X == 0)
            {
                //total wall lengths
                int wallNum = Math.Abs((int)diff.Y / 50);
                for (int i = 0; i < wallNum; i++)
                {
                    locX = p.p1.X;
                    locY = (p.p1.Y + (segmentSize * i));

                    DrawObjectWithTransform(canvas, p, locX, locY, drawAngle, WallDrawer);

                }


            }
        }
            foreach (Snake s in theWorld.Snakes.Values)
            {
                canvas.FillColor = colorChooser(s.snake % 10);
                for (int i = 0; i < s.body.Count - 1; i++)
                {
                // Loop through snake segments, calculate segment length and segment direction

                if (s.body[i].GetX() == s.body[i + 1].GetX())
                {
                    DrawObjectWithTransform(canvas, s.body[i].GetY() - s.body[i + 1].GetY(), s.body[i].X, s.body[i].Y, Vector2D.AngleBetweenPoints(s.body[i], s.body[i+1]), SnakeSegmentDrawer);
                }
                if (s.body[i].GetY() == s.body[i + 1].GetY())
                {
                    DrawObjectWithTransform(canvas, s.body[i].GetX() - s.body[i + 1].GetX(), s.body[i].X, s.body[i].Y, Vector2D.AngleBetweenPoints(s.body[i], s.body[i + 1]), SnakeSegmentDrawer);
                }
            }



            }


        
    }

    private Color colorChooser(int i)
    {
        if(i==0)
        {
            return Colors.Blue;
        }

        else if (i == 1)
        {
            return Colors.Red;
        }
        else if (i == 2)
        {
            return Colors.HotPink;
        }
        else if (i == 3)
        {
            return Colors.Honeydew;
        }
        else if (i == 4)
        {
            return Colors.Yellow;
        }
        else if (i == 5)
        {
            return Colors.Green;
        }
        else if (i == 6)
        {
            return Colors.Orange;
        }
        else if (i == 7)
        {
            return Colors.Lavender;
        }
        else if (i == 8)
        {
            return Colors.GhostWhite;
        }
        else if (i == 9)
        {
            return Colors.Gold;
        }
        else 
        {
            return Colors.Cyan;
        }
    }

}
