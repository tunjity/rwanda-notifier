using Microsoft.VisualBasic;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using notifier.Interface;
using notifier.Model;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Helpers;
using System.Web.WebPages;

namespace notifier.Services
{
    public class SendNotificationService : ISendNotificationService
    {
        private readonly IConfiguration _config;
        private static string _notificationLogPath = string.Empty;
        private static readonly string _serverPath = ServerRootPath.RootPath();

        public SendNotificationService(IConfiguration config)
        {
            _config = config;
            _notificationLogPath = ConfigHelpers.AppSetting("logservice", "log_path");
        }


        /// <summary>
        /// This is for Sending out Renewal Due Date Reminder:
        /// </summary>
        /// <param name="due_date"></param>
        /// <returns></returns>
        public async Task<MessageOut> SendReminder(DateTime? due_date)
        {
            var smsBodies = new List<SendSmsBody>();
            var logBodies = new List<LogServiceBody>();

            try
            {
                bool isSet = Convert.ToBoolean(_config["mode:reminder"]);
                string sample_notifications = _config["mode:sample_renewal_notifications"];
                string sample_notifications_log = _config["mode:sample_renewal_notifications_log"];
                var queryDate = due_date == default ? DateTime.Now : due_date.GetValueOrDefault();
                var sameday = queryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var next7days = queryDate.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var next3days = queryDate.AddDays(3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (isSet)
                {
                    var sameDayCustomers = new List<GetCustomerForNotification>();
                    sameDayCustomers = await GetCustomerForNotifications(sameday, "due_date");
                    var next7DayCustomers = new List<GetCustomerForNotification>();
                    next7DayCustomers = await GetCustomerForNotifications(next7days, "due_date");
                    var next3DayCustomers = new List<GetCustomerForNotification>();
                    next3DayCustomers = await GetCustomerForNotifications(next3days, "due_date");

                    if (!next7DayCustomers.Any() && !next3DayCustomers.Any() && !sameDayCustomers.Any())
                        return new MessageOut { status = false, message = "No One Falls In This Category" };

                    string smsRenewalTemplate = _config["smsurl:smsRenewalTemplate"];
                    smsBodies.AddRange(WrapRenewalSmsBody(sameDayCustomers, smsRenewalTemplate, "24hrs").Item1);
                    smsBodies.AddRange(WrapRenewalSmsBody(next3DayCustomers, smsRenewalTemplate, "3days").Item1);
                    smsBodies.AddRange(WrapRenewalSmsBody(next7DayCustomers, smsRenewalTemplate, "7days").Item1);
                    //log the sent sms
                    logBodies.AddRange(WrapRenewalSmsBody(sameDayCustomers, smsRenewalTemplate, "24hrs").Item2);
                    logBodies.AddRange(WrapRenewalSmsBody(next3DayCustomers, smsRenewalTemplate, "3days").Item2);
                    logBodies.AddRange(WrapRenewalSmsBody(next7DayCustomers, smsRenewalTemplate, "7days").Item2);

                    _ = await SendBulkSms(smsBodies);
                    _ = await LogSentSms(logBodies);

                }
                else
                {
                    var samples = JsonConvert.DeserializeObject<List<SendSmsBody>>(sample_notifications);
                    var logSamples = JsonConvert.DeserializeObject<List<LogServiceBody>>(sample_notifications_log);
                    if (samples != null)
                    {
                        _ = await SendBulkSms(samples);
                        _ = await LogSentSms(logSamples == null ? new List<LogServiceBody>() : logSamples, "renewal");
                    }
                }
                return new MessageOut { status = true, message = "success" };

            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return new MessageOut
                {
                    status = false,
                    message = "Internal error occurred! Please try again later."
                };
            }
        }


        // Done:
        public async Task<MessageOut> SendBirthdaySms(DateTime? dob)
        {
            var smsBodies = new List<SendSmsBody>();
            var logBodies = new List<LogServiceBody>();
            try
            {
                var queryDate = dob == default ? DateTime.Now : dob.GetValueOrDefault();
                string dobStr = queryDate.ToString("mm-dd");
                bool isSet = Convert.ToBoolean(_config["mode:birthday"]);
                string sample_notifications = _config["mode:sample_birthday_notifications"];
                string sample_notifications_log = _config["mode:sample_birthday_notifications_log"];

                if (isSet)
                {
                    var listCus = await GetCustomerForNotifications(dob.GetValueOrDefault().ToString("dd-MM"), "dob");
                    if (listCus.Count != 0)
                    {
                        string smsTemplate = _config["smsurl:smsBirthdayTemplate"];
                        smsBodies.AddRange(WrapBirthdaySmsBody(listCus, smsTemplate).Item1);
                        logBodies.AddRange(WrapBirthdaySmsBody(listCus, smsTemplate).Item2);

                        //send messages in bulk to all customer in that date range
                        _ = await SendBulkSms(smsBodies);
                        _ = await LogSentSms(logBodies);
                    }
                }
                else
                {
                    var samples = JsonConvert.DeserializeObject<List<SendSmsBody>>(sample_notifications);
                    var logSamples = JsonConvert.DeserializeObject<List<LogServiceBody>>(sample_notifications_log);
                    if (samples != null)
                    {
                        _ = await SendBulkSms(samples);
                        _ = await LogSentSms(logSamples == null ? new List<LogServiceBody>() : logSamples, "birthday");
                    }
                }
                return new MessageOut { status = true, message = "success" };
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return new MessageOut
                {
                    status = false,
                    message = "Internal error occurred! Please try again later."
                };
            }
        }

        // Done:
        public async Task<MessageOut> SendSmsToNewCustomers(DateTime? onboard_date)
        {
            var smsBodies = new List<SendSmsBody>();
            var samples = new List<SendSmsBody>();
            var logBodies = new List<LogServiceBody>();
            try
            {
                var onboarded_date = onboard_date == default ?
                    DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) :
                    onboard_date.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                bool isSet = Convert.ToBoolean(_config["mode:onboarded"]);
                string sample_notifications_1 = _config["mode:sample_onboarded_notifications1"];
                string sample_notifications_2 = _config["mode:sample_onboarded_notifications2"];
                string sample_notifications_2_log = _config["mode:sample_onboarded_notifications2_log"];

                if (isSet)
                {
                    var listCus = await GetCustomerForNotifications(onboarded_date, "new");
                    if (listCus.Any())
                    {
                        string smsTemplate1 = _config["smsurl:smsNewCustomerTemplate1"];
                        string smsTemplate2 = _config["smsurl:smsNewCustomerTemplate2"];
                        smsBodies.AddRange(WrapNewOnboardedSmsBody(listCus, smsTemplate1).Item1);
                        smsBodies.AddRange(WrapNewOnboardedSmsBody(listCus, smsTemplate2).Item1);
                        logBodies.AddRange(WrapNewOnboardedSmsBody(listCus, smsTemplate1).Item2);
                        logBodies.AddRange(WrapNewOnboardedSmsBody(listCus, smsTemplate2).Item2);

                        _ = await SendBulkSms(smsBodies);
                        _ = await LogSentSms(logBodies);
                    }
                }
                else
                {
                    samples.AddRange(JsonConvert.DeserializeObject<List<SendSmsBody>>(sample_notifications_1) ?? new List<SendSmsBody>());
                    samples.AddRange(JsonConvert.DeserializeObject<List<SendSmsBody>>(sample_notifications_2) ?? new List<SendSmsBody>());
                    logBodies.AddRange(JsonConvert.DeserializeObject<List<LogServiceBody>>(sample_notifications_2_log) ?? new List<LogServiceBody>());
                    if (samples != null)
                    {
                        _ = await SendBulkSms(samples);
                        _ = await LogSentSms(logBodies, "new_onboarded");
                    }
                }
                return new MessageOut { status = true, message = "success" };
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return new MessageOut
                {
                    status = false,
                    message = "Internal error occurred! Please try again later."
                };
            }
        }

        public async Task<MessageOut> FetchRecordsFromFile(DateTime? due_date, string notificationType)
        {
            IDictionary<string, string> response = new Dictionary<string, string>();
            var endDate = DateTime.Now;
            var listOfDays = GetDatesBetween(due_date.GetValueOrDefault(), endDate);

            foreach (var day in listOfDays)
            {
                IDictionary<string, string> ret = null;
                string logPath = $"{Path.Combine(_serverPath, _notificationLogPath)}{day}/{notificationType}";
                ret = Logger.ReadFromFile(logPath);
                foreach (var item in ret)
                {
                    if (item.Value != "Error: This Path Is Invalid")
                        response.Add(item);
                }
            }

            return new MessageOut { data = response, status = true, message = "success" };

        }
        public async Task<MessageOut> AionExtension(DateTime? due_date)
        {
            var renewDevices = new List<DeviceExtension>();
            var samples = new List<DeviceExtension>();
            try
            {
                var duedate = due_date == default ?
                    DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) :
                    due_date.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                //if (due_date == default)
                //    return new MessageOut
                //    {
                //        status = false,
                //        message = "No specific due date for this operation."
                //    };

                bool isSet = Convert.ToBoolean(_config["mode:extension"]);
                string sample_extensions = _config["mode:sample_device_extensions"];

                if (isSet)
                {
                    var listCus = await GetCustomerForNotifications(duedate, "due_date");
                    if (listCus.Any())
                    {
                        foreach (var c in listCus)
                        {
                            renewDevices.Add(new DeviceExtension
                            {
                                msisdn = c.Msisdn,
                                imei = c.Imei,
                                extensionHours = 72
                            });
                        }
                        // Call bulk Device extensions
                        _ = await SendBulkDeviceExtension(renewDevices);

                    }
                }
                else
                {
                    samples.AddRange(JsonConvert.DeserializeObject<List<DeviceExtension>>(sample_extensions) ?? new List<DeviceExtension>());
                    if (samples != null)
                    {
                        _ = await SendBulkDeviceExtension(samples);
                    }
                }
                return new MessageOut { status = true, message = "success" };

            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return new MessageOut
                {
                    status = false,
                    message = "Internal error occurred! Please try again later."
                };
            }
        }


        #region Helpers:

        private async Task<string> SendBulkSms(List<SendSmsBody> smsBodies)
        {
            var headers = new Dictionary<string, string>();
            try
            {
                //var apiUrl = ConfigHelpers.AppSetting("smsurl", "sendSmsurl");
                //var header = ConfigHelpers.AppSetting("smsurl", "sms_headers");

                //if (smsBodies != null)
                //{
                //    if (!string.IsNullOrWhiteSpace(header))
                //    {
                //        headers = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //            .Select(part => part.Split(':'))
                //            .ToDictionary(split => split[0], split => split[1]);
                //    }

                //    var responseString = await ApiHelper.MakeRequest(new ApiLinkDTO
                //    {
                //        final_url = apiUrl,
                //        headers = JsonConvert.SerializeObject(headers),
                //        method = "POST",
                //        body = JsonConvert.SerializeObject(smsBodies)
                //    });
                //    var respObj = JsonConvert.DeserializeObject<object>(responseString);
                //    return "Done";
                //}
                return "Failed due to empty Log payload.";
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return "Internal error occurred! Please try again later.";
            }

        }
        public List<string> GetDatesBetween(DateTime startDate, DateTime endDate)
        {
            List<string> allDates = new List<string>();
            startDate = startDate.Date; endDate = endDate.Date;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                allDates.Add(dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture).Replace("-", ""));
            }
            return allDates;
        }
        private async Task<string> LogSentSms(List<LogServiceBody> logBodies, string notificationType = "")
        {
            string caValue = "";
            try
            {
                if (logBodies != null)
                {
                    bool service_mode = Convert.ToBoolean(ConfigHelpers.AppSetting("logservice", "service_mode"));
                    if (service_mode)
                    {
                        bool isStage = Convert.ToBoolean(_config["mode:prod"]);
                        string apiUrlsendsms = _config["smsurl:log_service_url"];
                        string caKey = _config["smsurl:logServiceKey"];
                        if (isStage)
                            caValue = _config["smsurl:logServiceValueProd"];
                        else
                            caValue = _config["smsurl:logServiceValue"];


                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri(apiUrlsendsms);
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Add(caKey, caValue);
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            string request = JsonConvert.SerializeObject(logBodies);
                            var content = new StringContent(request, Encoding.UTF8, "application/json");
                            var res = await client.PostAsync(apiUrlsendsms, content);
                            var resString = await res.Content.ReadAsStringAsync();
                            var respObj = JsonConvert.DeserializeObject<object>(resString);
                        }
                    }
                    else // Log to File:
                    {
                        string logFileName = DateTime.Now.Ticks.ToString();
                        string logPath = $"{Path.Combine(_serverPath, _notificationLogPath)}{DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}/";
                        logPath = logPath + (string.IsNullOrWhiteSpace(notificationType) ? "" : $"{notificationType}/");

                        string request = JsonConvert.SerializeObject(logBodies);
                        Logger.WriteToFile(request, logPath, logFileName);
                    }

                    return "Done";
                }
                return "Failed due to empty Log payload.";

            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return "Failed";
            }
        }

        private async Task<string> SendBulkDeviceExtension(List<DeviceExtension> deviceExtensions)
        {
            var headers = new Dictionary<string, string>();
            try
            {
                var apiUrl = ConfigHelpers.AppSetting("smsurl", "bulk_aion_extension_url");
                var header = ConfigHelpers.AppSetting("smsurl", "sms_headers");
                if (deviceExtensions != null)
                {
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        headers = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(part => part.Split(':'))
                            .ToDictionary(split => split[0], split => split[1]);
                    }

                    #region Log Request:

                    string logFileName = DateTime.Now.Ticks.ToString();
                    string logPath = $"{Path.Combine(_serverPath, _notificationLogPath)}{DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}/";
                    logPath = logPath + ("device_extensions/");

                    string request = JsonConvert.SerializeObject(deviceExtensions);
                    Logger.WriteToFile(request, logPath, logFileName);

                    #endregion

                    var responseString = await ApiHelper.MakeRequest(new ApiLinkDTO
                    {
                        final_url = apiUrl,
                        headers = JsonConvert.SerializeObject(headers),
                        method = "POST",
                        body = JsonConvert.SerializeObject(deviceExtensions)
                    });
                    var respObj = JsonConvert.DeserializeObject<object>(responseString);

                    return "Done";
                }

                return "Failed due to empty Log payload.";
            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return "Internal error occurred! Please try again later.";
            }
        }

        private async Task<List<GetCustomerForNotification>> GetCustomerForNotifications(string ddate, string source)
        {
            var lstCus = new List<GetCustomerForNotification>();
            try
            {
                string mnth="", day="";
                day = ddate.Split("-").First();
                mnth = ddate.Split("-").Last();
                string MyConnection2 = ConfigHelpers.AppSetting("ConnectionStrings", "DefaultConnection");
                //Display query
                string Query = $"SELECT * FROM finance_one.customers WHERE MONTH(dob) = {mnth} AND DAY(dob) = {day}";
          //      string Query = $"select c.customer_id CustomerId,c.msisdn Msisdn,concat(c.name,\" \",c.surname) as CustomerName,c.gender Gender,c.dob DOB from customers c inner join customerloan cl on c.customer_id = cl.customer_id where c.customer_type = 'CUSTOMER' and TRIM(cl.bank_application_status) = 'APPROVED' and nullif(c.dob,'') is not null and DATE_FORMAT(c.dob,\"%m-%d\") = DATE_FORMAT({ddate},\"%m-%d\")";
                MySqlConnection MyConn2 = new MySqlConnection(MyConnection2);
                MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                //  MyConn2.Open();
                //For offline connection we weill use  MySqlDataAdapter class.
                MySqlDataAdapter MyAdapter = new MySqlDataAdapter();
                MyAdapter.SelectCommand = MyCommand2;
                DataTable dTable = new DataTable();
                MyAdapter.Fill(dTable);
                var headers = new Dictionary<string, string>();
                string apiUrl = "";
                string responseString = "";
                var json = JsonConvert.SerializeObject(dTable);
                lstCus = JsonConvert.DeserializeObject<List<GetCustomerForNotification>>(json);

                return lstCus;

            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return lstCus;
            }
        }



        private async Task<List<GetCustomerForNotification>> GetCustomerForNotification(string ddate, string source)
        {
            var headers = new Dictionary<string, string>();
            string apiUrl = "";
            string responseString = "";
            var lstCus = new List<GetCustomerForNotification>();
            try
            {
                var header = ConfigHelpers.AppSetting("smsurl", "sms_headers");
                switch (source)
                {
                    case "due_date":
                        apiUrl = ConfigHelpers.AppSetting("smsurl", "getcustomerurl");
                        apiUrl = apiUrl.Replace("[[due_date]]", ddate);
                        break;
                    case "dob":
                        apiUrl = ConfigHelpers.AppSetting("smsurl", "getcustomerbydoburl");
                        apiUrl = apiUrl.Replace("[[dob]]", ddate);
                        break;
                    case "new":
                        apiUrl = _config["smsurl:getnewcustomerurl"];
                        break;
                }

                if (!string.IsNullOrWhiteSpace(header))
                {
                    headers = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(part => part.Split(':'))
                        .ToDictionary(split => split[0], split => split[1]);
                }

                responseString = await ApiHelper.ProxyServiceHelper(new ApiLinkDTO
                {
                    final_url = apiUrl,
                    headers = JsonConvert.SerializeObject(headers),
                    method = "GET", // 'GET' or 'POST'
                    body = "" // if the method is POST, then you serialize the request object here
                });
                lstCus = JsonConvert.DeserializeObject<ListGetCustomerForNotification>(responseString)?.data;
                return lstCus;

            }
            catch (Exception ex)
            {
                Logger.SendErrorToText(ex);
                return lstCus;
            }
        }

        private static Tuple<List<SendSmsBody>, List<LogServiceBody>> WrapRenewalSmsBody(List<GetCustomerForNotification> lst, string msg, string duratation)
        {
            var lsb = new List<LogServiceBody>();
            var smsBodies = new List<SendSmsBody>();
            if (lst.Any() && !string.IsNullOrWhiteSpace(msg))
            {
                foreach (var l in lst)
                {
                    string msgBody = msg;
                    msgBody = msgBody.Replace("[[FIRSTNAME]]", l.Name.StartUntilOrEmpty(" ").Trim()).Replace("[[DURATION]]", duratation);
                    smsBodies.Add(new SendSmsBody
                    {
                        PhoneNumber = l.Msisdn,
                        Body = msgBody
                    });
                    lsb.Add(new LogServiceBody
                    {
                        message_body = msgBody,
                        msisdn = l.Msisdn,
                        message_type = "RenewalSms"
                    });
                }
            }
            return new Tuple<List<SendSmsBody>, List<LogServiceBody>>(smsBodies, lsb);

        }

        private static Tuple<List<SendSmsBody>, List<LogServiceBody>> WrapNewOnboardedSmsBody(List<GetCustomerForNotification> lst, string msg)
        {
            var lsb = new List<LogServiceBody>();
            var smsBodies = new List<SendSmsBody>();
            if (lst.Any() && !string.IsNullOrWhiteSpace(msg))
            {
                foreach (var l in lst)
                {
                    string msgBody = msg;
                    msgBody = msgBody.Replace("[[FIRSTNAME]]", l.Name.StartUntilOrEmpty(" ").Trim());
                    smsBodies.Add(new SendSmsBody
                    {
                        PhoneNumber = l.Msisdn,
                        Body = msgBody
                    });
                    lsb.Add(new LogServiceBody
                    {
                        message_body = msgBody,
                        msisdn = l.Msisdn,
                        message_type = "NewOnboarded"
                    });
                }
            }
            return new Tuple<List<SendSmsBody>, List<LogServiceBody>>(smsBodies, lsb);
        }

        private static Tuple<List<SendSmsBody>, List<LogServiceBody>> WrapBirthdaySmsBody(List<GetCustomerForNotification> lst, string msg)
        {
            var lsb = new List<LogServiceBody>();
            var smsBodies = new List<SendSmsBody>();
            if (lst.Any() && !string.IsNullOrWhiteSpace(msg))
            {
                foreach (var l in lst)
                {
                    string msgBody = msg;
                    msgBody = msgBody.Replace("[[FIRSTNAME]]", l.Name.StartUntilOrEmpty(" ").Trim());
                    smsBodies.Add(new SendSmsBody
                    {
                        PhoneNumber = l.Msisdn,
                        Body = msgBody
                    });
                    lsb.Add(new LogServiceBody
                    {
                        message_body = msgBody,
                        msisdn = l.Msisdn,
                        message_type = "Birthday"
                    });
                }
            }
            return new Tuple<List<SendSmsBody>, List<LogServiceBody>>(smsBodies, lsb);

        }

        #endregion


    }
}
