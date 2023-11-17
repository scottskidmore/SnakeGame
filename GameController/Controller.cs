using System;


namespace GameController
{
	public class Controller
	{
        //name of player
        private string name;
        private SocketState theServer;

        public Controller()
		{
		}
        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnConnectClicked(string serverAddress, string name)
        {
            if (serverAddress == "")
            {
                
                return;
            }
            this.name = name;

            Networking.ConnectToServer(OnConnect, serverAddress, 11000);
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
                // TODO: Left as an exercise, allow the user to try to reconnect
              //  Dispatcher.Dispatch(() => DisplayAlert("Error", "Error connecting to server. Please restart the client.", "OK"));
                return;
            }
            theServer = state;
            //send player name
            string message = name + "\n";
            Networking.Send(theServer.TheSocket, message);

           



            // Start an event loop to receive messages from the server
           // state.OnNetworkAction = ReceiveMessage;
            Networking.GetData(state);
        }


        /// <summary>
        /// This is the event handler when the enter key is pressed in the messageToSend box
        /// </summary>
        /// <param name="sender">The Form control that fired the event</param>
        /// <param name="e">The key event arguments</param>
        private void OnMessageEnter(object sender)
        {
            // Append a newline, since that is our protocols terminating character for a message.
          //  string message = messageToSendBox.Text + "\n";
            // Reset the textbox
          //  messageToSendBox.Text = "";
            // Send the message to the server
           // Networking.Send(theServer.TheSocket, message);
        }


    }
}

