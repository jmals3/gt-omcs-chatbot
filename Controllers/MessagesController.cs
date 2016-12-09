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
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using java.util;
using edu.stanford.nlp;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.util;

namespace GT_Chatbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public DataTable BotResponses = new DataTable();
        public DataTable BotSynonyms = new DataTable();
        public DataTable BotQuestionWords = new DataTable();

        public string jarRoot;
        public Properties props;
        public string curDir;
        public StanfordCoreNLP pipeline;

        public string lastEntity = string.Empty;
        public Conversation conversation = new Conversation();
        //public bool questionPrompt = false;

        public MessagesController()
        {
            #region SQL Connection
            string connString = "Data Source=gtomcschatbot.database.windows.net;Initial Catalog=BotResponses;Integrated Security=False;User ID=jmals3;Password=OMCSbot1;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
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
                Debug.WriteLine("Load Successful.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Did not load DBs. " + e);
            }
            #endregion
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
                    conversation = _botData.GetProperty<Conversation>("Conversation");
                    try
                    {
                        var conv = conversation.getConversant();
                    }
                    catch (Exception e)
                    {
                        conversation = new Conversation();
                    }
                }
                catch (Exception e)
                {
                    conversation = new Conversation();
                    Debug.WriteLine(e.ToString());
                }

                //initialization code borrowed from https://www.rhyous.com/2014/10/20/splitting-sentences-in-c-using-stanford-nlp/
                //var jarRoot = @"stanford-english-corenlp-2016-01-10-models"; //path for running bot locally
                var jarRoot = @"D:\home\site\wwwroot\stanford-english-corenlp-2016-01-10-models";

                // Annotation pipeline configuration
                var props = new Properties();
                props.setProperty("annotators", "tokenize, ssplit, pos, lemma, ner, parse, dcoref");
                props.setProperty("sutime.binders", "0");

                // We should change current directory, so StanfordCoreNLP could find all the model files automatically 
                var curDir = Environment.CurrentDirectory;
                Directory.SetCurrentDirectory(jarRoot);
                var pipeline = new StanfordCoreNLP(props);
                Directory.SetCurrentDirectory(curDir);

                //Create Annotation for input
                var doc = new Annotation(input);
                pipeline.annotate(doc);

                //Separate sentences in input
                var sentences = doc.get(typeof(CoreAnnotations.SentencesAnnotation));

                //Create senetence frame for each sentence in input and add to conversation
                foreach (CoreMap sentence in sentences as ArrayList)
                {
                    conversation.addSentence(NLP.createSentence(conversation.getSentences().Count + 1, sentence, BotQuestionWords));
                }
                //Set the topic for the conversation based on the thematic object of the most recent input with a thematic object
                foreach (Sentence s in conversation.getSentences().Reverse<Sentence>())
                {
                    if (s.getThematicObject() != string.Empty)
                    {
                        conversation.setTopic(s.getThematicObject());
                        break;
                    }
                }

                //determine if the senetence is a declarative, interrogative, exclamatory, or imperative
                string sentenceType;
                try
                {
                    sentenceType = conversation.getLastSentence().getType();
                }
                catch (Exception e)
                {
                    sentenceType = "interrogative";
                }
                //check if the input was a question
                bool ques = (sentenceType == "interrogative");

                //replace any synonyms from the synonyms table with their entity counterparts
                input = ResponseGenerator.checkSynonyms(BotSynonyms, input);

                //Replace "it" pronoun with entity from last input
                if (Regex.IsMatch(input, @" it\W"))
                {
                    input = Regex.Replace(input, @"it\W?", lastEntity);
                }
                //send the updated input to LUIS (this code was borrowed from https://github.com/Microsoft/BotBuilder/tree/master/CSharp/Samples/Stock_Bot)
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
                    Debug.WriteLine(e.ToString());
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
                    Debug.WriteLine(e.ToString());
                    entity = ResponseGenerator.checkForEntity(BotSynonyms, input);
                }

                try
                {
                    strRet = ResponseGenerator.getResponse(BotResponses, intent, entity);
                    Debug.WriteLine(strRet);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("x" + e.ToString());
                }

                if (strRet == "N/A")
                {
                    //this occurs when the intent and entity hit a match in the response matrix
                    //but there has been no response pre-determined for the combination.
                    strRet = "I am not sure.  Ask me about something else.";
                }

                if (ques == false && intent.ToLower() != "greet" && intent.ToLower() != "thank" && intent.ToLower() != "bye")
                {
                    if(sentenceType == "imperative")
                    {
                        strRet = "No thanks.  I don't take orders.  Ask me a question about it.";
                    }
                    else
                    {
                        strRet = ResponseGenerator.getResponse(conversation, BotSynonyms);
                    }
                }
                lastEntity = entity;
                _botData.SetProperty<string>("LastEntity", entity);
                _botData.SetProperty<Conversation>("Conversation", conversation);
                _stateClient.BotState.SetUserData(activity.ChannelId, activity.Conversation.Id, _botData);
                Debug.WriteLine("intent = " + intent);
                Debug.WriteLine("entity = " + entity);
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