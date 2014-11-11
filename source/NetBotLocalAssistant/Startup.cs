using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartup(typeof(NetBotLocalAssistant.Startup))]

namespace NetBotLocalAssistant
{
    public class Startup
    {
        private readonly string _basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private HttpClient _client = new HttpClient();

        public void Configuration(IAppBuilder app)
        {
            app.Run(context =>
            {
                var path = GetPath(context);
                var contentType = GetContentType(path);

                string stringToRender = null;
                if (contentType == "text/json")
                {
                    stringToRender = RelayJsonRequest(context).Result;
                }
                else
                {
                    stringToRender = GetFile(path);
                }

                context.Response.ContentType = contentType;
                return context.Response.WriteAsync(stringToRender);
            });
        }

        private async Task<string> RelayJsonRequest(IOwinContext context)
        {
            try
            {
                byte[] bytes = GetBytesFromBody(context.Request.Body);
                var myString = System.Text.Encoding.Default.GetString(bytes);
                myString = System.Net.WebUtility.UrlDecode(myString);
                var myObject = JObject.Parse(myString);
                var payload = myObject["payload"];
                var address = (string) myObject["destination"];
                if (!address.StartsWith("http://") && !address.StartsWith("https://"))
                {
                    address = "http://" + address;
                }
                string payloadJson = payload.ToString();
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(address, content);
                response.EnsureSuccessStatusCode();
                var responeString = await response.Content.ReadAsStringAsync();
                if (String.IsNullOrWhiteSpace(responeString))
                {
                    responeString = "{ }";
                }
                return responeString;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error communicating with local testing bot. Full error was:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
                return "{ Error: " + ex.Message + " }";
            }
        }

        private byte[] GetBytesFromBody(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private string GetContentType(string path)
        {
            var extension = GetExtension(path);
            switch (extension)
            {
                case "js":  return "text/javascript";
                case "html":
                case "htm": return "text/html";
                case "png": return "image/png";
                case "css": return "text/css";
                case "json": return "text/json";
                default: return "text";
            }
        }

        private string GetExtension(string path)
        {
            var lastPeriod = path.LastIndexOf('.');
            var extension = path.Substring(lastPeriod + 1, path.Length - lastPeriod - 1);
            return extension;
        }

        private static string GetPath(IOwinContext context)
        {
            var path = context.Request.Path.ToString();
            if (path == "/")
            {
                path += "index.html";
            }
            return path;
        }

        private string GetFile(string path)
        {
            try
            {
                var fullPath = _basePath + "\\Website" + path;
                fullPath = fullPath.Replace('/', '\\');
                using (StreamReader sr = File.OpenText(fullPath))
                {
                    var text = sr.ReadToEnd();
                    return text;
                }
            }
            catch
            {
                return "Could not find that page";
            }
        }
    }
}
