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
                context.Response.ContentType = contentType;

                string stringToRender = null;
                if (contentType == "text/json")
                {
                    stringToRender = RelayJsonRequest(context).Result;
                    return context.Response.WriteAsync(stringToRender);
                }
                else if (contentType.Contains("text"))
                {
                    stringToRender = GetFileString(path);
                    return context.Response.WriteAsync(stringToRender);
                }
                else
                {
                    byte[] file = GetFileBytes(path);
                    return context.Response.WriteAsync(file);
                }
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
                WriteErrorToConsole(ex, "Error communicating with local testing bot. Full error was:");
                return "{ Error: " + ex.Message + " }";
            }
        }

        private static void WriteErrorToConsole(Exception ex, string errorMessage)
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex);
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
                case "js": 
                case "html":
                case "htm": 
                case "css":
                case "json": return "text/" + extension;

                case "png": 
                case "jpg":
                case "gif": return "image/" + extension;

                case "woff": return "application/font-woff";
                default: return "application/" + extension;
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

        private string GetFileString(string path)
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
            catch (Exception ex)
            {
                WriteErrorToConsole(ex, "Error loading text file " + path);
                return "Could not find that page";
            }
        }

        private byte[] GetFileBytes(string path)
        {
            try
            {
                var fullPath = _basePath + "\\Website" + path;
                fullPath = fullPath.Replace('/', '\\');
                byte[] bytes = File.ReadAllBytes(fullPath);
                return bytes;
            }
            catch (Exception ex)
            {
                WriteErrorToConsole(ex, "Error loading binary file " + path);
                return null;
            }
        }
    }
}
