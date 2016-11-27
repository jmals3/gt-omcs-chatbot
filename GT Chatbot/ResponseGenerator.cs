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
    }
}