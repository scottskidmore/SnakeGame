using System;
using SnakeGame;

namespace World
{
	public class PowerUp
	{
		int power;
		Vector2D loc;
		bool died;
		public PowerUp(int p,Vector2D l,bool d)
		{
			power = p;
			loc = l;
			died = d;
		}
	}
}

