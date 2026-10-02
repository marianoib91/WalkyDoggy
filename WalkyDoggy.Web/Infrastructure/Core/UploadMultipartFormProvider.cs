using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    public class UploadMultipartFormProvider : MultipartFormDataStreamProvider
    {
        public UploadMultipartFormProvider(string rutaRaiz) : base(rutaRaiz) { }

        public override string GetLocalFileName(HttpContentHeaders encabezados)
        {
            if (encabezados != null &&
                encabezados.ContentDisposition != null)
            {
                return encabezados
                    .ContentDisposition
                    .FileName.TrimEnd('"').TrimStart('"');
            }

            return base.GetLocalFileName(encabezados);
        }
    }
}