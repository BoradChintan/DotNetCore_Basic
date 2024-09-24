using DotNETBasic.Services;
using Microsoft.AspNetCore.Mvc;
using DotNETBasic.Models;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace DotNETBasic.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataverseService _dataverseService;
        public ChatController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }
        public IActionResult ChatRoom()
        {
            var service = _dataverseService.GetServiceClient();

            QueryExpression queryExpression = new QueryExpression("cb_chatmessage");
            queryExpression.ColumnSet = new ColumnSet(true);    
            var data = service.RetrieveMultiple(queryExpression);

            List<Message> messages = new List<Message>();

            foreach (var item in data.Entities)
            {
                messages.Add(new Message
                {
                    cb_name = item.GetAttributeValue<string>("cb_name") ,
                    cb_chatmessageid = item.GetAttributeValue<Guid>("cb_chatmessageid") ,
                    createdon = item.GetAttributeValue<DateTime>("createdon").AddMinutes(330),
                    ownerid = item.GetAttributeValue<EntityReference>("ownerid").Id,
                    userName = item.GetAttributeValue<EntityReference>("ownerid").Name ,
                });
            }
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
    }
}
