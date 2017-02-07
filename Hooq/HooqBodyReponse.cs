using System.Collections.Generic;

namespace Hooq
{
    public class HooqBodyReponse<T>
    {
        public T Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Querystrings { get; set; }
    }
}