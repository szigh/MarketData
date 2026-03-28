using MarketData.Models;
using MarketData.PriceSimulator;
using System.Text.Json;
using static MarketData.DTO.ModelConfigurationsDTO;

namespace MarketData.DTO
{
    public class InstrumentsDTO
    {

        public static InstrumentConfigurationsResponseDto MapToDto(Instrument instrument)
        {
            RandomMultiplicativeConfigDto? randomMultiplicative = null;
            if (instrument.RandomMultiplicativeConfig != null)
            {
                randomMultiplicative = new RandomMultiplicativeConfigDto(
                    instrument.RandomMultiplicativeConfig.StandardDeviation,
                    instrument.RandomMultiplicativeConfig.Mean
                );
            }

            MeanRevertingConfigDto? meanReverting = null;
            if (instrument.MeanRevertingConfig != null)
            {
                meanReverting = new MeanRevertingConfigDto(
                    instrument.MeanRevertingConfig.Mean,
                    instrument.MeanRevertingConfig.Kappa,
                    instrument.MeanRevertingConfig.Sigma,
                    instrument.MeanRevertingConfig.Dt
                );
            }

            RandomAdditiveWalkConfigDto? randomAdditiveWalk = null;
            if (instrument.RandomAdditiveWalkConfig != null)
            {
                var walkSteps = JsonSerializer.Deserialize<List<RandomWalkStep>>(
                    instrument.RandomAdditiveWalkConfig.WalkStepsJson
                ) ?? new List<RandomWalkStep>();

                randomAdditiveWalk = new RandomAdditiveWalkConfigDto(
                    walkSteps.Select(s => new WalkStepDto(
                        s.Probability,
                        s.Value
                    )).ToList()
                );
            }

            return new InstrumentConfigurationsResponseDto(
                InstrumentName: instrument.Name,
                ActiveModel: instrument.ModelType ?? "None",
                RandomMultiplicative: randomMultiplicative,
                MeanReverting: meanReverting,
                FlatConfigured: instrument.FlatConfig != null,
                RandomAdditiveWalk: randomAdditiveWalk,
                TickIntervalMs: instrument.TickIntervalMillieconds
            );
        }

        public record CreateInstrumentRequestDto(
            string InstrumentName,
            int TickIntervalMs,
            decimal InitialPriceValue,
            DateTime InitialPriceTimestamp,
            string? ModelType
        );

        public record CreateInstrumentResponseDto(
            string Message,
            bool Added,
            string InstrumentName,
            string ActiveModel,
            int TickIntervalMs
        );

        public record UpdateTickIntervalRequestDto(
            int TickIntervalMs
        );

        public record UpdateInstrumentResponseDto(
            string Message,
            bool Success
        );

        public record RemoveInstrumentResponseDto(
            string Message,
            bool Removed
        );

    }
}
