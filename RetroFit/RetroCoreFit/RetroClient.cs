using System;
using System.Net.Http;

namespace RetroCoreFit
{

    public class RetroClient
    {

        public static T Create<T, TBase>(Uri baseUrl, HttpClient client = null)
            where T:class
        {
            return InterfaceBuilder.Instance.Build<T>(baseUrl, client, typeof(TBase));
        }

    }




}
