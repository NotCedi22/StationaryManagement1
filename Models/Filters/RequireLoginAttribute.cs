using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StationaryManagement1.Models.Filters
{
    /// <summary>
    /// Action filter that requires user to be logged in.
    /// Redirects to Login page if session is empty.
    /// </summary>
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // Check if user is logged in (has EmployeeId in session)
            if (session.GetInt32("EmployeeId") == null)
            {
                // Redirect to login page
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
