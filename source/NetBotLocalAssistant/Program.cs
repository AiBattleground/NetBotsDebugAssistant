using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetBotLocalAssistant
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = "http://localhost:9001";
            using (Microsoft.Owin.Hosting.WebApp.Start<Startup>(address))
            {
                Console.WriteLine("Point your browser to {0}", address);
                Console.WriteLine("Some errors may appear in this console screen.");
                Console.WriteLine("Press [Enter] to close this window and shut down the debug server...");
                Console.ReadLine();
            }
        }
    }
}
