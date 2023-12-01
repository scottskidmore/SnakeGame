using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using NetworkUtil;
using World;

namespace Server
{
	public class ServerController
	{
        private int time;
        private int respawnRate;
        private int worldSize;
        private World.World world;
        // A map of clients that are connected, each with an ID
        private Dictionary<long, SocketState> clients ;

        static void Main(string[] args)
        {
            ServerController server = new ServerController();
            server.StartServer();
            
        }
        /// <summary>
        /// Initialized the server's state
        /// </summary>
        public ServerController()
        {
            clients = new Dictionary<long, SocketState>();
            time = 0;
            respawnRate = 0;
            world = new();

        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            // This begins an "event loop"
            XmlReader reader = XmlNodeReader.Create("settings.xml");
            reader.ReadToDescendant("GameSettings");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "MSPerFrame")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "MSPerFrame";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
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
                    DataContractSerializer ser = new(typeof(World.Wall));
                    World.Wall? w = (World.Wall?)ser.ReadObject(reader);
                    if (w != null)
                    {
                        world.Walls.Add(w);
                    }


                }

            }
            Networking.StartServer(AcceptConnection, 11000);

            Console.WriteLine("Server is running");
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


        public void AcceptConnection(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

           

            // change the state's network action to the 
            // receive handler so we can process data when something
            // happens on the network
            state.OnNetworkAction = ReceiveHandshake;

            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a network action occurs (see lines 64-66)
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveHandshake(SocketState state)
        {
            // Remove the client if they aren't still connected
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                return;
            }
            //recieve name
            
            string name = state.GetData();

            //create snake
            World.Snake newSnake = new World.Snake((int)state.ID, name);
            world.Snakes.Add(newSnake.snake, newSnake);
            //send world data
            
            Networking.Send(state.TheSocket!, newSnake.snake + "\n" + worldSize + "\n" + JsonSerializer.Serialize(world.Walls)+ "\n");
           


            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }
            Console.WriteLine("accepted new client:"+ state.ID);
            state.OnNetworkAction = ProcessMessage;
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }

    

        /// <summary>
        /// Given the data that has arrived so far, 
        /// potentially from multiple receive operations, 
        /// determine if we have enough to make a complete message,
        /// and process it (print it and broadcast it to other clients).
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessMessage(SocketState state)
        {
            string totalData = state.GetData();

            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {

                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                

                // Remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);

                //if we recieved a json moving
                if (p.StartsWith("{"))
                {
                    JsonDocument doc = JsonDocument.Parse(p);
                    string? s = JsonSerializer.Deserialize<String?>(p);
                    if (s != null)
                    {
                        if (s.Contains("up"))
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(0,-1);
                        }
                        else if (s.Contains("down"))
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(0, 1);
                        }
                        else if (s.Contains("right"))
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(1, 0);
                        }
                        else if (s.Contains("left"))
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(-1, 0);
                        }
                    }


                }

                // Broadcast the message to all clients
                // Lock here beccause we can't have new connections 
                // adding while looping through the clients list.
                // We also need to remove any disconnected clients.
                HashSet<long> disconnectedClients = new HashSet<long>();
                lock (clients)
                {
                    foreach (SocketState client in clients.Values)
                    {
                        if (!Networking.Send(client.TheSocket!, "Message from client " + state.ID + ": " + p))
                            disconnectedClients.Add(client.ID);
                    }
                }
                foreach (long id in disconnectedClients)
                    RemoveClient(id);
            }
        }


        /// <summary>
        /// Removes a client from the clients dictionary
        /// </summary>
        /// <param name="id">The ID of the client</param>
        private void RemoveClient(long id)
    {
        Console.WriteLine("Client " + id + " disconnected");
        lock (clients)
        {
            clients.Remove(id);
        }
    }
    }
}

