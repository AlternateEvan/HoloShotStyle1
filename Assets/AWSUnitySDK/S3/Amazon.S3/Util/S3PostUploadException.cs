﻿/*
 * Copyright 2014-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 *
 * Licensed under the AWS Mobile SDK for Unity Developer Preview License Agreement (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located in the "license" file accompanying this file.
 * See the License for the specific language governing permissions and limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Xml.Serialization;

namespace Amazon.S3.Util
{
    /// <summary>
    /// An exception detailing a failed HTTP POST UploadAndPublishAssetBundle atempt to Amazon S3.
    /// </summary>
    public class S3PostUploadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of S3PostUploadException with a specified error message
        /// </summary>
        /// <param name="message">The error message</param>
        public S3PostUploadException(string message) : base(message) {}
        
        /// <summary>
        /// Initializes a new instance of S3PostUploadException with a specified error code and error message
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="message">The error message</param>
        public S3PostUploadException(string errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// The error code returned by S3
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// The S3 request id
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// The S3 host id
        /// </summary>
        public string HostId { get; set; }

        /// <summary>
        /// The HTTP error status code returned by S3
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Additional information about the error
        /// </summary>
        /// <remarks>
        /// Some errors are accompanied by more specific information, which vary from error to error
        /// </remarks>
        public IDictionary<string, string> ExtraFields { get; set; }

        /// <summary>
        /// Parse an S3 Error response and create an S3PostUploadException
        /// </summary>
        /// <param name="response">The response from S3</param>
        /// <returns>An S3PostUploadException with the information from the response</returns>
        public static S3PostUploadException FromResponse(HttpWebResponse response)
        {
            var serializer = new XmlSerializer(typeof(S3PostUploadError));

            S3PostUploadError err = null;

            try
            {
                err = serializer.Deserialize(response.GetResponseStream()) as S3PostUploadError;
            }
            catch
            {
                return new S3PostUploadException("Unknown", "Unknown error");
            }

            var ex = new S3PostUploadException(err.ErrorCode, err.ErrorMessage)
            {
                RequestId = err.RequestId,
                HostId = err.HostId,
            };

            ex.StatusCode = response.StatusCode;
            ex.ExtraFields = new Dictionary<string, string>();
            if (err.elements != null)
            {
                foreach (var f in err.elements)
                {
                    ex.ExtraFields.Add(f.LocalName, f.InnerText);
                }
            }

            return ex;
        }
    }

    /// <summary>
    /// Class for unmarshalling response XML
    /// </summary>
    [XmlRoot("Error")]
    public class S3PostUploadError
    {
        [XmlElement("Code")]
        public string ErrorCode { get; set; }

        [XmlElement("Message")]
        public string ErrorMessage { get; set; }

        [XmlElement("RequestId")]
        public string RequestId { get; set; }

        [XmlElement("HostId")]
        public string HostId { get; set; }

        [XmlAnyElement()]
        public XmlElement[] elements { get; set; }
    }
}
