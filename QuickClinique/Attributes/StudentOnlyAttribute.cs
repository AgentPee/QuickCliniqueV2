using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuickClinique.Attributes
{
    public class StudentOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var studentId = session.GetInt32("StudentId");

            if (studentId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Student", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}