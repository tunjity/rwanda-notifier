using Microsoft.VisualBasic;
using notifier.Services;

namespace notifier.Interface
{
    public interface ISendNotificationService
    {
        //Task<MessageOut> SendReminder(string due_date, string det);


        Task<MessageOut> SendReminder(DateTime? due_date);
        Task<MessageOut> AionExtension(DateTime? due_date);
        Task<MessageOut> SendSmsToNewCustomers(DateTime? onboard_date);
        Task<MessageOut> SendBirthdaySms(DateTime? dob);
        Task<MessageOut> FetchRecordsFromFile(DateTime? due_date, string notificationType);
    }
}
