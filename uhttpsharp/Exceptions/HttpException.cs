using System;

namespace uhttpsharp.Exceptions
{
    public class HttpException : Exception
    {
        private readonly HttpResponseCode _responseCode;

        public HttpResponseCode ResponseCode
        {
            get { return _responseCode; }
        }

        public HttpException(HttpResponseCode responseCode)
        {
            _responseCode = responseCode;
        }
        public HttpException(HttpResponseCode responseCode, string message) : base(message)
        {
            _responseCode = responseCode;
        }
    }
}
