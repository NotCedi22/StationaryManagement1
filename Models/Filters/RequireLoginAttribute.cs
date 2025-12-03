using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StationaryManagement.Filters
{
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetInt32("EmployeeId") == null)
            {
                context.Result = new RedirectToRouteResult(new
                {
                    controller = "Account",
                    action = "Login"
                });
            }
        }
    }
}
