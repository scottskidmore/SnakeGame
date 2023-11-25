using System;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;


using System.Xml.Linq;



using World;

namespace GameController
{
	public class Controller
	{
        //name of player
        private string? name;
        private SocketState? theServer;
        public World.World world;

    
       

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
                if (state.ErrorMessage == null)
                {
                    Error?.Invoke("Error is null");
                    return;
                }
                // inform the view
                Error?.Invoke(state.ErrorMessage);
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
                        if (doc.RootElement.TryGetProperty("wall", out _))
                        {



                            Wall? wall = JsonSerializer.Deserialize<Wall>(s);
                            if (!world.Walls.Contains(wall))
                                world.Walls.Add(wall);
                        }
                        if (doc.RootElement.TryGetProperty("power", out _))
                        {
                            PowerUp? power = JsonSerializer.Deserialize<PowerUp>(s);
                            if (power != null)
                            {
                                if (world.PowerUps.ContainsKey(power.power))
                                    world.PowerUps.Remove(power.power);
                                world.PowerUps.Add(power.power,power);
                            }
                        }


                        if (doc.RootElement.TryGetProperty("snake", out _))
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
            // Continue the event loop
            // state.OnNetworkAction has not been changed, 
            // so this same method (ReceiveMessage) 
            // will be invoked when more data arrives
            Networking.GetData(state);
        }






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
                         if (doc.RootElement.TryGetProperty("power", out _))
                        {
                            PowerUp? power = JsonSerializer.Deserialize<PowerUp>(s);
                            if (power != null)
                            {
                                if (world.PowerUps.ContainsKey(power.power))
                                    world.PowerUps.Remove(power.power);
                                world.PowerUps.Add(power.power, power);
                            }
                        }



                        if (doc.RootElement.TryGetProperty("snake", out _))
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

                    //switch to normal recieve
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
        /// Send a message to the server
        /// </summary>
        /// <param name="message"></param>
        public void MessageEntered(string message)
        {
            if (theServer is not null)
            {
               string s = "{\"moving\":\"" + message + "\"}";
                Networking.Send(theServer.TheSocket, s + "\n");
            }
        }

        public World.World GetWorld()
        {
            return world;
        }

        public void CleanUp()
        {
            lock (world)
            {
                foreach (PowerUp p in world.PowerUps.Values)
                {
                    if (p.died == true)
                        world.PowerUps.Remove(p.power);
                }
                foreach (Snake s in world.Snakes.Values)
                {
                    if (s.died == true && !world.DeadSnakes.ContainsKey(s.snake))
                    {
                        DeadSnake ds = new DeadSnake(s.snake, s.body[s.body.Count - 1]);
                        world.DeadSnakes.Add(s.snake, ds);
                    }

                }
                foreach (DeadSnake ds in world.DeadSnakes.Values)
                {
                    if (world.Snakes.TryGetValue(ds.snake, out Snake? s))
                        if (s.alive == true || s.dc == true)
                            world.DeadSnakes.Remove(s.snake);
                }
            }
        }


    }
}

