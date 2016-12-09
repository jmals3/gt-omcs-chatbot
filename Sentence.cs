using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using edu.stanford.nlp;
using edu.stanford.nlp.pipeline;

namespace GT_Chatbot
{
    public class Sentence
    {
        #region orig plan
        //StanfordCoreNLP pipeline = new StanfordCoreNLP();
        //Annotation annotation = new Annotation();

        //public Sentence(StanfordCoreNLP p, Annotation a)
        //{
        //    pipeline = p;
        //    annotation = a;
        //}
        #endregion

        int id;
        string text;
        string speaker;
        string type;
        string agent;
        string verb;
        string thematic_object;
        string beneficiary;

        //public Sentence(int i, string s, string t, string sub, string v, string tobj)
        //{
        //    id = i;
        //    speaker = s;
        //    type = t;
        //    agent = sub;
        //    verb = v;
        //    thematic_object = tobj;
        //}

        public Sentence()
        {
            int id = 0;
            string text = string.Empty;
            string speaker = string.Empty;
            string type = string.Empty;
            string agent = string.Empty;
            string verb = string.Empty;
            string thematic_object = string.Empty;
            string beneficiary = string.Empty;
        }

        #region get/set id
        public int getId()
        {
            return id;
        }
        public void setId(int i)
        {
            id = i;
        }
        #endregion

        #region get/set text
        public string getText()
        {
            return text;
        }
        public void setText(string t)
        {
            text = t;
        }
        #endregion

        #region get/set speaker
        public string getSpeaker()
        {
            return speaker;
        }
        public void setSpeaker(string s)
        {
            speaker = s;
        }
        #endregion

        #region get/set type
        public string getType()
        {
            return type;
        }
        public void setType(string t)
        {
            type = t;
        }
        #endregion

        #region get/set agent
        public string getAgent()
        {
            return agent;
        }
        public void setAgent(string sub)
        {
            agent = sub;
        }
        #endregion

        #region get/set verb
        public string getVerb()
        {
            return verb;
        }
        public void setVerb(string v)
        {
            verb = v;
        }
        #endregion

        #region get/set thematic object
        public string getThematicObject()
        {
            return thematic_object;
        }
        public void setThematicObject(string tobj)
        {
            thematic_object = tobj;
        }
        #endregion

        #region get/set beneficiary
        public string getBeneficiary()
        {
            return beneficiary;
        }
        public void setBeneficiary(string b)
        {
            beneficiary = b;
        }
        #endregion
    }
}