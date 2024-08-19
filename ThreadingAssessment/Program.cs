using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

namespace ThreadingAssessment
{
    internal class Program
    {
       // I created a global list to store integers and a lock object to synchronize access to this list.
        private static List<int> globalList = new List<int>();
        private static readonly object listLock = new object();
        private static AutoResetEvent startEvenThreadSignal = new AutoResetEvent(false);
        private static bool shouldStopThreads = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting the program...");

            // I created and started the initial threads to add odd numbers and prime numbers to the global list.
            var oddNumberThread = new Thread(AddOddNumbers);
            var primeNumberThread = new Thread(AddPrimeNumbers);
            var evenNumberThread = new Thread(AddEvenNumbers); // This thread will be started later

            oddNumberThread.Start();
            primeNumberThread.Start();

            // I also set up a monitoring thread that checks the global list's size and starts the even number thread when needed.
            var monitorThread = new Thread(() =>
            {
                while (true)
                {
                    lock (listLock)
                    {
                        if (globalList.Count >= 250000)
                        {
                            Console.WriteLine("Global list reached 250,000 items. Starting the even number thread...");
                            startEvenThreadSignal.Set(); // Signal to start the even numbers thread
                            if (!evenNumberThread.IsAlive)
                            {
                                evenNumberThread.Start(); // Start the even numbers thread
                            }
                            break; // Exit the monitoring loop
                        }

                        // I log progress periodically to monitor how many items are in the list.
                        if (globalList.Count % 50000 == 0 && globalList.Count > 0)
                        {
                            Console.WriteLine($"Progress: {globalList.Count} items in the list...");
                        }
                    }
                    Thread.Sleep(50); // Reduce CPU usage
                }
            });

            monitorThread.Start();

            // I wait until the list reaches exactly 1,000,000 items.
            WaitForThreshold(1000000);

            // I signal all threads to stop.
            shouldStopThreads = true;

            // I ensure all threads have completed execution.
            oddNumberThread.Join();
            primeNumberThread.Join();
            evenNumberThread.Join();

            // I display the results of the operation.
            DisplayResults();
        }

        private static void WaitForThreshold(int threshold)
        {
            while (true)
            {
                lock (listLock)
                {
                    if (globalList.Count >= threshold)
                    {
                        Console.WriteLine($"Global list reached {threshold} items.");
                        break;
                    }

                    // I log progress periodically while waiting for the threshold.
                    if (globalList.Count % 100000 == 0 && globalList.Count > 0)
                    {
                        Console.WriteLine($"Waiting for {threshold} items: current count = {globalList.Count}");
                    }
                }
                Thread.Sleep(50); // Reduce CPU usage
            }
        }

        private static void AddOddNumbers()
        {
            Random random = new Random();
            while (!shouldStopThreads)
            {
                int number = random.Next(1, int.MaxValue);
                if (number % 2 != 0) // Check if the number is odd
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                        {
                            globalList.Add(number);
                        }
                    }
                }
                // Temporarily remove Thread.Sleep to increase speed
                // Thread.Sleep(10);
            }
        }

        private static void AddPrimeNumbers()
        {
            int number = 2; // Start with the first prime number
            while (!shouldStopThreads)
            {
                if (IsPrime(number))
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                        {
                            globalList.Add(-number);
                        }
                    }
                }
                number++;
                // Temporarily remove Thread.Sleep to increase speed
                // Thread.Sleep(10);
            }
        }

        private static void AddEvenNumbers()
        {
            Random random = new Random();
            startEvenThreadSignal.WaitOne(); // Wait for the signal to start

            while (!shouldStopThreads)
            {
                int number = random.Next(1, int.MaxValue);
                if (number % 2 == 0) // Check if the number is even
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                        {
                            globalList.Add(number);
                        }
                    }
                }
                // Temporarily remove Thread.Sleep to increase speed
                // Thread.Sleep(10);
            }
        }

        private static bool IsPrime(int number)
        {
            if (number < 2) return false;
            for (int i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        private static void DisplayResults()
        {
            Console.WriteLine("Sorting the global list...");

            lock (listLock)
            {
                globalList.Sort(); // Sort the list in ascending order
            }

            Console.WriteLine("Global list sorted.");

            // Count the number of odd and even numbers
            int oddCount = 0;
            int evenCount = 0;

            foreach (int number in globalList)
            {
                if (number % 2 == 0)
                {
                    evenCount++;
                }
                else
                {
                    oddCount++;
                }
            }

            // Display the counts
            Console.WriteLine($"Total items in the list: {globalList.Count}");
            Console.WriteLine($"Count of odd numbers: {oddCount}");
            Console.WriteLine($"Count of even numbers: {evenCount}");

            // Serialize the list to binary and XML files
            SerializeList(globalList);
        }

        private static void SerializeList(List<int> list)
        {
            // Serialize to binary
            using (var fs = new FileStream("globalList.bin", FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                foreach (int number in list)
                {
                    writer.Write(number);
                }
            }

            // Serialize to XML
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<int>));
            using (var fs = new FileStream("globalList.xml", FileMode.Create))
            {
                xmlSerializer.Serialize(fs, list);
            }
        }
    }
}
