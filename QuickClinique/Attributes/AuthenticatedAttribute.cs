using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AuthenticatedAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var isStaff = context.HttpContext.Session.GetInt32("ClinicStaffId") != null;
        var isStudent = context.HttpContext.Session.GetInt32("StudentId") != null;

        if (!isStaff && !isStudent)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", new
            {
                message = "Please log in to access this page."
            });
        }

        base.OnActionExecuting(context);
    }
}