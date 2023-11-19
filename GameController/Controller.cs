using System;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameController
{
	public class Controller
	{
        //name of player
        private string name;
        private SocketState theServer;
        public World.World world = new();

        //Events for view to subscribe to
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

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
                Error?.Invoke(state.ErrorMessage);
                return;
            }
            theServer = state;
            World.World world = new World.World();
            //send player name
            string message = name + "\n";
            Networking.Send(theServer.TheSocket, message);





            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveData;
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
            ProcessData(state);

            // Continue the event loop
            // state.OnNetworkAction has not been changed, 
            // so this same method (ReceiveMessage) 
            // will be invoked when more data arrives
            Networking.GetData(state);
        }


        private void ProcessData(SocketState state)
        {
            string data=state.GetData();
            string[] list=data.Split( "\n");
            foreach (string s in list)
            {
                if (s.StartsWith("{")){
                    JsonDocument doc = JsonDocument.Parse(s);
                    if (doc.RootElement.TryGetProperty("wall", out _))
                    {
                        World.Wall wall = JsonSerializer.Deserialize<World.Wall>(s);
                        world.Walls.Append(wall);
                    }
                    if (doc.RootElement.TryGetProperty("power", out _))
                    {
                        World.PowerUp power = JsonSerializer.Deserialize<World.PowerUp>(s);
                        world.PowerUps.Add(power);
                    }
                    if (doc.RootElement.TryGetProperty("snake", out _))
                    {
                        World.Snake snake = JsonSerializer.Deserialize<World.Snake>(s);
                        world.Snakes.Add(snake);
                    }
                }
            }
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
                Networking.Send(theServer.TheSocket, message + "\n");
        }

    }
}

