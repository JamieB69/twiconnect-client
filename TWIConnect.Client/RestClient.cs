﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using TWIConnect.Client.Utilities;
using System.Net.Security;

namespace TWIConnect.Client
{
    internal class RestClient
    {
        internal RestClient(Domain.Configuration configuration)
        {
            Configuration = configuration;
        }

        private Domain.Configuration Configuration {get;set;}

        private WebRequest CreateRequest(string url, string method = Constants.Protocol.MethodPost, string contentType = Constants.Protocol.ContentTypeForm)
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback ( delegate { return true; } );

            WebRequest request = WebRequest.Create(url);
            request.Method = method;
            request.ContentType = contentType;
            return request;
        }

        private string PostRequest(string url, string content)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                System.Diagnostics.Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
                Utilities.Logger.Log(NLog.LogLevel.Trace, Resources.Messages.StartClientRequest, url, content);

                string payLoad = Constants.Protocol.FormFieldContent + "=" + content;
                byte[] payloadBin = Encoding.UTF8.GetBytes(payLoad);

                WebRequest request = this.CreateRequest(url);
                
                request.GetRequestStream().Write(payloadBin, 0, payloadBin.Length);

                request.GetResponse().GetResponseStream().CopyTo(memoryStream);

                string response = Encoding.UTF8.GetString(memoryStream.ToArray());
                Utilities.Logger.Log(NLog.LogLevel.Trace, Resources.Messages.EndOfExecution, "RestClient.PostRequest", Logger.GetTimeElapsed(stopWatch));

                return response;
            }
        }

        private T ParseResponse<T>(string response)
        {
            try
            {
                T configurationResponse = response.Deserialize<T>();
                return configurationResponse;
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(NLog.LogLevel.Error, Resources.Messages.FailureDeserializingResponse, response ?? string.Empty);
                throw ex;
            }
        }

        internal Domain.ConfigurationResponse GetRemoteConfiguration(Domain.ConfigurationRequest configurationRequest)
        {
            string response = this.PostRequest(this.Configuration.UrlPostFile, configurationRequest.Serialize());
            return ParseResponse<Domain.ConfigurationResponse>(response);
        }

        internal Domain.ConfigurationResponse UploadFile(Domain.PostFileRequest fileRequest)
        {
            string response = string.Empty;

            try
            {
                string content = fileRequest.Serialize();
                System.Diagnostics.Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
                Utilities.Logger.Log(NLog.LogLevel.Trace, Resources.Messages.StartUploadFile, this.Configuration.UrlPostFile, content);
                response = this.PostRequest(this.Configuration.UrlPostFile, content);
                Utilities.Logger.Log(NLog.LogLevel.Trace, Resources.Messages.EndOfExecution, "RestClient.UploadFile", Logger.GetTimeElapsed(stopWatch));
                Utilities.Logger.Log(NLog.LogLevel.Trace, "RestClient.UploadFile Response: " + response);
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(NLog.LogLevel.Error, Resources.Messages.FailurePostingFile, fileRequest.Name);
                throw ex;
            }

            return this.ParseResponse<Domain.ConfigurationResponse>(response);
        }

        internal Domain.ConfigurationResponse SelectFile(Domain.SelectFileRequest request)
        {
            string response = this.PostRequest(this.Configuration.UrlPostFile, request.Serialize());
            return ParseResponse<Domain.ConfigurationResponse>(response);
        }

    }
}
