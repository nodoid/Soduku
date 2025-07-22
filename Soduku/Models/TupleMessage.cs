namespace Soduku.Models
{
    public class DoubleTupleMessage
    {
#nullable enable
        public Tuple<object, object>? Message { get; set; }
#nullable disable
        public string Sender { get; set; } = "";
    }

    public class TripleTupleMessage
    {
#nullable enable
        public Tuple<object, object, object>? Message { get; set; }
#nullable disable
        public string Sender { get; set; } = "";
    }

    public class QuadTupleMessage
    {
#nullable enable
        public Tuple<object, object, object, object>? Message { get; set; }
#nullable disable
        public string Sender { get; set; } = "";
    }
}
