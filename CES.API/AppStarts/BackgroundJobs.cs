using CES.BusinessTier.Services;
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

        var corn = Cron.Daily();
        RecurringJob.AddOrUpdate<IWalletServices>(x => x.ResetAllAfterExpired(), corn);
    }
}