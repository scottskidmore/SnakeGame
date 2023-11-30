using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;
using SnakeGame;

namespace World
{
    /// <summary>
    /// Class for the Wall object that holds the id
    /// and the position of the wall or line of walls.
    /// </summary>
    [DataContract(Namespace = "")]
    public class Wall
    {
        [DataMember(Name = "ID")]
        public int wall { get; set; }
        [DataMember(Name = "p1")]
        public Vector2D p1 { get; set; }
        [DataMember(Name = "p2")]
        public Vector2D p2 { get; set; }

        public Wall(int wall,Vector2D p1,Vector2D p2)
		{
			this.wall = wall;
			this.p1 = p1;
			this.p2 = p2;

		}
    }
}

