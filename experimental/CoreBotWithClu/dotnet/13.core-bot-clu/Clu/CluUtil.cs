// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.AI.Language.Conversations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Clu
{
    // Utility functions used to extract and transform data from CLU
    internal static class CluUtil
    {
        /// <summary>
        /// Returns a RecognizerResult from a conversations project response.
        /// 
        /// Intents: List of Intents with their confidence scores.
        /// Entities: has the object: { "entities" : [{entity1}, {entity2}] }
        /// Properties: Additional information returned by the service.
        /// 
        /// </summary>
        internal static RecognizerResult BuildRecognizerResultFromConversations(ConversationPrediction conversationPrediction, RecognizerResult recognizerResult)
        {
            recognizerResult.Intents = GetIntents(conversationPrediction);
            recognizerResult.Entities = ExtractEntitiesAndMetadata(conversationPrediction);
            return recognizerResult;
        }

        /// <summary>
        /// Returns a RecognizerResult from a question answering response received by an orchestration project.
        /// The recognizer result has similar format to the one returned by the QnAMaker Recognizer:
        /// 
        /// Intents: Indicates whether an answer has been found and contains the confidence score.
        /// Entities: has the object: { "answer" : ["topAnswer.answer"] }
        /// Properties: All answers returned by the Question Answering service.
        /// 
        /// </summary>
        internal static RecognizerResult BuildRecognizerResultFromQuestionAnswering(QuestionAnsweringTargetIntentResult qaResult, RecognizerResult recognizerResult)
        {
            var qnaAnswers = qaResult.Result.Answers;

            if (qnaAnswers.Count > 0)
            {
                var topAnswer = qnaAnswers[0];
                foreach (var answer in qnaAnswers)
                {
                    if (answer.ConfidenceScore > topAnswer.ConfidenceScore)
                    {
                        topAnswer = answer;
                    }
                }

                recognizerResult.Intents.Add(CluRecognizer.QuestionAnsweringMatchIntent, new IntentScore {Score = topAnswer.ConfidenceScore});

                var answerArray = new JArray {topAnswer.Answer};
                ObjectPath.SetPathValue(recognizerResult, "entities.answer", answerArray);

                recognizerResult.Properties["answers"] = qnaAnswers;
            }
            else
            {
                recognizerResult.Intents.Add("None", new IntentScore {Score = 1.0f});
            }

            return recognizerResult;
        }

        /// <summary>
        /// Returns a RecognizerResult from a luis response received by an orchestration project.
        /// The recognizer result has similar format to the one returned by the LUIS Recognizer:
        /// 
        /// Intents: Dictionary with (Intent, confidenceScores) pairs.
        /// Entities: The Luis entities Object, same as the one returned by the LUIS Recognizer.
        /// Properties: Sentiment result (if available) as well as the raw luis prediction result.
        /// </summary>
        internal static RecognizerResult BuildRecognizerResultFromLuis(LuisTargetIntentResult luisResult, RecognizerResult recognizerResult)
        {
            var luisPredictionObject = (JObject)JObject.Parse(luisResult.Result.ToString())["prediction"];
            recognizerResult.Intents = LuisUtil.GetIntents(luisPredictionObject);
            recognizerResult.Entities = LuisUtil.ExtractEntitiesAndMetadata(luisPredictionObject);

            LuisUtil.AddProperties(luisPredictionObject, recognizerResult);
            recognizerResult.Properties.Add("luisResult", luisPredictionObject);

            return recognizerResult;
        }

        private static IDictionary<string, IntentScore> GetIntents(ConversationPrediction prediction)
        {
            var result = new Dictionary<string, IntentScore>();
            foreach (var intent in prediction.Intents)
            {
                result.Add(intent.Category, new IntentScore {Score = intent.ConfidenceScore});
            }

            return result;
        }

        private static JObject ExtractEntitiesAndMetadata(ConversationPrediction prediction)
        {
            var entities = prediction.Entities;
            var entityObject = JsonConvert.SerializeObject(entities);
            var jsonArray = JArray.Parse(entityObject);
            var returnedObject = new JObject {{"entities", jsonArray}};

            return returnedObject;
        }

        internal static void AddProperties(AnalyzeConversationResult conversationResult, RecognizerResult result)
        {
            var topIntent = conversationResult.Prediction.TopIntent;
            var projectKind = conversationResult.Prediction.ProjectKind;
            var detectedLanguage = conversationResult.DetectedLanguage;

            result.Properties.Add("projectKind", projectKind.ToString());

            if (topIntent != null)
            {
                result.Properties.Add("topIntent", topIntent);
            }

            if (detectedLanguage != null)
            {
                result.Properties.Add("detectedLanguage", detectedLanguage);
            }

            if (projectKind == ProjectKind.Workflow)
            {
                var prediction = (OrchestratorPrediction)conversationResult.Prediction;
                var targetProject = prediction.Intents[prediction.TopIntent];

                // temporarily renamed until next release of CLU SDK
                // var targetProjectKind = targetProject.TargetKind
                var targetProjectKind = targetProject.GetType();
                result.Properties.Add("targetIntentKind", targetProjectKind);
            }
        }
    }
}
