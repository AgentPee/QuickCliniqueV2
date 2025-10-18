using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ClinicStaffOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("ClinicStaffId") == null)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", new
            {
                message = "This page is only accessible to clinic staff members."
            });
        }

        base.OnActionExecuting(context);
    }
}