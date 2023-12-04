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
        private int respawnRate=24;
        private int snakeSpeed;
        private int worldSize;
        private int maxPowerups=20;
        private int nextPowerID;
        private int maxPowerupFrame;
        private int randomPowerupFrame;

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
            maxPowerupFrame = 75;
            Random rnd = new Random();
          
            randomPowerupFrame = rnd.Next(0, maxPowerupFrame);

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
            //populate powerups
            for (int i = 0; i<maxPowerups; i++)
            {
              
                world.PowerUps.Add(i, NewPowerUpMaker(i));
                nextPowerID++;
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
            lock (world)
            {
                foreach (PowerUp? p in world.PowerUps.Values)
                {
                    if (p.died == true)
                    {
                        //advance timer by 1
                        p.deathTimer++;
                        if (p.deathTimer >= respawnRate)
                        {
                            world.PowerUps.Remove(p.power);

                        }

                    }
                }
                if (world.PowerUps.Count < maxPowerups)
                {
                    if (randomPowerupFrame > 0)
                        randomPowerupFrame--;
                    else
                    {
                        Random rnd = new Random();
                        randomPowerupFrame = rnd.Next(0, maxPowerupFrame);

                        PowerUp newPower = NewPowerUpMaker(nextPowerID);
                        world.PowerUps.Add(newPower.power, newPower);
                        nextPowerID++;

                    }

                }

                foreach (SocketState client in clients.Values)
                {
                    if (world.Snakes.TryGetValue((int)client.ID, out Snake? snake))
                    {
                        if (snake.join)
                        {
                            world.Snakes.Remove((int)client.ID);
                            Snake newSnake = NewSnakeMaker((int)client.ID, snake.name);
                            SnakeCollider(newSnake);
                            while (newSnake.died == true)
                            {
                                newSnake = NewSnakeMaker((int)client.ID, snake.name);
                                SnakeCollider(newSnake);
                            }
                            world.Snakes.Add((int)client.ID, newSnake);
                        }

                    }
                    //respawn snake  
                    if (world.Snakes.TryGetValue((int)client.ID, out Snake? deadSnake))
                    {
                        if (deadSnake.died == true)
                            deadSnake.died = false;
                        if (deadSnake.framesDead >= respawnRate)
                        {
                            world.Snakes.Remove((int)client.ID);
                            Snake newSnake = NewSnakeMaker((int)client.ID, deadSnake.name);
                            SnakeCollider(newSnake);
                            while (newSnake.died == true)
                            {
                                newSnake = NewSnakeMaker((int)client.ID, deadSnake.name);
                                SnakeCollider(newSnake);
                            }
                            world.Snakes.Add((int)client.ID, newSnake);
                        }
                    }

                    if (world.Snakes.TryGetValue((int)client.ID, out Snake? snakeMove))
                    {
                        SnakeMover(snakeMove);
                        //check for snake teleportation
                        snakeTeleporter(snakeMove);
                        if (snakeMove.growing == true && snakeMove.growingFrames >= 24)
                        {
                            snakeMove.growing = false;


                        }

                    }
                    if (world.Snakes.TryGetValue((int)client.ID, out Snake? snakeCollide))
                    {
                        SnakeCollider(snakeCollide);
                    }


                }

            }

                HashSet<long> disconnectedClients = new HashSet<long>();
                string jsonToSend = "";
            lock (world)
            {
                foreach (Snake sendSnake in world.Snakes.Values)
                {
                    jsonToSend += JsonSerializer.Serialize(sendSnake) + "\n";
                }
                foreach (PowerUp powerUp in world.PowerUps.Values)
                {
                    jsonToSend += JsonSerializer.Serialize(powerUp) + "\n";
                }
                foreach (SocketState client in clients.Values)
                {


                    if (!Networking.Send(client.TheSocket, jsonToSend))
                    {
                        world.Snakes[(int)client.ID].dc = true;
                        world.Snakes[(int)client.ID].died = true;
                        world.Snakes[(int)client.ID].alive = false;
                        disconnectedClients.Add(client.ID);

                    }


                }
            }
                foreach (long id in disconnectedClients)
                {
                    RemoveClient(id);
                }
            
        }
        private PowerUp NewPowerUpMaker(int nextPower)
        {
            return new PowerUp(nextPower, ValidSpawnPoint(), false);
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
                double x = rnd.Next(-worldSize/2, worldSize/2);
                double y = rnd.Next(-worldSize / 2, worldSize / 2);
                Vector2D newPoint = new Vector2D(x,y);

                foreach(Wall? wall in world.Walls)

                {
                    Vector2D? diff = newPoint - wall.p2;
                    if (wall.p1.GetX() == wall.p2.GetX())
                    {


                        if (wall.p1.GetY() < wall.p2.GetY())
                        {
                            if (y >= wall.p1.GetY() - 35 && y <= wall.p2.GetY() + 35)
                                if (diff.GetX() <= 35 && diff.GetX() >= -35)
                                {
                                    valid = false;
                                    break;
                                }
                        }
                        else if (wall.p1.GetY() > wall.p2.GetY())
                            if (y <= wall.p1.GetY() + 35 && x >= wall.p2.GetY() - 35)
                                if (diff.GetX() <= 35 && diff.GetX() >= -35)
                                {
                                    valid = false;
                                    break;
                                }
                    }
                    //if wall is Horizontal
                    if (wall.p1.GetY() == wall.p2.GetY())
                    {
                        if (wall.p1.GetX() < wall.p2.GetX())
                        {
                            if (x >= wall.p1.GetX() - 35 && x <= wall.p2.GetX() + 35)
                                if (diff.GetY() <= 35 && diff.GetY() >= -35)
                                {
                                    valid = false;
                                    break;
                                }
                        }

                        else if (wall.p1.GetX() > wall.p2.GetX())
                            if (x <= wall.p1.GetX() + 35 && x >= wall.p2.GetX() - 35)
                                if (diff.GetY() <= 35 && diff.GetY() >= -35)
                                {
                                    valid = false;
                                    break;
                                }
                    }

                }
                
                if (valid == true)
                {
                    return new Vector2D(x, y);
                }

            }

        }
        private void SnakeCollider(Snake s)
        {
            
            double wallCollisionRange = 30;
            double powerUpCollisionRange = 20;


            //wall collisions

            foreach (Wall? wall in world.Walls)
            {
                foreach (Vector2D body in s.body)
                {
                    if (wall != null)
                    {
                        Vector2D diff = wall.p1 - body;
                        //if wall is Verticle
                        if (wall.p1.GetX() == wall.p2.GetX())
                        {


                            if (wall.p1.GetY() < wall.p2.GetY())
                            {
                                if (body.GetY() >= wall.p1.GetY() - wallCollisionRange && body.GetY() <= wall.p2.GetY() + wallCollisionRange)
                                    if (diff.GetX() <= wallCollisionRange && diff.GetX() >= -wallCollisionRange)
                                    {
                                        s.alive = false;
                                        s.died = true;
                                        s.score = 0;
                                        break;
                                    }
                            }
                            else if (wall.p1.GetY() > wall.p2.GetY())
                                if (body.GetY() <= wall.p1.GetY() + wallCollisionRange && body.GetY() >= wall.p2.GetY() - wallCollisionRange)
                                    if (diff.GetX() <= wallCollisionRange && diff.GetX() >= -wallCollisionRange)
                                    {
                                        s.alive = false;
                                        s.died = true;
                                        s.score = 0;
                                        break;
                                    }
                        }
                        //if wall is Horizontal
                        if (wall.p1.GetY() == wall.p2.GetY())
                        {
                            if (wall.p1.GetX() < wall.p2.GetX())
                            {
                                if (body.GetX() >= wall.p1.GetX() - wallCollisionRange && body.GetX() <= wall.p2.GetX() + wallCollisionRange)
                                    if (diff.GetY() <= wallCollisionRange && diff.GetY() >= -wallCollisionRange)
                                    {
                                        s.alive = false;
                                        s.died = true;
                                        s.score = 0;
                                        break;
                                    }
                            }

                            else if (wall.p1.GetX() > wall.p2.GetX())
                                if (body.GetX() <= wall.p1.GetX() + wallCollisionRange && body.GetX() >= wall.p2.GetX() - wallCollisionRange)
                                    if (diff.GetY() <= wallCollisionRange && diff.GetY() >= -wallCollisionRange)
                                    {
                                        s.alive = false;
                                        s.died = true;
                                        s.score = 0;
                                        break;
                                    }
                        }

                    }
                }
            }
            Vector2D head = s.body.Last<Vector2D>();
            //collision detection for powerup
            foreach(PowerUp? p in world.PowerUps.Values)
            {
                if (p.died == false)
                {
                    Vector2D diff = p.loc - head;
                    if (diff.GetX() <= powerUpCollisionRange && diff.GetX() >= -powerUpCollisionRange && diff.GetY() <= powerUpCollisionRange && diff.GetY() >= -powerUpCollisionRange)
                    {
                        //set to grow
                        s.growing = true;
                        s.growingFrames = 0;
                        //increase score
                        s.score++;
                        //set powerup to died
                        p.died = true;
                        break;
                    }
                }
            }

        }
        private void SnakeMover (Snake s)
        {
            if (s.alive)
            {

                double headMoveX = s.dir.GetX();
                double headMoveY = s.dir.GetY();

                Vector2D head = s.body.Last<Vector2D>();
                Vector2D newHead = new Vector2D(head.GetX() + (headMoveX * snakeSpeed), head.GetY() + (headMoveY * snakeSpeed));



                Vector2D tail = s.body.First<Vector2D>();
                double newTailX = tail.GetX();
                double newTailY = tail.GetY();
                if (tail.GetX() == s.body[1].GetX() && s.growing == false)
                {

                    if (tail.GetY() < s.body[1].GetY())
                    {

                        newTailY = tail.GetY() + snakeSpeed;
                    }
                    else if (tail.GetY() > s.body[1].GetY())
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
                else s.growingFrames++;
                Vector2D newTail = new Vector2D(newTailX, newTailY);


                s.body[0] = newTail;
                s.body[s.body.Count - 1] = newHead;
                
            }
            else { s.framesDead++; }
        }

        private void snakeTeleporter(Snake s)
        {
            Vector2D head = s.body[s.body.Count - 1];
            Vector2D tail = s.body[0];
            
            //if x point is off the world +
            if (head.GetX() >= 1000)
            {

                s.body[s.body.Count - 1]= new Vector2D(1000, head.GetY());
                s.body.Add(new Vector2D(-1000, head.GetY()));
                s.body.Add(new Vector2D(-1000, head.GetY()));
            }
            //if x point is off the world -
            else if (head.GetX() <= -1000)
            {
                s.body[s.body.Count - 1] = new Vector2D(-1000, head.GetY());
                s.body.Add(new Vector2D(1000, head.GetY()));
                s.body.Add(new Vector2D(1000, head.GetY()));
            }
            //if y point is off the world +
            else if (head.GetY() >= 1000)
            {
                s.body.Remove(head);
                s.body.Add(new Vector2D(head.GetX(), 1000));
                s.body.Add(new Vector2D(head.GetX(), -1000));
                s.body.Add(new Vector2D(head.GetX(), -1000));
            }
            //if y point is off the world -
            else if (head.GetY() <= -1000)
            {
                s.body.Remove(head);
                s.body.Add(new Vector2D(head.GetX(), -1000));
                s.body.Add(new Vector2D(head.GetX(), 1000));
                s.body.Add(new Vector2D(head.GetX(), 1000));
            }

            //check for snake teleportation tail
            //if x point is off the world +
            if (tail.GetX() >= 1000)
            {

                s.body.RemoveAt(0);
                s.body.RemoveAt(0);

            }
            //if x point is off the world -
            else if (tail.GetX() <= -1000)
            {
                s.body.RemoveAt(0);
                s.body.RemoveAt(0);
            }
            //if y point is off the world +
            else if (tail.GetY() >= 1000)
            {
                s.body.RemoveAt(0);
                s.body.RemoveAt(0);
            }
            //if y point is off the world -
            else if (tail.GetY() <= -1000)
            {
                s.body.RemoveAt(0);
                s.body.RemoveAt(0);
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
            // Remove the client if they aren't still connected
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                return;
            }

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
                        if (world.Snakes[(int)state.ID].alive == true)
                        {
                            if (p.Contains("up") && world.Snakes[(int)state.ID].dir.GetY() == 0)
                            {
                                world.Snakes[(int)state.ID].dir = new SnakeGame.Vector2D(0, -1);
                                world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
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

