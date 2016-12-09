using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace GT_Chatbot
{
    public class ResponseGenerator
    {
        public static bool checkIfQuestion(DataTable QuestionWordTable, string input)
        {
            bool question = false;
            foreach (DataRow row in QuestionWordTable.Rows)
            {
                if (input.Contains(row["QuestionWord"].ToString().ToLower()))
                {
                    question = true;
                }
            }
            if (question == false)
            {
                if (input.Contains("?"))
                {
                    question = true;
                }
            }
            return question;
        }
        
        public static string checkSynonyms(DataTable SynonymTable, string input)
        {
            string newInput = input;
            foreach (DataRow row in SynonymTable.Rows)
            {
                string syn = row["Synonym"].ToString().ToLower();
                if (Regex.IsMatch(input, @"\b" + syn + "\b"))
                {
                    newInput = input.Replace(syn, row["Entity"].ToString().ToLower());
                }
            }
            return newInput;
        }

        public static string checkForEntity(DataTable SynonymTable, string input)
        {
            string newInput = input;
            foreach (DataRow row in SynonymTable.Rows)
            {
                string syn = row["Synonym"].ToString().ToLower();
                if (Regex.IsMatch(input, @"\W?" + syn + @"\W?"))
                {
                    return row["Entity"].ToString().ToLower();
                }
            }
            return "N/A";
        }

        public static bool checkEntity(DataTable SynonymTable, string entity)
        {
            bool check = false;
            foreach (DataRow row in SynonymTable.Rows)
            {
                if (entity.ToLower() == row["Entity"].ToString().ToLower())
                {
                    check = true;
                }
            }
            return check;
        }

        public static string getResponse(DataTable BotResponseTable, string Intent, string Entity)
        {
            string response = "I am not sure.  Ask me about something else.";
            DataRow[] rows = BotResponseTable.Select("Entity='" + Entity + "'");
            foreach (DataRow row in rows)
            {
                response = row[Intent].ToString();
            }
            return response;
        }

        //Determines response to a Declarative or Exclamatory sentence by using the Conversation data
        public static string getResponse(Conversation conversation, DataTable SynonymTable)
        {
            Sentence lastSentence = conversation.getLastSentence();
            string agent = lastSentence.getAgent().Replace("user", "you");
            string verb = lastSentence.getVerb();
            string verb_noun = string.Empty;
            string conjugate = string.Empty;

            //check to see if a verb conjugate exists, i.e. "I want to apply."
            if (Regex.IsMatch(lastSentence.getText(), @" want to "))
            {
                conjugate = Regex.Match(lastSentence.getText(), @".*want to\s*(\w+)").Groups[1].Value;
            }

            //create a verb_noun to be the object of the response
            //first check verb, then conjugate
            for (int i = 0; i <2; i++) {
                if (i == 1)
                {
                    verb = conjugate;
                }
                DataRow[] rows = SynonymTable.Select("Synonym='" + verb + "'");
                foreach (DataRow row in rows)
                {
                    var entity = row["Entity"].ToString();
                    DataRow[] rows_2 = SynonymTable.Select("Entity='" + entity + "'");
                    foreach (DataRow row_2 in rows_2)
                    {
                        var syn = row_2["Synonym"].ToString();
                        if (syn.Substring(syn.Length - 3) == "ing" || syn.Substring(syn.Length - 4) == "tion")
                        {
                            verb_noun = syn;
                            break;
                        }
                    }
                }
                if (verb_noun != string.Empty)
                {
                    break;
                }
            }

            #region Debugging
            System.Diagnostics.Debug.WriteLine("agent: " + agent);
            System.Diagnostics.Debug.WriteLine("verb: " + verb);
            System.Diagnostics.Debug.WriteLine("verb_noun: " + verb_noun);
            System.Diagnostics.Debug.WriteLine("topic: " + conversation.topic);
            #endregion

            if (verb_noun != string.Empty)
            {
                if(agent != string.Empty)
                {
                    return "What would " + agent + " like to learn about " + verb_noun + "?";
                }
                else
                {
                    return "What about " + verb_noun + "?";
                }
            }
            else
            {
                if(agent != string.Empty)
                {
                    return "What would " + agent + " like to learn about " + conversation.getTopic() + "?";
                }
                else
                {
                    return "What about " + conversation.getTopic() + "?";
                }
            }                       
        }
    }
}