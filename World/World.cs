using System;
namespace World
{
	public class World
	{
		public Dictionary<int,Snake> Snakes;
		public List<Wall?> Walls;
		public Dictionary<int,PowerUp> PowerUps;
		public int PlayerID;
		public int WorldSize;
		public World()
		{

			Snakes = new Dictionary<int,Snake>();

			Walls = new List<Wall?>();
			PowerUps = new Dictionary<int, PowerUp>();

		}
	}
}

