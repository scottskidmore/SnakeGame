using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using NetworkUtil;
using SnakeGame;
using World;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server
{
	public class ServerController
	{
        private int time;
        private int respawnRate;
        private int snakeSpeed;
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
            snakeSpeed = 6;
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
                watch.Start();
                while (watch.ElapsedMilliseconds < time)
                {
                    
                }
                watch.Restart();
                Update();
                //update the world
            }
            
        }

        private void Update()
        {
            foreach (SocketState client in clients.Values)
            {
                if (world.Snakes.TryGetValue((int)client.ID, out Snake? snake))
                {
                    if (snake.join)
                    {
                        world.Snakes.Remove((int)client.ID);
                        Snake newSnake = NewSnakeMaker((int)client.ID, snake.name);
                        world.Snakes.Add((int)client.ID, newSnake);
                    }

                }
                if (world.Snakes.TryGetValue((int)client.ID, out Snake? snakeMove))
                {
                    SnakeMover(snakeMove);
                }
                if (world.Snakes.TryGetValue((int)client.ID, out Snake? snakeColide))
                {
                    SnakeColider(snakeColide);
                }
            }
            foreach (SocketState client in clients.Values)
            {
                
                foreach (Snake sendSnake in world.Snakes.Values)
                {
                    Networking.Send(client.TheSocket, JsonSerializer.Serialize(sendSnake)+"\n");
                }
                foreach (PowerUp powerUp in world.PowerUps.Values)
                {
                    Networking.Send(client.TheSocket, JsonSerializer.Serialize(powerUp));
                }
            }
        }
        private Snake NewSnakeMaker(int id,string name)
        {
            Vector2D startPoint = ValidSpawnPoint();
            Random rnd = new Random();
            int x = rnd.Next(0, 3);
            Vector2D dir;
            Vector2D endPoint;
            
                dir = new Vector2D(0, 1);
            endPoint = new Vector2D(startPoint.GetX(), startPoint.GetY() - 120);
            
            if (x == 1)
            {
                dir = new Vector2D(0, -1);
                endPoint = new Vector2D(startPoint.GetX(), startPoint.GetY() + 120);
            }
            if (x == 2)
            {
                dir = new Vector2D(1, 0);
                endPoint = new Vector2D(startPoint.GetX()-120, startPoint.GetY());
            }
            if (x == 3)
            {
                dir = new Vector2D(-1, 0);
                endPoint = new Vector2D(startPoint.GetX()+120, startPoint.GetY());
            }
            List<Vector2D> list = new List<Vector2D>() { endPoint, startPoint };
            return new Snake(id,name,0,list,dir,false,true,false,false);
        }
        private Vector2D ValidSpawnPoint()
        {
            while (true)
            {
                bool valid = true;
                Random rnd = new Random();
                int x = rnd.Next(-worldSize/2, worldSize/2);
                int y = rnd.Next(-worldSize / 2, worldSize / 2);
                foreach(Wall? wall in world.Walls)
                {
                    if (wall != null)
                    {
                        if (wall.p1.GetX() == wall.p2.GetX())
                        {
                            if(wall.p1.GetY() > wall.p2.GetY())
                            {
                                if(y< wall.p1.GetY()|| y > wall.p2.GetY())
                                {
                                    if (x <= 25 + 120 + wall.p2.GetX())
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (wall.p1.GetX() == wall.p2.GetX())
                        {
                            if (wall.p1.GetY() <wall.p2.GetY())
                            {
                                if (y < wall.p2.GetY() || y > wall.p1.GetY())
                                {
                                    if (x <= 25 + 120 + wall.p2.GetX())
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (wall.p1.GetY() == wall.p2.GetY())
                        {
                            if (wall.p1.GetX() > wall.p2.GetX())
                            {
                                if (y < wall.p1.GetX() || y > wall.p2.GetX())
                                {
                                    if (x <= 25 + 120 + wall.p2.GetY())
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (wall.p1.GetY() == wall.p2.GetY())
                        {
                            if (wall.p1.GetX() < wall.p2.GetX())
                            {
                                if (y < wall.p2.GetX() || y > wall.p1.GetX())
                                {
                                    if (x <= 25 + 120 + wall.p2.GetY())
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (valid == true)
                {
                    return new Vector2D(x, y);
                }

            }

        }
        private void SnakeColider(Snake s)
        {
            Vector2D head = s.body.Last<Vector2D>();
            double wallCollisionRange = 35;
            double powerUpCollisionRange = 20;


            //wall colisions

            foreach (Wall? wall in world.Walls)
            {
                if (wall != null)
                {
                    Vector2D diff = wall.p1-head;
                    //if wall is Verticle
                    if (wall.p1.GetX() == wall.p2.GetX())
                    {


                        if (wall.p1.GetY() < wall.p2.GetY())
                        {
                            if (head.GetY() >= wall.p1.GetY() - 25 && head.GetY() <= wall.p2.GetY() + 25)
                                if (diff.GetX() <= wallCollisionRange && diff.GetX() >= -wallCollisionRange)
                                {
                                    s.alive = false;
                                    s.died = true;
                                    break;
                                }
                        }
                        else if (wall.p1.GetY() > wall.p2.GetY())
                            if (head.GetY() <= wall.p1.GetY() + 25 && head.GetY() >= wall.p2.GetY() - 25)
                                if (diff.GetX() <= wallCollisionRange && diff.GetX() >= -wallCollisionRange)
                                {
                                    s.alive = false;
                                    s.died = true;
                                    break;
                                }
                    }
                    //if wall is Horizontal
                    if (wall.p1.GetY() == wall.p2.GetY())
                    {
                        if (wall.p1.GetX() < wall.p2.GetX())
                        {
                            if (head.GetX() >= wall.p1.GetX() - 25 && head.GetX() <= wall.p2.GetX() + 25)
                                if (diff.GetY() <= wallCollisionRange && diff.GetY() >= -wallCollisionRange)
                                {
                                    s.alive = false;
                                    s.died = true;
                                    break;
                                }
                        }
                            
                       else if (wall.p1.GetX() > wall.p2.GetX())
                               if (head.GetX() <= wall.p1.GetX()+25 && head.GetX() >= wall.p2.GetX()-25)
                                 if (diff.GetY() <= wallCollisionRange && diff.GetY() >= -wallCollisionRange)
                                        {
                                            s.alive = false;
                                            s.died = true;
                                            break;
                                        }
                    }

                }
            }

            //collision detection for powerup
            foreach(PowerUp? p in world.PowerUps.Values)
            {
                Vector2D diff = p.loc - head;
                if(diff.GetX()<= powerUpCollisionRange&&diff.GetX()>= -powerUpCollisionRange&& diff.GetY() <= powerUpCollisionRange && diff.GetY() >= -powerUpCollisionRange)
                {
                    //set to grow
                    s.growing = true;
                    //increase score
                    s.score++;
                    //set powerup to died
                    p.died = true;
                }
            }

        }
        private void SnakeMover (Snake s)
        {
            double headMoveX = s.dir.GetX();
            double headMoveY = s.dir.GetY();

            Vector2D head = s.body.Last<Vector2D>();
            Vector2D newHead = new Vector2D(head.GetX()+(headMoveX * snakeSpeed),head.GetY() + (headMoveY * snakeSpeed));
            
            Vector2D tail = s.body.First<Vector2D>();
            double newTailX = tail.GetX();
            double newTailY = tail.GetY();
            if (tail.GetX() == s.body[1].GetX() && s.growing == false)
            {
               
                if (tail.GetY() < s.body[1].GetY())
                {

                    newTailY = tail.GetY() + snakeSpeed;
                }
                else if (tail.GetY() > s.body[1].GetY() )
                {

                    newTailY = tail.GetY() - snakeSpeed;
                }
                else s.body.Remove(tail);

            }
            else if (tail.GetY() == s.body[1].GetY() && s.growing == false)
            {
                if (tail.GetX() < s.body[1].GetX())
                {
                    newTailX = tail.GetX() + snakeSpeed;
                }
                else if (tail.GetX() > s.body[1].GetX())
                {
                    newTailX = tail.GetX() - snakeSpeed;
                }
                else s.body.Remove(tail);

            }
            Vector2D newTail = new Vector2D(newTailX, newTailY);
           
            
            s.body[0] = newTail;
            s.body[s.body.Count - 1] = newHead;
            

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
            Snake newSnake = new Snake((int)state.ID, name);
            state.RemoveData(0, name.Length);
            world.Snakes.Add(newSnake.snake, newSnake);
            //send world data
            string walls = "";
            foreach(Wall? wall in world.Walls)
            {
               if(wall != null)
                {
                    walls=walls+JsonSerializer.Serialize(wall) + "\n";
                }
            }
            Networking.Send(state.TheSocket, newSnake.snake + "\n" + worldSize + "\n"+walls);



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
                    
                    if (p != null)
                    {
                        if (p.Contains("up")&&world.Snakes[(int)state.ID].dir.GetY()==0)
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(0,-1);
                            world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count-1]);
                        }
                        else if (p.Contains("down") && world.Snakes[(int)state.ID].dir.GetY() == 0)
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(0, 1);
                            world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                        }
                        else if (p.Contains("right") && world.Snakes[(int)state.ID].dir.GetX() == 0)
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(1, 0);
                            world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                        }
                        else if (p.Contains("left") && world.Snakes[(int)state.ID].dir.GetX() == 0)
                        {
                            world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(-1, 0);
                            world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                        }
                    }


                }

             
            }
            Networking.GetData(state);
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

