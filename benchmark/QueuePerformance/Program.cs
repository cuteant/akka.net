using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Akka.Util;

namespace QueuePerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            //var deque1 = new Deque<int>(new int[] { 1, 2, 3 });
            //deque1.AddToFront(1);
            //deque1.AddToFront(2);
            //deque1.AddToFront(3);
            //deque1.AddToFront(4);
            //deque1.AddToFront(5);
            //deque1.AddToFront(6);
            //deque1.AddToFront(7);
            //deque1.AddToFront(8);
            //deque1.AddToFront(9);
            //deque1.AddToFront(10);

            //foreach (var item in deque1)
            //{
            //    Console.WriteLine(item);
            //}
            //Console.WriteLine(deque1.RemoveFromBack());
            //Console.WriteLine(deque1.RemoveFromBack());
            //Console.WriteLine(deque1.RemoveFromBack());

            //Console.WriteLine("Queue");
            //var queue1 = new Queue<int>();
            //queue1.Enqueue(1);
            //queue1.Enqueue(2);
            //queue1.Enqueue(3);
            //queue1.Enqueue(4);
            //queue1.Enqueue(5);
            //queue1.Enqueue(6);
            //queue1.Enqueue(7);
            //queue1.Enqueue(8);
            //queue1.Enqueue(9);
            //queue1.Enqueue(10);
            //foreach (var item in queue1)
            //{
            //    Console.WriteLine(item);
            //}

            //Console.WriteLine("Stack");
            //var stack1 = new Stack<int>();
            //stack1.Push(1);
            //stack1.Push(2);
            //stack1.Push(3);
            //stack1.Push(4);
            //stack1.Push(5);
            //stack1.Push(6);
            //stack1.Push(7);
            //stack1.Push(8);
            //stack1.Push(9);
            //stack1.Push(10);
            //foreach (var item in stack1)
            //{
            //    Console.WriteLine(item);
            //}

            //Console.WriteLine("按");
            //Console.ReadKey();
            //return;

            Random rand = new Random();
            Stopwatch sw = new Stopwatch();
            Queue<int> queue = new Queue<int>(8);
            Stack<int> stack = new Stack<int>(8);
            StackX<int> akkaStack = new StackX<int>(8);
            QueueX<int> akkaQueue = new QueueX<int>(8);
            Deque<int> deque = new Deque<int>();
            SingleProducerSingleConsumerQueue<int> spscQueue = new SingleProducerSingleConsumerQueue<int>();
            LinkedList<int> linkedlist1 = new LinkedList<int>();
            int dummy;


            sw.Reset();
            Console.Write("{0,40}", "Push to Stack...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                stack.Push(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "Pop from Stack...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = stack.Count;
                var isEmpty = stack.Count <= 0;
                dummy = stack.Pop();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "Push to AkkaStack...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                akkaStack.Push(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "Pop from AkkaStack...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = akkaStack.Count;
                var isEmpty = akkaStack.Count <= 0;
                dummy = akkaStack.Pop();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "Enqueue to Queue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                queue.Enqueue(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "Dequeue from Queue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = queue.Count;
                var isEmpty = queue.Count <= 0;
                dummy = queue.Dequeue();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "Enqueue to AkkaQueue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                akkaQueue.Enqueue(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "Dequeue from AkkaQueue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                var isEmpty = akkaQueue.Count <= 0;
                dummy = akkaQueue.Dequeue();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "AddToBack to Deque...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                deque.AddToBack(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "RemoveFromFront from Deque...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = deque.Count;
                var isEmpty = deque.Count <= 0;
                dummy = deque.RemoveFromFront();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            deque = new Deque<int>(8);
            sw.Reset();
            Console.Write("{0,40}", "AddToFront to Deque...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                deque.AddToFront(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "RemoveFromBack from Deque...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = deque.Count;
                var isEmpty = deque.Count <= 0;
                dummy = deque.RemoveFromBack();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "Enqueue to SingleProducerSingleConsumerQueue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                spscQueue.Enqueue(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "TryDequeue from SingleProducerSingleConsumerQueue...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = spscQueue.Count;
                var isEmpty = spscQueue.IsEmpty;
                spscQueue.TryDequeue(out dummy);
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            sw.Reset();
            Console.Write("{0,40}", "AddLast to LinkedList...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                linkedlist1.AddLast(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "RemoveFirst from LinkedList...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = linkedlist1.Count;
                var isEmpty = !linkedlist1.Any();
                dummy = linkedlist1.First.Value;
                linkedlist1.RemoveFirst();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            linkedlist1 = new LinkedList<int>();
            sw.Reset();
            Console.Write("{0,40}", "AddFirst to LinkedList...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                linkedlist1.AddFirst(rand.Next());
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);
            sw.Reset();
            Console.Write("{0,40}", "RemoveFirst from LinkedList...");
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                //var count = linkedlist1.Count;
                var isEmpty = !linkedlist1.Any();
                dummy = linkedlist1.First.Value;
                linkedlist1.RemoveFirst();
                dummy++;
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks\n", sw.ElapsedTicks);


            Console.WriteLine("按任意键退出");
            Console.ReadKey();
        }
    }
}
