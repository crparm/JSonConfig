using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace JSonConfig
{
    public class JSonConfigurator
    {
        public void Parse(string inputfile, string outputfolder, string env)
        {
            // read JSON directly from a file
            using (StreamReader file = File.OpenText(inputfile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject o2 = (JObject)JToken.ReadFrom(reader);
                var sb = new StringBuilder();
                IEnumerable<JToken> configs = o2.SelectTokens("$.Configs");
                foreach (JToken configitem in configs.Children())
                {
                    JToken configname= configitem.SelectToken(".Name");
                    JToken configtp = configitem.SelectToken(".Template");
                    JToken configavs = configitem.SelectToken(".ApplyVariableSet");
                    JToken configvs = configitem.SelectToken(".VariableSet");
                    var tp=GetTemplate(o2, configtp.ToString());
                    var vars = GetVariables(o2, configavs, configvs,env);
                    foreach(string varname in vars.Keys)
                    {
                        tp=tp.Replace("%%"+varname+"%%", vars[varname]);
                    }
                    sb.AppendLine(tp);
                }

                Console.Write(sb.ToString());
            }
        }

        Hashtable varsetsht = null;
        private Hashtable GetVariableSets(JObject o2)
        {
            if (varsetsht == null)
            {
                varsetsht = new Hashtable();
                IEnumerable<JToken> varsets = o2.SelectTokens("$.VariableSets");
                foreach (var curvarset in varsets.Children())
                {
                    string setname = null;
                    foreach (var curvarenvset in curvarset.Children())
                    {
                        var nodename = ((JProperty)curvarenvset).Name;
                        if (nodename == "Name")
                        {
                            setname = ((JProperty)curvarenvset).Value.ToString();
                        }
                        else
                        {
                            Dictionary<string, string> nv = new Dictionary<string, string>();
                            foreach (var vardata in curvarenvset.Children())
                            {
                                foreach (var vardata1 in vardata.Children())
                                {
                                    var varname = ((JProperty)vardata1).Name;
                                    var varval = ((JValue)((JProperty)vardata1).Value).Value.ToString();
                                    nv.Add(varname, varval);
                                }
                            }
                            varsetsht.Add(setname + "_" + nodename, nv);
                        }
                    }
                }
            }
            return varsetsht;
        }

        private Hashtable GetVariables(JToken varset)
        {
            Hashtable ht = new Hashtable();
            var setname = "__Config__";
            foreach (var curvarenvset in varset.Children())
            {
                var nodename = ((JProperty)curvarenvset).Name;
                Dictionary<string, string> nv = new Dictionary<string, string>();
                foreach (var vardata in curvarenvset.Children())
                {
                    foreach (var vardata1 in vardata.Children())
                    {
                        var varname = ((JProperty)vardata1).Name;
                        var varval = ((JValue)((JProperty)vardata1).Value).Value.ToString();
                        nv.Add(varname, varval);
                    }
                }
                ht.Add(setname + "_" + nodename, nv);
            }
            return ht;
        }

        private Dictionary<string,string> Merge(Dictionary<string,string> d1, Dictionary<string, string> d2)
        {
            var d3 = new Dictionary<string, string>();
            foreach(var item in d1.Keys)
            {
                d3[item] = d1[item];
            }
            foreach (var item in d2.Keys)
            {
                d3[item]= d2[item];
            }
            return d3;
        }
        public Dictionary<string,string> GetVariables(JObject  o2, JToken avarset,JToken varset, string env)
        {
            var varsets = GetVariableSets(o2);
            var varsetdata = GetVariables(varset);
            Dictionary<string, string> retdata = new Dictionary<string, string>();
            foreach(var avarsetitem in avarset.Children())
            {
                var itemname=((JValue)avarsetitem).Value.ToString();
                retdata=Merge(retdata,((Dictionary<string, string>)varsets[itemname + "_" + env]));
            }
            retdata = Merge(retdata, ((Dictionary<string, string>)varsetdata["__Config___" + env]));

            return retdata;
        }

        Hashtable templatecol = null;
        public String GetTemplate(JObject o2,string tpname)
        {
            if (templatecol == null)
            {
                templatecol = new Hashtable();
                JToken templates = o2.SelectToken("$.Templates");
                foreach (var tp in templates)
                {
                    var tmpname = tp.SelectToken(".Name").ToString();
                    var data = tp.SelectToken(".Data");
                    templatecol.Add(tmpname, data.ToString());                    
                }
                foreach (var tp in templates)
                {
                    var tmpname = tp.SelectToken(".Name").ToString();
                    var inherits = tp.SelectToken(".Inherits");
                    StringBuilder sb = null;
                    if (inherits != null)
                    {
                        sb = new StringBuilder();
                        foreach (var inhtmp in inherits)
                        {
                            if (templatecol.ContainsKey(inhtmp.ToString()))
                            {
                                var dt = templatecol[inhtmp.ToString()].ToString();
                                sb.AppendLine(dt.ToString());
                            }
                        }

                    }                    
                    if(sb!=null)
                    {
                        sb.AppendLine(templatecol[tmpname].ToString());
                        templatecol[tmpname] = sb.ToString();
                    }
                }
            }            
            return templatecol[tpname].ToString();
        }
    }
}
