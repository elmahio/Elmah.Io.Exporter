using System;
using Microsoft.Extensions.CommandLineUtils;
using Elmah.Io.Client;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using ShellProgressBar;

namespace Elmah.Io.Export
{
	//<summary>
	//The program fetches messages from the Elmah.Io api and streams them to a file.</summary>
	class Program
	{
		//<summary>
		//The constructor reads the arguments given to the program and checks if all the necesary ones are present before calling the connectNStream function.</summary>
		static void Main(string[] args)
		{
			var app = new Microsoft.Extensions.CommandLineUtils.CommandLineApplication();
			app.Name = "";

			app.Description = "Exports the Logs to a json file.";
			app.HelpOption("-?|-h|--help");
			app.ExtendedHelpText = "following arguments are required -ApiKey, -LogId, -DateFrom and -DateTo";

			var apiKey = app.Option("-ApiKey|--ApiKey <ApiKey>",
                            "Defines the API key. Ex \" -ApiKey 8890a01cdd9c403aac9a6f89829867f3\"",
							CommandOptionType.SingleValue);

			var filename = app.Option("-Filename|--Filename <Filename>",
							"Defines the path and filename of the file to export to. Ex \" -Filename C:\\myDirectory\\myFile.json\"",
							CommandOptionType.SingleValue);

			var logId = app.Option("-LogId|--LogId <LogId>",
                            "Defines the log ID. Ex \" -LogId cfc710e7-06fb-4d30-be24-a96d5caf3d19\"",
							CommandOptionType.SingleValue);

			var dateFrom = app.Option("-DateFrom|--DateFrom <DateFrom>",
							"Defines the Date from which the logs start. Ex. \" -DateFrom 2017-10-24\"",
							CommandOptionType.SingleValue);

			var dateTo = app.Option("-DateTo|--DateTo <DateTo>",
							"Defines the Date to which the logs ends. Ex. \" -DateTo 2018-11-30\"",
							CommandOptionType.SingleValue);

			var query = app.Option("-Query|--Query <Query>",
							"Defines the query that is passed to the API",
							CommandOptionType.SingleValue);

			var includeHeadersOption = app.Option("-IncludeHeaders|--IncludeHeaders",
							"Option for including the headers in output",
							CommandOptionType.NoValue);
			app.OnExecute(() =>
			{
				int check = 0;
				if (apiKey.HasValue())
				{
					check++;
				}
				else
				{
					Console.WriteLine("You need to define an API key. Ex \" -ApiKey 8890a01cdd9c403aac9a6f89829867f3\"");
				}

				if (logId.HasValue())
				{
					check++;
				}
				else
				{
					Console.WriteLine("You need to define a log ID. Ex \" -LogId cfc710e7-06fb-4d30-be24-a96d5caf3d19\"");
				}

				if (check == 2)
				{
					connectNStream(filename, apiKey.Value(), logId.Value(), dateFrom.Value(), dateTo.Value(), query.Value(), includeHeadersOption.HasValue());
				}

				return 0;

			});


			try
			{
				var result = app.Execute(args);
				Environment.Exit(result);
			}
			catch (CommandParsingException cpe)
			{
				Console.WriteLine("Couldn't pass your command try using -h to find out more");
				Console.WriteLine(cpe.Message);
			}
			catch (Exception e)
			{
				Console.WriteLine("An unexpected error occured try redefining your arguments or find help using -h");
				Console.WriteLine(e.Message);
			}
		}

		//<summary>
		//The method takes the options from the constructor and creates a api object used for writing the specified messages to a file.</summary>
		private static void connectNStream(CommandOption f, String a, String l, String df, String dt, String q, bool ih)
		{
			String filename;
			if (f.HasValue())
			{
				filename = f.Value();
			}
			else
			{
                var ticks = DateTime.Now.Ticks;
				filename = Path.Combine(Directory.GetCurrentDirectory().ToString(), $"Export-{ticks}.json");
			}

            var dateFrom = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(df) && !DateTime.TryParse(df, out dateFrom))
            {
                throw new FormatException("The given -DateFrom was not a valid Date. Ex. \" -DateFrom 2017-10-24\"");
            }

            var dateTo = DateTime.MaxValue;
            if (!string.IsNullOrWhiteSpace(dt) && !DateTime.TryParse(dt, out dateTo))
			{
				throw new FormatException("The given -DateTo was not a valid Date. Ex. \" -DateTo 2017-11-30\"");
			}

			var api = new ElmahioAPI(new ApiKeyCredentials(a));
			var startResult = api.Messages.GetAll(l, 0, 1, q, dateFrom, dateTo, ih);
			if (startResult == null)
			{
				Console.WriteLine("Could not find any messages for this API key and log ID combination");
			}
			else
			{
				int messSum = startResult.Total.Value;
				int i = 0;
				var options = new ProgressBarOptions
				{
					ProgressCharacter = '=',
					ProgressBarOnBottom = false,
					ForegroundColorDone = ConsoleColor.Green,
					ForegroundColor = ConsoleColor.White
				};
				using (var pbar = new ProgressBar(messSum, "Exporting log from API", options))
				{
					if (File.Exists(filename)) File.Delete(filename);
					using (StreamWriter w = File.AppendText(filename))
					{
						w.WriteLine("[");
						while (i < messSum)
						{
							var respons = api.Messages.GetAll(l, i / 10, 10, q, dateFrom, dateTo, ih);
							List<Client.Models.MessageOverview> messages = respons.Messages.ToList();
							foreach (Client.Models.MessageOverview message in messages)
							{
								w.WriteLine(JValue.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented));
								i++;
								if (i != messSum) w.WriteLine(",");
								pbar.Tick("Step " + i + " of " + messSum);
							}
						}
						w.WriteLine("]");
						pbar.Tick("Done with export to " + filename);
					}
				}
			}
		}
	}
}