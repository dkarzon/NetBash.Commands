using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NDesk.Options;
using System.Web;
using NetBash.Formatting;

namespace NetBash.Cache
{
    [WebCommand("cache", "NetBash Cache manager")]
    public class CacheCommand : IWebCommand
    {
        private Command _command;
        private bool _showHelp;

        public string Process(string[] args)
        {
            var sw = new StringWriter();

            var p = new OptionSet() {
                { "l|list",  "displays all items in the cache",
                  v => _command = Command.list },
                { "r|remove",  "Clears cache given the key (use -A to clear all) \nUSAGE: cache -r [key]", 
                  v => _command = Command.remove },
                { "h|help",  "show this message and exit", 
                  v => _showHelp = v != null },
            };

            List<string> extras;
            try
            {
                extras = p.Parse(args);
            }
            catch (OptionException e)
            {
                sw.Write("cache: ");
                sw.WriteLine(e.Message);
                sw.WriteLine("Try `cache --help' for more information.");
                return sw.ToString();
            }

            if (_showHelp || !args.Any())
                return showHelp(p);

            switch (_command)
            {
                case Command.list:
                    return ShowList();
                case Command.remove:
                    return DoRemove(extras.FirstOrDefault());
            }

            throw new ArgumentException("Invalid arguments");
        }

        private string DoRemove(string p)
        {
            if (string.IsNullOrWhiteSpace(p))
                throw new ArgumentNullException("No Key provided use -A for to clear all");

            var sw = new StringWriter();

            if (p.ToUpper() == "-A")
            {
                var resultsDeleted = ClearCache();
                sw.WriteLine(resultsDeleted + " items removed from the cache.");
            }
            else
            {
                HttpContext.Current.Cache.Remove(p);
                sw.WriteLine("'{0}' removed from the cache.", p);
            }

            return sw.ToString();
        }


        private int ClearCache()
        {
            var deletedCount = 0;

            var cacheEnumerator = HttpContext.Current.Cache.GetEnumerator();
            while (cacheEnumerator.MoveNext())
            {
                if (!cacheEnumerator.Key.ToString().StartsWith("__"))
                {
                    HttpContext.Current.Cache.Remove(cacheEnumerator.Key.ToString());
                    deletedCount++;
                }
            }

            return deletedCount;
        }

        private string showHelp(OptionSet p)
        {
            var sb = new StringWriter();

            sb.WriteLine("Usage: cache [OPTIONS]+");
            sb.WriteLine("Options:");

            p.WriteOptionDescriptions(sb);

            return sb.ToString();
        }

        private string ShowList()
        {
            var cacheItems = new Dictionary<string, string>();

            var cacheEnumerator = HttpContext.Current.Cache.GetEnumerator();
            while (cacheEnumerator.MoveNext())
            {
                if (!cacheEnumerator.Key.ToString().StartsWith("__"))
                {
                    cacheItems.Add(cacheEnumerator.Key.ToString(), cacheEnumerator.Value.ToString());
                }
            }

            return cacheItems.ToConsoleTable();
        }

        public bool ReturnHtml
        {
            get { return false; }
        }

        private enum Command
        {
            list,
            remove
        }
    }
}
