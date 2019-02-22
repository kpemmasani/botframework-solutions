using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using PhoneSkillTest.Flow.Utterances;
using static Luis.PhoneLuis;

namespace PhoneSkillTest.TestDouble
{
    public class MockPhoneLuisRecognizer : ITelemetryRecognizer
    {
        private OutgoingCallUtterances phoneUtterancesManager;

        public MockPhoneLuisRecognizer()
        {
            this.phoneUtterancesManager = new OutgoingCallUtterances();
        }

        public bool LogPersonalInformation { get; set; } = false;

        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var intent = new PhoneLuis
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            intent.Intents.Add(Intent.None, new IntentScore() { Score = 1.0 });

            var test = intent as object;
            var mockResult = (T)test;

            return Task.FromResult(mockResult);
        }

        public Task<T> RecognizeAsync<T>(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }

        public Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}