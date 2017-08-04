using System;
using Microsoft.Azure.EventHubs;
using RetryPolicy = Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.RetryPolicy;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace NetCoreGameEventsGenerator
{
    class EventHubHelper
    {
        // TODO: what is it for?
        private static readonly RetryPolicy RetryPolicy = new RetryPolicy(
            new EventHubTransientErrorDetectionStrategy(),
            new ExponentialBackoff(
                "EventHubInputAdapter",
                5,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(500),
                true));

        private class EventHubTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                if ((ex is EventHubsCommunicationException messagingException && messagingException.IsTransient) || ex is TimeoutException)
                {
                    Console.WriteLine(ex);
                    return true;
                }
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
