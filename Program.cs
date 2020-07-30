using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Npgsql;
using SlackAPI;


namespace consoleApp
{
    class Program
    {
        static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static int LCD1602_ADDRESS = 0x27;
        private static SlackSocketClient client;
        private static bool stop = false;

        private static Dictionary<string, Func<string, List<string>, string>> router = new Dictionary<string, Func<string, List<string>, string>>
        {
            {"temp", GetTemp },
            {"display", Display },
            {"backlight", Backlight },
            {"sql", ExecuteSql },
            {"ping", (_, __) => "pong" },
            {"stop", (_, __) => {stop=true; return "Stopping..."; } }
        };

       
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .Build();

        private static string GetTemp(string _, List<string> __)
        {
            string ret;
            string temp = System.IO.File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
            Console.WriteLine(temp);
            double t = Convert.ToDouble(temp);
            string dc = String.Format("{0:N2}", t / 1000);
            string df = String.Format("{0:N2}", t / 1000 * 9 / 5 + 32);
            ret = dc + "C" + "    " + df + "F";

            return ret;
        }                                                                                                                   

        private static string Display(string data, List<string> _)
        {
            int numQuotes = data.Count(c => c == '\"');

            if (data.First() != '\"' || data.Last() != '\"' && (numQuotes != 2 && numQuotes != 4))
            {
                return "bad format";
            }

            Lcd1602 lcd = new Lcd1602();
            lcd.OpenDevice("/dev/i2c-1", LCD1602_ADDRESS);
            lcd.Init();
            lcd.Clear();

            if (numQuotes == 2)
            {
                lcd.Write(0, 0, data.Between("\"", "\""));
            }
            else
            {
                // two lines
                lcd.Write(0, 0, data.Between("\"", "\""));
                lcd.Write(0, 1, data.RightOf("\"").RightOf("\"").Between("\"", "\""));
            }

            lcd.CloseDevice();

            return "ok";
        }

        private static string Backlight(string data, List<string> _)
        {
            string ret = "ok";

            Lcd1602 lcd = new Lcd1602();
            lcd.OpenDevice("/dev/i2c-1", LCD1602_ADDRESS);
            lcd.Init();

            switch (data.ToLower())
            {
                case "on":
                    lcd.DisplayOn();
                    break;

                case "off":
                    lcd.DisplayOff();
                    break;

                default:
                    ret = "Usage: backlight on|off";
                    break;
            }

            lcd.CloseDevice();

            return ret;
        }

        enum OutputFormat
        {
            JSON,
            CSV,
            Tabular,
        }

        private static string ExecuteSql(string data, List<string> options)
        {
            string sql = data;
            var outputFormat = OutputFormat.JSON;
            string ret = "";
            string validOptionsErrorMessage = "Valid options are --json, --csv, --tabular";

            try
            {
                options.Match(
                    (o => o.Count == 0,         _ => { }),
                    (o => o.Count > 1,          _ => throw new Exception(validOptionsErrorMessage)),
                    (o => o[0] == "--json",     _ => outputFormat = OutputFormat.JSON),
                    (o => o[0] == "--csv",      _ => outputFormat = OutputFormat.CSV),
                    (o => o[0] == "--tabular",  _ => outputFormat = OutputFormat.Tabular),
                    (_ => true,                 _ => throw new Exception(validOptionsErrorMessage))
                    );

                string connStr = Configuration.GetValue<string>("ConnectionStrings:rpidb");
                var conn = new NpgsqlConnection(connStr);
                conn.Open();
                var cmd = new NpgsqlCommand(sql, conn);

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                ret = outputFormat.MatchReturn(
                    (f => f == OutputFormat.JSON, _ => Jsonify(dt)),
                    (f => f == OutputFormat.CSV,  _ => Csvify(dt)),
                    (f => f == OutputFormat.Tabular,  _ => Tabify(dt))
                    );

                ret = "```\r\n" + ret + "```";
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        static string Jsonify(DataTable dt)
        {
            string ret = JsonConvert.SerializeObject(dt, Formatting.Indented);

            return ret.ToString();
        }

        static string Csvify(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Join(", ", dt.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName)));
            
            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine(String.Join(", ", dt.Columns.Cast<DataColumn>().Select(dc => row[dc].ToString())));
            }

            return sb.ToString();
        }

        static string Tabify(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();
            int[] colWidth = new int[dt.Columns.Count];

            dt.Columns.Cast<DataColumn>().ForEachWithIndex((dc, idx) =>
                colWidth[idx] = Math.Max(colWidth[idx], dc.ColumnName.Length));

            
            dt.AsEnumerable().ForEach(r =>
            {
                dt.Columns.Cast<DataColumn>().ForEachWithIndex((dc, idx) =>
                    colWidth[idx] = Math.Max(colWidth[idx], r[dc].ToString().Length));
            });

            
            colWidth.ForEachWithIndex((n, idx) => colWidth[idx] = n + 3);

            
            sb.AppendLine(string.Concat(dt.Columns.Cast<DataColumn>().Select((dc, idx) =>
                dc.ColumnName.PadRight(colWidth[idx]))));


            
            dt.AsEnumerable().ForEach(r =>
            sb.AppendLine(string.Concat(dt.Columns.Cast<DataColumn>().Select((dc, idx) =>
                r[dc].ToString().PadRight(colWidth[idx])))));

            return sb.ToString();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            InitializeSlack();
            Console.WriteLine("Slack Ready.");
            Console.WriteLine("rpi:stop to exit program.");
            while (!stop) Thread.Sleep(1);
            Thread.Sleep(1000);  
        }

        static void InitializeSlack()
        {
            string botToken = Configuration["Slack:BotToken"];
            ManualResetEventSlim clientReady = new ManualResetEventSlim(false);
            client = new SlackSocketClient(botToken);

            client.Connect((connected) => {
                clientReady.Set();
            }, () => {
            });

            client.OnMessageReceived += (message) =>
            {
                Console.WriteLine(message.user + "(" + message.username + "): " + message.text);

                if (message.text.StartsWith("rpi:"))
                {
                    string cmd = message.text.RightOf("rpi:").Trim().LeftOf(" ");

                    List<string> options = new List<string>();

                    while (cmd.StartsWith("--"))
                    {
                        var opt = cmd.LeftOf(" ");
                        options.Add(opt);
                        cmd = message.text.RightOf(opt).Trim().LeftOf(" ");
                    }

                    string data = message.text.RightOf(cmd).Trim();

                    Console.WriteLine("cmd: " + cmd);
                    Console.WriteLine("data: " + data);

                    string ret = "Error occurred.";

                    try
                    {
                        if (router.TryGetValue(cmd, out Func<string, List<string>, string> fnc))
                        {
                            ret = fnc(data, options);
                        }
                        else
                        {
                            string cmdline = (cmd + " " + data).Trim();
                            ret = "```\r\n" + cmdline.Bash() + "```";
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = ex.Message;
                    }

                    client.PostMessage((mr) => { }, message.channel, ret);
                }
            };

            clientReady.Wait();
        }
    }
}
