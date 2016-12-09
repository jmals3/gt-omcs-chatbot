using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using edu.stanford.nlp;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using edu.stanford.nlp.semgraph;
using System.Text.RegularExpressions;
using edu.stanford.nlp.ling;
using System.Data;
using System.IO;

namespace GT_Chatbot
{
    public class NLP
    {
        public static Sentence createSentence(int id, CoreMap sentence, DataTable QuestionWordTable)
        {
            Sentence sentence_frame = new Sentence();
            sentence_frame.setId(id);
            sentence_frame.setSpeaker("user"); //this can be updated in the future so that the bot can partake in group conversations
            sentence_frame.setText(sentence.get(typeof(CoreAnnotations.TextAnnotation)).ToString());

            var sentence_data = getSentenceData(sentence);

            sentence_frame.setVerb(getVerb(sentence_data));
            sentence_frame.setAgent(getAgent(sentence_data));
            sentence_frame.setThematicObject(getThematicObject(sentence_data));
            sentence_frame.setBeneficiary(getBeneficiary(sentence_data));
            sentence_frame.setType(getSentenceType(QuestionWordTable, sentence_frame));

            #region Debugging
            //foreach (string key in sentence_data.Keys)
            //{
            //    Debug.WriteLine(key + ": " + sentence_data[key]);
            //}
            //Debug.WriteLine(sentence_frame.getId());
            //Debug.WriteLine(sentence_frame.getSpeaker());
            //Debug.WriteLine(sentence_frame.getText());
            //Debug.WriteLine(sentence_frame.getVerb());
            //Debug.WriteLine(sentence_frame.getAgent());
            //Debug.WriteLine(sentence_frame.getThematicObject());
            //Debug.WriteLine(sentence_frame.getBeneficiary());
            //Debug.WriteLine(sentence_frame.getType());
            #endregion

            return sentence_frame;
        }

        public static Dictionary<string, string> getSentenceData(CoreMap sentence)
        {
            List<string> sentence_parts = sentence.get(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation)).ToString().Replace("->", string.Empty).Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Dictionary<string, string> sentence_props = new Dictionary<string, string>();
            foreach (string s in sentence_parts)
            {
                int level = s.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                string s_nospace = s.Replace(" ", string.Empty);
                var key = Regex.Match(s_nospace, @"\(([^)]*)\)").Groups[1].Value;
                var val = Regex.Match(s_nospace, @"([^/]*)\/").Groups[1].Value;
                var val_type = Regex.Match(s_nospace, @"\/([^(]*)\(").Groups[1].Value;
                int occurance = 0;
                string full_key = key + "-" + val_type + "-" + level + "." + occurance;
                bool key_exists = sentence_props.Keys.Contains(full_key);
                while (key_exists)
                {
                    occurance = occurance + 1;
                    full_key = key + "-" + val_type + "-" + level + "." + occurance;
                    key_exists = sentence_props.Keys.Contains(full_key);
                }
                sentence_props.Add(full_key, val);
            }
                return sentence_props;
        }

        public static string getAgent(Dictionary<string, string> data)
        {
            string agent = string.Empty;
            var keys = data.Keys;
            foreach (string key in keys)
            {
                if (Regex.IsMatch(key, @"nsubj-\w+-3.0") && agent == string.Empty)
                {
                    agent = data[key];
                }
            }
            if (agent == "i")
            {
                agent = "user";
            }
            else if (agent == "you")
            {
                agent = "me";
            }
            return agent;
        }

        public static string getVerb(Dictionary<string, string> data)
        {
            string verb = string.Empty;
            var keys = data.Keys;
            foreach (string key in keys)
            {
                if (key.Contains("root-V") && verb == string.Empty)
                {
                    verb = data[key];
                }
            }
            return verb;
        }

        public static string getThematicObject(Dictionary<string, string> data)
        {
            string thematic_object = string.Empty;
            var keys = data.Keys;
            foreach (string key in keys)
            {
                if (key.Contains("dobj") && thematic_object == string.Empty)
                {
                    thematic_object = data[key];
                }
            }
            if (thematic_object == string.Empty)
            {
                foreach (string key in keys)
                {
                    if (key.Contains("xcomp-NN") && thematic_object == string.Empty)
                    {
                        thematic_object = data[key];
                    }
                }
            }
            if (thematic_object == string.Empty)
            {
                foreach (string key in keys)
                {
                    if (Regex.IsMatch(key, @"prep_\w+-NN") && thematic_object == string.Empty)
                    {
                        thematic_object = data[key];
                    }
                }
            }
            return thematic_object;
        }

        public static string getBeneficiary(Dictionary<string, string> data)
        {
            string beneficiary = string.Empty;
            var keys = data.Keys;
            foreach (string key in keys)
            {
                if (key.Contains("iobj") && beneficiary == string.Empty)
                {
                    beneficiary = data[key];
                }
            }
            if (beneficiary == string.Empty)
            {
                foreach (string key in keys)
                {
                    if (key.Contains("nsubj-NNP-5.0") && beneficiary == string.Empty)
                    {
                        beneficiary = data[key];
                    }
                }
            }            
            return beneficiary;
        }

        public static string getSentenceType(DataTable QuestionWordTable, Sentence sentence)
        {
            string input = sentence.getText();
            string type = string.Empty;
            foreach (DataRow row in QuestionWordTable.Rows)
            {
                if (input.Contains(row["QuestionWord"].ToString().ToLower()))
                {
                    type = "interrogative";
                }
            }
            if (type == string.Empty)
            {
                if (input.Contains("?"))
                {
                    type = "interrogative";
                }
                else if (input.Split(null)[0] == sentence.getVerb())
                {
                    type = "imperative";
                }
                else if (input.Contains("!"))
                {
                    type = "exclamatory";
                }
                else
                {
                    type = "declarative";
                }
            }
            return type;
        }
    }
}