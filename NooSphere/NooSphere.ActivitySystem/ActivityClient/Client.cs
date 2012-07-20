﻿/// <licence>
/// 
/// (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)
/// 
/// Pervasive Interaction Technology Laboratory (pIT lab)
/// IT University of Copenhagen
///
/// This library is free software; you can redistribute it and/or 
/// modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
/// as published by the Free Software Foundation. Check 
/// http://www.gnu.org/licenses/gpl.html for details.
/// 
/// </licence>

using System.ServiceModel;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Discovery;
using System.ServiceModel.Description;
using System.Net;
using System.IO;
using System.Web;
using System.ServiceModel.Web;

using NooSphere.ActivitySystem.Contracts;
using NooSphere.ActivitySystem.Contracts.NetEvents;
using NooSphere.Core.ActivityModel;
using NooSphere.Core.Devices;
using NooSphere.ActivitySystem.Events;
using NooSphere.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NooSphere.ActivitySystem.ActivityClient
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Client : NetEventHandler
    {
        #region Events
        public event ConnectionEstablishedHandler ConnectionEstablished = null;
        #endregion

        #region Private Members
        private Dictionary<Type, ServiceHost> callbackServices = new Dictionary<Type, ServiceHost>();
        #endregion

        #region Properties
        public string IP { get; set; }
        public string ClientName { get; set; }
        public string DeviceID { get; private set; }
        public string ServiceAddress { get; set; }
        public User CurrentUser { get; set; }
        #endregion

        #region Constructor
        public Client(string address)
        {
            Connect(address);
        }
        #endregion

        #region Private Methods
        private void TestConnection(string addr)
        {
            Console.WriteLine("BasicClient: Found running service at " + addr);
            ServiceAddress = addr;
            bool res = JsonConvert.DeserializeObject<bool>(RestHelper.Get(ServiceAddress));
            Console.WriteLine("BasicClient: Service active? -> " + res);

        }
        private void Connect(string address)
        {
            this.IP = NetHelper.GetIP(IPType.All);
            TestConnection(address);
        }
        private Type TypeFromEnum(EventType type)
        {
            switch (type)
            {
                case EventType.ActivityEvents:
                    return typeof(IActivityNetEvent);
                case EventType.ComEvents:
                    return typeof(IComNetEvent);
                case EventType.DeviceEvents:
                    return typeof(IDeviceNetEvent);
                case EventType.FileEvents:
                    return typeof(IFileNetEvent);
                case EventType.UserEvent:
                    return typeof(IUserEvent);
                default:
                    return null;
            }
        }
        private int StartCallbackService(Type service)
        {
            int port = NetHelper.FindPort();

            ServiceHost eventHandlerService = new ServiceHost(this);
            ServiceEndpoint se = eventHandlerService.AddServiceEndpoint(service, new WebHttpBinding(), GetUrl(this.IP, port, ""));
            se.Behaviors.Add(new WebHttpBehavior());
            try
            {
                eventHandlerService.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Basic Client: error launching callback service: " + ex.ToString());
                throw new ApplicationException(ex.ToString());
            }
            callbackServices.Add(service, eventHandlerService);
            return port;
        }
        private Uri GetUrl(string ip, int port, string relative)
        {
            return new Uri(string.Format("http://{0}:{1}/{2}", ip, port, relative));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Registers the current device with the activity client
        /// </summary>
        public void Register()
        {
            Device d = new Device();
            d.BaseAddress = NetHelper.GetIP(IPType.All);
            Register(d);
        }

        /// <summary>
        /// Register a given device with the activity client
        /// </summary>
        /// <param name="d">The device that needs to be registered with the activity client</param>
        public void Register(Device d)
        {
            DeviceID = JsonConvert.DeserializeObject<String>(RestHelper.Post(ServiceAddress + Url.devices, d));
            Console.WriteLine("BasicClient: Received device id: " + DeviceID);
        }

        /// <summary>
        /// Unregister a device from the activity client
        /// </summary>
        /// <param name="id">The id of the device that needs to be unregistered</param>
        public void Unregister(string id)
        {
            RestHelper.Delete(ServiceAddress + Url.devices, id);
        }

        /// <summary>
        /// Subscribe the activity client to an activity manager event
        /// </summary>
        /// <param name="type">The type of event for which the client needs to subscribe</param>
        public void Subscribe(EventType type)
        {
            int port = StartCallbackService(TypeFromEnum(type));
            var subscription = new
            {
                id = DeviceID,
                port = port,
                type = type
            };
            RestHelper.Post(ServiceAddress + Url.subscribers, subscription);
        }

        /// <summary>
        /// Unsubscribe the activity client from an activity client event
        /// </summary>
        /// <param name="type">The type of event to which the client has to unsubscribe</param>
        public void UnSubscribe(EventType type)
        {
            var unSubscription = new
            {
                id = DeviceID,
                type = type
            };
            RestHelper.Delete(ServiceAddress + Url.subscribers, unSubscription);
            Type t = TypeFromEnum(type);
            callbackServices[t].Close();
            callbackServices.Remove(t);
        }

        /// <summary>
        /// Unsubscribe the client for all events
        /// </summary>
        public void UnSubscribeAll()
        {
            foreach (EventType ev in Enum.GetValues(typeof(EventType)))
                UnSubscribe(ev);
        }

        /// <summary>
        /// Sends an "add activity" request to the activity manager
        /// </summary>
        /// <param name="act">The activity that needs to be included in the request</param>
        public void AddActivity(Activity act)
        {
            RestHelper.Post(ServiceAddress + Url.activities, act);
        }

        /// <summary>
        /// Sends a "Remove activity" request to the activity manager
        /// </summary>
        /// <param name="act">The id (of the activity) that needs to be included in the request</param>
        public void RemoveActivity(Guid id)
        {
            RestHelper.Delete(ServiceAddress + Url.activities, id);
        }

        /// <summary>
        /// Sends an "Update activity" request to the activity manager
        /// </summary>
        /// <param name="act">The activity that needs to be included in the request</param>
        public void UpdateActivity(Activity act)
        {
            RestHelper.Put(ServiceAddress + Url.activities, act);
        }

        /// <summary>
        /// Sends a "Get Activities" request to the activity manager
        /// </summary>
        /// <returns>A list of retrieved activities</returns>
        public List<Activity> GetActivities()
        {
            return JsonConvert.DeserializeObject<List<Activity>>(RestHelper.Get(ServiceAddress + Url.activities));
        }

        /// <summary>
        /// Sends a "Get Activity" request to the activity manager
        /// </summary>
        /// <param name="id">The id (of the activity) that needs to be included in the request</param>
        /// <returns></returns>
        public Activity GetActivity(string id)
        {
            return JsonConvert.DeserializeObject<Activity>(RestHelper.Get(ServiceAddress + Url.activities + "/" + id));
        }

        /// <summary>
        /// Sends a "Send Message" request to the activity manager
        /// </summary>
        /// <param name="msg">The message that needs to be included in the request</param>
        public void SendMessage(string msg)
        {
            var message = new
            {
                id = DeviceID,
                message = msg
            };
            RestHelper.Post(ServiceAddress + Url.messages, message);
        }

        /// <summary>
        /// Gets all users in the friendlist
        /// </summary>
        /// <returns>A list with all users in the friendlist</returns>
        public List<User> GetUsers()
        {
            return JsonConvert.DeserializeObject<List<User>>(RestHelper.Get(ServiceAddress + Url.users)); 
        }

        /// <summary>
        /// Request friendship with another user
        /// </summary>
        /// <param name="email">The email of the user that needs to be friended</param>
        public void RequestFriendShip(string email)
        {
            JsonConvert.DeserializeObject<List<User>>(RestHelper.Post(ServiceAddress + Url.users,email)); 
        }

        /// <summary>
        /// Removes a user from the friendlist
        /// </summary>
        /// <param name="friendId">The id of the friend that needs to be removed</param>
        public void RemoveFriend(Guid friendId)
        {
            JsonConvert.DeserializeObject<List<User>>(RestHelper.Delete(ServiceAddress + Url.users, friendId)); 
        }

        /// <summary>
        /// Sends a response on a friend request from another user
        /// </summary>
        /// <param name="friendId">The id of the friend that is requesting friendship</param>
        /// <param name="approval">Bool that indicates if the friendship was approved</param>
        public void RespondToFriendRequest(Guid friendId, bool approval)
        {
            var response = new
            {
                friendId = friendId,
                approval = approval
            };
            RestHelper.Put(ServiceAddress + Url.users, response);
        }

        #endregion

        #region Internal Event Handlers
        protected void OnConnectionEstablishedEvent(EventArgs e)
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(this, e);
        }
        #endregion

        #region Event Handlers
        private void discoveryClient_FindProgressChanged(object sender, FindProgressChangedEventArgs e)
        {
            Connect(e.EndpointDiscoveryMetadata.Address.ToString());
        }
        #endregion

    }
    public enum Url
    {
        activities,
        devices,
        subscribers,
        messages,
        users
    }
}
