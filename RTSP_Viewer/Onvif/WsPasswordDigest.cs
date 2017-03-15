using Microsoft.Web.Services3.Security.Tokens;
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

        public PasswordDigestMessageInspector(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        #region IClientMessageInspector Members

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            Debug.Print(string.Format("PasswordDigestMessageInspector AfterReceiveReply: {0}", reply.ToString()));
            //throw new NotImplementedException();
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
        {
            // Use the WSE 3.0 security token class
            UsernameToken token = new UsernameToken(this.Username, this.Password, PasswordOption.SendHashed);

            // Serialize the token to XML
            XmlElement securityToken = token.GetXml(new XmlDocument());

            //
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

        public PasswordDigestBehavior(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            Debug.Print("PasswordDigestBehavior AddBindingParameters");
            //throw new NotImplementedException();
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new PasswordDigestMessageInspector(this.Username, this.Password));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            throw new NotImplementedException();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            Debug.Print("PasswordDigestBehavior Validate");
            //throw new NotImplementedException();
        }

        #endregion
    }
}
