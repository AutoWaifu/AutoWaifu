using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using uhttpsharp;
using uhttpsharp.Handlers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace AutoWaifu2
{
    class StatusServer
    {
        public int Port { get; set; } = 4444;

        public string Content { get; set; }

        public StatusServer(ProcessingStatus status)
        {
            this.workingStatus = status;
        }

        ProcessingStatus workingStatus;
        HttpServer server;
        ILogger logger = Log.ForContext<StatusServer>();

        public void Start()
        {
            if (server != null)
                return;

            server = new HttpServer(new HttpRequestProvider());

            server.Use(new TcpListenerAdapter(new TcpListener(IPAddress.Any, Port)));

            server.Use(async (context, next) =>
            {
                try
                {
                    context.Response = new HttpResponse(HttpResponseCode.Ok, GenerateHtmlResponse(), true);
                }
                catch (Exception e)
                {
                    string errorMessage = "An exception occurred while generating status response: " + e.ToString();

                    this.logger.Warning(errorMessage);
                    context.Response = new HttpResponse(HttpResponseCode.InternalServerError, errorMessage, true);
                }

                await Task.FromResult(0);

                //return next();
            });

            server.Start();
        }

        public void Stop()
        {
            if (server == null)
                return;

            server.Dispose();
            server = null;
        }

        int FindLineNumber(string inString, int stringIndex)
        {
            return inString.Substring(0, stringIndex).Count(c => c == '\n');
        }

        string ConvertToHtmlString(string text)
        {
            string html = text;
            html = html.Replace("\n", "<br>");

            string tabHtml = "<span style='display:inline-block;width:2em;'></span>";

            html = html.Replace("  ", tabHtml);
            html = html.Replace("\t", tabHtml);

            return html;
        }

        bool IsRealPrimitive(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string);
        }

        string GenerateHtmlResponse()
        {
            if (Content != null)
                return Content;

            if (this.workingStatus == null)
                throw new Exception();

            string htmlTemplate = Embedded.GetTextFile("Web.index.html");
            if (htmlTemplate == null)
                throw new FileNotFoundException("Couldn't get the embedded HTML for the status server response");

            if (this.workingStatus == null)
                throw new InvalidOperationException("");


            this.workingStatus.RefreshImplicits();


            string dataJson = JsonConvert.SerializeObject(this.workingStatus, Formatting.Indented);

            string responseHtml = Regex.Replace(htmlTemplate, @"\<\%(.*)\%\>", (match) =>
            {
                string key = match.Groups[1].Value?.Trim();

                string newValue;

                if (string.IsNullOrEmpty(key) || key == "=")
                {
                    newValue = dataJson;
                }
                else
                {
                    var modelProperty = typeof(ProcessingStatus).GetProperty(key);
                    if (modelProperty == null)
                    {
                        int lineNumber = FindLineNumber(htmlTemplate, match.Index);
                        logger.Warning("Status HTML template references property key '{PropertyKey}' at line {LineNumber} but that property could not be found on " + nameof(ProcessingStatus), lineNumber, key);
                        newValue = string.Empty;
                    }
                    else
                    {
                        var propertyValue = modelProperty.GetValue(this.workingStatus);

                        if (propertyValue == null)
                        {
                            newValue = "null";
                        }
                        else
                        {
                            if (IsRealPrimitive(propertyValue.GetType()))
                            {
                                newValue = propertyValue.ToString();
                                if (!modelProperty.Name.ToLower().Contains("html"))
                                    newValue = ConvertToHtmlString(newValue);
                            }
                            else
                            {
                                newValue = JsonConvert.SerializeObject(propertyValue);
                            }
                        }
                    }
                }

                return newValue;
            });

            return responseHtml;
        }
    }
}
