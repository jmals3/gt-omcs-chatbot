using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace GT_Chatbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public DataTable BotResponses = new DataTable();
        public DataTable BotSynonyms = new DataTable();
        public DataTable BotQuestionWords = new DataTable();
        public string lastEntity = string.Empty;
        public bool questionPrompt = false;

        public MessagesController()
        {
            string connString = "Data Source=gtomcsbot.database.windows.net;Initial Catalog=BotResponses;Integrated Security=False;User ID=jmals3;Password=OMCSbot1;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            SqlConnection botConnection = new SqlConnection(connString);
            SqlCommand botCommandResponses = new SqlCommand("SELECT * FROM BotResponse", botConnection);
            SqlCommand botCommandSynonyms = new SqlCommand("SELECT * FROM Synonyms", botConnection);
            SqlCommand botCommandQuestions = new SqlCommand("SELECT * FROM QuestionWords", botConnection);
            try
            {
                botConnection.Open();
                SqlDataAdapter botAdapterResponses = new SqlDataAdapter(botCommandResponses);
                SqlDataAdapter botAdapterSynonyms = new SqlDataAdapter(botCommandSynonyms);
                SqlDataAdapter botAdapterQuestions = new SqlDataAdapter(botCommandQuestions);
                botAdapterResponses.Fill(BotResponses);
                botAdapterSynonyms.Fill(BotSynonyms);
                botAdapterQuestions.Fill(BotQuestionWords);
                botConnection.Close();
                botAdapterResponses.Dispose();
                botAdapterSynonyms.Dispose();
                System.Diagnostics.Debug.WriteLine("Load Successful.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Did not load DBs. " + e);
            }
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == ActivityTypes.Message)
            {
                string input = activity.Text.ToLower();
                
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                // Get the stateClient to get/set Bot Data
                StateClient _stateClient = activity.GetStateClient();
                BotData _botData = _stateClient.BotState.GetUserData(activity.ChannelId, activity.Conversation.Id);

                //Retrieve persisting variables from bot data
                try
                {
                    lastEntity = _botData.GetProperty<string>("LastEntity");
                    questionPrompt = _botData.GetProperty<bool>("QuestionPrompt");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }

                //check if the input was a question
                bool ques = ResponseGenerator.checkIfQuestion(BotQuestionWords, input);
                if (ques)
                {
                    questionPrompt = true;
                }
                //replace any synonyms from the synonyms table with their entity counterparts
                input = ResponseGenerator.checkSynonyms(BotSynonyms, input);

                //Replace "it" pronoun with entity from last input
                if (Regex.IsMatch(input, @" it\W"))
                {
                    input = Regex.Replace(input, @"it\W?", lastEntity);
                }

                //send the updated input to LUIS (this code was borrowed from reference #2 in my paper)
                Luis.GTLUIS gtLuis = await Luis.LUISGTOMCSClient.ParseUserInput(input);
                string intent = string.Empty;
                string entity = string.Empty;
                string strRet = string.Empty;

                

                //try to retrieve the highest scoring intent and entity from LUIS
                try
                {
                    intent = gtLuis.topScoringIntent.intent;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
                try
                {
                    entity = gtLuis.entities[0].entity;
                    if (ResponseGenerator.checkEntity(BotSynonyms, entity) == false)
                    {
                        entity = ResponseGenerator.checkForEntity(BotSynonyms, input);
                    }                    
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    entity = ResponseGenerator.checkForEntity(BotSynonyms, input);
                }

                try
                {
                    strRet = ResponseGenerator.getResponse(BotResponses, intent, entity);
                    System.Diagnostics.Debug.WriteLine(strRet);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("x" + e.ToString());
                }

                if (strRet == "N/A")
                {
                    System.Diagnostics.Debug.WriteLine("x");
                    //this occurs when the intent and entity hit a match in the response matrix
                    //but there has been no response pre-determined for the combination.
                    strRet = "I am not sure.  Ask me about something else.";
                }

                if (ques == false && intent.ToLower() != "greet")
                {
                    if (questionPrompt == false)
                    {
                        strRet = "Please ask in the form of a question.";
                    }
                    else if (entity == "N/A")
                    {
                        strRet = "I am not sure.  Ask me something else.";
                    }
                    else
                    {
                        strRet = "What would you like to learn about " + entity + "?";
                    }
                    questionPrompt = false;
                }

                lastEntity = entity;
                _botData.SetProperty<string>("LastEntity", entity);
                _botData.SetProperty<bool>("QuestionPrompt", questionPrompt);
                _stateClient.BotState.SetUserData(activity.ChannelId, activity.Conversation.Id, _botData);
                System.Diagnostics.Debug.WriteLine("intent = " + intent);
                System.Diagnostics.Debug.WriteLine("entity = " + entity);
                Activity reply = activity.CreateReply(strRet);
                await connector.Conversations.ReplyToActivityAsync(reply);

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        #region System Messages (Ignore)
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            else if (message.Type == "BotAddedToConversation")
            {
                return message.CreateReply("Hello bro");
            }

            return null;
        }
        #endregion
    }
}