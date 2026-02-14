namespace MarketData.Models
{
    public class Price
    {
        public int Id { get; set; }
        public string? Instrument { get; set; }
        public decimal Value { get; set; } 
        public DateTime Timestamp { get; set; }  

        public override string ToString() { return $"[{Timestamp:O}] {Instrument}: {Value}"; }
    }
}
