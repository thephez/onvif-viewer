//using Microsoft.Web.Services3.Security.Tokens;
using Microsoft.ServiceModel.Samples.CustomToken;
using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace SDS.WebServices.WsSecurity
{
    // These classes are used to insert the SOAP Security header containing User Token info

    public class PasswordDigestMessageInspector : IClientMessageInspector
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public double DeviceTimeOffset { get; set; }

        public PasswordDigestMessageInspector(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Create password digest message inspector to insert SOAP security
        /// header into the HTTP request (allows for time offset corrections 
        /// in case device time and client time aren't synchronized)
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="password">Password</param>
        /// <param name="deviceTimeOffset">Difference between client time and device time (seconds)</param>
        public PasswordDigestMessageInspector(string username, string password, double deviceTimeOffset)
        {
            this.Username = username;
            this.Password = password;
            this.DeviceTimeOffset = deviceTimeOffset;
        }

        #region IClientMessageInspector Members

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            Debug.Print(string.Format("PasswordDigestMessageInspector AfterReceiveReply: {0}...", reply.ToString().Substring(0, 100)));
            //throw new NotImplementedException();
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
        {
            // Use a custom security token class (from Microsoft.ServiceModel.Samples.CustomToken)
            UsernameToken token = new UsernameToken(new UsernameInfo(Username, Password), DeviceTimeOffset);

            // Serialize the token to XML
            XmlElement securityToken = token.GetXml(new XmlDocument());

            // Add header to the request
            MessageHeader securityHeader = MessageHeader.CreateHeader("Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", securityToken, mustUnderstand: true);
            request.Headers.Add(securityHeader);

            // complete
            return Convert.DBNull;
        }

        #endregion
    }

    public class PasswordDigestBehavior : IEndpointBehavior
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public double DeviceTimeOffset { get; set; }

        public PasswordDigestBehavior(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Create password digest behavior that allows for time offset corrections 
        /// in case device time and client time aren't synchronized.  
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="password">Password</param>
        /// <param name="deviceTimeOffset">Difference between client UTC time and device UTC time in seconds (i.e. (DeviceDateTime - (System.DateTime.UtcNow)).TotalSeconds</param>
        public PasswordDigestBehavior(string username, string password, double deviceTimeOffset)
        {
            this.Username = username;
            this.Password = password;
            this.DeviceTimeOffset = deviceTimeOffset;
        }

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            Debug.Print("PasswordDigestBehavior AddBindingParameters");
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new PasswordDigestMessageInspector(this.Username, this.Password, this.DeviceTimeOffset));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            throw new NotImplementedException();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            Debug.Print("PasswordDigestBehavior Validate");
        }

        #endregion
    }
}
