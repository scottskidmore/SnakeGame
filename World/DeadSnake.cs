using System;
using SnakeGame;
using System.Text.Json.Serialization;

namespace World
{
    /// <summary>
    /// Class for the DeadSnake object that holds the snake id
    /// the location it died and the frames it dies on.
    /// </summary>
	public class DeadSnake
	{
        public int snake { get; set; }
        public int framesDead;
        public Vector2D loc { get; set; }
       
        [JsonConstructor]
        public DeadSnake(int snake, Vector2D loc)
        {
            this.snake = snake;
            this.loc = loc;
            framesDead = 0;
           
        }
    }
}

