﻿using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;
using MyCouch.Extensions;
using MyCouch.Serialization;

namespace MyCouch.Responses.Materializers
{
    public class EntityResponseMaterializer
    {
        protected readonly ISerializer Serializer;

        public EntityResponseMaterializer(ISerializer serializer)
        {
            Ensure.That(serializer, "serializer").IsNotNull();

            Serializer = serializer;
        }

        public virtual void Materialize<T>(EntityResponse<T> response, HttpResponseMessage httpResponse) where T : class
        {
            SetContent(response, httpResponse);
        }

        protected virtual async void SetContent<T>(EntityResponse<T> response, HttpResponseMessage httpResponse) where T : class
        {
            using (var content = await httpResponse.Content.ReadAsStreamAsync().ForAwait())
            {
                if (response.RequestMethod == HttpMethod.Get)
                    response.Content = Serializer.DeserializeCopied<T>(content);

                Serializer.Populate(response, content);
                SetMissingIdFromRequestUri(response, httpResponse.RequestMessage);
                SetMissingRevFromRequestHeaders(response, httpResponse.Headers);
            }
        }

        protected virtual void SetMissingIdFromRequestUri<T>(EntityResponse<T> response, HttpRequestMessage request) where T : class
        {
            if (string.IsNullOrWhiteSpace(response.Id) && request.Method != HttpMethod.Post)
                response.Id = request.GetUriSegmentByRightOffset();
        }

        protected virtual void SetMissingRevFromRequestHeaders<T>(EntityResponse<T> response, HttpResponseHeaders responseHeaders) where T : class
        {
            if (string.IsNullOrWhiteSpace(response.Rev))
                response.Rev = responseHeaders.GetETag();
        }
    }
}