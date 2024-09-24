namespace DotNETBasic.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string email { get; set; }    

        public List<string> roles { get; set; } 
    }
}
