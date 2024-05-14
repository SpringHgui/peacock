using MQTTnet.Diagnostics;
using Serilog;

namespace Scheduler.Master.Server
{
    class MyLog : IMqttNetLogger
    {
        public bool IsEnabled => true;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            switch (logLevel)
            {
                case MqttNetLogLevel.Verbose:
                    Log.Verbose(message);
                    break;
                case MqttNetLogLevel.Info:
                    Log.Information(message);
                    break;
                case MqttNetLogLevel.Warning:
                    Log.Warning(message);
                    break;
                case MqttNetLogLevel.Error:
                    Log.Error(message);
                    break;
                default:
                    break;
            }
        }
    }
}
