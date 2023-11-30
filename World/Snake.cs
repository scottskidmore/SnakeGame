﻿using System;
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
		public int snake { get; }
        public string name { get; }
        public int score { get; set; }
        public List<Vector2D> body { get; }
        public Vector2D dir { get; }
        public bool died { get; }
        public bool alive { get; }
        public bool dc { get; }
        public bool join { get; }
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
        public Snake(int snake, string name)
		{
            this.snake = snake;
            this.name = name;
            this.score = 0;
            this.body = new List<Vector2D> ();
            this.dir = new Vector2D();
            this.died = false;
            this.alive = false;
            this.dc = false;
            this.join = true;

        }

    }
}

