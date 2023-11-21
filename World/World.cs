using System;
namespace World
{
	public class World
	{
		public Dictionary<int,Snake> Snakes;
		public List<Wall?> Walls;
		public List<PowerUp> PowerUps;
		public int PlayerID;
		public int WorldSize;
		public World()
		{
<<<<<<< Updated upstream
			Snakes = new Dictionary<int,Snake>();
=======
			Snakes = new Dictionary<int, Snake>();
>>>>>>> Stashed changes
			Walls = new List<Wall?>();
			PowerUps = new List<PowerUp>();

		}
	}
}

