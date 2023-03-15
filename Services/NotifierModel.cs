using Newtonsoft.Json;
using System.Text;

namespace notifier.Services
{
    public class BirthdayTemplateModel
    {
        public string FullName { get; set; }
    }
    public class RenewalTemplateModel : BirthdayTemplateModel
    {
        public string due_date { get; set; }
    }

    public class SendSmsBody
    {
        public string PhoneNumber { get; set; }
        public string Body { get; set; }
    }
    public class LogServiceBody
    {
        public string msisdn { get; set; }
        public string message_body { get; set; }
        public string message_type { get; set; }
    }

    public class DeviceExtension
    {
        public string msisdn { get; set; }
        public string imei { get; set; }
        public int extensionHours { get; set; }
    }

    public class ListGetCustomerForNotification
    {
        public List<GetCustomerForNotification> data { get; set; }

    }
    public class GetCustomerForNotification
    {
        [JsonProperty("msisdn")]
        public string Msisdn { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("imei")]
        public string Imei { get; set; }

        [JsonProperty("dob")]
        public string Dob { get; set; }

        [JsonProperty("dueDate")]
        public DateTimeOffset DueDate { get; set; }

        [JsonProperty("phoneModel")]
        public string PhoneModel { get; set; }
    }


}
