namespace MarketData.DTO
{
    public class ModelDTO
    {
        public record SupportedModelsResponseDto(
            IReadOnlyList<string> SupportedModels
        );
    }
}
