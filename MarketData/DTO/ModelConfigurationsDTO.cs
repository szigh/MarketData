using MarketData.Grpc;

namespace MarketData.DTO
{
    public class ModelConfigurationsDTO
    {
        public record UpdateRandomAdditiveWalkRestRequest(
            List<WalkStepDto> WalkSteps
        );

        public record WalkStepDto(
            double Probability,
            double StepValue
        );

        public record RandomAdditiveWalkConfigDto(
            List<WalkStepDto> WalkSteps
        );

        public record InstrumentConfigurationsResponseDto(
            string InstrumentName,
            string ActiveModel,
            RandomMultiplicativeConfigDto? RandomMultiplicative,
            MeanRevertingConfigDto? MeanReverting,
            bool FlatConfigured,
            RandomAdditiveWalkConfigDto? RandomAdditiveWalk,
            int TickIntervalMs
        );

        public record RandomMultiplicativeConfigDto(
            double StandardDeviation,
            double Mean
        );

        public record MeanRevertingConfigDto(
            double Mean,
            double Kappa,
            double Sigma,
            double Dt
        );

        public record SwitchModelRequestDto(
            string ModelType
        );

        public record SwitchModelResponseDto(
            string Message,
            string PreviousModel,
            string NewModel
        );

        public record UpdateRandomMultiplicativeRequestDto(
            double StandardDeviation,
            double Mean
        );

        public record UpdateConfigResponseDto(
            string Message,
            bool Success
        );

        public record UpdateMeanRevertingRequestDto(
            double Mean,
            double Kappa,
            double Sigma,
            double Dt
        );

        public record WalkStepRestDto(
            double Probability,
            double StepValue
        );

    }
}
