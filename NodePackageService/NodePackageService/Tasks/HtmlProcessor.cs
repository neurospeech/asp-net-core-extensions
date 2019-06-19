using AngleSharp;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.Tasks
{
    public class HtmlProcessor
    {
        public static HtmlProcessor Instance = new HtmlProcessor();

        internal string RedirectNPM(string text, string currentPackage, string cdn)
        {
            var parser = new HtmlParser(new Configuration().WithCss());
            var document = parser.Parse(text);

            var all = document.All.Where(x =>
                x.TagName.EqualsIgnoreCase("link") && x.HasAttribute("href"));

            foreach (var item in all)
            {
                UpdateAttribute(item, "href", currentPackage, cdn);
            }

            all = document.All.Where(x =>
              x.TagName.EqualsIgnoreCase("script") && x.HasAttribute("src"));

            foreach (var item in all)
            {
                UpdateAttribute(item, "src", currentPackage, cdn);
            }

            all = document.All.Where(x =>
                x.TagName.EqualsIgnoreCase("img") && x.HasAttribute("src"));

            foreach (var item in all)
            {
                UpdateAttribute(item, "src", currentPackage, cdn);
            }

            all = document.All.Where(x =>
                x.Style?.Any(s =>
               s.Name.EqualsIgnoreCase("background-image")
               ||
               s.Name.EqualsIgnoreCase("list-style-image")
                ) ?? false);

            foreach (var item in all)
            {
                //UpdateAttribute(item, "src", currentPackage);
                var img = ExtractUrl(item.Style?.BackgroundImage);
                if (!string.IsNullOrWhiteSpace(img))
                {
                    img = UpdateAttributeValue(img, currentPackage, cdn);
                    item.Style.BackgroundImage = $"url({img})";
                }

                img = ExtractUrl(item.Style?.ListStyleImage);
                if (!string.IsNullOrWhiteSpace(img))
                {
                    img = UpdateAttributeValue(img, currentPackage, cdn);
                    item.Style.ListStyleImage = $"url({img} )";
                }
            }

            return document.DocumentElement.OuterHtml;
        }

        private string ExtractUrl(string attribute)
        {
            if (attribute == null)
                return null;
            attribute = attribute.Trim();
            if (!attribute.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
                return null;
            attribute = attribute.Substring(4);
            attribute = attribute.Trim('(', ')', '"', '\'');

            return attribute;
        }

        private static string UpdateAttributeValue(string href, string currentPackage, string cdn)
        {
            int i = href.IndexOf("node_modules/", StringComparison.OrdinalIgnoreCase);
            if (i != -1)
            {
                href = $"//{cdn}/npm/package/{href.Substring(i + 13)}";
                return href;
            }

            if (href.StartsWith("/"))
                return null;

            if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            href = href.TrimStart('.', '/');
            href = $"https://{cdn}/npm/package/{currentPackage}/{href}";
            return href;
        }

        private static void UpdateAttribute(AngleSharp.Dom.IElement item, string attribute, string currentPackage, string cdn)
        {

            string href = item.GetAttribute(attribute);
            href = UpdateAttributeValue(href, currentPackage, cdn);
            if (href != null)
            {
                item.SetAttribute(attribute, href);
            }

        }

    }
}
