using System;
using System.Collections.Generic;
using System.Linq;
using EsentMapper.Mapping;
using StockSample.Entities;

namespace StockSample
{
	internal class Program
	{
		private static Repository repo;

		private static void Main()
		{
			repo = new Repository(StockSampleSettings.Default.DatabasePath, "stocksample.edb");

			DoStocksStuff();
			DoEventsStuff();
		}

		private static void DoEventsStuff()
		{
			int num = CalcHighestInstanceNum();
			AddSomeEvents(num);
			AddYesterdaysEvents(num);
			AddTomorrowsEvents(num);
			ScanEvents();
			ScanYesterdaysEvents();
			DeleteYesterdaysEvents();
			ScanTodaysEvents();
			DeleteTodaysEvents();
		}

		private static void ScanTodaysEvents()
		{
			DateTime today = DateTime.Today;
			var todaysEvents = repo.IterateOver<Event>(new BoundedRange<Event>("StartTime", e => e.StartTime, today, today.AddDays(1).AddMilliseconds(-1)));
			int numEvents = todaysEvents.Count();
			Console.WriteLine("There are {0} events from today", numEvents);
		}

		private static void ScanEvents()
		{
			int numEvents = repo.Iterate<Event>()
			                    .Count();
			Console.WriteLine("There are {0} events", numEvents);
		}

		private static void ScanYesterdaysEvents()
		{
			DateTime yesterday = DateTime.Today.AddDays(-1);
			var yesterdaysEvents = repo.IterateOver<Event>(new BoundedRange<Event>("StartTime", e => e.StartTime, yesterday, DateTime.Today.AddMilliseconds(-1)));
			int numEvents = yesterdaysEvents.Count();
			Console.WriteLine("There are {0} events from yesterday", numEvents);
		}

		private static void AddSomeEvents(int instanceNum)
		{
			const int numToAdd = 5000;
			Console.Write("Adding {0} events...", numToAdd);

			foreach (int i in Enumerable.Range(++instanceNum, numToAdd))
			{
				Event e = new Event()
				{
					Id = Guid.NewGuid(),
					Description = "Event #" + i,
					Price = i,
					StartTime = DateTime.UtcNow,
					InstanceNum = i
				};
				repo.Add(e);
			}

			Console.WriteLine("Done");
		}

		private static void AddYesterdaysEvents(int num)
		{
			const int numToAdd = 1000;
			DateTime yesterday = DateTime.Now.AddDays(-1);
			Console.Write("Adding {0} events for yesterday...", numToAdd);

			foreach (int i in Enumerable.Range(++num, numToAdd))
			{
				Event e = new Event()
				{
					Id = Guid.NewGuid(),
					Description = "Event #" + i,
					Price = i,
					StartTime = yesterday,
					InstanceNum = i
				};
				repo.Add(e);
			}

			Console.WriteLine("Done");
		}

		private static void AddTomorrowsEvents(int num)
		{
			const int numToAdd = 10;
			DateTime tomorrow = DateTime.Now.AddDays(1);
			Console.Write("Adding {0} events for tomorrow...", numToAdd);

			foreach (int i in Enumerable.Range(++num, numToAdd))
			{
				Event e = new Event()
				{
					Id = Guid.NewGuid(),
					Description = "Event #" + i,
					Price = i,
					StartTime = tomorrow,
					InstanceNum = i
				};
				repo.Add(e);
			}

			Console.WriteLine("Done");
		}

		private static void DeleteTodaysEvents()
		{
			Console.WriteLine("Deleting today's events");
			DateTime today = DateTime.Today;
			BoundedRange<Event> boundedRange = new BoundedRange<Event>("StartTime", e => e.StartTime, today, today.AddDays(1));

			foreach (var e in repo.IterateOver<Event>(boundedRange))
				repo.Delete(e);

			ScanTodaysEvents();
			ScanEvents();
		}

		private static void DeleteYesterdaysEvents()
		{
			Console.WriteLine("Deleting yesterday's events");
			DateTime yesterday = DateTime.Today.AddDays(-1);
			BoundedRange<Event> boundedRange = new BoundedRange<Event>("StartTime", e => e.StartTime, yesterday, DateTime.Today.AddMilliseconds(-1));

			foreach (var e in repo.IterateOver<Event>(boundedRange))
				repo.Delete(e);
			
			ScanYesterdaysEvents();
			ScanEvents();
		}

		private static int CalcHighestInstanceNum()
		{
			return repo.Iterate<Event>()
			           .Select(e => e.InstanceNum)
			           .Concat(new[] {0})
			           .Max();
		}

		private static void DoStocksStuff()
		{
			EmptyStocks();
			AddSomeStocks();
			DumpByNoIndex();
			DumpByNameIndex();
			FindByName();
		}

		private static void FindByName()
		{
			FindByName("Google");
			FindByName("IBB");
		}

		private static void FindByName(string name)
		{
			Console.Write("Locating {0}...", name);

			var s = repo.IterateOver<Stock>(new ExactRange<Stock>("Name", x => x.Name, name)).FirstOrDefault();

			Console.WriteLine("{0}", s == null ? "Not Found" : "Found");
		}

		private static void EmptyStocks()
		{
			IEnumerable<Stock> existingStocks = repo.Iterate<Stock>();
			foreach (Stock stock in existingStocks)
				repo.Delete(stock);
		}

		private static void AddSomeStocks()
		{
			var stocks = new[]
			{
				new Stock() {Symbol = "SBUX", Name = "Starbucks", Price = 988, Shares = 0},
				new Stock() {Symbol = "MSFT", Name = "Microsoft", Price = 1965, Shares = 200},
				new Stock() {Symbol = "AAPL", Name = "Apple", Price = 2342, Shares = 200},
				new Stock() {Symbol = "GOOG", Name = "Google", Price = 3356, Shares = 20},
				new Stock() {Symbol = "MMM", Name = "3M", Price = 9823, Shares = 450},
				new Stock() {Symbol = "IBM", Name = "IBM", Price = 6500, Shares = 240}
			};

			foreach (var stock in stocks)
				repo.Add(stock);
		}

		private static void DumpByNameIndex()
		{
			Console.WriteLine("Dumping by Name index");

			foreach (var stock in repo.IterateOver<Stock>(new IndexRange("Name")))
				Dump(stock);
		}

		private static void DumpByNoIndex()
		{
			Console.WriteLine("Dumping by no index");

			foreach (Stock stock in repo.Iterate<Stock>())
				Dump(stock);
		}

		private static void Dump(Stock aStock)
		{
			Console.WriteLine("Symbol = {0}, Name = {1}, Price = {2}, Shares = {3}", aStock.Symbol, aStock.Name, aStock.Price, aStock.Shares);
		}
	}
}