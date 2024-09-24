namespace DotNETBasic.Models
{
    public class Message
    {  
        public Guid cb_chatmessageid { get; set; }
        public string cb_name { get; set; }
        public string userName { get; set; }
        public Guid ownerid { get; set; }
        public DateTime createdon { get; set; }
    }
}
