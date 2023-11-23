using System;
using SnakeGame;
using System.Text.Json.Serialization;

namespace World
{
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

