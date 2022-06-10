﻿// <copyright file="TranscriptionBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using MeetingTranscription.Helpers;
using MeetingTranscription.Models.Configuration;
using MeetingTranscription.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MeetingTranscription.Bots
{
    public class TranscriptionBot : TeamsActivityHandler
    {
        /// <summary>
        /// Helper instance to make graph calls.
        /// </summary>
        private readonly GraphHelper graphHelper;

        /// <summary>
        /// Stores the Azure configuration values.
        /// </summary>
        private readonly IOptions<AzureSettings> azureSettings;

        /// <summary>
        /// Store details of meeting transcript.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> transcriptsDictionary;

        /// <summary>
        /// Instance of card factory to create adaptive cards.
        /// </summary>
        private readonly ICardFactory cardFactory;

        /// <summary>
        /// Creates bot instance.
        /// </summary>
        /// <param name="azureSettings">Stores the Azure configuration values.</param>
        /// <param name="transcriptsDictionary">Store details of meeting transcript.</param>
        /// <param name="cardFactory">Instance of card factory to create adaptive cards.</param>
        public TranscriptionBot(IOptions<AzureSettings> azureSettings, ConcurrentDictionary<string, string> transcriptsDictionary, ICardFactory cardFactory)
        {
            this.transcriptsDictionary = transcriptsDictionary;
            this.azureSettings = azureSettings;
            graphHelper = new GraphHelper(azureSettings);
            this.cardFactory = cardFactory;
        }

        /// <summary>
        /// Activity handler for on message activity.
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyText = $"Echo: {turnContext.Activity.Text}";
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        /// <summary>
        /// Activity handler for meeting end event.
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnTeamsMeetingEndAsync(MeetingEndEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var meet = await TeamsInfo.GetMeetingInfoAsync(turnContext);

            var result = await graphHelper.GetMeetingTranscriptionsAsync(meet.Details.MsGraphResourceId);
            if (result != string.Empty)
            {
                transcriptsDictionary.AddOrUpdate(meet.Details.MsGraphResourceId, result, (key, newValue) => result);

                var attachment = this.cardFactory.CreateAdaptiveCardAttachement(new { MeetingId = meet.Details.MsGraphResourceId });
                await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
            }
            else
            {
                var attachment = this.cardFactory.CreateNotFoundCardAttachement();
                await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
            }
        }

        /// <summary>
        /// Activity handler for Task module fethc event.
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="taskModuleRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                var meetingId = JObject.FromObject(taskModuleRequest.Data)["meetingId"];

                return new TaskModuleResponse
                {
                    Task = new TaskModuleContinueResponse
                    {
                        Type = "continue",
                        Value = new TaskModuleTaskInfo()
                        {
                            Url = $"{this.azureSettings.Value.AppBaseUrl}/home?meetingId={meetingId}",
                            Height = 600,
                            Width = 600,
                            Title = "Upload file",
                        },
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return new TaskModuleResponse
                {
                    Task = new TaskModuleContinueResponse
                    {
                        Type = "continue",
                        Value = new TaskModuleTaskInfo()
                        {
                            Url = this.azureSettings.Value.AppBaseUrl + "/home",
                            Height = 350,
                            Width = 350,
                            Title = "Upload file",
                        },
                    }
                };
            }
        }
    }
}