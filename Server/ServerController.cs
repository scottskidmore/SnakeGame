using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Serialization;
using NetworkUtil;

namespace Server
{
	public class ServerController
	{
        // A map of clients that are connected, each with an ID
        private Dictionary<long, SocketState> clients ;

        static void Main(string[] args)
        {
            ServerController server = new ServerController();
            server.StartServer();
            XmlReader reader = XmlNodeReader.Create("/Users/scottskidmore/game-dreamweavers_game/settings.xml");
            string wantedNodeContents = string.Empty;
            int time=0;
            int respawnRate = 0;
            int worldSize = 0;
            World.World world = new();
            reader.ReadToDescendant("GameSettings");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "MSPerFrame")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "MSPerFrame";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int),xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null) {
                        time = (int)loadedObjectXml;
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "RespawnRate")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "RespawnRate";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
                        respawnRate = (int)loadedObjectXml;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "UniverseSize")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "UniverseSize";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
                        worldSize = (int)loadedObjectXml;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Wall")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "Wall";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(World.Wall), xRoot);
                    World.Wall? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as World.Wall;
                    if (loadedObjectXml != null)
                    {
                        world.Walls.Add(loadedObjectXml);
                    }

                }
                
            }
            while (true)
            {
               
                Stopwatch watch = new Stopwatch();
                //while (watch.ElapsedMilliseconds < time)
                //{

                //}
                //watch.Restart();
               // Update();
                    //update the world
            }
        }
        /// <summary>
        /// Initialized the server's state
        /// </summary>
        public ServerController()
        {
            clients = new Dictionary<long, SocketState>();
        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            // This begins an "event loop"
            Networking.StartServer(AcceptConnection, 11000);

            Console.WriteLine("Server is running");
        }


        public void AcceptConnection(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }

            // change the state's network action to the 
            // receive handler so we can process data when something
            // happens on the network
            

            Networking.GetData(state);
        }

        public static void HandleClientCommand(SocketState state)
        {
            string request = state.GetData();
            //Handle the command from client
           // Networking.SendAndClose(state.TheSocket, response);
        }
    }
}

