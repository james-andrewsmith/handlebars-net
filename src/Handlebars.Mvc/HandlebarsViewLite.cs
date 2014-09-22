
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

using handlebars.cs;

namespace HandlebarsViewEngine
{

    public sealed class HandlebarsViewLite : IView
    {
        private IViewContextSerializer _viewContextSerializer;
        private HandleBarsClearScript _handlebars;
        private string _path;

        public HandlebarsViewLite(HandleBarsClearScript handlebars,
                                  IViewContextSerializer viewContextSerializer,
                                  string path)
        {
            _handlebars = handlebars;
            _viewContextSerializer = viewContextSerializer;
            _path = path;
        }

        #region IView Members
         
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            string html = string.Empty;
            string json = string.Empty;

            try
            {               
                json = _viewContextSerializer.Serialize(viewContext);

                if (_path.ToLower().EndsWith("/error"))
                    _path = "error/index";

                html = _handlebars.Run(_path, json);
                html = FillSectionData(html, json);
            }
            catch (Exception exp)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<!--");
                sb.AppendLine("path:  " + _path);
                sb.AppendLine("json:  " + json);
                sb.AppendLine("msg:   " + exp.Message);
                sb.AppendLine("trace: " + exp.StackTrace);
                sb.AppendLine("-->");
                html = sb.ToString();
            }
             

            writer.Write(html); 
        }
        
        #endregion

        private static readonly Regex MasterRegex = new Regex(@"(####master:(.+)####)", RegexOptions.IgnoreCase);
        private static readonly Regex SectionRegex = new Regex(@"(<!--####section:(.+)####-->)", RegexOptions.IgnoreCase);

        private class SectionData
        {
            public int NameLength;
            public int Start;
            public int Stop;
            public string Contents;
        }

        public string FillSectionData(string contents, string json)
        {
            // extract other actions from the views.
            // -> header
            // -> footer
            // -> menu(s), admin or top
            // -> cart            
            // return Regex.Replace(contents, "\\{(.+)\\}", m => GetMatch(m, viewdata));

           
            // instead of just returning the template the template provider can 
            // return the sections, master and their positions?

            // ideally this "find what sections exist" only happens once per template
            // then we cache the "replace" tags and find the lastindex and replace 
            // working backwards so we don't need to rerun the regex


            
            var sectionMatches = SectionRegex.Matches(contents);
            var sections = new Dictionary<string, SectionData>();

            // int start = -1, stop = -1;

            foreach (Match match in sectionMatches)
            {
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>');

                // extract the matched sections into variables
                // match.Value.Split(':')[1]
                // match.Value.Split(':')[2]

                if (!sections.ContainsKey(name))
                    sections.Add(name, new SectionData() { NameLength = name.Length });

                switch (operation)
                {
                    case "start":
                        sections[name].Start = match.Index + match.Length;
                        break;
                    case "stop":
                        sections[name].Stop = match.Index;
                        sections[name].Contents = contents.Substring(sections[name].Start, sections[name].Stop - sections[name].Start).Trim(' ', '\n', '\t', '\r');
                        break;
                }
            }
            
            // find the master for this template
            // ###master            
            // todo:
            // return an HTML error describing the missing
            var masterMatch = MasterRegex.Match(contents, 0);
            if (!masterMatch.Success)
                return contents;

            var removal = sections.Values.OrderByDescending(_ => _.Stop);
            foreach (SectionData sd in removal)
            {
                // <!--####section:start:####-->
                // <!--####section:stop:####-->
                int start = sd.Start - sd.NameLength - 29;
                int stop = sd.Stop + sd.NameLength + 28;
                contents = contents.Remove(start, stop - start);
            }


            // remove the master tag from the render pipeline
            contents = contents.Remove(masterMatch.Index, masterMatch.Length);

            // this logic is only needed if there is a master template with sections
            // any content not in a section will be automatically assumed as the 
            // "content" section and appended to it (if it was already created)
            if (!sections.ContainsKey("contents"))
            {
                sections.Add("contents", new SectionData { });
                sections["contents"].Contents = contents.Trim(' ', '\n', '\t', '\r');
            }

            var masterPath = masterMatch.Value.Split(':')[1].TrimEnd('#');
            string master = _handlebars.Run(masterPath, json);

            // recycle variable for efficiency
            sectionMatches = SectionRegex.Matches(master);

            // foreach section in the master, 
            // replace the section with the contents from the template
            // if the sections don't exist then leave them because there
            // might be default content

            var masterSections = new Dictionary<string, SectionData>();
            foreach (Match match in sectionMatches)
            {
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>');

                // extract the matched sections into variables
                // match.Value.Split(':')[1]
                // match.Value.Split(':')[2]

                if (!masterSections.ContainsKey(name))
                    masterSections.Add(name, new SectionData() { NameLength = name.Length });

                switch (operation)
                {
                    case "start":
                        masterSections[name].Start = match.Index + match.Length;
                        break;
                    case "stop":
                        masterSections[name].Stop = match.Index;                            
                        break;
                }
            }

            var replacement = masterSections.OrderByDescending(_ => _.Value.Stop);
            foreach (KeyValuePair<string, SectionData> kvp in replacement)
            {
                if (sections.ContainsKey(kvp.Key))
                {
                    master = master.Remove(masterSections[kvp.Key].Start, masterSections[kvp.Key].Stop - masterSections[kvp.Key].Start);
                    master = master.Insert(masterSections[kvp.Key].Start, "\n" + sections[kvp.Key].Contents + "\n");
                }
            }
                     
            return master;
        }
         
    }
}
