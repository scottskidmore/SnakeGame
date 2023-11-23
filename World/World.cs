using System;
namespace World
{
	public class World
	{
		public Dictionary<int,Snake> Snakes;
		public List<Wall?> Walls;
		public Dictionary<int,PowerUp> PowerUps;
        public Dictionary<int,DeadSnake> DeadSnakes;
        public int PlayerID;
		public int WorldSize;
		public World()
		{

			Snakes = new Dictionary<int,Snake>();
			DeadSnakes = new Dictionary<int,DeadSnake>();
            Walls = new List<Wall?>();
			PowerUps = new Dictionary<int, PowerUp>();

		}
	}
}

