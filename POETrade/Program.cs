using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{

    public class Program
    {
        private const string URL = "http://api.pathofexile.com/public-stash-tabs";

        static void Main(string[] args)
        {
            int count = 0;
            string nextChangeId = getNextChangeId();
            while (count == 0)
            {
                HttpWebRequest webrequest = null;
                if (!string.IsNullOrEmpty(nextChangeId))
                    webrequest = (HttpWebRequest)WebRequest.Create(URL + "/?id=" + nextChangeId);
                else
                    webrequest = (HttpWebRequest)WebRequest.Create(URL);

                using (var response = webrequest.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var result = reader.ReadToEnd();
                        string json = Convert.ToString(result);
                        JObject rss = JObject.Parse(json);

                        var items =
                            from p in rss["stashes"].Children()["items"].Children()
                            where (
                                ((string)p["name"]).Contains("Lion's Roar") 
                                && ((string)p["league"]).Equals("Harbinger") 
                                && !string.IsNullOrEmpty((string)p["note"])
                                && ((string)p["note"]).Contains("chaos")
                                && decimal.Parse(Regex.Match((string)p["note"], @"\d+").Value) < 20
                            )
                            select new { account = p.Parent.Parent.Parent["lastCharacterName"], item = p["name"], price = p["note"], league=p["league"] };

                        nextChangeId = (string)rss["next_change_id"];

                        using (StreamWriter writeText = File.AppendText(@"C:\temp\log.txt"))
                        {
                            foreach (var i in items)
                            {
                                writeText.WriteLine(i.account + "-" + i.item + "-" + i.price);
                                Console.WriteLine("Item found");
                            }
                        }
                    }
                }
            }
        }

        private static string getNextChangeId()
        {
            var webrequest = (HttpWebRequest)WebRequest.Create("http://api.poe.ninja/api/Data/GetStats");

            using (var response = webrequest.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var result = reader.ReadToEnd();
                    string json = Convert.ToString(result);
                    JObject rss = JObject.Parse(json);

                    return (string)rss["nextChangeId"];
                }
            }
        }
    }
}