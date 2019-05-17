﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Bots;
using PhoneSkill.Common;
using PhoneSkill.Dialogs.Main;
using PhoneSkill.Dialogs.OutgoingCall;
using PhoneSkill.Models;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.ServiceClients;
using PhoneSkill.Services;
using PhoneSkillTest.TestDouble;

namespace PhoneSkillTest.Flow
{
    public class PhoneSkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public EndpointService EndpointService { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public IServiceManager ServiceManager { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize mock service manager
            ServiceManager = new FakeServiceManager();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = "Microsoft", Provider = "Microsoft" }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, ITelemetryRecognizer>
                            {
                                { "general", new MockGeneralLuisRecognizer() },
                                { "phone", new MockPhoneLuisRecognizer() },
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                new string[] { "en" },
                new PhoneMainResponses(),
                new OutgoingCallResponses());
            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<OutgoingCallDialog>();
            Services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<PhoneSkillState>(nameof(PhoneSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new PhoneSkillState());
                state.SourceOfContacts = ContactSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var eventActivity = activity.AsEventActivity();
                Assert.AreEqual(eventActivity.Name, "tokens/request");
            };
        }

        protected Activity GetAuthResponse()
        {
            var providerTokenResponse = new ProviderTokenResponse
            {
                TokenResponse = new TokenResponse(token: "test"),
                AuthenticationProvider = OAuthProvider.AzureAD
            };
            return new Activity(ActivityTypes.Event, name: "tokens/response", value: providerTokenResponse);
        }

        protected Action<IActivity> Message(string templateId, StringDictionary tokens = null)
        {
            return activity =>
            {
                Assert.AreEqual("message", activity.Type);
                var messageActivity = activity.AsMessageActivity();

                // Work around a bug in ParseReplies.
                if (tokens == null)
                {
                    tokens = new StringDictionary();
                }

                var expectedTexts = ParseReplies(templateId, tokens);
                var actualText = messageActivity.Text;
                CollectionAssert.Contains(expectedTexts, actualText, $"Expected one of: {expectedTexts.ToPrettyString()}\nActual: {actualText}\n");
            };
        }
    }
}
