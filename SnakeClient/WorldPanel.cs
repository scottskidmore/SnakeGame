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
using Microsoft.Maui.Graphics;

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
        
        canvas.DrawImage(wall, -(50/ 2), -(50/ 2), 50, 50);

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
        
        canvas.FillRectangle(-5, -5, 10, -(float)snakeSegmentLength);
        
    }

    private void SnakeHeadAndTailDrawer(object o, ICanvas canvas)
    {
        int width = 10;
        canvas.FillEllipse(-5, 0, width, width);
    }

    private void DeadSnakeDrawer(object o, ICanvas canvas)
    {
        DeadSnake ds = o as DeadSnake;
        int width = 10+(1*ds.framesDead);
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
        ds.framesDead += 1;
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


        


        canvas.Translate(-playerX + (dirtyRect.Width / 2), -playerY + (dirtyRect.Height / 2));

        canvas.DrawImage(background, -theWorld.WorldSize / 2, -theWorld.WorldSize / 2, theWorld.WorldSize, theWorld.WorldSize);
        // undo previous transformations from last frame
        canvas.ResetState();
        // center the view on the middle of the world

        // example code for how to draw
        // (the image is not visible in the starter code)
        lock (theWorld)
        {
            foreach (var p in theWorld.Walls)
            {
                double drawAngle = Vector2D.AngleBetweenPoints(p.p1, p.p2);
                int segmentSize = -50;
                if (drawAngle == -90 || drawAngle == 0)
                {
                    segmentSize = 50;
                }


                Vector2D diff = p.p1 - p.p2;
                double locX;
                double locY;
                //if wall is horizontal
                if (diff.Y == 0)
                {
                    //total wall lengths
                    int wallNum = Math.Abs((int)diff.X / 50);
                    for (int i = 0; i < wallNum + 1; i++)
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
                    for (int i = 0; i < wallNum + 1; i++)
                    {
                        locX = p.p1.X;
                        locY = (p.p1.Y + (segmentSize * i));

                        DrawObjectWithTransform(canvas, p, locX, locY, drawAngle, WallDrawer);

                    }


                }
            }

            foreach (PowerUp p in theWorld.PowerUps.Values)
            {
                DrawObjectWithTransform(canvas, p,
                      p.loc.GetX(), p.loc.GetY(), 0,
                      PowerupDrawer);

            }
            foreach (Snake s in theWorld.Snakes.Values)
            {
                if (s.alive == true)
                {

                    canvas.FillColor = colorChooser(s.snake % 10);
                    for (int i = 0; i < s.body.Count - 1; i++)
                    {
                        // Loop through snake segments, calculate segment length and segment direction

                        //draw tail
                        if (i == 0)
                        {
                            DrawObjectWithTransform(canvas, s.body[i], s.body[i].GetX(), s.body[i].GetY(), Vector2D.AngleBetweenPoints(s.body[i], s.body[i + 1]), SnakeHeadAndTailDrawer);
                        }
                        //draw head
                        if (i == s.body.Count - 2)
                        {

                            DrawObjectWithTransform(canvas, s.body[i + 1], s.body[i + 1].GetX(), s.body[i + 1].GetY(), Vector2D.AngleBetweenPoints(s.body[i], s.body[i + 1]), SnakeHeadAndTailDrawer);
                            // canvas.FontColor = Colors.GhostWhite;

                        }

                        if (s.body[s.body.Count - i - 1].GetX() == s.body[s.body.Count - i - 2].GetX())
                        {
                            DrawObjectWithTransform(canvas, Math.Abs(s.body[s.body.Count - i - 1].GetY() - s.body[s.body.Count - i - 2].GetY()), s.body[s.body.Count - i - 2].X, s.body[s.body.Count - i - 2].Y, Vector2D.AngleBetweenPoints(s.body[s.body.Count - i - 1], s.body[s.body.Count - i - 2]), SnakeSegmentDrawer);
                        }
                        if (s.body[s.body.Count - i - 1].GetY() == s.body[s.body.Count - i - 2].GetY())
                        {
                            DrawObjectWithTransform(canvas, Math.Abs(s.body[s.body.Count - i - 1].GetX() - s.body[s.body.Count - i - 2].GetX()), s.body[s.body.Count - i - 2].X, s.body[s.body.Count - i - 2].Y, Vector2D.AngleBetweenPoints(s.body[s.body.Count - i - 1], s.body[s.body.Count - i - 2]), SnakeSegmentDrawer);
                        }
                    }
                }
                else
                {
                    theWorld.DeadSnakes.TryGetValue(s.snake, out DeadSnake ds);

                    DrawObjectWithTransform(canvas, ds, ds.loc.X, ds.loc.Y, Vector2D.AngleBetweenPoints(s.body[s.body.Count - 2], s.body[s.body.Count - 1]), DeadSnakeDrawer);
                }
             
            }

            //draw head tail and write name

            //canvas.DrawString(s.name, (float)s.body[s.body.Count - 1].GetX(), (float)s.body[s.body.Count - 1].GetY(), HorizontalAlignment.Right);
            //Vector2D head = s.body[s.body.Count - 1];
            //Vector2D Tail = s.body[0];
            //DrawObjectWithTransform(canvas, head.X - 5, head.X, head.Y, head.ToAngle(), SnakeHeadAndTailDrawer);
            //DrawObjectWithTransform(canvas, Tail.X - 5, Tail.X, Tail.Y, Tail.ToAngle(), SnakeHeadAndTailDrawer);


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
