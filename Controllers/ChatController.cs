using DotNETBasic.Services;
using Microsoft.AspNetCore.Mvc;
using DotNETBasic.Models;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace DotNETBasic.Controllers
{
    public class ChatController : Controller
    {
        // Thread-safe collection for storing active WebSocket connections
        private static ConcurrentBag<WebSocket> _webSockets = new ConcurrentBag<WebSocket>();

        private readonly DataverseService _dataverseService;
        private readonly ChatService _chat;
        public ChatController(DataverseService dataverseService, ChatService chat)
        {
            _dataverseService = dataverseService;
            _chat = chat;
        }

        public IActionResult ChatRoom()
        {
           List<Message> messages =  _chat.GetChatMessages();
            return View(messages);
        }

        [HttpPost]
        public IActionResult SendMessage([FromBody] Message data)
        {
            if (data == null || string.IsNullOrEmpty(data.cb_name))
            {
                return BadRequest("Message content is missing.");
            }

            var service = _dataverseService.GetServiceClient();
             
            Entity chatMessage = new Entity("cb_chatmessage");
            chatMessage["cb_name"] = data.cb_name;
            chatMessage["ownerid"] = new EntityReference("systemuser", data.ownerid);
            service.Create(chatMessage);

            return Json(new { success = true, message = "Message sent successfully!" });
        }


        [HttpGet("/chat")]
        public async Task<IActionResult> Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _webSockets.Add(webSocket);
                await HandleWebSocketCommunication(webSocket);
                return new EmptyResult();
            }
            else
            {
                return BadRequest("WebSocket connection expected.");
            }
        }

        private async Task HandleWebSocketCommunication(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    //Store to dataverse 
                    _chat.CreateChatMessage(JsonSerializer.Deserialize<Message>(message));

                    // Broadcast the message to all connected clients
                    await BroadcastMessageToAllClients(message);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally {
                // Remove closed WebSocket
                _webSockets = new ConcurrentBag<WebSocket>(_webSockets.Except(new[] { webSocket }));
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
          
        }

        private async Task BroadcastMessageToAllClients(string message)
        {
            var serverMsg = Encoding.UTF8.GetBytes($"Server: {message}");
            var tasks = new List<Task>();

            // Broadcast message to all connected WebSockets
            foreach (var socket in _webSockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    tasks.Add(socket.SendAsync(new ArraySegment<byte>(serverMsg), WebSocketMessageType.Text, true, CancellationToken.None));
                }
            }

            await Task.WhenAll(tasks); // Send messages to all clients concurrently
        }
    }
}
