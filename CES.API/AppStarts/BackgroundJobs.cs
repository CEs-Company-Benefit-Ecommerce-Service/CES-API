using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Hangfire;

namespace CES.API.AppStarts;

public static class BackgroundJobs
{
    public static void RecurringJobs()
    {
        // var corn = Cron.Daily();    // "0 0 * * *"  Every night at 12:00 AM
        //
        // var corn1 = Cron.Daily(11, 30);   // "30 11 * * *"
        //
        // var corn2 = Cron.Daily(12);   // "0 12 * * *"
        //
        // var corn3 = Cron.Daily(23,55);  // "55 23 * * *"
        // RecurringJob.AddOrUpdate<IAccountServices>(x => x.GetAccountByEmail("asdasdasd"), "* 9 * * *");
        
        //PaymentDueNotice1 = "0 7 25 * *"
        //PaymentDueNotice1 = "0 7 27 * *"
        //PaymentDueNotice1 = "0 7 30 * *"

        var corn = Cron.Daily();
        var firstNotiForExpireDate = "0 0 25 * *";
        var secondNotiForExpireDate = "0 0 27 * *";
        var thirdNotiForExpireDate = "0 0 1 * *";
        var everyLastDateOfMonthNotiExpireDate = "0 0 28-31 * *";
        RecurringJob.AddOrUpdate<IWalletServices>(x => x.ResetAllAfterExpired(), corn);
        RecurringJob.AddOrUpdate<INotificationServices>(x => x.CreateNotificationForEmployeesInActive(), corn);
        RecurringJob.AddOrUpdate<INotificationServices>(x => x.ScheduleFirstNotificationWhenExpireDateIsComming(), firstNotiForExpireDate);
        RecurringJob.AddOrUpdate<INotificationServices>(x => x.ScheduleSecondNotificationWhenExpireDateIsComming(), secondNotiForExpireDate);
        RecurringJob.AddOrUpdate<INotificationServices>(x => x.ScheduleThirdNotificationWhenExpireDateIsComming(), thirdNotiForExpireDate);
        RecurringJob.AddOrUpdate<INotificationServices>(x => x.ScheduleCurrentNotificationWhenExpireDateIsComming(), everyLastDateOfMonthNotiExpireDate);
    }
}