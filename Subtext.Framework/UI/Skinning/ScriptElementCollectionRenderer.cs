#region Disclaimer/Info

///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at Google Code at http://code.google.com/p/subtext/
// The development mailing list is at subtext@googlegroups.com 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Subtext.Framework.Web;

namespace Subtext.Framework.UI.Skinning
{
    /// <summary>
    /// Provides rendering facilities for script elements in the head element of the page
    /// </summary>
    public class ScriptElementCollectionRenderer
    {
        private readonly SkinEngine _skinEngine;

        public ScriptElementCollectionRenderer(SkinEngine skinEngine)
        {
            _skinEngine = skinEngine;
        }

        private IDictionary<string, SkinTemplate> Templates
        {
            get
            {
                var templates = _templates;
                if(templates == null)
                {
                    templates = _skinEngine.GetSkinTemplates(false /* mobile */);
                    _templates = templates;
                }
                return templates;
            }
        }
        IDictionary<string, SkinTemplate> _templates;

        private static string RenderScriptAttribute(string attributeName, string attributeValue)
        {
            return attributeValue != null ? string.Format(" {0}=\"{1}\"", attributeName, attributeValue) : String.Empty;
        }

        public static string RenderScriptElement(string skinPath, Script script)
        {
            return string.Format("<script{0}{1}{2}></script>{3}", RenderScriptAttribute("type", script.Type), RenderScriptAttribute("src", GetScriptSourcePath(skinPath, script)), RenderScriptAttribute("defer", script.Defer ? "defer" : null), Environment.NewLine);
        }

        public static string RenderScriptElement(string scriptPath)
        {
            return string.Format("<script{0}{1}></script>{2}", RenderScriptAttribute("type", "text/javascript"), RenderScriptAttribute("src", scriptPath), Environment.NewLine);
        }

        private static string GetScriptSourcePath(string skinPath, Script script)
        {
            if(script.Src.StartsWith("~"))
            {
                return HttpHelper.ExpandTildePath(script.Src);
            }
            if(script.Src.StartsWith("/") || script.Src.StartsWith("http://") || script.Src.StartsWith("https://"))
            {
                return script.Src;
            }
            return skinPath + script.Src;
        }

        /// <summary>
        /// Gets the skin path.
        /// </summary>
        /// <param name="skinTemplateFolder">Name of the skin.</param>
        /// <returns></returns>
        private static string GetSkinPath(string skinTemplateFolder)
        {
            string applicationPath = HttpContext.Current.Request.ApplicationPath;
            return string.Format("{0}/Skins/{1}/", (applicationPath == "/" ? String.Empty : applicationPath), skinTemplateFolder);
        }

        /// <summary>
        /// Renders the script element collection for thes kin key.
        /// </summary>
        /// <param name="skinKey">The skin key.</param>
        /// <returns></returns>
        public string RenderScriptElementCollection(string skinKey)
        {
            var result = new StringBuilder();

            SkinTemplate skinTemplate = Templates.ItemOrNull(skinKey);
            if(skinTemplate != null && skinTemplate.Scripts != null)
            {
                string skinPath = GetSkinPath(skinTemplate.TemplateFolder);
                if(CanScriptsBeMerged(skinTemplate))
                {
                    result.Append(RenderScriptElement(string.Format("{0}js.axd?name={1}", skinPath, skinKey)));
                }
                else
                {
                    foreach(Script script in skinTemplate.Scripts)
                    {
                        result.Append(RenderScriptElement(skinPath, script));
                    }
                }
            }
            return result.ToString();
        }

        public ScriptMergeMode GetScriptMergeMode(string skinName)
        {
            SkinTemplate skinTemplate = Templates.ItemOrNull(skinName);
            return skinTemplate.ScriptMergeMode;
        }

        public ICollection<string> GetScriptsToBeMerged(string skinName)
        {
            var scripts = new List<string>();

            SkinTemplate skinTemplate = Templates.ItemOrNull(skinName);

            if(skinTemplate != null && skinTemplate.Scripts != null)
            {
                if(CanScriptsBeMerged(skinTemplate))
                {
                    string skinPath = CreateStylePath(skinTemplate.TemplateFolder);
                    foreach(Script script in skinTemplate.Scripts)
                    {
                        if(script.Src.StartsWith("~"))
                        {
                            scripts.Add(HttpHelper.ExpandTildePath(script.Src));
                        }
                        else
                        {
                            scripts.Add(skinPath + script.Src);
                        }
                    }
                }
            }
            return scripts;
        }

        private static string CreateStylePath(string skinTemplateFolder)
        {
            string applicationPath = HttpContext.Current.Request.ApplicationPath;
            string path = string.Format("{0}/Skins/{1}/", (applicationPath == "/" ? String.Empty : applicationPath), skinTemplateFolder);
            return path;
        }

        public static bool CanScriptsBeMerged(SkinTemplate template)
        {
            if(!template.MergeScripts)
            {
                return false;
            }
            if(template.Scripts == null)
            {
                return false;
            }
            foreach(Script script in template.Scripts)
            {
                if(script.Src.Contains("?"))
                {
                    return false;
                }
                if(IsScriptRemote(script))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsScriptRemote(Script script)
        {
            if(script.Src.StartsWith("http://") || script.Src.StartsWith("https://"))
            {
                return true;
            }
            return false;
        }
    }
}