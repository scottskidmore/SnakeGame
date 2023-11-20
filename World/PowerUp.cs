using System;
using System.Text.Json.Serialization;
using SnakeGame;

namespace World
{
	public class PowerUp
	{
        public int power { get; set; }
        public Vector2D loc { get; set; }
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

