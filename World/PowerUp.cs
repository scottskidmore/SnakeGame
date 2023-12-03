using System;
using System.Text.Json.Serialization;
using SnakeGame;

namespace World
{
	/// <summary>
	/// Class for the PowerUp object that holds the
	/// id the location and a died bool.
	/// </summary>
	public class PowerUp
	{
        public int power { get; }
        public Vector2D loc { get; }
        public bool died { get; set; }
        [JsonConstructor]
        public PowerUp(int power,Vector2D loc,bool died)
		{
			this.power = power;
			this.loc = loc;
			this.died = died;
		}
	}
}

