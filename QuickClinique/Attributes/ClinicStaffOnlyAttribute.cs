using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuickClinique.Attributes
{
    public class ClinicStaffOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var clinicStaffId = session.GetInt32("ClinicStaffId");

            if (clinicStaffId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Clinicstaff", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}