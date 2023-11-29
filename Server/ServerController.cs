using System;
using System.Diagnostics.Metrics;

namespace Server
{
	public class ServerController
	{
        private static void Main(string[] args)
        {
            Networking.StartServer(HandleHttpConnection, 80);
            Console.Read();
        }

        public static void HandleHttpConnection(SocketState state)
        {
            state.OnNetworkAction = ServeHttpRequest;
            Networking.GetData(state);
        }

        public static void ServeHttpRequest(SocketState state)
        {
            string request = state.GetData();
            Console.WriteLine(request);
            string response = httpOkHeader;
            if (request.Contains("GET /games"))
            {
                response += "here is your scores";
            }
            else if (request.Contains("GET / "))
            {
                counter += 1;
                response += "<table>";
                response += "<tr><td>Travis</td><td>100</td></tr>";
                response += "<tr><td>Jo</td><td>900000</td></tr>";
                response += "</table>";
                response += "People that have visited this webpage: " + counter;
            }
            else
                response = httpBadHeader;

            Networking.SendAndClose(state.TheSocket, response);
        }
    }
}

