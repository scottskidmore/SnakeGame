using System;
namespace World
{
	/// <summary>
	/// Class for the World object which holds a list
	/// for the snakes, walls and power ups and saves
	/// the world size and player id.
	/// </summary>
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

