using DotNETBasic.Models;
using Microsoft.Extensions.Azure;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Net.WebSockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using models = DotNETBasic.Models;

namespace DotNETBasic.Services
{
    public class ChatService
    {
        private readonly DataverseService _dataverseService;
        private readonly ChatService _chatService;
        private readonly List<WebSocket> _webSockets = new List<WebSocket>();
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatService(DataverseService dataverseService, IHttpContextAccessor httpContextAccessor)
        {
            _dataverseService = dataverseService;   
            _httpContextAccessor = httpContextAccessor;
        }


        public Guid CreateChatMessage(models.Message data)
        {
            ServiceClient service = _dataverseService.GetServiceClient();
            Entity entity = new Entity("cb_chatmessage");
            entity["cb_name"] = data.cb_name; 
            //entity["createdon"] = data.createdon;
            entity["ownerid"] = new EntityReference("systemuser", data.ownerid);

            try
            {
                var id = service.Create(entity);
                return id;
            }
            catch(Exception e) { 
            return Guid.Empty;
            }
           
        }

        public string getUserClaims()
        {
            ClaimsPrincipal d = _httpContextAccessor.HttpContext?.User;
            var name = "Guest";
            if (d != null)
            {
             name = d.Claims?.FirstOrDefault(c => c.Type == "name")?.Value;
            }
            return name;
        }

        public List<models.Message> GetChatMessages()
        {
            ServiceClient service = _dataverseService.GetServiceClient();

            QueryExpression queryExpression = new QueryExpression("cb_chatmessage")
            {
                ColumnSet = new ColumnSet(true),
                Orders = { new OrderExpression("createdon", OrderType.Ascending )}
            };

           var chatData = service.RetrieveMultiple(queryExpression);

           var chatMessages = new List<models.Message>();   
            foreach (var chat in chatData.Entities)
            {
                chatMessages.Add(new models.Message
                {
                    userName = chat.GetAttributeValue<EntityReference>("ownerid").Name,
                    cb_name = chat.GetAttributeValue<string>("cb_name"),
                    createdon = chat.GetAttributeValue<DateTime>("createdon"),
                    cb_chatmessageid = chat.GetAttributeValue<Guid>("cb_chatmessageid"),
                    ownerid = chat.GetAttributeValue<EntityReference>("ownerid").Id
                }); 
            }
            return chatMessages;
        }



        public async Task HandleWebSocketAsync(WebSocket webSocket)
        { 
            _webSockets.Add(webSocket);

            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(message))
                    {
                        Console.WriteLine("Parsed JSON:");
                        Console.WriteLine(doc.RootElement.ToString());
                    } 
                    var data = JsonSerializer.Deserialize<models.Message>(message);
                    if (data != null)
                    {
                        Console.WriteLine(data);
                       var id =  CreateChatMessage(data);

                        if (id != Guid.Empty)
                        {
                            data.userName = getUserClaims();
                        }
                        await BroadcastMessageAsync(data);
                    }
                    else
                    {
                        Console.WriteLine("Invalid message format.");
                    }

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                }
                catch (Exception e)
                {

                    throw e;
                }


               
            }

            // Remove the WebSocket when it closes
            _webSockets.Remove(webSocket);
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        private async Task BroadcastMessageAsync(models.Message message)
        {
            var serverMsg = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            foreach (var socket in _webSockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(serverMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
