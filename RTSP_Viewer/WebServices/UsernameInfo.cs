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

namespace Microsoft.ServiceModel.Samples.CustomToken
{
    public class UsernameInfo
    {
        string _userName;
        string _password;

        public UsernameInfo(string userName, string password)
        {
            this._userName = userName;
            this._password = password;
        }

        public string Username { get { return _userName; } }
        public string Password { get { return _password; } }
    }
}
