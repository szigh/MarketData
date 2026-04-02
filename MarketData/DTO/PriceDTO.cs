namespace MarketData.DTO
{
    public class PriceDTO
    {

        public record PriceDto(
            string Instrument,
            decimal Value,
            DateTime Timestamp
        );

        public record HistoricalPriceDto(
            string Instrument,
            decimal Value,
            DateTime Timestamp
        );

        public record HistoricalPricesResponseDto(
            string Instrument,
            DateTime Start,
            DateTime End,
            List<HistoricalPriceDto> Prices
        );

    }
}
