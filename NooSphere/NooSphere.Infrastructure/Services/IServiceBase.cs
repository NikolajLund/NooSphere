﻿using System.ServiceModel;
using System.ServiceModel.Web;


namespace NooSphere.Infrastructure.Services
{
    [ServiceContract]
    public interface IServiceBase
    {
        [OperationContract]
        [WebGet( ResponseFormat = WebMessageFormat.Json, UriTemplate = "" )]
        bool Alive();

        [OperationContract]
        [WebInvoke( ResponseFormat = WebMessageFormat.Json, UriTemplate = "ServiceDown" )]
        void ServiceDown();
    }

    public enum Status
    {
        ServiceDown
    }
}