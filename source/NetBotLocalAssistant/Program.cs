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
                Console.WriteLine("Press [enter] to quit the server...");
                Console.ReadLine();
            }
        }
    }
}
