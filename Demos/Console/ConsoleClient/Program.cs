﻿using RiptideNetworking;
using System;
using System.Collections.Generic;
using System.Timers;

namespace ConsoleClient
{
    class Program
    {
        private static Client client = new Client();
        private static void Main(string[] args)
        {
            Console.Title = "Client";

            Console.WriteLine("Press any key to connect.");
            Console.ReadKey();

            RiptideLogger.Initialize(Console.WriteLine, true);

            Dictionary<ushort, Client.MessageHandler> messageHandlers = new Dictionary<ushort, Client.MessageHandler>()
            {
                { (ushort)MessageId.reliableTest, HandleReliableTest }
            };

            client.Connected += (e, s) => StartReliableTest();
            client.Disconnected += (e, s) => StopReliableTest();
            client.Connect("127.0.0.1", 7777, messageHandlers);

            Console.ReadKey();

            client.Disconnect();
            StopReliableTest();

            Console.ReadKey();
        }

        private static void StartReliableTest()
        {
            Console.WriteLine("Commencing reliability test!");
            reliableTestIds = new List<int>(testIdAmount);
            for (int i = 0; i < testIdAmount; i++)
            {
                reliableTestIds.Add(i + 1);
            }

            reliableTestTimer = new Timer(25);
            reliableTestTimer.Elapsed += (e, s) => ReliableTestElapsed();
            reliableTestTimer.Start();
        }

        private static void StopReliableTest()
        {
            reliableTestTimer.Stop();
            Console.WriteLine("Cancelled reliability test due to disconnection.");
        }

        private static List<int> reliableTestIds;
        private static Timer reliableTestTimer;
        private static int nextReliableTestId = 1;
        private static int testIdAmount = 1000;

        private static void ReliableTestElapsed()
        {
            if (nextReliableTestId < testIdAmount)
                SendReliableTest(nextReliableTestId++);
            else if (nextReliableTestId == testIdAmount)
            {
                SendReliableTest(nextReliableTestId++);
                reliableTestTimer.Interval = 2500;
            }
            else
            {
                reliableTestTimer.Stop();
                Console.WriteLine();
                Console.WriteLine($"Reliability test complete:");
                Console.WriteLine($"  Messages sent: {testIdAmount}");
                Console.WriteLine($"  Messages lost: {reliableTestIds.Count}");
                if (reliableTestIds.Count > 0)
                    Console.WriteLine($"  Test IDs lost: {string.Join(",", reliableTestIds)}");
                Console.WriteLine();
            }
        }

        private static void SendReliableTest(int reliableTestId)
        {
            Message message = new Message((ushort)MessageId.reliableTest);
            message.Add(reliableTestId);

            client.SendReliable(message);
        }

        private static void HandleReliableTest(Message message)
        {
            int reliableTestId = message.GetInt();

            lock (reliableTestIds)
            {
                if (!reliableTestIds.Remove(reliableTestId))
                {
                    Console.WriteLine($"Duplicate packet received (Test ID: {reliableTestId}).");
                }
            }
        }
    }

    public enum MessageId : ushort
    {
        reliableTest = 1
    }
}