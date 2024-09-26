using Microsoft.PowerPlatform.Dataverse.Client;
using DotNETBasic.Models;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.AspNetCore.Http;

namespace DotNETBasic.Services
{
    public class DataverseService
    {
        private readonly ServiceClient _crmServiceClient;
        IHttpContextAccessor _httpContextAccessor;



        public DataverseService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            // Retrieve the connection string from configuration
            var connectionString = configuration.GetSection("Dataverse:ConnectionString").Value;
            _crmServiceClient = new ServiceClient(connectionString);
            _httpContextAccessor = httpContextAccessor;

            if (!_crmServiceClient.IsReady)
            {
                throw new Exception("Failed to connect to Dataverse.");
            }
        }

        public void SetSessionValue(string key, string value)
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            session?.SetString(key, value);

            // Log all session keys and their values
            foreach (var sessionKey in session.Keys)
            {
                Console.WriteLine($"{sessionKey}: {session.GetString(sessionKey)}");
            }
        }

        //public string getUserName() {
        //    var users = Users.
        //    return "";
        //}

        public string GetSessionValue(string key)
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            var val = session?.GetString(key);
            return val;
        }

        public ServiceClient GetServiceClient()
        {
            return _crmServiceClient;
        }

        public List<string> GetRoles(string email)
        {
            var service  = GetServiceClient();
            Guid userID;
            QueryExpression query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("systemuserid") // Only select the user ID
            };
             
            query.Criteria.AddCondition("internalemailaddress", ConditionOperator.Equal, email);

            EntityCollection results = service.RetrieveMultiple(query);
             
            if (results.Entities.Count > 0)
            {
                userID= results.Entities[0].Id;
            }
            else
            { 
                throw new Exception($"User with email {email} not found.");
            }
             
            SetSessionValue("userID" , userID.ToString());


            // Query the systemuserroles entity
            QueryExpression query1 = new QueryExpression("systemuserroles")
            {
                ColumnSet = new ColumnSet("roleid")  
            }; 
            query1.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, userID); 
            EntityCollection results1 = service.RetrieveMultiple(query1);
             
            List<string> userRoles = new List<string>();

            if (results1.Entities.Count > 0)
            {
                foreach (var entity in results1.Entities)
                { 
                    Guid roleId = entity.GetAttributeValue<Guid>("roleid");

                    // Retrieve the role name from the role entity
                    Entity role = service.Retrieve("role", roleId, new ColumnSet("name"));
                    if (role != null && role.Contains("name"))
                    {
                        userRoles.Add(role.GetAttributeValue<string>("name"));
                    }
                }
            }

            var data = new User
            {
                Id = userID,
                email = email,
                roles = userRoles
            };
            return userRoles;
        }
    }
}
