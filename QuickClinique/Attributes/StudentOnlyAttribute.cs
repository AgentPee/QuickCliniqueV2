using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class StudentOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("StudentId") == null)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", new
            {
                message = "This page is only accessible to students."
            });
        }

        base.OnActionExecuting(context);
    }
}