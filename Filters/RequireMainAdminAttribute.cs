using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TimerApp.Filters;

public class RequireMainAdminAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isMainAdmin = context.HttpContext.User.FindFirst("IsMainAdmin")?.Value == "True";

        if (!isMainAdmin)
        {
            context.Result = new ForbidResult();
        }
    }
}