using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using SnakeGame;

namespace World
{
    /// <summary>
    /// Class for the Wall object that holds the id
    /// and the position of the wall or line of walls.
    /// </summary>
    [XmlRoot(ElementName = "Wall")]
    public class Wall
	{
        [XmlElement("ID")]
        public int wall { get; }
        [XmlElement("p1")]
        public Vector2D p1 { get; }
        [XmlElement("p2")]
        public Vector2D p2 { get; }
        [JsonConstructor]
        public Wall(int wall,Vector2D p1,Vector2D p2)
		{
			this.wall = wall;
			this.p1 = p1;
			this.p2 = p2;

		}
        public Wall()
        {
            

        }
    }
}

