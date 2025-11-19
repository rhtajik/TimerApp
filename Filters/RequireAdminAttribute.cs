using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TimerApp.Filters;

public class RequireAdminAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isAdmin = context.HttpContext.User.FindFirst("IsAdmin")?.Value == "True";
        var isMainAdmin = context.HttpContext.User.FindFirst("IsMainAdmin")?.Value == "True";

        if (!isAdmin && !isMainAdmin)
        {
            context.Result = new ForbidResult();
        }
    }
}