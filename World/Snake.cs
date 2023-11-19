using System;
using SnakeGame;

namespace World
{
	public class Snake
	{
		int snake;
		string name;
		List<Vector2D> body;
		Vector2D dir;
		bool died;
		bool alive;
		bool dc;
		bool join;
		public Snake(int s,string n,List<Vector2D> b,Vector2D d,bool die, bool a,bool dc,bool j)
		{
			snake = s;
			name = n;
			body = b;
			dir = d;
			died = die;
			alive = a;
			this.dc = dc;
			join = j;

		}
	}
}

