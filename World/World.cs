using System;
namespace World
{
	public class World
	{
		public List<Snake> Snakes;
		public List<Wall?> Walls;
		public List<PowerUp> PowerUps;
		public World()
		{
			Snakes = new List<Snake>();
			Walls = new List<Wall?>();
			PowerUps = new List<PowerUp>();

		}
	}
}

