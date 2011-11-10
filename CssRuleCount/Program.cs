using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CssRuleCount
{
    public class Program
    {
        public const string HarnessTemplate = @"
<html>
    <head>
        <link rel=""stylesheet"" type=""text/css"" href=""{0}"" />
    </head>
</html>";

        public const int RulesLimit = 4095;
        public static bool RuleLimitExceeded { get; private set; }

        public static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (String.IsNullOrWhiteSpace(arg))
                    continue;

                if (!File.Exists(arg))
                {
                    Console.Error.WriteLine("Skipping unrecognized file: {0}", arg);
                    continue;
                }

                LaunchBrowser(new Uri(Path.GetFullPath(arg)).AbsoluteUri);
            }

            Environment.Exit(RuleLimitExceeded ? 1 : 0);
        }

        private static void LaunchBrowser(string stylesheet)
        {
            Thread t = new Thread(() =>
            {
                WebBrowser wb = new WebBrowser();

                wb.DocumentCompleted += WebBrowser_DocumentCompleted;
                wb.ScriptErrorsSuppressed = true;
                wb.DocumentText = String.Format(HarnessTemplate, stylesheet);

                Application.Run();
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        private static void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;

            if (wb != null)
            {
                dynamic dom = wb.Document.DomDocument;
                string stylesheet = dom.styleSheets[0].href;
                int rules = dom.styleSheets[0].rules.length;

                string file = Path.GetFileName(new Uri(stylesheet).LocalPath);

                Console.WriteLine("Stylesheet \"{0}\" contains {1} rule{2}.", file, rules, rules != 1 ? "s" : "");

                if (rules > RulesLimit)
                    RuleLimitExceeded = true;
            }

            Application.ExitThread();
        }
    }
}
