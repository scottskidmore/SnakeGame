using System;
using System.Text.Json.Serialization;
using SnakeGame;

namespace World
{
	/// <summary>
	/// Class for the Snake object. It holds all the information
	/// that the server provides for the snake object.
	/// </summary>
	public class Snake
	{
		public int snake { get; set; }
        public string name { get; set; }
        public int score { get; set; }
        public List<Vector2D> body { get; set; }
        public Vector2D dir { get; set; }
        public bool died { get; set; }
        public bool alive { get; set; }
        public bool dc { get; set; }
        public bool join { get; set; }
        [JsonConstructor]
        public Snake(int snake,string name,int score,List<Vector2D> body,Vector2D dir,bool died, bool alive,bool dc,bool join)
		{
			this.snake = snake;
			this.name = name;
			this.score = score;
			this.body = body;
			this.dir = dir;
			this.died = died;
			this.alive = alive;
			this.dc = dc;
			this.join = join;

		}
	}
}

