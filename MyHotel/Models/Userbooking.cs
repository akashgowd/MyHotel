namespace MyHotel.Models
{
    public class Userbooking
    {
        public Roombooking roombooking { get; set; }
        public Payment payment { get; set; }
        public Customer customer { get; set; }
        public Room room { get; set; }
    }
}
