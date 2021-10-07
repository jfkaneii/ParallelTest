using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelTest
{
	class Program
	{
		class ThreadsStat
		{
			static public object lockObj = new object();
			static public int sum = 0;
			static public Dictionary<int, Tuple<int, Stopwatch>> threadInfo = new Dictionary<int, Tuple<int, Stopwatch>>();
		}

		// Demonstrated features:
		// 		Parallel.ForEach()
		//		Thread-local state
		// Expected results:
		//      This example sums up the elements of an int[] in parallel.
		//      Each thread maintains a local sum. When a thread is initialized, that local sum is set to 0.
		//      On every iteration the current element is added to the local sum.
		//      When a thread is done, it safely adds its local sum to the global sum.
		//      After the loop is complete, the global sum is printed out.
		// Documentation:
		//		http://msdn.microsoft.com/library/dd990270(VS.100).aspx
		static void Main()
		{
			// The sum of these elements is 40.
			int[] input = { 4, 1, 6, 2, 9, 5, 10, 3 };

			try
			{
				// First type parameter is the type of the source elements
				// Second type parameter is the type of the thread-local variable (partition subtotal)
				Parallel.ForEach<int, int>(
						input,                          // source collection
						() => 0,                        // thread local initializer
						(n, loopState, localSum) =>     // body
						{
							Stopwatch timeLoop = new Stopwatch();
							timeLoop.Start();
							lock (ThreadsStat.lockObj)
							{
								ThreadsStat.threadInfo.Add(Thread.CurrentThread.ManagedThreadId, new Tuple<int, Stopwatch>(n, timeLoop));
							}

							//Do things
							Random waitTime = new Random(n);
							int msecs = waitTime.Next(0 * 1000, 3 * 1000);

							//Put the thread to sleep
							System.Threading.Thread.Sleep(msecs);

							localSum += n;
							Console.WriteLine($"Thread={Thread.CurrentThread.ManagedThreadId}, n={n}, localSum={localSum}, delay={msecs}");
							return localSum;
						},
						(localSum) =>
						{
							Tuple<int, Stopwatch> tInfo = null;
							lock (ThreadsStat.lockObj)
							{
								try
								{
									tInfo = ThreadsStat.threadInfo[Thread.CurrentThread.ManagedThreadId];
									ThreadsStat.threadInfo.Remove(Thread.CurrentThread.ManagedThreadId);
								}
								catch (KeyNotFoundException)
								{
									Console.WriteLine($"Key =[{Thread.CurrentThread.ManagedThreadId}] is not found.");
								}
							}
							Console.WriteLine($"Thread={Thread.CurrentThread.ManagedThreadId}, n={tInfo.Item1}, localSum={localSum}, elapsed msec={tInfo.Item2.ElapsedMilliseconds.ToString()}");
							Interlocked.Add(ref ThreadsStat.sum, localSum);                    // thread local aggregator
						}
					);

				Console.WriteLine($"\nSum={ThreadsStat.sum}");
			}
			// No exception is expected in this example, but if one is still thrown from a task,
			// it will be wrapped in AggregateException and propagated to the main thread.
			catch (AggregateException e)
			{
				Console.WriteLine($"Parallel.ForEach has thrown an exception. THIS WAS NOT EXPECTED.\n{e}");
			}

			Console.ReadKey();




			//// A simple source for demonstration purposes. Modify this path as necessary.
			//string[] files = Directory.GetFiles(@"C:\Users\Public\Pictures\Sample Pictures", "*.jpg");
			//string newDir = @"C:\Users\Public\Pictures\Sample Pictures\Modified";
			//Directory.CreateDirectory(newDir);

			//// Method signature: Parallel.ForEach(IEnumerable<TSource> source, Action<TSource> body)
			//Parallel.ForEach<string>(files, (currentFile) =>
			//{
			//	// The more computational work you do here, the greater 
			//	// the speedup compared to a sequential foreach loop.
			//	string filename = Path.GetFileName(currentFile);
			//	var bitmap = new Bitmap(currentFile);

			//	bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
			//	bitmap.Save(Path.Combine(newDir, filename));

			//	// Peek behind the scenes to see how work is parallelized.
			//	// But be aware: Thread contention for the Console slows down parallel loops!!!

			//	Console.WriteLine($"Processing {filename} on thread {Thread.CurrentThread.ManagedThreadId}");
			//	//close lambda expression and method invocation
			//});

			//// Keep the console window open in debug mode.
			//Console.WriteLine("Processing complete. Press any key to exit.");
			//Console.ReadKey();

		}
	}
}
