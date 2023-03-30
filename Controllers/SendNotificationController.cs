using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using notifier.Interface;
using notifier.Model;
using notifier.Services;
using System.Text;

namespace notifier.Controllers
{



    //Features
    //A1 -> Send Bulk SMS(use sms api)
    //A2 -> Send Birthday SMS(use sms api)
    //B1 -> Send Renewal Notification SMS(7days, 3days, D-Day) (use sms api)
    //B2 -> Extend by 72hrs on 24hrs to D-Day(call aion extend api with customer imei and 72hrs)
    //(e.g. if customer is due on 10th-Jan then by 9th Jan we extend the person by 3days)

    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SendNotificationController : ControllerBase
    {
        private readonly ISendNotificationService _notiService;
        public SendNotificationController(ISendNotificationService notiService, IConfiguration config)
        {
            _notiService = notiService;
        }


        [HttpGet("notify-customers-renewal")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        public async Task<ActionResult> SendRenewalSmsToCustomers([FromQuery] DateTime? due_date)
        {
            var resp = await _notiService.SendReminder(due_date);
            return Ok(resp);
        }

        [HttpGet("extend-customers-device")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        public async Task<ActionResult> ExtendCustomersRenewalDays([FromQuery] DateTime? due_date)
        {
            var resp = await _notiService.AionExtension(due_date);
            return Ok(resp);
        }


        [HttpGet("send-customers-birthday-notifications")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        public async Task<ActionResult> SendBirthdayWishToCustomers([FromQuery] DateTime? dob)
        {
            var resp = await _notiService.SendBirthdaySms(dob);
            return Ok(resp);
        }


        [HttpGet("notify-newly-onboarded-customers")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        public async Task<ActionResult> SendMessageToNewCustomers([FromQuery] DateTime? due_date)
        {
            var resp = await _notiService.SendSmsToNewCustomers(due_date);
            return Ok(resp);
        }


    }
}
