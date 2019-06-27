// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AppCenter.Ingestion.Models;
using Newtonsoft.Json;
using System.Text;

namespace Microsoft.AppCenter.Crashes
{
    /// <summary>
    /// Error attachment log.
    /// </summary>
    [JsonObject("errorAttachment")]
    public partial class ErrorAttachmentLog : Log
    {
        private const string ContentTypePlainText = "text/plain";

        static ErrorAttachmentLog PlatformAttachmentWithText(string text, string fileName)
        {
            var data = Encoding.UTF8.GetBytes(text);
            return PlatformAttachmentWithBinary(data, fileName, ContentTypePlainText);
        }

        static ErrorAttachmentLog PlatformAttachmentWithBinary(byte[] data, string fileName, string contentType)
        {
            return new ErrorAttachmentLog()
            {
                Data = data,
                FileName = fileName,
                ContentType = contentType
            };
        }

        /// <summary>
        /// Gets or sets error attachment identifier.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public System.Guid Id { get; set; }

        /// <summary>
        /// Gets or sets error log identifier to attach this log to.
        /// </summary>
        [JsonProperty(PropertyName = "errorId")]
        public System.Guid ErrorId { get; set; }

        /// <summary>
        /// Gets or sets content type (text/plain for text).
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets data encoded as base 64.
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public byte[] Data { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public override void Validate()
        {
            base.Validate();
            if (ContentType == null)
            {
                throw new ValidationException(ValidationException.Rule.CannotBeNull, "ContentType");
            }
            if (Data == null)
            {
                throw new ValidationException(ValidationException.Rule.CannotBeNull, "Data");
            }
        }
    }
}
