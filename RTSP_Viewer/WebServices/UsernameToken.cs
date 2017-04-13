// From: https://blogs.msdn.microsoft.com/aszego/2010/06/24/usernametoken-profile-vs-wcf/
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.

/* THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED “AS IS” 
 * WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
 * PARTICULAR PURPOSE.We grant You a nonexclusive, royalty-free right to use 
 * and modify the Sample Code and to reproduce and distribute the object code 
 * form of the Sample Code, provided that. 
 * 
 * You agree: 
 *      (i) to not use Our name, logo, or trademarks to market Your software 
 *          product in which the Sample Code is embedded; 
 *      (ii) to include a valid copyright notice on Your software product in 
 *          which the Sample Code is embedded; and
 *      (iii) to indemnify, hold harmless, and defend Us and Our suppliers 
 *          from and against any claims or lawsuits, including attorneys’ fees,
 *          that arise or result from the use or distribution of the Sample Code.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.ServiceModel.Samples.CustomToken
{
    public class UsernameToken : SecurityToken
    {
        UsernameInfo _usernameInfo;
        ReadOnlyCollection<SecurityKey> _securityKeys;
        DateTime _created = DateTime.UtcNow; // DateTime.Now;
        DateTime _expiration = DateTime.Now + new TimeSpan(10, 0, 0);
        Guid _id = Guid.NewGuid();
        byte[] _nonce = new byte[16];

        public UsernameToken(UsernameInfo usernameInfo, string nonce, string created)
        {
            if (usernameInfo == null)
                throw new ArgumentNullException("usernameInfo");

            _usernameInfo = usernameInfo;

            if (nonce != null)
            {
                _nonce = Convert.FromBase64String(nonce);
            }

            if (created != null)
            {
                _created = DateTime.Parse(created);
            }

            // the user name token is not capable of any crypto
            _securityKeys = new ReadOnlyCollection<SecurityKey>(new List<SecurityKey>());
        }

        public UsernameToken(UsernameInfo usernameInfo) : this(usernameInfo, null, null) { }

        /// <summary>
        /// UsernameToken that allows for time offset corrections in case device time and client time
        /// aren't synchronized.  
        /// </summary>
        /// <param name="usernameInfo"></param>
        /// <param name="DeviceTimeOffset">Difference between device UTC time and client UTC time in seconds</param>
        public UsernameToken(UsernameInfo usernameInfo, double DeviceTimeOffset) : this(usernameInfo, null, null)
        {
            _created = DateTime.UtcNow + TimeSpan.FromSeconds(DeviceTimeOffset);
        }

        public UsernameInfo UsernameInfo { get { return _usernameInfo; } }

        public override ReadOnlyCollection<SecurityKey> SecurityKeys { get { return _securityKeys; } }

        public override DateTime ValidFrom { get { return _created; } }
        public override DateTime ValidTo { get { return _expiration; } }
        public override string Id { get { return _id.ToString(); } }

        public string GetPasswordDigestAsBase64()
        {
            // generate a cryptographically strong random value
            RandomNumberGenerator rndGenerator = new RNGCryptoServiceProvider();
            rndGenerator.GetBytes(_nonce);

            // get other operands to the right format
            byte[] time = Encoding.UTF8.GetBytes(GetCreatedAsString());
            byte[] pwd = Encoding.UTF8.GetBytes(_usernameInfo.Password);
            byte[] operand = new byte[_nonce.Length + time.Length + pwd.Length];
            Array.Copy(_nonce, operand, _nonce.Length);
            Array.Copy(time, 0, operand, _nonce.Length, time.Length);
            Array.Copy(pwd, 0, operand, _nonce.Length + time.Length, pwd.Length);

            // create the hash
            SHA1 sha1 = SHA1.Create();
            return Convert.ToBase64String(sha1.ComputeHash(operand));
        }

        public string GetNonceAsBase64()
        {
            return Convert.ToBase64String(_nonce);
        }

        public string GetCreatedAsString()
        {
            return XmlConvert.ToString(_created.ToUniversalTime(), "yyyy-MM-ddTHH:mm:ssZ");
        }

        public bool ValidateToken(string password)
        {
            byte[] pwd = Encoding.UTF8.GetBytes(password);
            byte[] createdBytes = Encoding.UTF8.GetBytes(GetCreatedAsString());
            byte[] operand = new byte[_nonce.Length + createdBytes.Length + pwd.Length];
            Array.Copy(_nonce, operand, _nonce.Length);
            Array.Copy(createdBytes, 0, operand, _nonce.Length, createdBytes.Length);
            Array.Copy(pwd, 0, operand, _nonce.Length + createdBytes.Length, pwd.Length);
            SHA1 sha1 = SHA1.Create();
            string trueDigest = Convert.ToBase64String(sha1.ComputeHash(operand));

            return String.Compare(trueDigest, _usernameInfo.Password) == 0;
        }

        public XmlElement GetXml(XmlDocument xml)
        {
            xml = new XmlDocument();

            XNamespace wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            XNamespace wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
            
            XDocument xDoc = new XDocument(
                new XElement(wsse + "UsernameToken",
                    // wsu:Id Security token
                    new XElement(wsse + "Username", _usernameInfo.Username),
                    new XElement(wsse + "Password", GetPasswordDigestAsBase64(),
                        new XAttribute("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest")),
                    new XElement(wsse + "Nonce", GetNonceAsBase64()),
                    new XElement(wsu + "Created", GetCreatedAsString()),
                    new XAttribute(XNamespace.Xmlns + "wsu", wsu),
                    new XAttribute(XNamespace.Xmlns + "wsse", wsse)
                    )
                );

            var doc = new XmlDocument();
            doc.LoadXml(xDoc.ToString());
            return doc.DocumentElement;
        }
    }
}
