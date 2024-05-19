using TimeCrontab;

namespace Scheduler.Master.Models
{
    public class CrontabUtility
    {
        public static Crontab Parse(string TimeExpression)
        {
            var len = TimeExpression.Trim().Split(" ").Length;
            CronStringFormat cronStringFormat;
            if (len == 6)
            {
                cronStringFormat = CronStringFormat.WithSeconds;
            }
            else if (len == 7)
            {
                cronStringFormat = CronStringFormat.WithSecondsAndYears;
            }
            else
            {
                throw new ArgumentException($"表达式错误: {TimeExpression}");
            }

            var crontab = Crontab.Parse(TimeExpression, cronStringFormat);
            return crontab;
        }
    }
}
