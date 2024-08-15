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
        private static List<int> globalList = new List<int>();
        private static object listLock = new object();
        private static AutoResetEvent triggerEvenThread = new AutoResetEvent(false);
        private static bool stopThreads = false;

        static void Main(string[] args)
        {
            // Create and start threads
            var oddNumberThread = new Thread(AddOddNumbers);
            var primeNumberThread = new Thread(AddPrimeNumbers);
            var evenNumberThread = new Thread(AddEvenNumbers);

            oddNumberThread.Start();
            primeNumberThread.Start();

            // Wait for the global list to reach 250,000 items to start the even number thread
            while (true)
            {
                lock (listLock)
                {
                    if (globalList.Count >= 250000)
                    {
                        triggerEvenThread.Set(); // Signal to start even number thread
                        break;
                    }
                }
            }

            evenNumberThread.Start();

            // Wait until the list reaches exactly 1,000,000 items
            while (true)
            {
                lock (listLock)
                {
                    if (globalList.Count == 1000000)
                    {
                        stopThreads = true; // Signal to stop all threads
                        break;
                    }
                }
            }

            // Join threads to ensure they finish execution
            oddNumberThread.Join();
            primeNumberThread.Join();
            evenNumberThread.Join();

            // Sort the list and count odd and even numbers
            globalList.Sort();
            int oddCount = globalList.Count(x => x % 2 != 0);
            int evenCount = globalList.Count(x => x % 2 == 0);

            Console.WriteLine($"Odd Numbers Count: {oddCount}");
            Console.WriteLine($"Even Numbers Count: {evenCount}");

            // Serialize the list to binary and XML files
            SerializeList(globalList);
        }

        static void AddOddNumbers()
        {
            Random rand = new Random();
            while (!stopThreads)
            {
                int number = rand.Next(1, int.MaxValue);
                if (number % 2 != 0)
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                            globalList.Add(number);
                    }
                }
            }
        }

        static void AddPrimeNumbers()
        {
            int number = 2;
            while (!stopThreads)
            {
                if (IsPrime(number))
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                            globalList.Add(-number);
                    }
                }
                number++;
            }
        }

        static void AddEvenNumbers()
        {
            Random rand = new Random();
            triggerEvenThread.WaitOne(); // Wait for the signal to start

            while (!stopThreads)
            {
                int number = rand.Next(1, int.MaxValue);
                if (number % 2 == 0)
                {
                    lock (listLock)
                    {
                        if (globalList.Count < 1000000)
                            globalList.Add(number);
                    }
                }
            }
        }

        static bool IsPrime(int number)
        {
            if (number < 2) return false;
            for (int i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        static void SerializeList(List<int> list)
        {
            // Serialize to binary
            using (FileStream fs = new FileStream("globalList.bin", FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(fs);
                foreach (int number in list)
                {
                    writer.Write(number);
                }
            }

            // Serialize to XML
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<int>));
            using (FileStream fs = new FileStream("globalList.xml", FileMode.Create))
            {
                xmlSerializer.Serialize(fs, list);
            }
        }
    }
}
