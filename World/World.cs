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
		public Dictionary<int,Snake> Snakes{ get; }
		public List<Wall?> Walls { get; }
        public Dictionary<int,PowerUp> PowerUps { get; }
        public Dictionary<int,DeadSnake> DeadSnakes { get; }
        public int PlayerID { get; set; }
        public int WorldSize { get; set; }
        public World()
		{

			Snakes = new Dictionary<int,Snake>();
			DeadSnakes = new Dictionary<int,DeadSnake>();
            Walls = new List<Wall?>();
			PowerUps = new Dictionary<int, PowerUp>();

		}
	}
}

