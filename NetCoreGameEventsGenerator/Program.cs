using System;
using System.Text;
// ServiceBus Messaging is not available in .Net Core / standard
// using Microsoft.ServiceBus.Messaging;
// using: https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send
using Microsoft.Azure.EventHubs;
using System.Threading;
using System.Globalization;

/// <summary>
/// This game events generator has been simplified and refactored to run in .Net Core
/// to be able to run in a linux container
/// </summary>
namespace NetCoreGameEventsGenerator
{
    class Program
    {
        
        private static Timer timer;
        static void Main(string[] args)
        {
            SendData(Environment.EventHubConnectionString, Environment.EventHubPath);
        }

        /// <summary>
        /// Sending entry and exit events to two different event hubs
        /// </summary>
        /// <param name="serviceBusConnectionString"></param>
        /// <param name="entryHubName"></param>
        /// <param name="existHubName"></param>
        public static void SendData(string serviceBusConnectionString, string entryHubName, string existHubName)
        {
            var entryEventHub = EventHubClient.CreateFromConnectionString(serviceBusConnectionString);
            var exitEventHub = EventHubClient.CreateFromConnectionString(serviceBusConnectionString);

            var timerInterval = TimeSpan.FromSeconds(1);
            var generator = GameDataEventGenerator.Generator();

            TimerCallback timerCallback = state =>
            {
                var startTime = DateTime.UtcNow;
                generator.Next(startTime, timerInterval, 5);

                foreach (var e in generator.GetEvents(startTime))
                {
                    if (e is EntryEvent)
                    {
                        Console.WriteLine("Sending start event data '{0}','{1}'", e.PlayerId, e.GameId);
                        entryEventHub.SendAsync(
                           new EventData(Encoding.UTF8.GetBytes(e.Format())));
                        // ToDo: CHeck what to do about the partition key -- maybe in the connection string? Or just use entity path?
                           //{
                           //   PartitionKey = e.GameId.ToString(CultureInfo.InvariantCulture)
                           //});
                    }
                    else
                    {
                        Console.WriteLine("Sending stop event data '{0}','{1}'", e.PlayerId, e.GameId);
                        exitEventHub.SendAsync(
                           new EventData(Encoding.UTF8.GetBytes(e.Format())));
                    }
                }

                timer.Change((int)timerInterval.TotalMilliseconds, Timeout.Infinite);
            };

            timer = new Timer(timerCallback, null, Timeout.Infinite, Timeout.Infinite);
            timer.Change(0, Timeout.Infinite);

            Console.WriteLine("Sending event hub data... Press Ctrl+c to stop.");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Stopping...");
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();
            Console.WriteLine("Shutting down all resources...");
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Thread.Sleep(timerInterval);
            timer.Dispose();
            entryEventHub.Close();
            exitEventHub.Close();
            Console.WriteLine("Stopped.");

        }


        /// <summary>
        /// Sending entry and exit game events to a single event hub
        /// </summary>
        /// <param name="serviceBusConnectionString"></param>
        /// <param name="eventHubName"></param>
        public static void SendData(string serviceBusConnectionString, string eventHubName)
        {
            var eventHub = EventHubClient.CreateFromConnectionString(serviceBusConnectionString);
            // var eventHub = EventHubClient.Create(serviceBusConnectionString);

            var timerInterval = TimeSpan.FromSeconds(1);
            var generator = GameDataEventGenerator.Generator();

            TimerCallback timerCallback = state =>
            {
                var startTime = DateTime.UtcNow;
                generator.Next(startTime, timerInterval, 5);

                foreach (var e in generator.GetEvents(startTime))
                {
                    if (e is EntryEvent)
                    {
                        Console.WriteLine("Sending start event data '{0}','{1}'", e.PlayerId, e.GameId);
                    }
                    else
                    {
                        Console.WriteLine("Sending stop event data '{0}','{1}'", e.PlayerId, e.GameId);
                    }
                    eventHub.SendAsync(new EventData(Encoding.UTF8.GetBytes(e.Format())));
                    
                }

                timer.Change((int)timerInterval.TotalMilliseconds, Timeout.Infinite);
            };

            timer = new Timer(timerCallback, null, Timeout.Infinite, Timeout.Infinite);
            timer.Change(0, Timeout.Infinite);

            Console.WriteLine("Sending event hub data... Press Ctrl+c to stop.");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Stopping...");
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();
            Console.WriteLine("Shutting down all resources...");
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Thread.Sleep(timerInterval);
            timer.Dispose();
            eventHub.Close();
            Console.WriteLine("Stopped.");

        }

    }
}
