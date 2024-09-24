using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace DotNETBasic.Services
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {

        private readonly string[] _roles; 
        public CustomAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Retrieve the user's roles from session
            var sessionRolesString = context.HttpContext.Session.GetString("UserRoles");

            if (string.IsNullOrEmpty(sessionRolesString))
            {
                // If no roles are stored in session, return unauthorized
                context.Result = new UnauthorizedResult();
                return;
            }

            // Deserialize the roles
            var userRoles = JsonConvert.DeserializeObject<List<string>>(sessionRolesString);

            // Check if the user has any of the required roles
            if (!_roles.Any(role => userRoles.Contains(role)))
            {
                // Redirect to the AccessDenied action instead of returning ForbidResult
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
            }
        }
    }
}
