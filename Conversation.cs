using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GT_Chatbot
{
    public class Conversation
    {
        public string conversant;
        public string topic;
        public List<Sentence> sentences;

        public Conversation()
        {
            conversant = "user";
            topic = string.Empty;
            sentences = new List<Sentence>();
        }

        public string getConversant()
        {
            return conversant;
        }
        public void setConversant(string con)
        {
            conversant = con;
        }

        public string getTopic()
        {
            return topic;
        }
        public void setTopic(string top)
        {
            topic = top;
        }

        public List<Sentence> getSentences()
        {
            return sentences;
        }
        public Sentence getLastSentence()
        {
            return sentences[sentences.Count - 1];
        }
        public void addSentence(Sentence s)
        {
            sentences.Add(s);
        }
    }
}