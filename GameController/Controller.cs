using System;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NetworkUtil;

using System.Xml.Linq;



using World;
using System.Diagnostics;

namespace GameController
{
    /// <summary>
    /// MVC Controler this class allows the view and model to work
    /// together and handles all the network calls.
    /// </summary>
	public class Controller
	{
        //name of player
        private string? name;
        private SocketState? theServer;
        public World.World world;

        private string? message;
        public string? Message {  set { message = (string?)value; } }

    
       

        //Events for view to subscribe to
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

        public delegate void GameUpdateHandler();
        public event GameUpdateHandler? NewUpdate;

        public Controller()
        {
            world = new();
            
        }

        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Connect(string addr , string name)
        {
            this.name = name;
            Networking.ConnectToServer(OnConnect, addr, 11000);
        }


        /// <summary>
        /// Method to be invoked by the networking library when a connection is made
        /// (see line 34)
        /// </summary>
        /// <param name="state"></param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Cannot connect to Server");
                return;
            }
            theServer = state;
            World.World world = new World.World();
            //send player name
            string message = name + "\n";
            Networking.Send(theServer.TheSocket, message);
            // Start an event loop to receive messages from the server
            state.OnNetworkAction = FirstRecieve;
            Networking.GetData(state);



        }


        /// <summary>
        /// Method to be invoked by the networking library when 
        /// data is available
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Lost connection to server");
                return;
            }
           
            string data = state.GetData();
            string[] list = Regex.Split(data, @"(?<=[\n])");
            lock (world)
            {
                foreach (string s in list)
                {
                    if (s.StartsWith("{"))
                    {
                        // Ignore empty strings added by the regex splitter
                        if (s.Length == 0)
                            continue;
                        // The regex splitter will include the last string even if it doesn't end with a '\n',
                        // So we need to ignore it if this happens. 
                        if (s[s.Length - 1] != '\n')
                            break;

                        // Then remove it from the SocketState's growable buffer
                        state.RemoveData(0, s.Length);
                        JsonDocument doc = JsonDocument.Parse(s);
                        if (doc.RootElement.TryGetProperty("power", out _))
                        {
                            PowerUp? power = JsonSerializer.Deserialize<PowerUp>(s);
                            if (power != null)
                            {
                                if (power.died == true)
                                    world.PowerUps.Remove(power.power);

                                else if (world.PowerUps.ContainsKey(power.power))
                                {
                                    world.PowerUps.Remove(power.power);
                                    world.PowerUps.Add(power.power, power);

                                }
                                else world.PowerUps.Add(power.power, power);

                            }
                        }
                        else if (doc.RootElement.TryGetProperty("snake", out _))
                        {
                            Snake? snake = JsonSerializer.Deserialize<Snake>(s);
                            if (snake != null)
                            {
                               
                                if (world.Snakes.ContainsKey(snake.snake))
                                    world.Snakes.Remove(snake.snake);
                                world.Snakes.Add(snake.snake, snake);
                                if (snake.died == true && !world.DeadSnakes.ContainsKey(snake.snake))
                                {
                                    DeadSnake ds = new DeadSnake(snake.snake, snake.body[snake.body.Count - 1]);
                                    world.DeadSnakes.Add(snake.snake, ds);
                                }
                            }

                        }
                        else if (doc.RootElement.TryGetProperty("wall", out _))
                        {

                            Wall? wall = JsonSerializer.Deserialize<Wall>(s);
                            if (!world.Walls.Contains(wall))
                                world.Walls.Add(wall);
                        }

                        
                    }
                }
                //check each deadsnake to see if the snake is now alive or if snake DCed
                foreach (DeadSnake ds in world.DeadSnakes.Values)
                {

                    if (world.Snakes.TryGetValue(ds.snake, out Snake? s))
                    {
                        //if it is remove it
                        if (s.alive == true || s.dc == true)
                            world.DeadSnakes.Remove(s.snake);
                       // if not add a frame to the framecount
                        else ds.framesDead += 1;

                    }

                }
            }
            //tell view to update world
            NewUpdate?.Invoke();
            //if message exists send it
            if (message != null)
            {
                MessageEntered();
                
            }
            Networking.GetData(state);
        }





        /// <summary>
        /// Handles the first recieve that includes the player ID
        /// and the world size.
        /// </summary>
        /// <param name="state">The current socket state</param>
        private void FirstRecieve(SocketState state)
        {

            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Lost connection to server");
                return;
            }

            string data = state.GetData();
            string[] list = Regex.Split(data, @"(?<=[\n])");
            Int32.TryParse(list[0], out int x);
            world.PlayerID = x;
            Int32.TryParse(list[1], out int y);
            world.WorldSize = y;
            lock (world)
            {
                foreach (string s in list)
                {
                    if (s.StartsWith("{"))
                    {
                        // Ignore empty strings added by the regex splitter
                        if (s.Length == 0)
                            continue;
                        // The regex splitter will include the last string even if it doesn't end with a '\n',
                        // So we need to ignore it if this happens. 
                        if (s[s.Length - 1] != '\n')
                            break;

                        // Then remove it from the SocketState's growable buffer
                        state.RemoveData(0, s.Length);
                        JsonDocument doc = JsonDocument.Parse(s);
                        if (doc.RootElement.TryGetProperty("wall", out _))
                        {

                            Wall? wall = JsonSerializer.Deserialize<Wall>(s);
                            if (!world.Walls.Contains(wall))
                                world.Walls.Add(wall);
                        }
                         else if (doc.RootElement.TryGetProperty("power", out _))
                        {
                            PowerUp? power = JsonSerializer.Deserialize<PowerUp>(s);
                            if (power != null)
                            {
                                if (world.PowerUps.ContainsKey(power.power))
                                    world.PowerUps.Remove(power.power);
                                world.PowerUps.Add(power.power, power);
                            }
                        }



                        else if (doc.RootElement.TryGetProperty("snake", out _))
                        {


                            Snake? snake = JsonSerializer.Deserialize<Snake>(s);
                            if (snake != null)
                            {
                                if (world.Snakes.ContainsKey(snake.snake))
                                    world.Snakes.Remove(snake.snake);
                                world.Snakes.Add(snake.snake, snake);
                            }


                        }
                    }
                }
            }
                    //tell view to update world
                    NewUpdate?.Invoke();

                    //switch to normal receive
                    state.OnNetworkAction = ReceiveData;
                    Networking.GetData(state);

                
            

        }
        /// <summary>
        /// Closes the connection with the server
        /// </summary>
        public void Close()
        {
            theServer?.TheSocket.Close();
        }

        /// <summary>
        /// Send a moving/direction change message to the server 
        /// </summary>
        /// <param name="message"></param>
        private void MessageEntered()
        {
            if (theServer is not null)
            {
                //create a json string for the moving message
                string s = "{\"moving\":\"" + message + "\"}";
                Networking.Send(theServer.TheSocket, s + "\n");
                message = null;
            }
           
        }

        public World.World GetWorld()
        {
            return world;
        }

    }
}

