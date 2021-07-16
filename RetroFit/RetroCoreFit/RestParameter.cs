using System.Threading;

namespace RetroCoreFit
{
    public struct RestParameter {

        public RestAttribute Type { get; set; }

        public object Value { get; set; }

        public static RestParameter Cancel(CancellationToken token)
        {
            return new RestParameter { 
                Type = new CancelAttribute(),
                Value = token
            };
        }

        public static RestParameter Query(string name, object v = null)
        {
            return new RestParameter {
                Type = new QueryAttribute(name),
                Value = v
            };
        }

        public static RestParameter Form(string name, object v = null)
        {
            return new RestParameter
            {
                Type = new FormAttribute(name),
                Value = v
            };
        }

        public static RestParameter Path(string name, object v = null)
        {
            return new RestParameter
            {
                Type = new PathAttribute(name),
                Value = v
            };
        }
        public static RestParameter Cookie(string name, object v = null)
        {
            return new RestParameter
            {
                Type = new CookieAttribute(name),
                Value = v
            };
        }

        public static RestParameter Body(object v)
        {
            return new RestParameter { 
                Type = new BodyAttribute(),
                Value = v
            };
        }

        public static RestParameter Header(string name, object v = null)
        {
            return new RestParameter
            {
                Type = new HeaderAttribute(name),
                Value = v
            };
        }


        public static RestParameter Multipart(string name, object v = null)
        {
            return new RestParameter
            {
                Type = new MultipartAttribute(name),
                Value = v
            };
        }

        public static RestParameter MultipartFile(string name, string fileName, object v = null)
        {
            return new RestParameter
            {
                Type = new MultipartFileAttribute(name) { 
                    FileName = fileName
                },
                Value = v
            };
        }
    }




}
