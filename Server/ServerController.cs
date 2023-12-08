using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.VisualBasic;
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
        private int maxPowerups;
        private int nextPowerID;
        private int maxPowerupFrame;
        private int randomPowerupFrame;
        private int powerupLength;
        private bool deathMatch;

        private World.World world;
        // A map of clients that are connected, each with an ID
        private Dictionary<long, SocketState> clients;
        private delegate void SnakeDeath(Snake s, Snake e);
        private delegate void PowerUpEffect(Snake s);
        private SnakeDeath snakeDeath;
        private PowerUpEffect powerUpEffect;
        private List<int> wallIDs;

        /////////////////////////////////////////////////////////////////////////////////////////
        // Code to Start and Maintain Server
        /////////////////////////////////////////////////////////////////////////////////////////
        
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
            snakeDeath = new(SnakeNormDeath);
            powerUpEffect = new(PowerUpEffectNorm);
            wallIDs = new List<int>();

            Random rnd = new Random();

            randomPowerupFrame = rnd.Next(0, maxPowerupFrame);
            

        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            string path = Path.GetFullPath("settings.xml");
            // This begins an "event loop"
            XmlReader reader = XmlNodeReader.Create(path);
            reader.ReadToDescendant("GameSettings");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "GrowthRate")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "GrowthRate";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
                       
                            powerupLength = (int)loadedObjectXml;
                        
                        
                    }
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "SnakeSpeed")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "SnakeSpeed";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
                        snakeSpeed = (int)loadedObjectXml;

                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "MaxPowerUps")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "MaxPowerUps";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int), xRoot);
                    int? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as int?;
                    if (loadedObjectXml != null)
                    {
                        maxPowerups = (int)loadedObjectXml;

                    }
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "DeathMatch")
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "DeathMatch";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(bool), xRoot);
                    bool? loadedObjectXml = xmlSerializer.Deserialize(reader.ReadSubtree()) as bool?;
                    if (loadedObjectXml != null)
                    {
                           deathMatch = (bool)loadedObjectXml;
                        if (deathMatch)
                        {

                            snakeDeath = new(SnakeMatchDeath);
                            powerUpEffect = new(PowerUpEffectDM);
                            //invincibility is 5 seconds when framerate is 34 seconds
                            powerupLength = 170;
                            maxPowerups = 5;


                        }
                       
                        
                    }
                    else
                    {
                        snakeDeath = new(SnakeNormDeath);
                        powerUpEffect = new(PowerUpEffectNorm);
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
                        wallIDs.Add(w.wall);
                    }


                }

            }
            //populate powerups
            for (int i = 0; i < maxPowerups; i++)
            {

                world.PowerUps.Add(i, NewPowerUpMaker(i));
                nextPowerID++;
            }
           
            Networking.StartServer(AcceptConnection, 11000);
            int update = 0;
            Stopwatch watch1 = new Stopwatch();
            watch1.Start();
            Console.WriteLine("Server is running");
            while (true)
            {
                
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while (watch.ElapsedMilliseconds < time)
                {

                }
                watch.Restart();
                update++;
                //update the world
                lock (world) { Update(); }
                if (watch1.ElapsedMilliseconds >= 1000)
                {
                    Console.WriteLine("FPS " + update);
                    update = 0;
                    watch1.Restart();
                }
                //send the update



            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Server Code to Update and Send World to Clients
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Updates world by performing respawning of powerups and snakes, moving all snakes that
        /// are alive, and checking for any snkae collisions
        /// compiles all changes in a JSON file and gives/starts the sending method.
        /// </summary>
        private void Update()
        {


            foreach (PowerUp? p in world.PowerUps.Values)
            {
                if (p.died)
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

            foreach (int key in world.Snakes.Keys)
            {
                Snake newSnake = world.Snakes[key];

                //check if snake is new or if it needs to be respawned
                if (newSnake.join || newSnake.framesDead >= respawnRate)
                {
                    
                        newSnake = NewSnakeMaker(newSnake.snake, newSnake.name);
                    

                }
                //check if snake is dead
                if (newSnake.died == true)
                    newSnake.died = false;

                if (newSnake.body.Count > 0)
                {
                    SnakeMover(newSnake);


                }

                world.Snakes[key] = newSnake;


            }
            string jsonToSend = "";
            foreach (Snake sendSnake in world.Snakes.Values)
            {

                jsonToSend += JsonSerializer.Serialize(sendSnake) + "\n";
                if (sendSnake.dc)
                {
                    world.Snakes.Remove(sendSnake.snake);
                }

            }
            foreach (PowerUp powerUp in world.PowerUps.Values)
            {
                jsonToSend += JsonSerializer.Serialize(powerUp) + "\n";
            }

            SendUpdate(jsonToSend);

        }

        /// <summary>
        /// Once update JSON has been recieved this method sends the JSON to all clients,
        /// handles client DCs, and removes DCed clients from client list.
        /// </summary>
        /// <param name="jsonToSend">JSON string of world update to be sent to the clients/param>
        private void SendUpdate( string jsonToSend)

        {
            HashSet<long> disconnectedClients = new HashSet<long>();

            lock (clients)
            {
                foreach (SocketState client in clients.Values)
                {
                    
                   
                    if (!Networking.Send(client.TheSocket, jsonToSend))
                    {
                        if (world.Snakes.ContainsKey((int)client.ID))
                        {
                            world.Snakes[(int)client.ID].dc = true;
                            world.Snakes[(int)client.ID].died = true;
                            world.Snakes[(int)client.ID].alive = false;
                            disconnectedClients.Add(client.ID);
                        }

                    }


                }
            }

            foreach (long id in disconnectedClients)
            {
                RemoveClient(id);
            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Server Methods to Create and Spawn New World Objects
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new PowerUp object
        /// </summary>
        /// <param name="nextPower">int representing the next valid PowerUp ID/param>
        private PowerUp NewPowerUpMaker(int nextPower)
        {
            PowerUp newPower = new PowerUp(nextPower, ValidSpawnPoint(), false);
            newPower.deathTimer = 0;
            return newPower;
        }

       

        /// <summary>
        /// Creates a new Snake object
        /// </summary>
        /// <param name="id">int Client ID used as new snake ID/param>
        /// <param name="name">string of client's name used as new snake name/param>
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
            dir.Normalize();
            List<Vector2D> list = new List<Vector2D>() { endPoint, startPoint };
            Snake tempSnake = new Snake(id, name, 0, list, dir, false, true, false, false);
            //check if snake is valid
            SnakeCollider(tempSnake);
            if (tempSnake.died==true)
                return NewSnakeMaker(id, name);
            else
                return tempSnake;
        }

        /// <summary>
        /// Finds a valid vector point for a new object to spawn
        /// </summary>
        private Vector2D ValidSpawnPoint()
        {
            while (true)
            {
                bool valid = true;
                Random rnd = new Random();
                double x = rnd.Next(-worldSize/2+120, worldSize/2-120);
                double y = rnd.Next(-worldSize / 2+120, worldSize / 2-120);
                Vector2D newPoint = new Vector2D(x,y);
                foreach (PowerUp? powerup in world.PowerUps.Values)
                {
                    if (powerup != null)
                    {
                        if (y >= powerup.loc.GetY() - 15 && y <= powerup.loc.GetY() + 15)
                        {
                            valid = false;
                            break;
                        }
                        if (x >= powerup.loc.GetX() - 15 && x <= powerup.loc.GetX() + 15)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                int snakeCollisionRange= 15;
                foreach (Snake? snake in world.Snakes.Values)
                {
                    if (valid == false){
                        break;
                    }
                    if (snake != null && snake.alive&& snake.body.Count>0)
                    {
                       
                        for (int i = 1; i < snake.body.Count; i++)
                            {
                               
                                //if snake body is Verticle
                                if (snake.body[i - 1].GetX() == snake.body[i].GetX())
                                {


                                    if (snake.body[i - 1].GetY() < snake.body[i].GetY())
                                    {
                                        if (y >= snake.body[i - 1].GetY() - snakeCollisionRange && y <= snake.body[i].GetY() + snakeCollisionRange)
                                            if (x <= snake.body[i - 1].GetX()+snakeCollisionRange && x >= snake.body[i - 1].GetX() - snakeCollisionRange)
                                            {
                                            valid = false;
                                                break;
                                            }
                                    }
                                    else if (snake.body[i - 1].GetY() > snake.body[i].GetY())
                                        if (y <= snake.body[i - 1].GetY() + snakeCollisionRange && y >= snake.body[i].GetY() - snakeCollisionRange)
                                            if (x <= snake.body[i - 1].GetX()+snakeCollisionRange && x >= snake.body[i - 1].GetX() - snakeCollisionRange)
                                            {
                                            valid = false;
                                                break;
                                            }
                                }
                                //if snake body is Horizontal
                                if (snake.body[i - 1].GetY() == snake.body[i].GetY())
                                {
                                    if (snake.body[i - 1].GetX() < snake.body[i].GetX())
                                    {
                                        if (x >= snake.body[i - 1].GetX() - snakeCollisionRange && x <= snake.body[i].GetX() + snakeCollisionRange)
                                            if (y <= snake.body[i - 1].GetY() +snakeCollisionRange && y >= snake.body[i - 1].GetY() - snakeCollisionRange)
                                            {
                                            valid = false;
                                                break;
                                            }
                                    }

                                    else if (snake.body[i - 1].GetX() > snake.body[i].GetX())
                                        if (x <= snake.body[i - 1].GetX() + snakeCollisionRange && x >= snake.body[i].GetX() - snakeCollisionRange)
                                            if (y <= snake.body[i - 1].GetY() +snakeCollisionRange && y >= snake.body[i - 1].GetY() - snakeCollisionRange)
                                            {
                                            valid = false;
                                                break;
                                            }
                                }
                            }
                        }

                        
                    }
                
                foreach (Wall? wall in world.Walls)

                {
                    if (wall != null)
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
                }
                
                if (valid == true)
                {
                    
                    return newPoint;
                }

            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Server Methods for Moving and Coliding Snakes
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the next position of the snake and moves the snake to the
        /// next frames position. calls the teleporter to check for teleportation
        /// </summary>
        /// <param name="s">The snake to be moved</param>
        private void SnakeMover(Snake s)
        {


            //only move if snake has two points and is alive (maybe change)
            if (s.alive && s.body.Count > 1)
            {
                s.dir.Normalize();
                double headMoveX = s.dir.GetX();
                double headMoveY = s.dir.GetY();

                Vector2D head = s.body.Last<Vector2D>();
                Vector2D newHead = new Vector2D(head.GetX() + (headMoveX * snakeSpeed), head.GetY() + (headMoveY * snakeSpeed));



                Vector2D tail = s.body.First<Vector2D>();

                double newTailX = tail.GetX();
                double newTailY = tail.GetY();
                bool growing = false;

                //check if powerup needs to stop
                if (!deathMatch)
                    growing = PowerupTimer(s);

                if (tail.GetX() == s.body[1].GetX() && growing == false)
                {

                    if (tail.GetY() < s.body[1].GetY())
                    {

                        newTailY = tail.GetY() + snakeSpeed;
                        if (newTailY == s.body[1].GetY())
                            s.body.Remove(tail);
                    }
                    else if (tail.GetY() > s.body[1].GetY())
                    {

                        newTailY = tail.GetY() - snakeSpeed;
                        if (newTailY == s.body[1].GetY())
                            s.body.Remove(tail);
                    }


                }
                else if (tail.GetY() == s.body[1].GetY() && growing == false)
                {
                    if (tail.GetX() < s.body[1].GetX())
                    {
                        newTailX = tail.GetX() + snakeSpeed;
                        if (newTailX == s.body[1].GetX())
                            s.body.Remove(tail);
                    }
                    else if (tail.GetX() > s.body[1].GetX())
                    {
                        newTailX = tail.GetX() - snakeSpeed;
                        if (newTailX == s.body[1].GetX())
                            s.body.Remove(tail);
                    }


                }

                Vector2D newTail = new Vector2D(newTailX, newTailY);



                s.body[0] = newTail;
                s.body[s.body.Count - 1] = newHead;
                //check for snake teleportation
                snakeTeleporter(s);
            }
            else { s.framesDead++; }


        }

        /// <summary>
        /// This method checks if the snake has left the world map and must be teleported to the other side of the map
        /// and informs the controller if it has
        /// </summary>
        /// <param name="s">The snake to be checked</param>
        private void snakeTeleporter(Snake s)
        {


            Vector2D head = s.body[s.body.Count - 1];
            Vector2D tail = s.body[0];


            //if x point is off the world +
            if (head.GetX() >= worldSize / 2)
            {

                s.body.Add(new Vector2D(head.GetX(), head.GetY()));
                s.body.Add(new Vector2D(-worldSize/2, head.GetY()));
                s.body.Add(new Vector2D(-worldSize / 2, head.GetY()));
            }

            //if x point is off the world -

            else if (head.GetX() <= -worldSize / 2)
            {
                s.body.Add( new Vector2D(head.GetX(), head.GetY()));
                s.body.Add(new Vector2D(worldSize / 2, head.GetY()));
                s.body.Add(new Vector2D(worldSize / 2, head.GetY()));
            }
            //if y point is off the world +
            else if (head.GetY() >= worldSize / 2)
            {
                
                s.body.Add(new Vector2D(head.GetX(), head.GetY()));
                s.body.Add(new Vector2D(head.GetX(), -worldSize / 2));
                s.body.Add(new Vector2D(head.GetX(), -worldSize / 2));
            }
            //if y point is off the world -
            else if (head.GetY() <= -worldSize / 2)
            {
                
                s.body.Add(new Vector2D(head.GetX(), head.GetY()));
                s.body.Add(new Vector2D(head.GetX(), worldSize / 2));
                s.body.Add(new Vector2D(head.GetX(), worldSize / 2));
                
            }


            //check for snake teleportation tail
            //if x point or y p[oint is outside of world bounds for tail

            if (tail.GetX() >= worldSize / 2 || tail.GetX() <= -worldSize / 2|| tail.GetY() >= worldSize / 2 || tail.GetY() <= -worldSize / 2)
            {
                List <Vector2D> newBody = new List<Vector2D>();
                foreach (Vector2D part in s.body) 
                {

                    if (part.Equals( tail))
                        continue;

                    newBody.Add(part);


                }
                
                s.body = newBody;
            
      
            }

            //check for collisions
            SnakeCollider(s);

        }

        /// <summary>
        /// This method checks if the snake has collided with any world objects after moving and/or teleporting
        /// </summary>
        /// <param name="s">The snake to be checked</param>
        private void SnakeCollider(Snake s)
        {
            
            double wallCollisionRange = 30;
            double powerUpCollisionRange = 20;
            double snakeCollisionRange = 10;
            //if deathmatch is active check if snake is invincible
            bool invincible = false;
            if (deathMatch)
                invincible = PowerupTimer(s);


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
                foreach (PowerUp? p in world.PowerUps.Values)
                {
                    if (p.died == false)
                    {
                        Vector2D diff = p.loc - head;
                        if (diff.GetX() <= powerUpCollisionRange && diff.GetX() >= -powerUpCollisionRange && diff.GetY() <= powerUpCollisionRange && diff.GetY() >= -powerUpCollisionRange)
                        {
                            //choose pawerup effect
                            powerUpEffect(s);
                            //set powerup to died
                            p.died = true;
                            break;
                        }
                    }
                
            }

           //if the snake is not invincible
            if (!invincible)
            {
                //collision detection for snakes
                foreach (Snake? snake in world.Snakes.Values)
                {
                    if (snake != null && snake.alive)
                    {
                        if (snake.snake != s.snake)
                        {
                            for (int i = 1; i < snake.body.Count; i++)
                            {
                                Vector2D diff = snake.body[i - 1] - head;
                                //if snake body is Verticle
                                if (snake.body[i - 1].GetX() == snake.body[i].GetX())
                                {


                                    if (snake.body[i - 1].GetY() < snake.body[i].GetY())
                                    {
                                        if (head.GetY() >= snake.body[i - 1].GetY() - snakeCollisionRange && head.GetY() <= snake.body[i].GetY() + snakeCollisionRange)
                                            if (diff.GetX() <= snakeCollisionRange && diff.GetX() >= -snakeCollisionRange)
                                            {
                                                snakeDeath(s, snake);
                                                break;
                                            }
                                    }
                                    else if (snake.body[i - 1].GetY() > snake.body[i].GetY())
                                        if (head.GetY() <= snake.body[i - 1].GetY() + snakeCollisionRange && head.GetY() >= snake.body[i].GetY() - snakeCollisionRange)
                                            if (diff.GetX() <= snakeCollisionRange && diff.GetX() >= -snakeCollisionRange)
                                            {
                                                snakeDeath(s, snake);
                                                break;
                                            }
                                }
                                //if snake body is Horizontal
                                if (snake.body[i - 1].GetY() == snake.body[i].GetY())
                                {
                                    if (snake.body[i - 1].GetX() < snake.body[i].GetX())
                                    {
                                        if (head.GetX() >= snake.body[i - 1].GetX() - snakeCollisionRange && head.GetX() <= snake.body[i].GetX() + snakeCollisionRange)
                                            if (diff.GetY() <= snakeCollisionRange && diff.GetY() >= -snakeCollisionRange)
                                            {
                                                snakeDeath(s, snake);
                                                break;
                                            }
                                    }

                                    else if (snake.body[i - 1].GetX() > snake.body[i].GetX())
                                        if (head.GetX() <= snake.body[i - 1].GetX() + snakeCollisionRange && head.GetX() >= snake.body[i].GetX() - snakeCollisionRange)
                                            if (diff.GetY() <= snakeCollisionRange && diff.GetY() >= -snakeCollisionRange)
                                            {
                                                snakeDeath(s, snake);
                                                break;
                                            }
                                }
                            }
                        }

                        else
                        {

                            bool checking = false;
                            for (int i = snake.body.Count - 1; i > 0; i--)
                            {

                                if (i <= snake.body.Count - 3 && !checking)
                                {
                                    Vector2D bToA = snake.body[i] - snake.body[i - 1];
                                    bToA.Normalize();
                                    if (snake.dir.GetX() == 1)
                                    {
                                        if (bToA.GetX() == -1)
                                        {
                                            checking = true;
                                            continue;
                                        }
                                    }
                                    if (snake.dir.GetX() == -1)
                                    {
                                        if (bToA.GetX() == 1)
                                        {
                                            checking = true;
                                            continue;
                                        }
                                    }
                                    if (snake.dir.GetY() == 1)
                                    {
                                        if (bToA.GetY() == -1)
                                        {
                                            checking = true;
                                            continue;
                                        }
                                    }
                                    if (snake.dir.GetY() == -1)
                                    {
                                        if (bToA.GetY() == 1)
                                        {
                                            checking = true;
                                            continue;
                                        }
                                    }
                                }
                                if (checking)
                                {
                                    Vector2D diff = snake.body[i] - head;
                                    //if snake is Verticle
                                    if (snake.body[i].GetX() == snake.body[i - 1].GetX())
                                    {


                                        if (snake.body[i].GetY() < snake.body[i - 1].GetY())
                                        {
                                            if (head.GetY() >= snake.body[i].GetY() - snakeCollisionRange && head.GetY() <= snake.body[i - 1].GetY() + snakeCollisionRange)
                                                if (diff.GetX() <= snakeCollisionRange && diff.GetX() >= -snakeCollisionRange)
                                                {
                                                    s.alive = false;
                                                    s.died = true;
                                                    s.score = 0;
                                                    break;
                                                }
                                        }
                                        else if (snake.body[i].GetY() > snake.body[i - 1].GetY())
                                            if (head.GetY() <= snake.body[i].GetY() + snakeCollisionRange && head.GetY() >= snake.body[i - 1].GetY() - snakeCollisionRange)
                                                if (diff.GetX() <= snakeCollisionRange && diff.GetX() >= -snakeCollisionRange)
                                                {
                                                    s.alive = false;
                                                    s.died = true;
                                                    s.score = 0;
                                                    break;
                                                }
                                    }
                                    //if snake is Horizontal
                                    if (snake.body[i].GetY() == snake.body[i - 1].GetY())
                                    {
                                        if (snake.body[i].GetX() < snake.body[i - 1].GetX())
                                        {
                                            if (head.GetX() >= snake.body[i].GetX() - snakeCollisionRange && head.GetX() <= snake.body[i - 1].GetX() + snakeCollisionRange)
                                                if (diff.GetY() <= snakeCollisionRange && diff.GetY() >= -snakeCollisionRange)
                                                {
                                                    s.alive = false;
                                                    s.died = true;
                                                    s.score = 0;
                                                    break;
                                                }
                                        }

                                        else if (snake.body[i].GetX() > snake.body[i - 1].GetX())
                                            if (head.GetX() <= snake.body[i].GetX() + snakeCollisionRange && head.GetX() >= snake.body[i - 1].GetX() - snakeCollisionRange)
                                                if (diff.GetY() <= snakeCollisionRange && diff.GetY() >= -snakeCollisionRange)
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
                    }
                }
            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Server Methods and Functions for Different Game Modes
        /////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// This method updates the snake score and death if a collison occured between two snakes
        /// for the deathmatch game mode
        /// </summary>
        /// <param name="s">The snake that died</param>
        /// <param name="e">The snake that was collieded into</param>
        private void SnakeMatchDeath( Snake s,  Snake e)
        {
            s.alive = false;
            s.died = true;
            e.score ++;
        }

        /// <summary>
        /// This method updates the snake score and death if a collison occured between two snakes
        /// for the normal game mode
        /// </summary>
        /// <param name="s">The snake that died</param>
        /// <param name="e">The snake that was collieded into</param>
        private void SnakeNormDeath(Snake s, Snake e)
        {
            s.alive = false;
            s.died = true;
            s.score = 0;
        }

        /// <summary>
        /// This method activates the powerup effect for deathmatch game mode (which is invincibility)
        /// </summary>
        /// <param name="s">The snake that consumed the PowerUp</param>
        private void PowerUpEffectDM(Snake s)
        {
            //set to Invincible
            s.invincible = true;
            s.invincibleFrames -= powerupLength;
            
        }

        /// <summary>
        /// This method activates the powerup effect for normal game mode (which is grow)
        /// </summary>
        /// <param name="s">The snake that consumed the PowerUp</param>
        private void PowerUpEffectNorm(Snake s)
        {
            //set to grow
            s.growing = true;
            s.growingFrames -= powerupLength;
            //increase score
            s.score++;
        }
        private bool PowerupTimer(Snake s)
        {
            if (deathMatch)
            {
                if (s.invincible && s.invincibleFrames >= powerupLength)
                    s.invincible = false;
                else if(s.invincible) s.invincibleFrames++;
                return s.invincible;

            }
            else if (s.growing && s.growingFrames >= powerupLength)
                s.growing = false;
            else if (s.growing) 
                s.growingFrames++;
            return s.growing;
        }
    

        /////////////////////////////////////////////////////////////////////////////////////////
        // Client Acceptance, Recieving, and Removing code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a new client connects and begins handshake process
        /// </summary>
        /// <param name="state">The SocketState representing the new client</param>
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
        /// when a network action occurs immediatly after a client has been accepted
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveHandshake(SocketState state)
        {
            // Remove the client if they aren't still connected
           
                if (state.ErrorOccurred)
                {
                lock (clients)
                {
                    RemoveClient(state.ID);
                }
                    return;
                
            }
            //recieve name

            string totalData = state.GetData();

            string[] parts = Regex.Split(totalData, @"(?=[\n])");

            //create snake
            Snake newSnake = new Snake((int)state.ID, parts[0]);
            state.RemoveData(0, parts[0].Length + parts[1].Length);
            lock (world)
            {
                world.Snakes.Add(newSnake.snake, newSnake);
            }
            //send world data
            string walls = "";
            foreach(Wall? wall in world.Walls)
            {
               if(wall != null)
                {
                    walls=walls+JsonSerializer.Serialize(wall) + "\n";
                }
            }
            Networking.Send(state.TheSocket, newSnake.snake + "\n" + worldSize + "\n");

            Networking.Send(state.TheSocket, walls);

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
        /// Method to be invoked by the networking library
        /// when a network action occurs, recieves client movement instructions
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessMessage(SocketState state)
        {
            // Remove the client if they aren't still connected
            if (state.ErrorOccurred)
            {
                lock (world)
                {
                    if (world.Snakes.ContainsKey((int)state.ID))
                    {
                        world.Snakes[(int)state.ID].died = true;
                        world.Snakes[(int)state.ID].dc = true;
                        world.Snakes[(int)state.ID].alive = false;
                    }
                    
                }
                lock (clients)

                {
                    RemoveClient(state.ID);
                }
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
                            lock (world)
                            {
                                if (world.Snakes[(int)state.ID].body.Count > 1){
                                    if (p.Contains("up") && world.Snakes[(int)state.ID].dir.GetY() == 0)
                                    {
                                        world.Snakes[(int)state.ID].dir = new Vector2D(0, -1);
                                        world.Snakes[(int)state.ID].dir.Normalize();
                                        world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                                    }
                                    else if (p.Contains("down") && world.Snakes[(int)state.ID].dir.GetY() == 0)
                                    {
                                        world.Snakes[(int)state.ID].dir = new Vector2D(0, 1);
                                        world.Snakes[(int)state.ID].dir.Normalize();
                                        world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                                    }
                                    else if (p.Contains("right") && world.Snakes[(int)state.ID].dir.GetX() == 0)
                                    {
                                        world.Snakes[(int)state.ID].dir = new Vector2D(1, 0);
                                        world.Snakes[(int)state.ID].dir.Normalize();
                                        world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                                    }
                                    else if (p.Contains("left") && world.Snakes[(int)state.ID].dir.GetX() == 0)
                                    {
                                        world.Snakes[(int)state.ID].dir = new Vector2D(-1, 0);
                                        world.Snakes[(int)state.ID].dir.Normalize();
                                        world.Snakes[(int)state.ID].body.Add(world.Snakes[(int)state.ID].body[world.Snakes[(int)state.ID].body.Count - 1]);
                                    }
                                }
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

